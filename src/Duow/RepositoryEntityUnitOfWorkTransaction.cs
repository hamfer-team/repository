using Hamfer.Repository.Data;
using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Duow;

public class RepositoryEntityUnitOfWorkTransaction<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public RepositoryEntityUnitOfWorkTransaction(TEntity? entity, RepositoryEntityRecordState state)
  {
    this.entity = entity;
    this.state = state;
  }

  public TEntity? entity { get; }
  public RepositoryEntityRecordState state { get; }
}