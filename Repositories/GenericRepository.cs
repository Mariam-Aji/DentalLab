using DentalLab.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly ApplicationDbContext _db;
    protected readonly DbSet<T> _set;
    public GenericRepository(ApplicationDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async Task AddAsync(T entity) => await _set.AddAsync(entity);
    public async Task<IEnumerable<T>> GetAllAsync() => await _set.ToListAsync();
    public async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);
    public void Remove(T entity) => _set.Remove(entity);
    public void Update(T entity) => _set.Update(entity);
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}