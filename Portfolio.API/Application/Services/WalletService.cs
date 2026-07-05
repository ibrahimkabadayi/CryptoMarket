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
    private const int MaxRetryAttempts = 3;
    private const int InitialDelayMs = 100;

    private async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName)
    {
        int attemptCount = 0;
        int delayMs = InitialDelayMs;

        while (attemptCount < MaxRetryAttempts)
        {
            try
            {
                await operation();
                return;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another process") && attemptCount < MaxRetryAttempts - 1)
            {
                attemptCount++;
                await Task.Delay(delayMs);
                delayMs *= 2; // Exponential backoff
            }
        }

        // If all retries failed, throw the exception
        throw new InvalidOperationException("Another operation modified your wallet. Please try again.");
    }

    public async Task BuyAsset(Guid walletId, string symbol, decimal currentPrice, decimal amount, bool isLimitOrder)
    {
        var wallet = await walletRepository.GetWalletWithAssetsAsync(walletId);

        if (wallet is null) return;

        decimal totalCost = amount * currentPrice;
        decimal feeRate = isLimitOrder ? feeSettingsOptions.Value.MakerFeeRate : feeSettingsOptions.Value.TakerFeeRate;
        decimal feeInCrypto = amount * feeRate;
        decimal finalAmountToUser = amount - feeInCrypto;

        try
        {
            wallet.Buy(totalCost, feeInCrypto, finalAmountToUser, currentPrice, symbol, walletId);
        }
        catch (InvalidOperationException)
        {
            return;
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
        var newWallet = new Wallet(userId);

        await walletRepository.AddAsync(newWallet);      
    }

    public async Task DepositMoney(Guid walletId, decimal amount)
    {
        var wallet = await walletRepository.GetByIdAsync(walletId) ?? 
            throw new ArgumentException("Wallet could not be found");

        wallet.Deposit(amount);

        
            await walletRepository.UpdateAsync(wallet);
        

        await transactionService.CreateTransactionRecordAsync(walletId, amount, TransactionType.Deposit);

        await hubContext.Clients.User(wallet.UserId.ToString())
            .SendAsync("UpdateBalance", wallet.FiatBalance);
    }

    public async Task TransferAsset(TransferAssetDto dto)
    {
        var sourceWallet = await walletRepository.GetWalletWithAssetsAsync(dto.FromWalletId)
        ?? throw new ArgumentException("Source wallet could not found.");

        var targetWallet = await walletRepository.GetWalletWithAssetsAsync(dto.TargetWalletAddress)
            ?? throw new ArgumentException("Target wallet could not found.");

        var asset = sourceWallet.Assets.FirstOrDefault(x => x.Symbol.Equals(dto.Symbol))
            ?? throw new ArgumentException("Transfer asset is present in source wallet.");

        if (asset.Quantity < dto.AssetAmount)
            throw new InvalidOperationException("Not enough asset amount.");

        if (targetWallet.Assets is not null && targetWallet.Assets.Any(x => x.Symbol.Equals(asset.Symbol)))
        {
            var assetInTargetWallet = targetWallet.Assets.First(x => x.Symbol.Equals(asset.Symbol));
            assetInTargetWallet.Receive(dto.AssetAmount, asset.CostBasis);
            await assetRepository.UpdateAsync(assetInTargetWallet);
        }
        else
        {
            var newAsset = new Asset(targetWallet.Id, asset.Symbol, dto.AssetAmount, asset.CostBasis);               
            await assetRepository.AddAsync(newAsset);
            targetWallet.Assets!.Add(newAsset);
        }

        sourceWallet.DeductValue(dto.AssetAmount, asset.CostBasis);

        targetWallet.AddValue(dto.AssetAmount, asset.CostBasis);

        await ExecuteWithRetryAsync(async () =>
        {
            await walletRepository.UpdateAsync(targetWallet);
        }, "TransferAsset target wallet update");

        asset.Deduct(dto.AssetAmount);       
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

        await ExecuteWithRetryAsync(async () =>
        {
            await walletRepository.UpdateAsync(sourceWallet);
        }, "TransferAsset source wallet update");
    }

    public async Task WithdrawMoney(Guid walletId, decimal amount)
    {
        var wallet = await walletRepository.GetByIdAsync(walletId)
            ?? throw new ArgumentException("Error: Wallet does not exist for withdrawal.");

        if (wallet.FiatBalance < amount)
        {
            throw new InvalidOperationException("Error: Insufficient fiat balance.");
        }

        wallet.Withdraw(amount);

        await ExecuteWithRetryAsync(async () =>
        {
            await walletRepository.UpdateAsync(wallet);
        }, "WithdrawMoney wallet update");

        await transactionService.CreateTransactionRecordAsync(walletId, amount, TransactionType.Withdraw);

        await hubContext.Clients.User(wallet.UserId.ToString())
            .SendAsync("UpdateBalance", wallet.FiatBalance);
    }

    public async Task SellAsset(Guid walletId, string symbol, decimal price, decimal amount, bool isLimitOrder)
    {
        var wallet = await walletRepository.GetWalletWithAssetsAsync(walletId) ?? throw new ArgumentException("Error: Wallet does not exist");

        var asset = wallet.Assets?.FirstOrDefault(x => x.Symbol == symbol);
        if (asset == null || asset.Quantity < amount)
        {
            throw new ArgumentException("Error: Not enough asset to sell or asset not found!");
        }

        decimal totalCost = amount * price;
        decimal feeRate = isLimitOrder ? feeSettingsOptions.Value.MakerFeeRate : feeSettingsOptions.Value.TakerFeeRate;
        decimal feeAmount = totalCost * feeRate;

        asset.Deduct(amount);        

        if (asset.Quantity == 0)
        {
            wallet.RemoveAsset(asset);
        }

        wallet.Sell(amount, price, feeAmount);

        await ExecuteWithRetryAsync(async () =>
        {
            await walletRepository.UpdateAsync(wallet);
        }, "SellAsset wallet update");

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
