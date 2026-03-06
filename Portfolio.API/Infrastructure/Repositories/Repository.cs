using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Context;

namespace Portfolio.API.Infrastructure.Repositories;

public class Repository<T>(ApplicationDbContext context) : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _context = context;
    private readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);
        return entity;
    }

    public async Task<List<T?>> GetAllAsync()
    {
        List<T?> list = (await _dbSet.ToListAsync())!;
        list.RemoveAll(x => x == null);
        return list;
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return;
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        var list = await _dbSet.Where(predicate).ToListAsync();
        return list.Count > 0;
    }
}
