namespace Market.API.Application.DTOs;

public record PriceHistoryDto(
    string Symbol,
    decimal OpenPrice,
    decimal ClosePrice,
    decimal HighPrice,
    decimal LowPrice,
    decimal Volume,
    DateTime Timestamp
);
