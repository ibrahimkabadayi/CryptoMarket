using Market.API.Application.DTOs;

namespace Market.API.Application.Interfaces;

public interface IPriceHistoryService
{
    Task<List<PriceHistoryDto>> GetPriceHistoryAsync(string symbol, int intervalMinutes, DateTime startDate, DateTime endDate);
}
