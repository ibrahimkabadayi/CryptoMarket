using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.Context;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Market.API.Infrastructure.Repositories;

public class Repository<T>(MarketDbContext context, string collectionName) : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _collection = context.Database.GetCollection<T>(collectionName);

    public async Task AddAsync(T entity)
        => await _collection.InsertOneAsync(entity);

    public async Task<List<T>> GetAllAsync()
        => await _collection.Find(_ => true).ToListAsync();

    public async Task<T> GetByIdAsync(string id)
        => await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task UpdateAsync(string id, T entity)
        => await _collection.ReplaceOneAsync(x => x.Id == id, entity);

    public async Task DeleteAsync(string id)
        => await _collection.DeleteOneAsync(x => x.Id == id);

    public async Task<bool> ExistsAsync(string id)
        => await _collection.Find(x => x.Id == id).AnyAsync();

    public async Task<List<T>> FindAsync(FilterDefinition<T> filter)
        => await _collection.Find(filter).ToListAsync();

    public async Task<List<T>> GetPagedAsync(int page, int pageSize)
        => await _collection.Find(_ => true)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

    public async Task<long> CountAsync()
        => await _collection.CountDocumentsAsync(_ => true);
}