# Implementation Checklist - Optimistic Concurrency Tokens

## ✅ Completed Changes

### 1. Domain Model Changes
- [x] **BaseEntity.cs** - Added `public uint RowVersion { get; set; }`
  - Location: `Portfolio.API/Domain/Entities/BaseEntity.cs`
  - Reason: RowVersion is inherited by all entities

### 2. EF Core Configuration Changes
- [x] **WalletConfiguration.cs** - Added `.IsRowVersion()` configuration
- [x] **AssetConfiguration.cs** - Added `.IsRowVersion()` configuration  
- [x] **TransactionConfiguration.cs** - Added `.IsRowVersion()` configuration
- [x] **LimitOrderConfiguration.cs** - Added `.IsRowVersion()` configuration
- [x] **TreasuryBalanceConfiguration.cs** - Added `.IsRowVersion()` configuration

### 3. Repository Layer Changes
- [x] **Repository.cs** - Enhanced `UpdateAsync()` method
  - Added try-catch for `DbUpdateConcurrencyException`
  - Converts to `InvalidOperationException` with descriptive message

### 4. Application Service Changes
- [x] **WalletService.cs** - Enhanced all wallet modification methods
  - [x] `BuyAsset()` - Added concurrency exception handling
  - [x] `SellAsset()` - Added concurrency exception handling
  - [x] `DepositMoney()` - Added concurrency exception handling
  - [x] `WithdrawMoney()` - Added concurrency exception handling
  - [x] `TransferAsset()` - Added concurrency exception handling (both wallets)

### 5. Database Migration
- [x] **20260520000000_AddRowVersionConcurrencyToken.cs** - Created migration
  - Adds `RowVersion` (bytea) column to all entity tables:
	- Wallets
	- Assets
	- Transactions
	- LimitOrders
	- TreasuryBalances

## 🚀 Next Steps to Deploy

### Step 1: Build the Solution
```bash
cd Portfolio.API
dotnet build
```

### Step 2: Update Database
```bash
# Apply the migration
dotnet ef database update

# Or with stack trace if needed:
dotnet ef database update --verbose
```

### Step 3: Test the Implementation
```bash
# Run existing tests to ensure no breaking changes
dotnet test

# Manual testing:
# 1. Send two concurrent BuyAsset requests to the same wallet (different idempotency keys)
# 2. Expected: First succeeds, second returns 409 or detailed error message
# 3. Verify the error message is clear and actionable
```

### Step 4: Monitor Production
- Watch error logs for concurrency exception messages
- Track retry rate of clients receiving "modified by another process" errors
- Ensure clients implement appropriate retry logic

## 📋 Code Changes Summary

### Total Files Modified: 8

1. **BaseEntity.cs** - +1 property
2. **WalletConfiguration.cs** - +3 lines
3. **AssetConfiguration.cs** - +3 lines
4. **TransactionConfiguration.cs** - +3 lines
5. **LimitOrderConfiguration.cs** - +3 lines
6. **TreasuryBalanceConfiguration.cs** - +3 lines
7. **Repository.cs** - +8 lines (try-catch block)
8. **WalletService.cs** - +40 lines (error handling in 5 methods)

### New Files Created: 2

1. **20260520000000_AddRowVersionConcurrencyToken.cs** - Migration
2. **CONCURRENCY_FIX_DOCUMENTATION.md** - Documentation

## 🔍 Verification Checklist

Before committing, verify:
- [ ] Solution builds without errors
- [ ] No compiler warnings
- [ ] All tests pass
- [ ] Migration file is syntactically correct
- [ ] No hardcoded values or debug code left
- [ ] Error messages are user-friendly

## 📊 Expected Behavior After Implementation

### Concurrent Request Scenario
```
Time    Request A                      Request B
-----   -----------                    -----------
T0      Load Wallet (v1)               Load Wallet (v1)
T1      Modify Wallet                  Modify Wallet
T2      SaveChanges ✓                  
T3                                     SaveChanges ✗
		Success                        "Another operation modified..."
```

### Single Request (Normal Case)
```
Time    Request
-----   -------
T0      Load Wallet (v1)
T1      Modify Wallet
T2      SaveChanges ✓ (becomes v2)
		Success
```

## 🆘 Troubleshooting

### Migration Rollback (if needed)
```bash
dotnet ef migrations remove
# Then delete the migration file manually
```

### Manual Row Version Check
```sql
-- View RowVersion columns in database
SELECT xmin, xmax FROM "Wallets" LIMIT 5;

-- PostgreSQL uses xmin/xmax for versioning
-- bytea column will be automatically managed by EF Core
```

### Testing Concurrency Locally
```csharp
// Simulate concurrent requests
var task1 = walletService.BuyAsset(walletId, "BTC", 50000, 1, false);
var task2 = walletService.BuyAsset(walletId, "ETH", 3000, 1, false);

await Task.WhenAll(task1, task2);
// Expected: One succeeds, one throws InvalidOperationException
```

## 📝 Notes

- RowVersion is automatically managed by EF Core and PostgreSQL
- No manual version tracking needed
- Compatible with existing business logic
- Transparent to API clients (error message is the interface)
- Can be extended to other entities in the future
