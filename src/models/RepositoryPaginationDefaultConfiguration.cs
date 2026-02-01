using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Models;

public class RepositoryPaginationDefaultConfiguration<TEntity> : IRepositoryPaginationConfiguration<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public const int PAGE_SIZE_DEFAULT = 10;
  public const int PAGE_NO_DEFAULT = 1;

  public RepositoryPaginationDefaultConfiguration()
  {
    bool defaultWhereClause(TEntity entity)
    {
      return true;
    }

    where = defaultWhereClause;
    sort = [new RepositorySortDefaultConfigurationItem()];
    pageSize = PAGE_SIZE_DEFAULT;
    pageNo = PAGE_NO_DEFAULT;
  }

  public int pageSize { get; set; }

  public int pageNo { get; set; }

  public Func<TEntity, bool>? where { get; set; }

  public ISortConfigurationItem[]? sort { get; set; }
}