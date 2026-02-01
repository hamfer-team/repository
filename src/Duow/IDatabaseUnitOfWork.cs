using Hamfer.Repository.Data;
using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Duow;

public interface IDatabaseUnitOfWork<TEntity>
  where TEntity: class, IRepositoryEntity<TEntity>
{
  void registerNew(TEntity entity);
  void registerModified(TEntity entity);
  void registerNewOrModified(TEntity entity);
  void registerDeleted(Guid entityId);
  
  void commit(bool withRefreshDatabaseContext = true);
  void rollBack(bool withRefreshDatabaseContext = true);

  void refresh();
  void Dispose();

  DatabaseContextRecordState getEntityState(TEntity entity);
  DatabaseContextRecordState getEntityState(Guid entityId);
}
