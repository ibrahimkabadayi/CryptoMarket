# Deployment Guide - DbUpdateConcurrencyException Fix

## Overview
This guide walks through deploying the optimistic concurrency token implementation to fix `DbUpdateConcurrencyException` errors in the Portfolio.API.

## Prerequisites
- Visual Studio / VS Code with .NET 9
- PostgreSQL database access
- Git repository access
- Admin or elevated privileges for database operations

## Phase 1: Pre-Deployment Verification (Development Environment)

### 1.1 Verify Code Compilation
```bash
cd Portfolio.API
dotnet clean
dotnet build
```
✅ Expected: Build succeeds with no errors or warnings

### 1.2 Verify All Tests Pass
```bash
dotnet test --no-build
```
✅ Expected: All tests pass

### 1.3 Review Migration File
```bash
# Verify migration file exists and is syntactically correct
ls -la Portfolio.API/Migrations/*RowVersion*.cs
cat Portfolio.API/Migrations/20260520000000_AddRowVersionConcurrencyToken.cs
```
✅ Expected: File should add RowVersion column to 5 tables

## Phase 2: Database Backup (Critical!)

### 2.1 Backup Production Database
```bash
# Using pg_dump for PostgreSQL
pg_dump -U <username> -h <server> -F c -d portfolio_db > portfolio_db_backup_$(date +%Y%m%d_%H%M%S).dump

# Or use your backup tool:
# - SQL Server: SQL Server Management Studio backup
# - pgAdmin: Backup in pgAdmin UI
```

### 2.2 Verify Backup
```bash
# Check backup file size and integrity
ls -lh portfolio_db_backup_*.dump
file portfolio_db_backup_*.dump
```
✅ Expected: Backup file exists and is > 1MB (confirms data is there)

## Phase 3: Staging Environment Deployment

### 3.1 Deploy Migration to Staging
```bash
# Apply migration to staging database
cd Portfolio.API
dotnet ef database update --environment Staging

# Or with connection string override:
dotnet ef database update --connection "Server=staging-db;Database=portfolio_db;..."
```
✅ Expected: Migration completes successfully with message like:
```
Applying migration '20260520000000_AddRowVersionConcurrencyToken'.
Done.
```

### 3.2 Verify Migration Applied
```sql
-- Connect to staging database
psql -U <username> -h <server> -d portfolio_db

-- Check if RowVersion column exists
\d "Wallets"
\d "Assets"
\d "Transactions"

-- Should see new "RowVersion" columns in each table
```
✅ Expected: All tables have new `RowVersion bytea` column

### 3.3 Verify Application Works with New Schema
```bash
# Deploy staging build with changes
dotnet publish -c Release -o ./staging-publish

# Copy to staging server and restart
# Run integration tests against staging
dotnet test --environment Staging
```
✅ Expected: All tests pass, no errors in logs

### 3.4 Load Testing (Optional but Recommended)
```bash
# Simulate concurrent wallet operations
# Test Results to Check:
# - No DbUpdateConcurrencyException in logs
# - Clear error messages appearing (not stack traces)
# - Proper HTTP status codes (409 for conflict)
```

## Phase 4: Production Deployment

### 4.1 Schedule Maintenance Window
- Coordinate with team to define maintenance window
- Notify users if applicable
- Ensure monitoring is active

### 4.2 Final Code Push
```bash
# Commit all changes
git add .
git commit -m "feat: implement optimistic concurrency tokens for wallet operations

- Add RowVersion concurrency token to all entities
- Update Repository.UpdateAsync with exception handling  
- Add error handling to WalletService methods
- Create migration for RowVersion columns"

# Push to main branch
git push origin main
```

### 4.3 Deploy Application Build
```bash
# Option A: Using CI/CD Pipeline (Recommended)
# Push triggers automatic build and deployment

# Option B: Manual Deployment
cd Portfolio.API
dotnet publish -c Release -o /deployment/release
# Copy to production server
```

### 4.4 Apply Database Migration
```bash
# CRITICAL: Do this during maintenance window
cd Portfolio.API

# Run migration with verbose output
dotnet ef database update --verbose

# Connection string should point to production
# Example environment-specific config:
# export ConnectionStrings__DefaultConnection="Server=prod-db;Database=portfolio_db;..."
```
✅ Expected: Migration completes with success message

### 4.5 Verify Production Deployment
```bash
# Check that application started successfully
docker logs portfolio-api  # or relevant container/service

# Verify database changes
psql -U <prod_user> -h <prod_server> -d portfolio_db -c "\d \"Wallets\";"

# Run health checks
curl https://api.example.com/health
```
✅ Expected: Application running, RowVersion columns exist, health check returns 200

## Phase 5: Post-Deployment Monitoring

### 5.1 Monitor Application Logs
```bash
# Watch for any errors
tail -f /var/log/portfolio-api/error.log | grep -i concurrency

# Should NOT see:
# - DbUpdateConcurrencyException (uncaught)
# - "data may have been modified or deleted"

# SHOULD see (if there are concurrent updates):
# - "Another operation modified your wallet. Please try again."
```

### 5.2 Monitor Database Performance
```sql
-- Check for any locks or slow operations
SELECT * FROM pg_locks WHERE NOT granted;
SELECT * FROM pg_stat_statements WHERE normalized_query LIKE '%Wallet%' ORDER BY mean_exec_time DESC LIMIT 5;
```
✅ Expected: No unusual locks or slow queries

### 5.3 Monitor Error Tracking System
- Check error reporting service (Sentry, ApplicationInsights, etc.)
- Look for new `InvalidOperationException` patterns
- Monitor retry rates from client applications
- Expected: Some "modified by another process" errors if users try concurrent operations (this is normal)

### 5.4 Verify Business Operations
```bash
# Test critical workflows
# 1. User deposits funds → Buys asset → Sells asset
# 2. Two concurrent buy orders (may result in conflict - expected)
# 3. Verify transaction records are correct
# 4. Check account balance accuracy
```

## Rollback Plan (If Issues Occur)

### Option 1: Immediate Rollback (Within 30 minutes)
```bash
# Stop current deployment
# Revert to previous application build (remove RowVersion handling code)
# This works because RowVersion column doesn't break old code - it's just ignored

# Application will work with columns present but ignoring RowVersion
# No database rollback needed
```

### Option 2: Database Rollback (If needed)
```bash
# If something went wrong during migration:
cd Portfolio.API

# Remove the latest migration
dotnet ef migrations remove

# Restore from backup
pg_restore -U <username> -h <server> -d portfolio_db portfolio_db_backup_YYYYMMDD_HHMMSS.dump
```

### Option 3: Gradual Rollback
```bash
# If issues detected post-deployment:
# 1. Leave RowVersion columns in database (harmless)
# 2. Revert application code to version without error handling
# 3. Old code will simply ignore RowVersion
# 4. Can re-deploy fix at later time
```

## Validation Checklist

Items to verify after deployment:

- [ ] Application starts without errors
- [ ] All health checks pass
- [ ] Database migration completed successfully
- [ ] RowVersion columns exist in all tables
- [ ] No unusual error logs
- [ ] Users can perform normal operations (buy, sell, deposit, withdraw)
- [ ] Concurrent operations either succeed or return clear error message
- [ ] Error rate within expected bounds
- [ ] Database performance acceptable
- [ ] Response times acceptable
- [ ] All critical business workflows functional

## Communication Template

After successful deployment, notify team:

```
🎉 Deployment Complete: Optimistic Concurrency Control

What was deployed:
- Added RowVersion concurrency tokens to wallet-related entities
- Enhanced error handling for concurrent wallet modifications
- Database migration (5 new columns added)

What changed for users:
- Rare concurrent operation conflicts now return clearer error messages
- Error: "Another operation modified your wallet. Please try again."
- Users should retry the operation

What to monitor:
- Watch for any "modified by another process" errors in logs
- These are EXPECTED if users try very rapid concurrent operations
- This is better than the previous silent failure

If issues:
- Report in #incidents channel
- Provide wallet ID and operation type
- Include timestamp and error message
```

## Performance Impact

Expected resource usage changes:
- **Database CPU**: +1-2% (minimal WHERE clause additions)
- **Memory**: No change
- **Disk**: +5 bytes per row (RowVersion column)
- **Network**: Negligible

Estimated downtime: **2-5 minutes** during migration execution

## Technical Support

For issues during deployment:

1. **Migration Fails**
   - Check database connectivity
   - Verify permissions for `ALTER TABLE`
   - Check available disk space

2. **Application Won't Start**
   - Verify all NuGet packages updated
   - Check application configuration
   - Review application logs for errors

3. **Concurrency Errors Still Occurring**
   - Verify RowVersion columns created
   - Check Repository.UpdateAsync exception handling
   - Verify configuration applied to all entities

Contact: Development Team @ [support-channel]
