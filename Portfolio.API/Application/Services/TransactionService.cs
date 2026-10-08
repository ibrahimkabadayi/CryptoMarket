using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Hubs;

namespace Portfolio.API.Application.Services;

public class TransactionService(
    ITransactionRepository transactionRepository,
    IMapper mapper,
    IHubContext<PortfolioHub> hubContext,
    IWalletRepository walletRepository
    ) : ITransactionService
{
    public async Task CreateTransactionRecordAsync(Guid walletId, string symbol, decimal amount, decimal? price, TransactionType type)
    {
        
        var transaction = new Transaction 
        {
            WalletId = walletId,
            Symbol = symbol,
            Amount = amount, 
            TransactionType = type,      
        };

        if (price is decimal x)
            transaction.PriceAtTransaction = x;

        await transactionRepository.AddAsync(transaction);

        var transactionDto = mapper.Map<TransactionDto>(transaction);

        var wallet = await walletRepository.GetByIdAsync(walletId);
        if (wallet is not null && wallet.UserId != Guid.Empty)
        {
            await hubContext.Clients.User(wallet.UserId.ToString())
                .SendAsync("NewTransaction", transactionDto);
        }

        Console.WriteLine(hubContext.ToString());
    }

    public async Task CreateTransactionRecordAsync(Guid walletId, decimal amount, TransactionType type)
    {
        if (!(type == TransactionType.Deposit || type == TransactionType.Withdraw)) 
        {
            throw new ArgumentException("Transaction type must be either deposit or withdraw");
        }

        var transaction = new Transaction
        {
            WalletId = walletId,
            Amount = amount,
            TransactionType = type
        };

        await transactionRepository.AddAsync(transaction);

        var transactionDto = mapper.Map<TransactionDto>(transaction);

        var wallet = await walletRepository.GetByIdAsync(walletId);
        if (wallet is not null && wallet.UserId != Guid.Empty)
        {
            await hubContext.Clients.User(wallet.UserId.ToString())
                .SendAsync("NewTransaction", transactionDto);
        }

        Console.WriteLine(hubContext.Groups.ToString());
    }

    public List<TransactionDto> GetTenLastTransaction(Guid walletId)
    {
        return mapper.Map<List<TransactionDto>>(transactionRepository.GetFirstTenTransactions(walletId));
    }
}
