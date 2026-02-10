using System.Reflection;
using Hamfer.Repository.Attributes;
using Hamfer.Repository.Entity;
using static Hamfer.Repository.Utils.SqlCommandTools;

namespace Hamfer.Repository.Services;

public static class RepositoryEntityHelper
{
  public const string DEFAULT_SCHEMA = "dbo";

  public static (string? schema, string? table) GetSchemaAndTable(Type type, bool handleNullAttributes = false)
  {
    string? schema = null;
    string? table = null;
    IEnumerable<RepositoryTableAttribute>? atts = type.GetCustomAttributes<RepositoryTableAttribute>(true);
    if (atts != null) {
      foreach (RepositoryTableAttribute att in atts)
      {
        if (att.param == SqlTableParam.Set_Name)
        {
          table = att.value;
        }

        if (att.param == SqlTableParam.Set_Schema)
        {
          schema = att.value;
        }
      }
    }

    if (handleNullAttributes)
    {
      schema ??= DEFAULT_SCHEMA;
      table ??= type.Name;
    }

    schema = schema != null ? RemoveEscapeCharacters(schema) : null;
    table = table != null ? RemovedDataModelPostfix(RemoveEscapeCharacters(table)) : null;

    return (schema, table);
  }

  public static (string? schema, string? table) GetSchemaAndTable<TEntity>(bool handleNullAttributes = false)
    where TEntity : class, IRepositoryEntity<TEntity>
    => GetSchemaAndTable(typeof(TEntity), handleNullAttributes);
}