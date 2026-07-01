using LogicFlowEnterpriseFramework.Domain.Entities;

namespace LogicFlowEnterpriseFramework.Domain.Interfaces;

public interface IUnitOfWork
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
