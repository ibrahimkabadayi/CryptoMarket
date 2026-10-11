using AutoMapper;
using CryptoMarket.UnitTests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Services;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Hubs;
using Xunit;

namespace CryptoMarket.UnitTests.Portfolio.Services;

public class TransactionServiceTests
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMapper _mapper;
    private readonly IHubContext<PortfolioHub> _hubContext;
    private readonly IClientProxy _clientProxy;
    private readonly IWalletRepository _walletRepository;
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _mapper = Substitute.For<IMapper>();
        _walletRepository = Substitute.For<IWalletRepository>();

        var mockHub = MockHubContext.Create<PortfolioHub>();
        _hubContext = mockHub.HubContext;
        _clientProxy = mockHub.ClientProxy;

        _sut = new TransactionService(
            _transactionRepository,
            _mapper,
            _hubContext,
            _walletRepository
        );
    }

    [Fact]
    public async Task CreateTransactionRecordAsync_WhenTradeTransaction_SavesRecordAndEmitsSignalR()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string symbol = "BTC";
        const decimal amount = 0.5m;
        const decimal price = 65000m;
        const TransactionType type = TransactionType.Buy;

        var wallet = new Wallet { Id = walletId, UserId = userId };
        var transactionDto = new TransactionDto { Symbol = symbol, Amount = amount, TransactionType = type };

        _walletRepository.GetByIdAsync(walletId).Returns(wallet);
        _mapper.Map<TransactionDto>(Arg.Any<Transaction>()).Returns(transactionDto);

        // Act
        await _sut.CreateTransactionRecordAsync(walletId, symbol, amount, price, type);

        // Assert
        await _transactionRepository.Received(1).AddAsync(Arg.Is<Transaction>(t =>
            t.WalletId == walletId &&
            t.Symbol == symbol &&
            t.Amount == amount &&
            t.PriceAtTransaction == price &&
            t.TransactionType == type
        ));

        await _clientProxy.Received(1).SendCoreAsync(
            "NewTransaction",
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is TransactionDto),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task CreateTransactionRecordAsync_WhenDepositTransaction_SavesRecordAndEmitsSignalR()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const decimal amount = 500m;
        const TransactionType type = TransactionType.Deposit;

        var wallet = new Wallet { Id = walletId, UserId = userId };
        var transactionDto = new TransactionDto { Amount = amount, TransactionType = type };

        _walletRepository.GetByIdAsync(walletId).Returns(wallet);
        _mapper.Map<TransactionDto>(Arg.Any<Transaction>()).Returns(transactionDto);

        // Act
        await _sut.CreateTransactionRecordAsync(walletId, amount, type);

        // Assert
        await _transactionRepository.Received(1).AddAsync(Arg.Is<Transaction>(t =>
            t.WalletId == walletId &&
            t.Amount == amount &&
            t.TransactionType == type
        ));

        await _clientProxy.Received(1).SendCoreAsync(
            "NewTransaction",
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is TransactionDto),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task CreateTransactionRecordAsync_WhenFiatWithInvalidType_ThrowsArgumentException()
    {
        // Arrange
        var walletId = Guid.NewGuid();

        // Act
        var act = async () => await _sut.CreateTransactionRecordAsync(walletId, 100m, TransactionType.Buy);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Transaction type must be either deposit or withdraw");
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public void GetTenLastTransaction_WhenCalled_ReturnsMappedTransactions()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            new() { WalletId = walletId, Amount = 100m, TransactionType = TransactionType.Deposit }
        };
        var expectedDtos = new List<TransactionDto>
        {
            new() { Amount = 100m, TransactionType = TransactionType.Deposit }
        };

        _transactionRepository.GetFirstTenTransactions(walletId).Returns(transactions);
        _mapper.Map<List<TransactionDto>>(transactions).Returns(expectedDtos);

        // Act
        var result = _sut.GetTenLastTransaction(walletId);

        // Assert
        result.Should().BeEquivalentTo(expectedDtos);
        _transactionRepository.Received(1).GetFirstTenTransactions(walletId);
    }
}
