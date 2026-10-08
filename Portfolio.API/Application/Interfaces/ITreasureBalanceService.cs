namespace Portfolio.API.Application.Interfaces;

public interface ITreasureBalanceService
{
    Task AddFee(string assetSymbol, decimal amount);
}
