using System.Reflection;
using Hamfer.Repository.Attributes;
using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Services;

public static class RepositoryEntityHelper
{
  public const string DEFAULT_SCHEMA = "dbo";

  public static (string schema, string table) GetSchemaAndTable<TEntity>()
      where TEntity : class, IRepositoryEntity<TEntity>
  {
    string schema = DEFAULT_SCHEMA;
    string table = typeof(TEntity).Name;
    var atts = typeof(TEntity).GetCustomAttributes<RepositoryTableAttribute>(true);
    foreach (var att in atts)
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

    return (schema, table);
  }
}