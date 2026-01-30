namespace Hamfer.Repository.models;

public class DatabaseUnitOfWorkQeue<TEntity> : Queue<DatabaseUnitOfWorkTransaction<TEntity>>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public DatabaseUnitOfWorkQeue()
  {
  }
}