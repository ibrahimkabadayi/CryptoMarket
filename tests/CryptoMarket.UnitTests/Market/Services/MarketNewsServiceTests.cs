using AutoMapper;
using FluentAssertions;
using Market.API.Application.DTOs;
using Market.API.Application.Services;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CryptoMarket.UnitTests.Market.Services;

public class MarketNewsServiceTests
{
    private readonly IMarketNewsRepository _newsRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<MarketNewsService> _logger;
    private readonly MarketNewsService _sut;

    public MarketNewsServiceTests()
    {
        _newsRepository = Substitute.For<IMarketNewsRepository>();
        _mapper = Substitute.For<IMapper>();
        _logger = Substitute.For<ILogger<MarketNewsService>>();

        _sut = new MarketNewsService(_newsRepository, _mapper, _logger);
    }

    [Fact]
    public async Task CreateMarketNewsAsync_WhenCalled_CreatesEntitySavesAndReturnsMappedDto()
    {
        // Arrange
        const string title = "Bitcoin Reaches New Heights";
        const string content = "BTC price broke resistance today.";
        var symbols = new List<string> { "BTC" };

        var expectedDto = new MarketNewsDto
        {
            Title = title,
            Content = content,
            Source = "Gemini AI",
            RelatedSymbols = symbols
        };

        _mapper.Map<MarketNewsDto>(Arg.Any<MarketNews>()).Returns(expectedDto);

        // Act
        var result = await _sut.CreateMarketNewsAsync(title, content, symbols);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);

        await _newsRepository.Received(1).AddAsync(Arg.Is<MarketNews>(n =>
            n.Title == title &&
            n.Content == content &&
            n.Source == "Gemini AI" &&
            n.RelatedSymbols.SequenceEqual(symbols)
        ));
    }

    [Fact]
    public async Task GetNewsByIdAsync_WhenNewsExists_ReturnsMappedDto()
    {
        // Arrange
        var newsId = Guid.NewGuid();
        var news = new MarketNews
        {
            Id = newsId,
            Title = "Ethereum Upgrade",
            Content = "Network upgraded successfully."
        };
        var expectedDto = new MarketNewsDto
        {
            Title = news.Title,
            Content = news.Content
        };

        _newsRepository.GetByIdAsync(newsId).Returns(news);
        _mapper.Map<MarketNewsDto>(news).Returns(expectedDto);

        // Act
        var result = await _sut.GetNewsByIdAsync(newsId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task GetNewsByIdAsync_WhenNewsNotFound_ReturnsNull()
    {
        // Arrange
        var newsId = Guid.NewGuid();
        _newsRepository.GetByIdAsync(newsId).Returns((MarketNews?)null);

        // Act
        var result = await _sut.GetNewsByIdAsync(newsId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentNewsAsync_WhenCalled_ReturnsRecentNewsOrderedByPublishedAt()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var newsList = new List<MarketNews>
        {
            new() { Title = "Old News", PublishedAt = now.AddHours(-5) },
            new() { Title = "Newest News", PublishedAt = now },
            new() { Title = "Intermediate News", PublishedAt = now.AddHours(-2) }
        };

        _newsRepository.GetAllAsync().Returns(newsList);
        _mapper.Map<List<MarketNewsDto>>(Arg.Any<List<MarketNews>>())
            .Returns(callInfo =>
            {
                var source = callInfo.Arg<List<MarketNews>>();
                return source.Select(n => new MarketNewsDto { Title = n.Title, PublishedAt = n.PublishedAt }).ToList();
            });

        // Act
        var result = await _sut.GetRecentNewsAsync(count: 2);

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Newest News");
        result[1].Title.Should().Be("Intermediate News");
    }

    [Fact]
    public async Task GetNewsByCoinSymbolAsync_WhenCalled_FiltersNewsCaseInsensitively()
    {
        // Arrange
        var newsList = new List<MarketNews>
        {
            new() { Title = "BTC News", RelatedSymbols = ["BTC", "ETH"] },
            new() { Title = "SOL News", RelatedSymbols = ["SOL"] },
            new() { Title = "Another BTC News", RelatedSymbols = ["btc"] }
        };

        _newsRepository.GetAllAsync().Returns(newsList);
        _mapper.Map<List<MarketNewsDto>>(Arg.Any<List<MarketNews>>())
            .Returns(callInfo =>
            {
                var source = callInfo.Arg<List<MarketNews>>();
                return source.Select(n => new MarketNewsDto { Title = n.Title }).ToList();
            });

        // Act
        var result = await _sut.GetNewsByCoinSymbolAsync("BTC");

        // Assert
        result.Should().HaveCount(2);
        result.Select(n => n.Title).Should().Contain("BTC News");
        result.Select(n => n.Title).Should().Contain("Another BTC News");
    }

    [Fact]
    public async Task DeleteNewsAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var newsId = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteNewsAsync(newsId);

        // Assert
        result.Should().BeTrue();
        await _newsRepository.Received(1).DeleteAsync(newsId);
    }

    [Fact]
    public async Task DeleteNewsAsync_WhenRepositoryThrows_ReturnsFalse()
    {
        // Arrange
        var newsId = Guid.NewGuid();
        _newsRepository.DeleteAsync(newsId).ThrowsAsync(new Exception("Database deletion error"));

        // Act
        var result = await _sut.DeleteNewsAsync(newsId);

        // Assert
        result.Should().BeFalse();
    }
}
