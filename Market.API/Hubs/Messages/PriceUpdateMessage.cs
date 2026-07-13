namespace Market.API.Hubs.Messages;

public record PriceUpdateMessage(string Symbol, decimal Price, decimal MarketCap, decimal percentChange);