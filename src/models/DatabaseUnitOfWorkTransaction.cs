using Hamfer.Repository.data;

namespace Hamfer.Repository.models;

public class DatabaseUnitOfWorkTransaction<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public DatabaseUnitOfWorkTransaction(TEntity? entity, DatabaseContextRecordState state)
  {
    Entity = entity;
    State = state;
  }

  public TEntity? Entity { get; }
  public DatabaseContextRecordState State { get; }
}