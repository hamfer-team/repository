namespace Hamfer.Repository.models;

public class RepositoryPaginationDefaultConfiguration<TEntity> : IRepositoryPaginationConfiguration<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public const int PageSizeDefault = 10;
  public const int PageNoDefault = 1;

  public RepositoryPaginationDefaultConfiguration()
  {
    bool defaultWhereClause(TEntity entity)
    {
      return true;
    }

    WhereClause = defaultWhereClause;
    Sort = [new RepositorySortDefaultConfigurationItem()];
    PageSize = PageSizeDefault;
    PageNo = PageNoDefault;
  }

  public int PageSize { get; set; }

  public int PageNo { get; set; }

  public Func<TEntity, bool>? WhereClause { get; set; }

  public ISortConfigurationItem[]? Sort { get; set; }
}