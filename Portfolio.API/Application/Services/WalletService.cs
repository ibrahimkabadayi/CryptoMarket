using MassTransit;
using MassTransit.Transports;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Application.Settings;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Shared.Messages;

namespace Portfolio.API.Application.Services;

public class WalletService
    (
        IWalletRepository walletRepository,
        IAssetRepository assetRepository,
        ITransactionService transactionService,
        IPublishEndpoint publishEndpoint,
        FeeSettings feeSettings
    ) : IWalletService
{
    public async Task<string> BuyAsset(Guid WalletId, string Symbol, decimal CurrentPrice, decimal Amount, bool isLimitOrder)
    {
        var wallet = await walletRepository.GetWalletWithAssetsAsync(WalletId);

        if (wallet is null) return "Error: Wallet does not exist";

        decimal totalCost = Amount * CurrentPrice;

        if (wallet.FiatBalance < totalCost)
            return "Error: Not Enough Money!";

        decimal feeRate = isLimitOrder ? feeSettings.MakerFeeRate : feeSettings.TakerFeeRate;

        decimal feeInCrypto = Amount * feeRate;

        decimal finalAmountToUser = Amount - feeInCrypto;

        wallet.FiatBalance -= totalCost;
        wallet.UpdatedDate = DateTime.UtcNow;

        var walletAssets = wallet.Assets ?? [];
        var asset = walletAssets.FirstOrDefault(x => x.Symbol == Symbol);

        if (asset != null)
        {
            asset.Quantity += finalAmountToUser;
            asset.UpdatedDate = DateTime.UtcNow;
        }
        else
        {
            asset = new Asset
            {
                Symbol = Symbol,
                Quantity = finalAmountToUser,
                WalletId = WalletId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            walletAssets.Add(asset);
            wallet.Assets = walletAssets;
        }

        await walletRepository.UpdateAsync(wallet);

        await transactionService.CreateTransactionRecordAsync(WalletId, Symbol, finalAmountToUser, CurrentPrice, TransactionType.Buy);

        if (feeInCrypto > 0)
        {
            var feeEvent = new FeeCollectionEvent
            {
                Symbol = Symbol,
                FeeAmount = feeInCrypto,
                UserId = wallet.UserId,
                OccurredOn = DateTime.UtcNow
            };

            await publishEndpoint.Publish(feeEvent);
        }

        return "Success: Asset bought successfully";
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

    public async Task<string> DepositMoney(Guid WalletId, decimal Amount)
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

    public async Task<string> TransferAsset(TransferAssetDto dto)
    {
        var sourceWallet = await walletRepository.GetWalletWithAssetsAsync(dto.FromWalletId);
        if (sourceWallet is null)
            return "Error: Could not find source wallet.";

        var targetWallet = await walletRepository.GetWalletWithAssetsAsync(dto.TargetWalletAddress);
        if (targetWallet is null)
            return "Error: Wrong address for target wallet.";

        var asset = sourceWallet.Assets.FirstOrDefault(x => x.Symbol.Equals(dto.Symbol));
        if (asset is null)
            return "Error: Could not find asset in the wallet.";

        if (asset.Quantity < dto.AssetAmount)
            return "Error: Transfer amount is bigger than asset quantity in the wallet.";

        if (targetWallet.Assets is not null && targetWallet.Assets.Any(x => x.Symbol.Equals(asset.Symbol)))
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
        }

        sourceWallet.Value -= asset.Quantity * asset.AverageBuyPrice;
        sourceWallet.UpdatedDate = DateTime.UtcNow;

        targetWallet.Value += dto.AssetAmount * asset.AverageBuyPrice;
        targetWallet.UpdatedDate = DateTime.UtcNow;
        await walletRepository.UpdateAsync(targetWallet);

        asset.Quantity -= dto.AssetAmount;
        asset.UpdatedDate = DateTime.UtcNow;
        await assetRepository.UpdateAsync(asset);

        await transactionService.CreateTransactionRecordAsync(sourceWallet.Id, asset.Symbol, dto.AssetAmount, asset.AverageBuyPrice, TransactionType.Transfer);
        await transactionService.CreateTransactionRecordAsync(targetWallet.Id, asset.Symbol, dto.AssetAmount, asset.AverageBuyPrice, TransactionType.Transfer);

        await publishEndpoint.Publish(new AssetTransferEvent
        {
            Message = $"{asset.Symbol} transaction from {sourceWallet.Address} to {dto.TargetWalletAddress} with amount of {dto.AssetAmount} is successfull",
            Quantity = dto.AssetAmount,
            Symbol = asset.Symbol,
            SourceWalletUserId = sourceWallet.UserId,
            TargetWalletUserId = targetWallet.UserId
        });

        if (asset.Quantity == 0)
        {
            sourceWallet.Assets.Remove(asset);
            await assetRepository.DeleteAsync(asset.Id);
        }

        await walletRepository.UpdateAsync(sourceWallet);

        return "Success: Transaction completed!";
    }

    public async Task WithdrawMoney(Guid WalletId, decimal Amount)
    {
        var wallet = await walletRepository.GetByIdAsync(WalletId);

        if (wallet == null) return;

        if (wallet.FiatBalance < Amount) return;

        wallet.FiatBalance -= Amount;
        wallet.Value -= Amount;

        await walletRepository.UpdateAsync(wallet);
    }

    public async Task<string> SellAsset(Guid WalletId, string Symbol, decimal Price, decimal Amount, bool isLimitOrder)
    {
        var wallet = await walletRepository.GetWalletWithAssetsAsync(WalletId);
        if (wallet is null) return "Error: Wallet does not exist";

        var asset = wallet.Assets?.FirstOrDefault(x => x.Symbol == Symbol);
        if (asset == null || asset.Quantity < Amount)
        {
            return "Error: Not enough asset to sell or asset not found!";
        }

        decimal totalCost = Amount * Price;
        decimal feeRate = isLimitOrder ? feeSettings.MakerFeeRate : feeSettings.TakerFeeRate;
        decimal feeAmount = totalCost * feeRate;

        asset.Quantity -= Amount;
        asset.UpdatedDate = DateTime.UtcNow;

        if (asset.Quantity == 0)
        {
            wallet.Assets!.Remove(asset);
        }

        wallet.FiatBalance += totalCost;
        wallet.FiatBalance -= feeAmount;
        wallet.UpdatedDate = DateTime.UtcNow;

        await walletRepository.UpdateAsync(wallet);

        await transactionService.CreateTransactionRecordAsync(WalletId, Symbol, Amount, Price, TransactionType.Sell);

        if (feeAmount > 0)
        {
            var feeEvent = new FeeCollectionEvent
            {
                Symbol = "USDT",
                FeeAmount = feeAmount,
                UserId = wallet.UserId,
                OccurredOn = DateTime.UtcNow
            };

            await publishEndpoint.Publish(feeEvent);
        }

        return "Success: Asset sold successfully";
    }

    public async Task<PortfolioDashboardDto> GetPortfolioDashboardAsync(Guid userId)
    {
        var walletId = await walletRepository.GetWalletIdByUserId(userId);
        if (walletId == Guid.Empty) return null!;

        var wallet = await walletRepository.GetWalletWithAssetsAsync(walletId);
        if (wallet == null) return null!;

        var assetDtos = wallet.Assets?.Select(a => new AssetDashboardDto
        {
            Symbol = a.Symbol,
            Quantity = a.Quantity,
            AverageBuyPrice = a.AverageBuyPrice
        }).ToList() ?? [];

        var recentTx = transactionService.GetTenLastTransaction(walletId);

        var dashboard = new PortfolioDashboardDto
        {
            WalletId = wallet.Id,
            Address = wallet.Address,
            FiatBalance = wallet.FiatBalance,
            Assets = assetDtos,
            RecentTransactions = recentTx,
            TotalInvestedValue = assetDtos.Sum(a => a.InvestedAmount)
        };

        return dashboard;
    }
}
