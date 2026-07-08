using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;

namespace Portfolio.API.Application.Services;

public class TreasureBalanceService(ITreasuryBalanceRepository treasuryBalanceRepository) : ITreasureBalanceService
{   
    public async Task AddAssetViaFee(string assetSymbol, decimal amount)
    {
        var treasureBalanceOfThatSymbol = await treasuryBalanceRepository.FindFirstAsync(x => x.AssetSymbol == assetSymbol);

        if(treasureBalanceOfThatSymbol == null) 
        {
            var newBalance = new TreasuryBalance { AssetSymbol = assetSymbol };
            newBalance.TotalAmount += amount;
            await treasuryBalanceRepository.AddAsync(newBalance);
            return;
        }

        treasureBalanceOfThatSymbol.TotalAmount += amount;
        await treasuryBalanceRepository.UpdateAsync(treasureBalanceOfThatSymbol);
    }

    public async Task AddAssetViaFee(decimal amount)
    {
        var treasureUSDTBalance = await treasuryBalanceRepository.FindFirstAsync(x => x.AssetSymbol == "USDT");
        if (treasureUSDTBalance == null) 
        {
            treasureUSDTBalance = new TreasuryBalance { AssetSymbol = "USDT" };
            await treasuryBalanceRepository.AddAsync(treasureUSDTBalance);
        }
        treasureUSDTBalance.TotalAmount += amount;
        await treasuryBalanceRepository.UpdateAsync(treasureUSDTBalance);
    }
}
