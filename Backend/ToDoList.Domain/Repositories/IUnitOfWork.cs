using System;
using System.Threading;
using System.Threading.Tasks;

namespace ToDoList.Domain.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class;

        Task<int> CompleteAsync(CancellationToken cancellationToken = default);
    }
}
