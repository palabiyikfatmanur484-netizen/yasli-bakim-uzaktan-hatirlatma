using System.Linq.Expressions;

namespace YasliBakim.DataAccess.Abstract;

public interface IGenericRepository<T> where T : class
{
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filtre = null);
    Task<T?> GetByIdAsync(int id);
    // Bileşik anahtarlı tablolar için (ör. YasliBakici: YasliId, BakiciId).
    Task<T?> GetByKeyAsync(params object[] anahtarlar);
    Task<bool> AnyAsync(Expression<Func<T, bool>> filtre);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveAsync();
}
