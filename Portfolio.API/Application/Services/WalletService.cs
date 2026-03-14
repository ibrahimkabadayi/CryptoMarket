using MassTransit;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Shared.Messages;

namespace Portfolio.API.Application.Services;

public class WalletService(IWalletRepository walletRepository, IAssetRepository assetRepository, ITransactionService transactionService, IPublishEndpoint publishEndpoint, IAssetService assetService) : IWalletService
{
    public async Task<string> BuyAsset(Guid WalletId, string Symbol, double CurrentPrice, double Amount)
    {
        var wallet = await walletRepository.GetWalletWithAssetsAsync(WalletId);

        if (wallet.FiatBalance < CurrentPrice * Amount)
            return "Error: Not Enough Money!";

        var walletAssets = wallet.Assets;

        walletAssets ??= [];

        if (walletAssets.Any(x => x.Symbol.Equals(Symbol)))
        {

            var asset = walletAssets.First(x => x.Symbol.Equals(Symbol));
            await assetService.AddAssetToWalletThatHasThatAsset(asset, CurrentPrice, Amount);
        }      
        else 
        {
            var boughtAsset = new Asset
            {
                AverageBuyPrice = CurrentPrice,
                WalletId = WalletId,
                Symbol = Symbol,
                Quantity = Amount
            };

            walletAssets.Add(boughtAsset);

            await assetRepository.AddAsync(boughtAsset);
        }

        wallet.UpdatedDate = DateTime.UtcNow;
        wallet.FiatBalance -= Amount * CurrentPrice;
        await walletRepository.UpdateAsync(wallet);
        await transactionService.CreateTransactionRecordAsync(WalletId, Symbol, Amount, CurrentPrice, TransactionType.Buy);

        return "Success: Asset purchase is successfull";
    }

    public async Task<Guid> GetWalletIdByUserId(Guid userId)
    {
        return await walletRepository.GetWalletIdByUserId(userId);
    }

    public async Task<string> CreateWallet(Guid UserId)
    {
        var generatedAddress = "0x" + Guid.NewGuid().ToString("N");

        var newWallet = new Wallet
        {
            UserId = UserId,
            Address = generatedAddress,
        };

        await walletRepository.AddAsync(newWallet);

        return generatedAddress;
    }

    public async Task<string> DepositMoney(Guid WalletId, double Amount)
    {
        try
        {
            var wallet = await walletRepository.GetByIdAsync(WalletId);
            if (wallet == null) return "Error: Could not find wallet";

            wallet.FiatBalance += Amount;
            wallet.Value += Amount;
            wallet.UpdatedDate = DateTime.UtcNow;

            await walletRepository.UpdateAsync(wallet);

            return "Success";
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex);
            return $"Error: {ex.Message}";
        }
    }

    public async Task<string> DepositMoney(string WalletAddress, double Amount)
    {
        try
        {
            var wallet = await walletRepository.FindFirstAsync(x => x.Address.Equals(WalletAddress));
            if (wallet == null) return "Error: Could not find wallet";

            wallet.FiatBalance += Amount;
            wallet.Value += Amount;
            wallet.UpdatedDate = DateTime.UtcNow;

            await walletRepository.UpdateAsync(wallet);

            await transactionService.CreateTransactionRecordAsync(wallet.Id, string.Empty, 100, null, TransactionType.Deposit);

            return "Success";
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return $"Error: {ex.Message}";
        }
    }

    public async Task<string> TransferAsset(TransferAssetDto dto)
    {
        var sourceWallet = await walletRepository.GetWalletWithAssetsAsync(dto.FromWalletId);
        if (sourceWallet is null)
            return "Error: Could not find source wallet.";

        var targetWallet = await walletRepository.GetWalletWithAssetsAsync(dto.TargetWalletAddress);
        if (targetWallet is null) return "Error: Wrong address for target wallet.";

        var asset = sourceWallet.Assets.First(x => x.Symbol.Equals(dto.Symbol));
        if (asset is null) return "Error: Could not found asset in the wallet.";
        if (asset.Quantity < dto.AssetAmount)
            return "Error: Transfer amount is bigger than asset quantity in the wallet.";

        if(targetWallet.Assets is not null && targetWallet.Assets.Any(x => x.Symbol.Equals(asset.Symbol)))
        {
            var assetInTargetWallet = targetWallet.Assets.First(x => x.Symbol.Equals(asset.Symbol));
            assetInTargetWallet.Quantity += dto.AssetAmount;
            await assetRepository.UpdateAsync(assetInTargetWallet);
        }
        else
        {
            var newAsset = new Asset 
            {
                Symbol = asset.Symbol,
                Quantity = dto.AssetAmount,
                WalletId = targetWallet.Id,
                AverageBuyPrice = asset.AverageBuyPrice,                
            };

            await assetRepository.AddAsync(newAsset);

            targetWallet.Assets!.Add(newAsset);                    
        };

        await transactionService.CreateTransactionRecordAsync(targetWallet.Id, asset.Symbol, dto.AssetAmount, asset.AverageBuyPrice, TransactionType.Transfer);

        sourceWallet.Value -= asset.Quantity * asset.AverageBuyPrice;
        sourceWallet.UpdatedDate = DateTime.UtcNow;

        targetWallet.UpdatedDate = DateTime.UtcNow;
        targetWallet.Value += asset.Quantity * asset.AverageBuyPrice;

        await walletRepository.UpdateAsync(targetWallet);

        asset.Quantity -= dto.AssetAmount;
        asset.UpdatedDate = DateTime.UtcNow;

        
        await assetRepository.UpdateAsync(asset);

        await publishEndpoint.Publish(new AssetTransferEvent 
        { 
            Message = $"{asset.Symbol} transaction from {sourceWallet.Address} to {dto.TargetWalletAddress} with amount of {dto.AssetAmount} is successfull", 
            Quantity = dto.AssetAmount,
            Symbol = asset.Symbol,
            SourceWalletUserId = sourceWallet.UserId,
            TargetWalletUserId = targetWallet.UserId
        });

        await transactionService.CreateTransactionRecordAsync(sourceWallet.Id, asset.Symbol, dto.AssetAmount, asset.AverageBuyPrice, TransactionType.Transfer);

        if (asset.Quantity == 0)
        {
            sourceWallet.Assets.Remove(asset);
        }

        await walletRepository.UpdateAsync(sourceWallet);

        return "Success: Transaction completed!";
    }
}
