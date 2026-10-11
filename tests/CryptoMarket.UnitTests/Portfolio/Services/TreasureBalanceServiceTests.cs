using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using Portfolio.API.Application.Services;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;
using Xunit;

namespace CryptoMarket.UnitTests.Portfolio.Services;

public class TreasureBalanceServiceTests
{
    private readonly ITreasuryBalanceRepository _treasuryBalanceRepository;
    private readonly TreasureBalanceService _sut;

    public TreasureBalanceServiceTests()
    {
        _treasuryBalanceRepository = Substitute.For<ITreasuryBalanceRepository>();
        _sut = new TreasureBalanceService(_treasuryBalanceRepository);
    }

    [Fact]
    public async Task AddFee_WhenAssetIsUsdtAndBalanceExists_IncrementsTotalAmount()
    {
        // Arrange
        var existingBalance = new TreasuryBalance
        {
            AssetSymbol = "USDT",
            TotalAmount = 100m
        };

        _treasuryBalanceRepository.FindFirstAsync(Arg.Any<Expression<Func<TreasuryBalance, bool>>>())
            .Returns(existingBalance);

        // Act
        await _sut.AddFee("USDT", 25m);

        // Assert
        existingBalance.TotalAmount.Should().Be(125m);
        await _treasuryBalanceRepository.Received(1).UpdateAsync(existingBalance);
    }

    [Fact]
    public async Task AddFee_WhenAssetIsUsdtAndBalanceDoesNotExist_CreatesAndIncrementsBalance()
    {
        // Arrange
        _treasuryBalanceRepository.FindFirstAsync(Arg.Any<Expression<Func<TreasuryBalance, bool>>>())
            .Returns((TreasuryBalance?)null);

        // Act
        await _sut.AddFee("USDT", 50m);

        // Assert
        await _treasuryBalanceRepository.Received(1).AddAsync(Arg.Is<TreasuryBalance>(b =>
            b.AssetSymbol == "USDT"
        ));
        await _treasuryBalanceRepository.Received(1).UpdateAsync(Arg.Is<TreasuryBalance>(b =>
            b.AssetSymbol == "USDT" && b.TotalAmount == 50m
        ));
    }

    [Fact]
    public async Task AddFee_WhenAssetIsCryptoAndBalanceExists_IncrementsTotalAmount()
    {
        // Arrange
        var existingBtc = new TreasuryBalance
        {
            AssetSymbol = "BTC",
            TotalAmount = 0.5m
        };

        _treasuryBalanceRepository.FindFirstAsync(Arg.Any<Expression<Func<TreasuryBalance, bool>>>())
            .Returns(existingBtc);

        // Act
        await _sut.AddFee("BTC", 0.1m);

        // Assert
        existingBtc.TotalAmount.Should().Be(0.6m);
        await _treasuryBalanceRepository.Received(1).UpdateAsync(existingBtc);
    }

    [Fact]
    public async Task AddFee_WhenAssetIsCryptoAndBalanceDoesNotExist_AddsNewBalance()
    {
        // Arrange
        _treasuryBalanceRepository.FindFirstAsync(Arg.Any<Expression<Func<TreasuryBalance, bool>>>())
            .Returns((TreasuryBalance?)null);

        // Act
        await _sut.AddFee("ETH", 1.5m);

        // Assert
        await _treasuryBalanceRepository.Received(1).AddAsync(Arg.Is<TreasuryBalance>(b =>
            b.AssetSymbol == "ETH" && b.TotalAmount == 1.5m
        ));
    }
}
