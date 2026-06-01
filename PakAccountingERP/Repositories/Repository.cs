using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Data;
using PakAccountingERP.Interfaces;
using PakAccountingERP.Models.Common;

namespace PakAccountingERP.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id) =>
        await DbSet.FirstOrDefaultAsync(e => e.Id == id);

    public virtual async Task<IEnumerable<T>> GetAllAsync() =>
        await DbSet.OrderByDescending(e => e.Id).ToListAsync();

    public virtual async Task<IEnumerable<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate) =>
        await DbSet.Where(predicate).ToListAsync();

    public virtual async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public virtual Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            await UpdateAsync(entity);
        }
    }

    public virtual async Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate) =>
        await DbSet.AnyAsync(predicate);

    public virtual async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null) =>
        predicate == null ? await DbSet.CountAsync() : await DbSet.CountAsync(predicate);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositories.ContainsKey(type))
        {
            if (!typeof(BaseEntity).IsAssignableFrom(type))
                throw new InvalidOperationException($"Entity {type.Name} must inherit BaseEntity.");
            var repoType = typeof(Repository<>).MakeGenericType(type);
            _repositories[type] = Activator.CreateInstance(repoType, _context)!;
        }
        return (IRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
