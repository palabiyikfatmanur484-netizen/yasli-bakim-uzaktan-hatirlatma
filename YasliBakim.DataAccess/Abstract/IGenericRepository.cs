using System.Linq.Expressions;

namespace YasliBakim.DataAccess.Abstract;

public interface IGenericRepository<T> where T : class
{
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filtre = null);
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveAsync();
}
