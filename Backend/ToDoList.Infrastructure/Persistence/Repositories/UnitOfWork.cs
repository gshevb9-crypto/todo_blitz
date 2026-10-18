using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ToDoList.Domain.Repositories;
using ToDoList.Infrastructure.Persistence.Context;

namespace ToDoList.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly ConcurrentDictionary<Type, object> _repositories = new();

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class
        {
            return (IRepository<TEntity, TKey>)_repositories.GetOrAdd(
                typeof(TEntity),
                _ => new Repository<TEntity, TKey>(_context));
        }

        public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
