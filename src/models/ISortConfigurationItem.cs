using Hamfer.Repository.Data;

namespace Hamfer.Repository.Models;

public interface ISortConfigurationItem
{
  string propertyName { get; }

  SortOrderBy sortOrder { get; }
}