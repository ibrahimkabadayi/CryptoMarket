# Code Examples - Optimistic Concurrency Tokens

## How It Works: Before vs After

### BEFORE: Without Concurrency Tokens

#### Bad Scenario: Race Condition
```csharp
// Request A
var wallet = await repo.GetWalletWithAssetsAsync(walletId); // Gets v1
wallet.Buy(...);
await repo.UpdateAsync(wallet); // ✓ Success - Database updated

// Request B (same time)
var wallet = await repo.GetWalletWithAssetsAsync(walletId); // Also gets v1
wallet.Buy(...);
await repo.UpdateAsync(wallet); // ✗ FAILS! DbUpdateConcurrencyException
								// "expected to affect 1 row(s), but actually affected 0"
```

**Problem**: No way to detect that the data changed between load and save

---

### AFTER: With Concurrency Tokens

#### Same Scenario: Now Handled Gracefully
```csharp
// Request A
var wallet = await repo.GetWalletWithAssetsAsync(walletId); 
// Gets: Wallet { Id: 123, RowVersion: 1, Balance: 100 }

wallet.Buy(...);
// Modifies: Wallet { Id: 123, RowVersion: 1, Balance: 50 }

await repo.UpdateAsync(wallet); 
// EF generates: UPDATE Wallets SET ... WHERE Id = 123 AND RowVersion = 1
// ✓ Success - RowVersion increments to 2


// Request B (same time)
var wallet = await repo.GetWalletWithAssetsAsync(walletId);
// Also gets: Wallet { Id: 123, RowVersion: 1, Balance: 100 }

wallet.Buy(...);
// Modifies: Wallet { Id: 123, RowVersion: 1, Balance: 50 }

await repo.UpdateAsync(wallet);
// EF generates: UPDATE Wallets SET ... WHERE Id = 123 AND RowVersion = 1
// ✗ FAILS - RowVersion is now 2, WHERE clause doesn't match
// ✓ Catches DbUpdateConcurrencyException
// ✓ Converts to: InvalidOperationException with clear message
// ✓ Returns to client: "Another operation modified your wallet. Please try again."
```

---

## Implementation Details

### 1. Entity Model

```csharp
public abstract class BaseEntity
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
	public DateTime? UpdatedDate { get; set; }

	// NEW: Optimistic concurrency token
	// Automatically managed by EF Core and database
	public uint RowVersion { get; set; }
}
```

**Why `uint`?**
- EF Core recommends uint for row versions
- Automatically incremented by database
- Efficient storage (4 bytes)

---

### 2. EF Core Configuration

```csharp
public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
	public void Configure(EntityTypeBuilder<Wallet> builder)
	{
		builder.HasKey(x => x.Id);

		// ... other properties ...

		// NEW: Mark RowVersion as concurrency token
		builder.Property(x => x.RowVersion)
			.IsRowVersion();

		// In PostgreSQL, this creates:
		// - A bytea column for version tracking
		// - Automatic increment on each update
		// - Used in WHERE clauses for conflict detection
	}
}
```

---

### 3. Repository Exception Handling

```csharp
public async Task UpdateAsync(T entity)
{
	_dbSet.Update(entity);

	try
	{
		await _context.SaveChangesAsync();
	}
	catch (DbUpdateConcurrencyException ex)
	{
		// Convert EF's exception to something more meaningful
		throw new InvalidOperationException(
			"The entity has been modified by another process. " +
			"Please reload and try again.", 
			ex);
	}
}
```

**Why this matters:**
- Raw `DbUpdateConcurrencyException` exposes database concepts
- New `InvalidOperationException` is business-domain appropriate
- Clear message tells users what to do (retry)
- Exception is chained `(ex)` for debugging if needed

---

### 4. Service Layer Handling

```csharp
public class WalletService
{
	public async Task BuyAsset(Guid walletId, string symbol, 
							   decimal currentPrice, decimal amount, bool isLimitOrder)
	{
		var wallet = await walletRepository.GetWalletWithAssetsAsync(walletId) 
			?? throw new ArgumentException("Wallet could not be found!");

		// ... business logic ...

		try
		{
			await walletRepository.UpdateAsync(wallet);
		}
		catch (InvalidOperationException ex) 
			when (ex.Message.Contains("modified by another process"))
		{
			// Specific handling for concurrency conflicts
			throw new InvalidOperationException(
				"Another operation modified your wallet. Please try again.", 
				ex);
		}

		// ... continue with post-update operations ...
	}
}
```

**Pattern Explanation:**
- Try: Attempt update
- Catch with `when`: Only catch concurrency-specific errors
- Throw: Re-throw with more specific message for HTTP layer
- Continue: Post-update operations only run if no conflict

---

## Real-World Scenarios

### Scenario 1: Normal Single Operation ✅

```
User: Clicks "Buy BTC" button once

Timeline:
T0  Load: Wallet { Balance: $1000, RowVersion: 5 }
T1  Modify: Balance -= $500
T2  Save: UPDATE WHERE RowVersion = 5 ✓
	Database: RowVersion becomes 6
T3  Success: Returns "Bought 0.01 BTC"
```

---

### Scenario 2: User Clicks Twice Quickly ⚠️

```
User: Clicks "Buy BTC" button twice rapidly
	  (Or two tabs open, both click submit)

Timeline:
T0  Request A: Load Wallet { Balance: $1000, RowVersion: 5 }
T0  Request B: Load Wallet { Balance: $1000, RowVersion: 5 }

T1  Request A: Modify Balance = $500
T1  Request B: Modify Balance = $500

T2  Request A: Save UPDATE WHERE RowVersion = 5 ✓
	Database: RowVersion becomes 6

T3  Request B: Save UPDATE WHERE RowVersion = 5 ✗
	WHERE clause doesn't match (RowVersion is now 6)
	Throws DbUpdateConcurrencyException

T4  Catch: Convert to InvalidOperationException
	Return to client: "Another operation modified your wallet..."

Client should:
	- Show error message to user
	- Offer "Retry" button
	- Or show current wallet state and let user try again
```

---

### Scenario 3: Multiple Users Same Wallet 🚨

```
Scenario: Business account, multiple traders accessing same wallet

User A: Buys ETH at 11:00:00
User B: Sells BTC at 11:00:02

Timeline:
T0  User A: Load Wallet { Balance: $5000, RowVersion: 10 }
T0  User B: Load Wallet { Balance: $5000, RowVersion: 10 }

T1  User A: Modifies (Buy ETH)
T1  User B: Modifies (Sell BTC)

T2  User A: Save ✓ (RowVersion: 10 → 11)
	Wallet: Balance: $3000

T3  User B: Save ✗ (RowVersion: 10, but now is 11)
	Error: "Another operation modified your wallet"
	User B: Must reload and retry
```

---

## HTTP Status Codes & Error Response

### Before Implementation
```
POST /api/wallet/123/assets/BTC
{
	"BuyingPrice": 50000,
	"Amount": 0.01
}

Response: 500 Internal Server Error
Body: Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException: 
	  The database operation was expected to affect 1 row(s)...
```

❌ Not helpful - looks like a system error, not a business logic issue

---

### After Implementation
```
POST /api/wallet/123/assets/BTC
{
	"BuyingPrice": 50000,
	"Amount": 0.01
}

Response: 409 Conflict (or 400 Bad Request depending on HTTP mapping)
Body: {
	"Message": "Another operation modified your wallet. Please try again."
}
```

✅ Clear - tells user exactly what happened and what to do

---

## Client-Side Implementation

### Recommended Client Retry Logic

```typescript
// Angular/TypeScript example
async function buyAsset(walletId: string, symbol: string, params: any) {
	const maxRetries = 3;
	let retries = 0;

	while (retries < maxRetries) {
		try {
			const response = await this.portfolioService.buyAsset(
				walletId, 
				symbol, 
				params
			);

			// Success
			this.showSuccessMessage(
				`Successfully bought ${params.amount} ${symbol}`
			);
			return response;

		} catch (error) {
			// Check if it's a concurrency conflict
			if (error.status === 409 || 
				error.message.includes("modified")) {

				retries++;
				if (retries < maxRetries) {
					// Exponential backoff: 100ms, 200ms, 400ms
					await this.delay(100 * Math.pow(2, retries - 1));
					// Retry the operation
					continue;
				}
			}

			// Other error - don't retry
			this.showErrorMessage(
				error.message || "Operation failed. Please try again."
			);
			throw error;
		}
	}

	throw new Error("Operation failed after multiple retries");
}

private delay(ms: number): Promise<void> {
	return new Promise(resolve => setTimeout(resolve, ms));
}
```

---

## Database-Level Verification

### Check if RowVersion Is Working

```sql
-- Check RowVersion column exists
SELECT * FROM information_schema.columns 
WHERE table_name = 'Wallets' AND column_name = 'RowVersion';

-- Create a test wallet
INSERT INTO "Wallets" (id, "UserId", address, "CreatedDate", "FiatBalance", value)
VALUES (gen_random_uuid(), gen_random_uuid(), '0x123', NOW(), 1000, 1000);

-- Check initial RowVersion
SELECT id, "FiatBalance", "RowVersion" FROM "Wallets" ORDER BY "CreatedDate" DESC LIMIT 1;

-- Update wallet
UPDATE "Wallets" 
SET "FiatBalance" = 900 
WHERE id = [wallet_id_from_above];

-- Check RowVersion incremented
SELECT id, "FiatBalance", "RowVersion" FROM "Wallets" WHERE id = [wallet_id_from_above];
-- RowVersion should be different!

-- Verify WHERE clause with RowVersion
UPDATE "Wallets"
SET "FiatBalance" = 800
WHERE id = [wallet_id_from_above] AND "RowVersion" = 1;  -- Old value
-- Should affect 0 rows (WHERE doesn't match)

SELECT id, "FiatBalance", "RowVersion" FROM "Wallets" WHERE id = [wallet_id_from_above];
-- FiatBalance should still be 900 (update failed)
```

---

## Performance Implications

### Query Before Concurrency Tokens
```sql
-- Simple update
UPDATE Wallets SET "FiatBalance" = $1, "UpdatedDate" = $2 WHERE id = $3
```

### Query After Concurrency Tokens
```sql
-- Now includes RowVersion check
UPDATE Wallets 
SET "FiatBalance" = $1, "UpdatedDate" = $2, "RowVersion" = "RowVersion" + 1 
WHERE id = $3 AND "RowVersion" = $4
```

**Performance Impact:** Negligible
- Additional condition in WHERE clause
- RowVersion is indexed implicitly (part of optimistic lock)
- No extra round-trips to database
- Can actually be faster (detects stale data earlier)

---

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task UpdateAsync_WhenConcurrencyConflict_ThrowsInvalidOperationException()
{
	// Arrange
	var mockContext = new Mock<ApplicationDbContext>();
	var mockDbSet = new Mock<DbSet<Wallet>>();

	mockContext.Setup(c => c.Set<Wallet>()).Returns(mockDbSet.Object);
	mockDbSet.Setup(d => d.Update(It.IsAny<Wallet>())).Verifiable();

	// Simulate concurrency exception
	mockContext.Setup(c => c.SaveChangesAsync(default))
		.ThrowsAsync(new DbUpdateConcurrencyException("", null));

	var repository = new Repository<Wallet>(mockContext.Object);
	var wallet = new Wallet(Guid.NewGuid());

	// Act & Assert
	var ex = await Assert.ThrowsAsync<InvalidOperationException>(
		() => repository.UpdateAsync(wallet)
	);

	Assert.Contains("modified by another process", ex.Message);
}
```

---

## Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Concurrency Detection** | None | RowVersion token |
| **Error Type** | DbUpdateConcurrencyException | InvalidOperationException |
| **Error Message** | Generic database error | "Another operation modified..." |
| **Client Knows** | System failure | Specific business event |
| **User Can Do** | Nothing, just try again | Clear retry option |
| **Developer Can Do** | Debug database | Implement retry logic |
| **Performance** | N/A | Negligible impact |

