using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Models;

public class RepositoryPaginationConfiguration<TEntity> : IRepositoryPaginationConfiguration<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public int pageSize { get; set; }

  public int pageNo { get; set; }

  public Func<TEntity, bool>? where { get; set; }

  public ISortConfigurationItem[]? sort { get; set; }
}