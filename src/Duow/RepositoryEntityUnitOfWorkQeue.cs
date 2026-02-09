using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Duow;

public class RepositoryEntityUnitOfWorkQeue<TEntity> : Queue<RepositoryEntityUnitOfWorkTransaction<TEntity>>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public RepositoryEntityUnitOfWorkQeue()
  {
  }
}