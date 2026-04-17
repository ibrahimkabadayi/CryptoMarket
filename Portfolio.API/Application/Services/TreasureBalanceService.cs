using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;

namespace Portfolio.API.Application.Services;

public class TreasureBalanceService(ITreasuryBalanceRepository treasuryBalanceRepository) : ITreasureBalanceService
{
    //Symbol could be BTC, ETH, SOL, XRP (default coin of that blockchain)
    public async Task AddAssetViaFee(string assetSymbol, decimal amount)
    {
        var treasureBalanceOfThatSymbol = await treasuryBalanceRepository.FindFirstAsync(x => x.AssetSymbol == assetSymbol);

        if(treasureBalanceOfThatSymbol == null) 
        {
            var newBalance = new TreasuryBalance(assetSymbol);
            newBalance.AddFunds(amount);
            await treasuryBalanceRepository.AddAsync(newBalance);
            return;
        }

        treasureBalanceOfThatSymbol.AddFunds(amount);
        await treasuryBalanceRepository.UpdateAsync(treasureBalanceOfThatSymbol);
    }

    //Symbol is USDT
    public async Task AddAssetViaFee(decimal amount)
    {
        var treasureUSDTBalance = await treasuryBalanceRepository.FindFirstAsync(x => x.AssetSymbol == "USDT");
        if (treasureUSDTBalance == null) 
        {
            treasureUSDTBalance = new TreasuryBalance("USDT");
            await treasuryBalanceRepository.AddAsync(treasureUSDTBalance);
        }
        treasureUSDTBalance.AddFunds(amount);
        await treasuryBalanceRepository.UpdateAsync(treasureUSDTBalance);
    }
}
