using System.Linq.Expressions;
using Market.API.Domain.Entities;
using MongoDB.Driver;

namespace Market.API.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task AddAsync(T entity);
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(string id);
    Task UpdateAsync(string id, T entity);      
    Task DeleteAsync(string id);                  
    Task<bool> ExistsAsync(string id);            
    Task<List<T>> FindAsync(FilterDefinition<T> filter);
    Task<List<T>> GetPagedAsync(int page, int pageSize);
    Task<long> CountAsync();                     
}