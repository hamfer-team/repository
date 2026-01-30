using Hamfer.Repository.data;

namespace Hamfer.Repository.models;

public interface IDatabaseUnitOfWork<TEntity>
  where TEntity: class, IRepositoryEntity<TEntity>
{
  void RegisterNew(TEntity entity);
  void RegisterModified(TEntity entity);
  void RegisterNewOrModified(TEntity entity);
  void RegisterDeleted(Guid entityId);
  
  void Commit(bool withRefreshDatabaseContext = true);
  void RollBack(bool withRefreshDatabaseContext = true);

  void Refresh();
  void Dispose();

  DatabaseContextRecordState GetEntityState(TEntity entity);
  DatabaseContextRecordState GetEntityState(Guid entityId);
}
