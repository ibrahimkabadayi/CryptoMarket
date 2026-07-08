using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Application.Settings;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Hubs;
using Shared.Messages;

namespace Portfolio.API.Application.Services;

public class WalletService
    (
        IWalletRepository walletRepository,
        IAssetRepository assetRepository,
        ITransactionService transactionService,
        IPublishEndpoint publishEndpoint,
        IOptions<FeeSettings> feeSettingsOptions,
        IHubContext<PortfolioHub> hubContext
    ) : IWalletService
{
    public async Task BuyAsset(Guid walletId, string symbol, decimal currentPrice, decimal amount, bool isLimitOrder)
    {
        var wallet = await walletRepository.GetWalletWithAssetsAsync(walletId) ?? throw new ArgumentException("Error: Wallet does not exist");
        decimal totalCost = amount * currentPrice;

        if (wallet.FiatBalance < totalCost)
            throw new ArgumentException("Error: Not Enough Money!");

        decimal feeRate = isLimitOrder ? feeSettingsOptions.Value.MakerFeeRate : feeSettingsOptions.Value.TakerFeeRate;

        decimal feeInCrypto = amount * feeRate;

        decimal finalAmountToUser = amount - feeInCrypto;

        wallet.FiatBalance -= totalCost;
        wallet.UpdatedDate = DateTime.UtcNow;

        var walletAssets = wallet.Assets ?? [];
        var asset = walletAssets.FirstOrDefault(x => x.Symbol == symbol);

        if (asset != null)
        {
            asset.Quantity += finalAmountToUser;
            asset.UpdatedDate = DateTime.UtcNow;
            await assetRepository.UpdateAsync(asset);
        }
        else
        {
            asset = new Asset
            {
                Symbol = symbol,
                Quantity = finalAmountToUser,
                WalletId = walletId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            await assetRepository.AddAsync(asset);
            walletAssets.Add(asset);
            wallet.Assets = walletAssets;
        }

        await walletRepository.UpdateAsync(wallet);

        await transactionService.CreateTransactionRecordAsync(walletId, symbol, finalAmountToUser, currentPrice, TransactionType.Buy);

        if (feeInCrypto > 0)
        {
            var feeEvent = new FeeCollectionEvent
            {
                Symbol = symbol,
                FeeAmount = feeInCrypto,
                UserId = wallet.UserId,
                OccurredOn = DateTime.UtcNow
            };

            await publishEndpoint.Publish(feeEvent);
        }

        var assetDtos = wallet.Assets?.Select(a => new AssetDashboardDto
        {
            Symbol = a.Symbol,
            Quantity = a.Quantity,
            AverageBuyPrice = a.CostBasis
        }).ToList() ?? [];

        var updatedDashboard = new PortfolioDashboardDto
        {
            FiatBalance = wallet.FiatBalance,
            TotalInvestedValue = wallet.Value,
            Assets = assetDtos
        };

        await hubContext.Clients.User(wallet.UserId.ToString()).SendAsync("UpdatePortfolio", updatedDashboard);
    }


    public async Task<Guid> GetWalletIdByUserId(Guid userId)
    {
        return await walletRepository.GetWalletIdByUserId(userId);
    }

    public async Task CreateWallet(Guid userId)
    {
        var generatedAddress = "0x" + Guid.NewGuid().ToString("N");

        var newWallet = new Wallet
        {
            UserId = userId,
            Address = generatedAddress,
        };

        await walletRepository.AddAsync(newWallet);      
    }

    public async Task DepositMoney(Guid walletId, decimal amount)
    {
        try
        {
            var wallet = await walletRepository.GetByIdAsync(walletId) ?? throw new ArgumentException("Error: Could not find wallet");
            wallet.FiatBalance += amount;
            wallet.Value += amount;
            wallet.UpdatedDate = DateTime.UtcNow;

            await walletRepository.UpdateAsync(wallet);

            await transactionService.CreateTransactionRecordAsync(walletId, amount, TransactionType.Deposit);

            await hubContext.Clients.User(wallet.UserId.ToString())
                .SendAsync("UpdateBalance", wallet.FiatBalance);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new ArgumentException($"Error: {ex.Message}");
        }
    }

    public async Task TransferAsset(TransferAssetDto dto)
    {
        var sourceWallet = await walletRepository.GetWalletWithAssetsAsync(dto.FromWalletId);
        if (sourceWallet is null)
            throw new ArgumentException("Error: Could not find source wallet.");

        var targetWallet = await walletRepository.GetWalletWithAssetsAsync(dto.TargetWalletAddress);
        if (targetWallet is null)
            throw new ArgumentException("Error: Wrong address for target wallet.");

        var asset = sourceWallet.Assets.FirstOrDefault(x => x.Symbol.Equals(dto.Symbol));
        if (asset is null)
            throw new ArgumentException("Error: Could not find asset in the wallet.");

        if (asset.Quantity < dto.AssetAmount)
            throw new ArgumentException("Error: Transfer amount is bigger than asset quantity in the wallet.");

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
                CostBasis = asset.CostBasis,
            };
            await assetRepository.AddAsync(newAsset);
            targetWallet.Assets!.Add(newAsset);
        }

        sourceWallet.Value -= asset.Quantity * asset.CostBasis;
        sourceWallet.UpdatedDate = DateTime.UtcNow;

        targetWallet.Value += dto.AssetAmount * asset.CostBasis;
        targetWallet.UpdatedDate = DateTime.UtcNow;
        await walletRepository.UpdateAsync(targetWallet);

        asset.Quantity -= dto.AssetAmount;
        asset.UpdatedDate = DateTime.UtcNow;
        await assetRepository.UpdateAsync(asset);

        await transactionService.CreateTransactionRecordAsync(sourceWallet.Id, asset.Symbol, dto.AssetAmount, asset.CostBasis, TransactionType.Transfer);
        await transactionService.CreateTransactionRecordAsync(targetWallet.Id, asset.Symbol, dto.AssetAmount, asset.CostBasis, TransactionType.Transfer);

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
    }

    public async Task WithdrawMoney(Guid walletId, decimal amount)
    {
        var wallet = await walletRepository.GetByIdAsync(walletId);

        if (wallet == null) return;

        if (wallet.FiatBalance < amount) return;

        await transactionService.CreateTransactionRecordAsync(walletId, amount, TransactionType.Withdraw);
        wallet.Value -= amount;

        await hubContext.Clients.User(wallet.UserId.ToString())
            .SendAsync("UpdateBalance", wallet.FiatBalance);
    }

    public async Task SellAsset(Guid walletId, string symbol, decimal price, decimal amount, bool isLimitOrder)
    {
        var wallet = await walletRepository.GetWalletWithAssetsAsync(walletId);
        if (wallet is null) throw new ArgumentException("Error: Wallet does not exist");

        var asset = wallet.Assets?.FirstOrDefault(x => x.Symbol == symbol);
        if (asset == null || asset.Quantity < amount)
        {
            throw new ArgumentException("Error: Not enough asset to sell or asset not found!");
        }

        decimal totalCost = amount * price;
        decimal feeRate = isLimitOrder ? feeSettingsOptions.Value.MakerFeeRate : feeSettingsOptions.Value.TakerFeeRate;
        decimal feeAmount = totalCost * feeRate;

        asset.Quantity -= amount;
        asset.UpdatedDate = DateTime.UtcNow;

        if (asset.Quantity == 0)
        {
            wallet.Assets!.Remove(asset);
        }

        wallet.FiatBalance += totalCost;
        wallet.FiatBalance -= feeAmount;
        wallet.UpdatedDate = DateTime.UtcNow;

        await walletRepository.UpdateAsync(wallet);

        await transactionService.CreateTransactionRecordAsync(walletId, symbol, amount, price, TransactionType.Sell);

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

        var assetDtos = wallet.Assets?.Select(a => new AssetDashboardDto
        {
            Symbol = a.Symbol,
            Quantity = a.Quantity,
            AverageBuyPrice = a.CostBasis
        }).ToList() ?? [];

        var updatedDashboard = new PortfolioDashboardDto
        {
            FiatBalance = wallet.FiatBalance,
            TotalInvestedValue = wallet.Value,
            Assets = assetDtos
        };

        await hubContext.Clients.User(wallet.UserId.ToString()).SendAsync("UpdatePortfolio", updatedDashboard);
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
            AverageBuyPrice = a.CostBasis
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
