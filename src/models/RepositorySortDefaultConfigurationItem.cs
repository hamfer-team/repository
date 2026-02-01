using Hamfer.Repository.Data;
using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Models;

public class RepositorySortDefaultConfigurationItem : ISortConfigurationItem
{
  public RepositorySortDefaultConfigurationItem()
  {
      propertyName = nameof(IRepositoryEntity<>.id);
      sortOrder = SortOrderBy.Ascending;
  }

  public string propertyName { get; }

  public SortOrderBy sortOrder { get; }
}