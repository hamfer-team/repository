using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Text;
using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Services;

public class RepositorySqlCommandHelper<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  private PropertyInfo[] _properties;

  public RepositorySqlCommandHelper()
  {
    _properties = typeof(TEntity).GetProperties();
  }

  /// <summary>
  /// For INSERT string
  /// </summary>
  /// <returns></returns>
  public string generateFieldValuesPattern()
  {
    var sb = new StringBuilder();

    foreach (var prop in _properties)
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
    var sb = new StringBuilder();

    foreach (var prop in _properties)
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

  public void applyFieldParameters(SqlCommand command, TEntity? entity)
  {
    foreach (var prop in _properties)
    {
      var name = prop.Name.ToLower();
      var value = prop.GetValue(entity, null) ?? DBNull.Value;

      command.Parameters.AddWithValue(name, value);
    }
  }
}