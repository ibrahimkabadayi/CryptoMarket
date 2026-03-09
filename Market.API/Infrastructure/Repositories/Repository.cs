using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.Context;
using MongoDB.Driver;

namespace Market.API.Infrastructure.Repositories;

public class Repository<T>(MarketDbContext context, string collectionName) : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _collection = context.Database.GetCollection<T>(collectionName);

    public async Task AddAsync(T entity)
    {
        await _collection.InsertOneAsync(entity);
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task<T> GetByIdAsync(string id)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }
}