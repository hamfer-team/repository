using Hamfer.Repository.data;

namespace Hamfer.Repository.models;

public class RepositorySortDefaultConfigurationItem : ISortConfigurationItem
{
  public RepositorySortDefaultConfigurationItem()
  {
      PropertyName = nameof(IRepositoryEntity<>.id);
      SortOrder = SortOrderBy.Ascending;
  }

  public string PropertyName { get; }

  public SortOrderBy SortOrder { get; }
}