using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Repositories;

namespace Portfolio.API.Application.Services;

public class TransactionService(ITransactionRepository transactionRepository) : ITransactionService
{
    public async Task CreateTransactionRecordAsync(Guid walletId, string symbol, double amount, double? price, TransactionType type)
    {
        Transaction transaction;

        if (price is not null)
            transaction = new Transaction
            {
                WalletId = walletId,
                Symbol = symbol,
                Amount = amount,
                PriceAtTransaction = (double)price,
                TransactionType = type
            };
        else
            transaction = new Transaction
            {
                WalletId = walletId,
                Symbol = symbol,
                Amount = amount,
                TransactionType = type
            };

        await transactionRepository.AddAsync(transaction);
    }
}
