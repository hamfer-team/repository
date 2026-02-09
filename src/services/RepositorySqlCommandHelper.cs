using System.Reflection;
using System.Text;
using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.Entity;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Services;

public class RepositorySqlCommandHelper
{
  private readonly PropertyInfo[] properties;

  public RepositorySqlCommandHelper(Type entityType)
  {
    if (!ReferenceTypeHelper.IsDerivedOfGenericInterface(entityType, typeof(IRepositoryEntity<>)))
    {
      throw new RepositoryError("The `entityType` must implements `IRepositoryEntity<entityType>`.");
    }

    this.properties = entityType.GetProperties();
  }

  /// <summary>
  /// For INSERT string
  /// </summary>
  /// <returns></returns>
  public string generateFieldValuesPattern()
  {
    StringBuilder sb = new();

    foreach (PropertyInfo prop in properties)
    {
      // We don't use a readonly property ;)
      if (prop.CanWrite)
      {
        sb.Append('[')
          .Append(prop.Name)
          .Append("], ");
      }
    }

    sb.Append(';')
      .Replace(", ;", "");
    return sb.ToString();
  }

  /// <summary>
  /// For UPDATE string
  /// </summary>
  /// <param name="exceptions"></param>
  /// <returns></returns>
  public string generateFieldAndValuesPattern(List<string>? exceptions = null)
  {
    StringBuilder sb = new();

    foreach (PropertyInfo prop in properties)
    {
      if (exceptions != null && exceptions.Contains(prop.Name))
      {
        continue;
      }

      // We don't use a readonly property ;)
      if (prop.CanWrite)
      {
        sb.Append($"[{prop.Name}]")
          .Append('=')
          .Append($"@{prop.Name.ToLower()}, ");
      }
    }

    sb.Append(';')
      .Replace(", ;", "");
    return sb.ToString();
  }

  public void applyFieldParameters<TEntity>(SqlCommand command, TEntity? entity)
    where TEntity : class, IRepositoryEntity<TEntity>
  {
    foreach (PropertyInfo prop in properties)
    {
      string name = prop.Name.ToLower();
      object? value = prop.GetValue(entity, null) ?? DBNull.Value;

      command.Parameters.AddWithValue(name, value);
    }
  }
  }

public class RepositorySqlCommandHelper<TEntity> : RepositorySqlCommandHelper
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public RepositorySqlCommandHelper(): base(typeof(TEntity))
  {
  }
}