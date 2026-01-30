using Hamfer.Repository.data;

namespace Hamfer.Repository.models;

public interface ISortConfigurationItem
{
  string PropertyName { get; }
  SortOrderBy SortOrder { get; }
}