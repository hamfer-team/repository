using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Models;

public interface IRepositoryPaginationConfiguration<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  /// <summary>
  /// The size or count of rows in a page
  /// </summary>
  int pageSize { get; set; }

  /// <summary>
  /// The number of current or expected page
  /// </summary>
  int pageNo { get; set; }

  /// <summary>
  /// The clause to filter results
  /// </summary>
  Func<TEntity, bool>? where { get; set; }

  /// <summary>
  /// The array of sort configs from top priority to bottom
  /// </summary>
  ISortConfigurationItem[]? sort { get; set; }
}
