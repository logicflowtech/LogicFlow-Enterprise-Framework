using LogicFlowEnterpriseFramework.Domain.Entities;
using LogicFlowEnterpriseFramework.Domain.Interfaces;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;

namespace LogicFlowEnterpriseFramework.Infrastructure.Repositories;

public sealed class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = [];

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        var type = typeof(TEntity);
        if (!_repositories.TryGetValue(type, out var repository))
        {
            repository = new Repository<TEntity>(dbContext);
            _repositories[type] = repository;
        }

        return (IRepository<TEntity>)repository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
