using System;
using System.Threading;
using System.Threading.Tasks;

namespace TechStore.Core.Interfaces;

/// <summary>
/// Unit of Work pattern interface điều phối giao dịch và cập nhật nhiều repository
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
