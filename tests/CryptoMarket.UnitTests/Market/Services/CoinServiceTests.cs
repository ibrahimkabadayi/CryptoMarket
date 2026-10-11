using AutoMapper;
using FluentAssertions;
using Market.API.Application.DTOs;
using Market.API.Application.Interfaces;
using Market.API.Application.Services;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using MassTransit;
using NSubstitute;
using Shared.Messages;
using Xunit;

namespace CryptoMarket.UnitTests.Market.Services;

public class CoinServiceTests
{
    private readonly ICoinRepository _coinRepository;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IRedisCacheService _cacheService;
    private readonly CoinService _sut;

    public CoinServiceTests()
    {
        _coinRepository = Substitute.For<ICoinRepository>();
        _mapper = Substitute.For<IMapper>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _cacheService = Substitute.For<IRedisCacheService>();

        _sut = new CoinService(
            _coinRepository,
            _mapper,
            _publishEndpoint,
            _cacheService
        );
    }

    [Fact]
    public async Task BuyCoin_WhenCalled_PublishesBuyCoinEventWithCorrectData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var buyCoinDto = new BuyCoinDto
        {
            Symbol = "BTC",
            BuyPrice = 65000m,
            UserId = userId,
            BuyAmount = 0.5m
        };

        // Act
        await _sut.BuyCoin(buyCoinDto);

        // Assert
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<BuyCoinEvent>(e =>
                e.Symbol == "BTC" &&
                e.BuyPrice == 65000m &&
                e.UserId == userId &&
                e.BuyAmount == 0.5m
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetAllCoins_WhenCacheHit_ReturnsMappedCoinsFromCacheWithoutCallingRepository()
    {
        // Arrange
        var cachedCoins = new List<Coin>
        {
            new() { Id = Guid.NewGuid(), Symbol = "BTC", Name = "Bitcoin", CurrentPrice = 65000m }
        };
        var expectedDtos = new List<CoinDto>
        {
            new() { Symbol = "BTC", Name = "Bitcoin", CurrentPrice = 65000m }
        };

        _cacheService.GetAsync<List<Coin>>("market:coins").Returns(cachedCoins);
        _mapper.Map<List<CoinDto>>(cachedCoins).Returns(expectedDtos);

        // Act
        var result = await _sut.GetAllCoins();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDtos);
        await _cacheService.Received(1).GetAsync<List<Coin>>("market:coins");
        await _coinRepository.DidNotReceive().GetAllAsync();
    }

    [Fact]
    public async Task GetAllCoins_WhenCacheMiss_FetchesFromRepositorySetsCacheAndReturnsMappedCoins()
    {
        // Arrange
        var dbCoins = new List<Coin>
        {
            new() { Id = Guid.NewGuid(), Symbol = "ETH", Name = "Ethereum", CurrentPrice = 3500m }
        };
        var expectedDtos = new List<CoinDto>
        {
            new() { Symbol = "ETH", Name = "Ethereum", CurrentPrice = 3500m }
        };

        _cacheService.GetAsync<List<Coin>>("market:coins").Returns((List<Coin>?)null);
        _coinRepository.GetAllAsync().Returns(dbCoins);
        _mapper.Map<List<CoinDto>>(dbCoins).Returns(expectedDtos);

        // Act
        var result = await _sut.GetAllCoins();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDtos);
        await _coinRepository.Received(1).GetAllAsync();
        await _cacheService.Received(1).SetAsync("market:coins", dbCoins, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetCoinBySymbol_WhenCalled_ReturnsMappedCoinDto()
    {
        // Arrange
        const string symbol = "BTC";
        var coin = new Coin { Id = Guid.NewGuid(), Symbol = symbol, Name = "Bitcoin", CurrentPrice = 65000m };
        var expectedDto = new CoinDto { Symbol = symbol, Name = "Bitcoin", CurrentPrice = 65000m };

        _coinRepository.GetCoinAsync(symbol).Returns(coin);
        _mapper.Map<CoinDto>(coin).Returns(expectedDto);

        // Act
        var result = await _sut.GetCoinBySymbol(symbol);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        await _coinRepository.Received(1).GetCoinAsync(symbol);
    }
}
