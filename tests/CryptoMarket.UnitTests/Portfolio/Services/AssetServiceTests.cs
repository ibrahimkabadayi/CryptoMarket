using FluentAssertions;
using NSubstitute;
using Portfolio.API.Application.Services;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;
using Xunit;

namespace CryptoMarket.UnitTests.Portfolio.Services;

public class AssetServiceTests
{
    private readonly IAssetRepository _assetRepository;
    private readonly AssetService _sut;

    public AssetServiceTests()
    {
        _assetRepository = Substitute.For<IAssetRepository>();
        _sut = new AssetService(_assetRepository);
    }

    [Fact]
    public async Task AddAssetToWalletThatHasThatAsset_WhenCalled_ComputesWeightedAverageCostBasisAndUpdates()
    {
        // Arrange
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Symbol = "BTC",
            Quantity = 2m,
            CostBasis = 50000m
        };

        const decimal buyingPrice = 65000m;
        const decimal newQuantity = 1m;

        // Total value = (2 * 50,000) + (1 * 65,000) = 165,000
        // Total quantity = 3
        // New CostBasis = 165,000 / 3 = 55,000
        const decimal expectedCostBasis = 55000m;
        const decimal expectedTotalQuantity = 3m;

        // Act
        var result = await _sut.AddAssetToWalletThatHasThatAsset(asset, buyingPrice, newQuantity);

        // Assert
        result.Should().BeSameAs(asset);
        result.Quantity.Should().Be(expectedTotalQuantity);
        result.CostBasis.Should().Be(expectedCostBasis);
        result.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        await _assetRepository.Received(1).UpdateAsync(asset);
    }
}
