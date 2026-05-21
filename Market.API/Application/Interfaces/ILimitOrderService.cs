namespace Market.API.Application.Interfaces;

public interface ILimitOrderService
{
    public Task SetLimitOrder(string symbol, Guid userId, decimal targetPrice, decimal amount, string orderType);
}
