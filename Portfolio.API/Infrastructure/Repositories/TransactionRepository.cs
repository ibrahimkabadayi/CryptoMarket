using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Context;

namespace Portfolio.API.Infrastructure.Repositories;

public class TransactionRepository(ApplicationDbContext context) : Repository<Transaction>(context), ITransactionRepository
{
    public List<Transaction> GetFirstTenTransactions(Guid walltId)
    {
        return [.. context.Transactions.OrderByDescending(x => x.CreatedDate).Take(10)];
    }
}
