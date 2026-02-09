using Hamfer.Repository.Data;
using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Duow;

public interface IRepositoryEntityUnitOfWork<TEntity>: IDisposable
  where TEntity: class, IRepositoryEntity<TEntity>
{
  void registerNew(TEntity entity);
  void registerModified(TEntity entity);
  void registerNewOrModified(TEntity entity);
  void registerDeleted(Guid entityId);

  Task commit(bool withRefreshDatabaseContext = true);
  Task rollBack(bool withRefreshDatabaseContext = true);

  Task refresh();
  
  RepositoryEntityRecordState getEntityState(TEntity entity);
  RepositoryEntityRecordState getEntityState(Guid entityId);
}
