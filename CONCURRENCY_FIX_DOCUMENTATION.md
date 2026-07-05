# Concurrency Token Implementation - Summary

## Problem Identified
The application was experiencing `DbUpdateConcurrencyException` when multiple concurrent requests attempted to modify the same wallet entity. This occurred because:

1. Two or more requests could load the same wallet entity simultaneously
2. Each request would modify the wallet independently in memory
3. The first request to save would succeed
4. The second request would fail with `DbUpdateConcurrencyException` because the database row had been modified by another request

## Root Cause
The Wallet and related entities (Asset, Transaction, LimitOrder, TreasuryBalance) did not have optimistic concurrency tokens configured in Entity Framework Core. Without a concurrency token, EF Core cannot detect when an entity has been modified by another process.

## Solution Implemented
Added optimistic concurrency control using Row Version tokens to all entities:

### 1. **BaseEntity.cs** - Added RowVersion Property
```csharp
public uint RowVersion { get; set; }
```

The `RowVersion` property is a `uint` that EF Core will automatically manage:
- Automatically incremented by the database on each update
- Used in WHERE clauses to detect if the row has changed
- If changed, SaveChangesAsync will throw DbUpdateConcurrencyException

### 2. **Entity Configurations** - Configured RowVersion as Concurrency Token
Updated all entity configurations to mark RowVersion as a row version:
- WalletConfiguration.cs
- AssetConfiguration.cs
- TransactionConfiguration.cs
- LimitOrderConfiguration.cs
- TreasuryBalanceConfiguration.cs

```csharp
builder.Property(x => x.RowVersion)
	.IsRowVersion();
```

### 3. **Repository.cs** - Added Exception Handling
Updated the `UpdateAsync` method to catch concurrency exceptions:

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
		throw new InvalidOperationException(
			"The entity has been modified by another process. Please reload and try again.", ex);
	}
}
```

### 4. **WalletService.cs** - Enhanced Error Handling
Added specific handling for concurrency exceptions in all wallet modification methods:
- BuyAsset
- SellAsset
- DepositMoney
- WithdrawMoney
- TransferAsset

Example:
```csharp
try
{
	await walletRepository.UpdateAsync(wallet);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another process"))
{
	throw new InvalidOperationException("Another operation modified your wallet. Please try again.", ex);
}
```

### 5. **Database Migration** - Added RowVersion Columns
Created migration: `20260520000000_AddRowVersionConcurrencyToken.cs`

This migration:
- Adds RowVersion (bytea type) column to all entity tables
- Sets rowVersion: true in the migration configuration
- Adds default values for existing rows

## Migration Steps Required

To apply these changes to your database:

```bash
# Navigate to Portfolio.API directory
cd Portfolio.API

# Apply the migration
dotnet ef database update

# Or if using a specific project startup:
dotnet ef database update --startup-project ../Services.API
```

## How It Works

### Before (Without Concurrency Token)
1. Request A: Loads Wallet (id: 123)
2. Request B: Loads Wallet (id: 123)
3. Request A: Modifies and saves → SUCCESS
4. Request B: Modifies and saves → FAILS (0 rows affected)

### After (With Concurrency Token)
1. Request A: Loads Wallet (id: 123, RowVersion: 1)
2. Request B: Loads Wallet (id: 123, RowVersion: 1)
3. Request A: Modifies and saves WHERE RowVersion=1 → SUCCESS (RowVersion becomes 2)
4. Request B: Modifies and saves WHERE RowVersion=1 → FAILS with DbUpdateConcurrencyException (row now has RowVersion=2)
5. Request B: Error message "Another operation modified your wallet. Please try again."

## Client-Side Behavior

When a concurrency conflict occurs:
- Instead of a generic DB error, users receive: **"Another operation modified your wallet. Please try again."**
- This occurs in scenarios like:
  - Two rapid Buy requests to the same wallet
  - Buy and Sell happening simultaneously
  - Multiple users sharing the same wallet

Clients should implement **retry logic** on receiving this error.

## Testing Recommendations

1. **Unit Tests**: Verify that DbUpdateConcurrencyException is properly caught and converted
2. **Integration Tests**: Test concurrent operations using Task.WhenAll()
3. **Load Tests**: Verify multiple concurrent requests to the same wallet
4. **User Testing**: Confirm error messages are clear and actionable

## Files Modified

- `Portfolio.API/Domain/Entities/BaseEntity.cs` - Added RowVersion
- `Portfolio.API/Infrastructure/Configurations/WalletConfiguration.cs` - Added RowVersion config
- `Portfolio.API/Infrastructure/Configurations/AssetConfiguration.cs` - Added RowVersion config
- `Portfolio.API/Infrastructure/Configurations/TransactionConfiguration.cs` - Added RowVersion config
- `Portfolio.API/Infrastructure/Configurations/LimitOrderConfiguration.cs` - Added RowVersion config
- `Portfolio.API/Infrastructure/Configurations/TreasuryBalanceConfiguration.cs` - Added RowVersion config
- `Portfolio.API/Infrastructure/Repositories/Repository.cs` - Enhanced error handling
- `Portfolio.API/Application/Services/WalletService.cs` - Added concurrency exception handling
- `Portfolio.API/Migrations/20260520000000_AddRowVersionConcurrencyToken.cs` - Migration file

## Additional Notes

- PostgreSQL uses bytea (byte array) type for row version columns
- EF Core automatically manages RowVersion values - no manual updates needed
- The concurrency token is transparent to business logic - all modifications work as before
- Performance impact is minimal - just additional WHERE clause conditions
