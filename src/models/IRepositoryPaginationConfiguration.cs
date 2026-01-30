namespace Hamfer.Repository.models;

public interface IRepositoryPaginationConfiguration<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  /// <summary>
  /// The size or count of rows in a page
  /// </summary>
  int PageSize { get; set; }

  /// <summary>
  /// The number of current or expected page
  /// </summary>
  int PageNo { get; set; }

  /// <summary>
  /// The clause to filter results
  /// </summary>
  Func<TEntity, bool>? WhereClause { get; set; }

  /// <summary>
  /// The array of sort configs from top priority to bottom
  /// </summary>
  ISortConfigurationItem[]? Sort { get; set; }
}
