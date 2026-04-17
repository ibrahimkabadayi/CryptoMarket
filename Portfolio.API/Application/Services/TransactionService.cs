using System.ComponentModel;
using AutoMapper;
using MassTransit.Initializers;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Repositories;

namespace Portfolio.API.Application.Services;

public class TransactionService(ITransactionRepository transactionRepository, IMapper mapper) : ITransactionService
{
    public async Task CreateTransactionRecordAsync(Guid walletId, string symbol, decimal amount, decimal? price, TransactionType type)
    {
        Transaction transaction;

        if (price is not null)
            transaction = new Transaction
            {
                WalletId = walletId,
                Symbol = symbol,
                Amount = amount,
                PriceAtTransaction = (decimal)price,
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

    public List<TransactionDto> GetTenLastTransaction(Guid walletId)
    {
        return mapper.Map<List<TransactionDto>>(transactionRepository.GetFirstTenTransactions(walletId));
    }
}
