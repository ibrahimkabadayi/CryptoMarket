using System.Linq.Expressions;
using FluentAssertions;
using Market.API.Application.Services;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace CryptoMarket.UnitTests.Market.Services;

public class PriceHistoryServiceTests
{
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly ICoinRepository _coinRepository;
    private readonly PriceHistoryService _sut;

    public PriceHistoryServiceTests()
    {
        _priceHistoryRepository = Substitute.For<IPriceHistoryRepository>();
        _coinRepository = Substitute.For<ICoinRepository>();

        _sut = new PriceHistoryService(_priceHistoryRepository, _coinRepository);
    }

    [Fact]
    public async Task GetPriceHistoryAsync_WhenCandlesExist_AggregatesCandlesCorrectlyPerInterval()
    {
        // Arrange
        const string symbol = "BTC";
        var startDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 1, 1, 10, 30, 0, DateTimeKind.Utc);
        const int intervalMinutes = 15;

        var existingCandles = new List<PriceHistory>
        {
            new()
            {
                Symbol = symbol,
                Timestamp = startDate.AddMinutes(2),
                OpenPrice = 60000m,
                ClosePrice = 60500m,
                HighPrice = 61000m,
                LowPrice = 59800m,
                Volume = 500m
            },
            new()
            {
                Symbol = symbol,
                Timestamp = startDate.AddMinutes(8),
                OpenPrice = 60500m,
                ClosePrice = 60800m,
                HighPrice = 61200m,
                LowPrice = 60400m,
                Volume = 300m
            }
        };

        _priceHistoryRepository.FindAsync(Arg.Any<Expression<Func<PriceHistory, bool>>>())
            .Returns(existingCandles);

        _coinRepository.GetCoinAsync(symbol)
            .Returns(new Coin { Symbol = symbol, CurrentPrice = 60800m });

        // Act
        var result = await _sut.GetPriceHistoryAsync(symbol, intervalMinutes, startDate, endDate);

        // Assert
        result.Should().NotBeEmpty();
        var firstInterval = result.First(i => i.Timestamp == startDate);
        firstInterval.OpenPrice.Should().Be(60000m);
        firstInterval.ClosePrice.Should().Be(60800m);
        firstInterval.HighPrice.Should().Be(61200m);
        firstInterval.LowPrice.Should().Be(59800m);
        firstInterval.Volume.Should().Be(800m);
    }

    [Fact]
    public async Task GetPriceHistoryAsync_WhenIntervalHasGaps_SynthesizesCandlesSmoothly()
    {
        // Arrange
        const string symbol = "ETH";
        var startDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 1, 1, 12, 30, 0, DateTimeKind.Utc);
        const int intervalMinutes = 15;

        // No candles returned for the range
        _priceHistoryRepository.FindAsync(Arg.Any<Expression<Func<PriceHistory, bool>>>())
            .Returns(new List<PriceHistory>());

        _coinRepository.GetCoinAsync(symbol)
            .Returns(new Coin { Symbol = symbol, CurrentPrice = 3200m });

        // Act
        var result = await _sut.GetPriceHistoryAsync(symbol, intervalMinutes, startDate, endDate);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(candle =>
        {
            candle.Symbol.Should().Be(symbol);
            candle.OpenPrice.Should().BeGreaterThan(0);
            candle.ClosePrice.Should().BeGreaterThan(0);
            candle.HighPrice.Should().BeGreaterThanOrEqualTo(candle.LowPrice);
            candle.Volume.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    [Fact]
    public async Task GetPriceHistoryAsync_WhenCoinNotFoundInRepository_HandlesSafelyWithoutCrashing()
    {
        // Arrange
        const string symbol = "UNKNOWN";
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 1, 1, 0, 15, 0, DateTimeKind.Utc);

        _priceHistoryRepository.FindAsync(Arg.Any<Expression<Func<PriceHistory, bool>>>())
            .Returns(new List<PriceHistory>());

        _coinRepository.GetCoinAsync(symbol)
            .Returns((Coin)null!);

        // Act
        var result = await _sut.GetPriceHistoryAsync(symbol, 15, startDate, endDate);

        // Assert
        result.Should().NotBeNull();
    }
}
