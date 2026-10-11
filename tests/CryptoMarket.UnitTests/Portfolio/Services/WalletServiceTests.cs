using CryptoMarket.UnitTests.Common.Helpers;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using NSubstitute;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Application.Services;
using Portfolio.API.Application.Settings;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Hubs;
using Shared.Messages;
using Xunit;

namespace CryptoMarket.UnitTests.Portfolio.Services;

public class WalletServiceTests
{
    private readonly IWalletRepository _walletRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ITransactionService _transactionService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IOptions<FeeSettings> _feeSettings;
    private readonly IHubContext<PortfolioHub> _hubContext;
    private readonly IClientProxy _clientProxy;
    private readonly WalletService _sut;

    private const decimal MakerFee = 0.001m; // 0.1%
    private const decimal TakerFee = 0.002m; // 0.2%

    public WalletServiceTests()
    {
        _walletRepository = Substitute.For<IWalletRepository>();
        _assetRepository = Substitute.For<IAssetRepository>();
        _transactionService = Substitute.For<ITransactionService>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();

        _feeSettings = Options.Create(new FeeSettings
        {
            MakerFeeRate = MakerFee,
            TakerFeeRate = TakerFee
        });

        var mockHub = MockHubContext.Create<PortfolioHub>();
        _hubContext = mockHub.HubContext;
        _clientProxy = mockHub.ClientProxy;

        _sut = new WalletService(
            _walletRepository,
            _assetRepository,
            _transactionService,
            _publishEndpoint,
            _feeSettings,
            _hubContext
        );
    }

    [Fact]
    public async Task BuyAsset_WhenInsufficientFiatBalance_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var wallet = new Wallet
        {
            Id = walletId,
            UserId = userId,
            FiatBalance = 500m // Needs 1 * 60,000 = 60,000
        };

        _walletRepository.GetWalletIdByUserId(userId).Returns(walletId);
        _walletRepository.GetWalletWithAssetsAsync(walletId).Returns(wallet);

        // Act
        var act = async () => await _sut.BuyAsset(userId.ToString(), "BTC", 60000m, 1m, isLimitOrder: false);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Error: Not Enough Money!");
    }

    [Fact]
    public async Task BuyAsset_WhenSufficientFiatAndAssetIsNew_CreatesAssetDeductsFiatPublishesEventsAndSignalR()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var wallet = new Wallet
        {
            Id = walletId,
            UserId = userId,
            FiatBalance = 70000m,
            Assets = []
        };

        _walletRepository.GetWalletIdByUserId(userId).Returns(walletId);
        _walletRepository.GetWalletWithAssetsAsync(walletId).Returns(wallet);

        const decimal price = 60000m;
        const decimal amount = 1m;
        const decimal expectedCost = 60000m;
        // Taker fee: 1 * 0.002 = 0.002 BTC
        const decimal expectedFeeInCrypto = 0.002m;
        const decimal finalCryptoAmount = 0.998m;

        // Act
        await _sut.BuyAsset(userId.ToString(), "BTC", price, amount, isLimitOrder: false);

        // Assert
        wallet.FiatBalance.Should().Be(10000m); // 70,000 - 60,000

        await _assetRepository.Received(1).AddAsync(Arg.Is<Asset>(a =>
            a.Symbol == "BTC" &&
            a.Quantity == finalCryptoAmount &&
            a.WalletId == walletId
        ));

        await _walletRepository.Received(1).UpdateAsync(wallet);

        await _transactionService.Received(1).CreateTransactionRecordAsync(
            walletId,
            "BTC",
            finalCryptoAmount,
            price,
            TransactionType.Buy
        );

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<FeeCollectionEvent>(e =>
                e.Symbol == "BTC" &&
                e.FeeAmount == expectedFeeInCrypto &&
                e.UserId == userId
            ),
            Arg.Any<CancellationToken>()
        );

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<TradeExecuted>(e =>
                e.WalletId == walletId &&
                e.BoughtAmount == finalCryptoAmount &&
                e.BoughtCurrency == "BTC" &&
                e.SoldAmount == expectedCost &&
                e.SoldCurrency == "USDT"
            ),
            Arg.Any<CancellationToken>()
        );

        await _clientProxy.Received(1).SendCoreAsync(
            "UpdatePortfolio",
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is PortfolioDashboardDto),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task BuyAsset_WhenAssetAlreadyExists_UpdatesAssetQuantityAndDeductsFiat()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var existingAsset = new Asset
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            Symbol = "ETH",
            Quantity = 2m
        };
        var wallet = new Wallet
        {
            Id = walletId,
            UserId = userId,
            FiatBalance = 10000m,
            Assets = [existingAsset]
        };

        _walletRepository.GetWalletIdByUserId(userId).Returns(walletId);
        _walletRepository.GetWalletWithAssetsAsync(walletId).Returns(wallet);

        const decimal price = 3000m;
        const decimal amount = 1m;
        // Limit order -> Maker fee: 1 * 0.001 = 0.001 ETH
        const decimal expectedFinalAddition = 0.999m;

        // Act
        await _sut.BuyAsset(userId.ToString(), "ETH", price, amount, isLimitOrder: true);

        // Assert
        existingAsset.Quantity.Should().Be(2m + expectedFinalAddition);
        wallet.FiatBalance.Should().Be(7000m); // 10,000 - 3,000
        await _assetRepository.Received(1).UpdateAsync(existingAsset);
    }

    [Fact]
    public async Task SellAsset_WhenAssetNotFoundOrInsufficient_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var wallet = new Wallet
        {
            Id = walletId,
            UserId = userId,
            Assets = [new Asset { Symbol = "BTC", Quantity = 0.1m }]
        };

        _walletRepository.GetWalletIdByUserId(userId).Returns(walletId);
        _walletRepository.GetWalletWithAssetsAsync(walletId).Returns(wallet);

        // Act (Trying to sell 0.5 BTC when only 0.1 exists)
        var act = async () => await _sut.SellAsset(userId.ToString(), "BTC", 60000m, 0.5m, isLimitOrder: false);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Error: Not enough asset to sell or asset not found!");
    }

    [Fact]
    public async Task SellAsset_WhenSufficientQuantity_DeductsAssetIncreasesFiatPublishesEventsAndSignalR()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            Symbol = "BTC",
            Quantity = 1m
        };
        var wallet = new Wallet
        {
            Id = walletId,
            UserId = userId,
            FiatBalance = 5000m,
            Assets = [asset]
        };

        _walletRepository.GetWalletIdByUserId(userId).Returns(walletId);
        _walletRepository.GetWalletWithAssetsAsync(walletId).Returns(wallet);

        const decimal price = 60000m;
        const decimal amount = 0.5m;
        const decimal totalGross = 30000m; // 0.5 * 60,000
        // Taker fee: 30,000 * 0.002 = 60 USDT
        const decimal expectedFee = 60m;
        const decimal expectedNetFiatGain = 29940m;

        // Act
        await _sut.SellAsset(userId.ToString(), "BTC", price, amount, isLimitOrder: false);

        // Assert
        asset.Quantity.Should().Be(0.5m);
        wallet.FiatBalance.Should().Be(5000m + expectedNetFiatGain);

        await _walletRepository.Received(1).UpdateAsync(wallet);

        await _transactionService.Received(1).CreateTransactionRecordAsync(
            walletId,
            "BTC",
            amount,
            price,
            TransactionType.Sell
        );

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<FeeCollectionEvent>(e =>
                e.Symbol == "USDT" &&
                e.FeeAmount == expectedFee &&
                e.UserId == userId
            ),
            Arg.Any<CancellationToken>()
        );

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<TradeExecuted>(e =>
                e.WalletId == walletId &&
                e.BoughtAmount == totalGross - expectedFee &&
                e.BoughtCurrency == "USDT" &&
                e.SoldAmount == amount &&
                e.SoldCurrency == "BTC"
            ),
            Arg.Any<CancellationToken>()
        );

        await _clientProxy.Received(1).SendCoreAsync(
            "UpdatePortfolio",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DepositMoney_WhenCalled_IncreasesFiatBalanceAndDispatchesNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var wallet = new Wallet
        {
            Id = walletId,
            UserId = userId,
            FiatBalance = 1000m,
            Value = 1000m
        };

        _walletRepository.GetWalletIdByUserId(userId).Returns(walletId);
        _walletRepository.GetByIdAsync(walletId).Returns(wallet);

        const decimal depositAmount = 2500m;

        // Act
        await _sut.DepositMoney(userId.ToString(), depositAmount);

        // Assert
        wallet.FiatBalance.Should().Be(3500m);
        wallet.Value.Should().Be(3500m);

        await _walletRepository.Received(1).UpdateAsync(wallet);

        await _transactionService.Received(1).CreateTransactionRecordAsync(
            walletId,
            depositAmount,
            TransactionType.Deposit
        );

        await _clientProxy.Received(1).SendCoreAsync(
            "UpdateBalance",
            Arg.Is<object?[]>(args => args.Length == 1 && (decimal)args[0]! == 3500m),
            Arg.Any<CancellationToken>()
        );

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<AssetDeposited>(e =>
                e.WalletId == walletId &&
                e.Amount == depositAmount &&
                e.Currency == "USDT"
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task WithdrawMoney_WhenInsufficientBalance_DoesNotWithdrawOrPublish()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var wallet = new Wallet
        {
            Id = walletId,
            UserId = userId,
            FiatBalance = 500m
        };

        _walletRepository.GetWalletIdByUserId(userId).Returns(walletId);
        _walletRepository.GetByIdAsync(walletId).Returns(wallet);

        // Act (Attempting to withdraw 1,000 from 500)
        await _sut.WithdrawMoney(userId.ToString(), 1000m);

        // Assert
        wallet.FiatBalance.Should().Be(500m);
        await _walletRepository.DidNotReceive().UpdateAsync(Arg.Any<Wallet>());
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<AssetWithdrawn>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawMoney_WhenSufficientBalance_DeductsBalanceAndPublishesEvents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var wallet = new Wallet
        {
            Id = walletId,
            UserId = userId,
            FiatBalance = 2000m,
            Value = 2000m
        };

        _walletRepository.GetWalletIdByUserId(userId).Returns(walletId);
        _walletRepository.GetByIdAsync(walletId).Returns(wallet);

        const decimal withdrawAmount = 800m;

        // Act
        await _sut.WithdrawMoney(userId.ToString(), withdrawAmount);

        // Assert
        wallet.FiatBalance.Should().Be(1200m);
        wallet.Value.Should().Be(1200m);

        await _walletRepository.Received(1).UpdateAsync(wallet);

        await _transactionService.Received(1).CreateTransactionRecordAsync(
            walletId,
            withdrawAmount,
            TransactionType.Withdraw
        );

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<AssetWithdrawn>(e =>
                e.WalletId == walletId &&
                e.Amount == withdrawAmount &&
                e.Currency == "USDT"
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task CreateWallet_WhenCalled_AddsNewWalletWithGeneratedAddress()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _sut.CreateWallet(userId);

        // Assert
        await _walletRepository.Received(1).AddAsync(Arg.Is<Wallet>(w =>
            w.UserId == userId &&
            w.Address.StartsWith("0x") &&
            w.Address.Length == 34 // "0x" + 32-char guid N
        ));
    }
}
