using Hamfer.Repository.Data;
using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Duow;

public class DatabaseUnitOfWorkTransaction<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public DatabaseUnitOfWorkTransaction(TEntity? entity, DatabaseContextRecordState state)
  {
    this.entity = entity;
    this.state = state;
  }

  public TEntity? entity { get; }
  public DatabaseContextRecordState state { get; }
}