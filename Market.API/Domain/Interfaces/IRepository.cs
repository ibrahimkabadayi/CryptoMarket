using System.Linq.Expressions;
using Market.API.Domain.Entities;

namespace Market.API.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task AddAsync(T entity);
    Task<List<T>> GetAllAsync();
    Task<T> GetByIdAsync(string id);
}
