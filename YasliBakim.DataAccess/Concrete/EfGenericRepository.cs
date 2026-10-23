using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using YasliBakim.DataAccess.Abstract;
using YasliBakim.DataAccess.Context;

namespace YasliBakim.DataAccess.Concrete;

public class EfGenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly YasliBakimDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public EfGenericRepository(YasliBakimDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filtre = null) =>
        filtre is null ? _dbSet.ToListAsync() : _dbSet.Where(filtre).ToListAsync();

    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

    public async Task<T?> GetByKeyAsync(params object[] anahtarlar) => await _dbSet.FindAsync(anahtarlar);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> filtre) => _dbSet.AnyAsync(filtre);

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public void Update(T entity) => _dbSet.Update(entity);

    public void Delete(T entity) => _dbSet.Remove(entity);

    public Task<int> SaveAsync() => _context.SaveChangesAsync();
}
