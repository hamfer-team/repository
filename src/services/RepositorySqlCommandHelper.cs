using System.Reflection;
using System.Text;
using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.Entity;
using Hamfer.Repository.Utils;
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
  public string generateFieldPatternInsert()
  {
    StringBuilder sb = new();

    foreach (PropertyInfo prop in properties)
    {
      // We don't use a readonly property ;)
      if (prop.CanWrite)
      {
        sb.Append('[')
          .Append(SqlCommandTools.RemoveEscapeCharacters(prop.Name))
          .Append("], ");
      }
    }

    sb.Append(';').Replace(", ;", "");
    return sb.ToString();
  }

  public string generateValuePatternInsert(string? columns = null, string? paramPostfix = null)
  {
    columns ??= this.generateFieldPatternInsert();
    string values = columns.ToLowerInvariant().Replace("]", paramPostfix ?? "").Replace('[', '@');
    return values;
  }

  /// <summary>
  /// For UPDATE string
  /// </summary>
  /// <param name="exceptions"></param>
  /// <returns></returns>
  public string generateFieldValuePatternUpdate(List<string>? exceptions = null)
  {
    StringBuilder sb = new();

    foreach (PropertyInfo prop in properties)
    {
      if (exceptions != null && exceptions.Contains(prop.Name))
      {
        continue;
      }

      string propName = SqlCommandTools.RemoveEscapeCharacters(prop.Name);
      // We don't use a readonly property ;)
      if (prop.CanWrite)
      {
        sb.Append($"[{propName}]")
          .Append('=')
          .Append($"@{propName.ToLowerInvariant()}, ");
      }
    }

    sb.Append(';').Replace(", ;", "");
    return sb.ToString();
  }

  public SqlCommand applyFieldParameters<TEntity>(SqlCommand command, TEntity? entity, string? paramPostfix = null)
    where TEntity : class, IRepositoryEntity
  {
    foreach (PropertyInfo prop in properties)
    {
      string name = $"@{prop.Name.ToLowerInvariant()}{paramPostfix ?? ""}";
      object? value = prop.GetValue(entity, null) ?? DBNull.Value;

      command.Parameters.AddWithValue(name, value);
      // Console.WriteLine($"🖤 {name}: {value}");
    }
    return command;
  }
  
}

public class RepositorySqlCommandHelper<TEntity> : RepositorySqlCommandHelper
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public RepositorySqlCommandHelper(): base(typeof(TEntity))
  {
  }
}