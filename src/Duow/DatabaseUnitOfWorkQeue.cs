using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Duow;

public class DatabaseUnitOfWorkQeue<TEntity> : Queue<DatabaseUnitOfWorkTransaction<TEntity>>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public DatabaseUnitOfWorkQeue()
  {
  }
}