using CryptoMarket.IntegrationTests.TestContainers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Infrastructure.Context;

namespace CryptoMarket.IntegrationTests;

public class WalletConcurrencyTests(PostgresFixture fx, ITestOutputHelper output) : IClassFixture<PostgresFixture>
{
    private async Task<Guid> SeedWalletAsync(decimal balance)
    {
        var userId = Guid.NewGuid();
        await using var scope = fx.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Wallets.Add(new Wallet { Id = Guid.NewGuid(), UserId = userId, FiatBalance = balance, Value = balance });
        await db.SaveChangesAsync();
        return userId;
    }

    private async Task<decimal> GetBalanceAsync(Guid userId)
    {
        await using var scope = fx.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Wallets.Where(w => w.UserId == userId).Select(w => w.FiatBalance).SingleAsync();
    }

    [Fact]
    public async Task Concurrent_withdrawals_keep_balance_consistent()
    {
        const decimal initial = 100m, amount = 10m;
        var userId = await SeedWalletAsync(initial);

        var tasks = Enumerable.Range(0, 50).Select(async _ =>
        {
            await using var scope = fx.Provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IWalletService>();
            try
            {
                await service.WithdrawMoney(userId.ToString(), amount);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }).ToArray();

        var results = await Task.WhenAll(tasks);
        var succeeded = results.Count(result => result);
        var balance = await GetBalanceAsync(userId);

        output.WriteLine($"succeeded={succeeded}, balance={balance}");

        Assert.Equal(10, succeeded);
        Assert.True(balance >= 0, $"Balance went negative: {balance}");
        Assert.Equal(initial - succeeded * amount, balance);
    }
}
