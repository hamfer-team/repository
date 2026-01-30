namespace Hamfer.Repository.models;

public class RepositoryPaginationConfiguration<TEntity> : IRepositoryPaginationConfiguration<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public int PageSize { get; set; }

  public int PageNo { get; set; }

  public Func<TEntity, bool>? WhereClause { get; set; }

  public ISortConfigurationItem[]? Sort { get; set; }
}