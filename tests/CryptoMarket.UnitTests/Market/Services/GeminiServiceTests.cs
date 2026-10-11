using System.Net;
using System.Text;
using FluentAssertions;
using Market.API.Application.Services;
using Market.API.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace CryptoMarket.UnitTests.Market.Services;

public class GeminiServiceTests
{
    private readonly ILogger<GeminiService> _logger;

    public GeminiServiceTests()
    {
        _logger = Substitute.For<ILogger<GeminiService>>();
    }

    [Fact]
    public async Task GenerateMarketNewsAsync_WhenApiKeyIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = Options.Create(new GeminiSettings { ApiKey = "" });
        var httpClient = new HttpClient();
        var sut = new GeminiService(settings, _logger, httpClient);

        // Act
        var act = async () => await sut.GenerateMarketNewsAsync(["BTC"]);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Gemini API Key is not configured.");
    }

    [Fact]
    public async Task GenerateMarketNewsAsync_WhenCoinSymbolsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var settings = Options.Create(new GeminiSettings { ApiKey = "fake-api-key" });
        var httpClient = new HttpClient();
        var sut = new GeminiService(settings, _logger, httpClient);

        // Act
        var act = async () => await sut.GenerateMarketNewsAsync([]);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("At least one coin symbol must be provided.");
    }

    [Fact]
    public async Task GenerateMarketNewsAsync_WhenApiReturnsSuccess_ReturnsParsedText()
    {
        // Arrange
        var settings = Options.Create(new GeminiSettings
        {
            ApiKey = "test-api-key",
            Model = "gemini-2.5-flash",
            MaxTokens = 500
        });

        const string expectedNews = "## Bitcoin Surges Above 65K\n\nBitcoin broke past major resistance levels today.";
        var responsePayload = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = expectedNews }
                        }
                    }
                }
            }
        };
        var fakeJsonResponse = System.Text.Json.JsonSerializer.Serialize(responsePayload);

        var mockHandler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(fakeJsonResponse, Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(mockHandler);
        var sut = new GeminiService(settings, _logger, httpClient);

        // Act
        var result = await sut.GenerateMarketNewsAsync(["BTC", "ETH"]);

        // Assert
        result.Should().Be(expectedNews);
        mockHandler.LastRequest.Should().NotBeNull();
        mockHandler.LastRequest!.RequestUri!.ToString().Should().Contain("key=test-api-key");
    }

    [Fact]
    public async Task GenerateMarketNewsAsync_WhenApiReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        var settings = Options.Create(new GeminiSettings { ApiKey = "test-api-key" });

        var mockHandler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Quota exceeded", Encoding.UTF8, "text/plain")
        });

        var httpClient = new HttpClient(mockHandler);
        var sut = new GeminiService(settings, _logger, httpClient);

        // Act
        var act = async () => await sut.GenerateMarketNewsAsync(["BTC"]);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private class TestHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(response);
        }
    }
}
