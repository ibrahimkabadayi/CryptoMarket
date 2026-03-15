using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Application.Interfaces;

public interface ITransactionService
{
    Task CreateTransactionRecordAsync(Guid walletId, string symbol, decimal amount, decimal? price, TransactionType type);
}
