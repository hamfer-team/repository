using Hamfer.Repository.Entity;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Migration;

public interface IRepositorySeedData
{
  /// <summary>
  /// A custom command that will execute before seed executed.
  /// </summary>
  public SqlCommand? preCommand { get; }

  /// <summary>
  /// A custom command that will execute after seed executed.
  /// </summary>
  public SqlCommand? postCommand { get; }

  /// <summary>
  /// The list of Repository entities those will be added in their tables.
  /// [Fact]: Tables are already created.
  /// **Note**: Entities will find by `id` when data found it skipped else it will inserted into table.
  /// If you need to remove old ones, use `preCommand` to delete them, consider that they will delete every time seed executed.
  /// </summary>
  public static readonly IEnumerable<IRepositoryEntity>? SeedEntities;
}