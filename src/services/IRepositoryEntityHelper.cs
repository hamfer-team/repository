using System.Reflection;
using Hamfer.Repository.data;
using Hamfer.Repository.models;
using Hamfer.Repository.utils;

namespace Hamfer.Repository.services;

public static class IRepositoryEntityHelper
{
  public const string DefaultSchema = "dbo";

  public static (string Schema, string Table) GetSchemaAndTable<TEntity>()
      where TEntity : class, IRepositoryEntity<TEntity>
  {
    string schema = DefaultSchema;
    string table = typeof(TEntity).Name;
    var atts = typeof(TEntity).GetCustomAttributes<RepositoryTableAttribute>(true);
    foreach (var att in atts)
    {
      if (att.Param == SqlTableParam.Set_Name)
      {
        table = att.Value;
      }

      if (att.Param == SqlTableParam.Set_Schema)
      {
        schema = att.Value;
      }
  }

    return (schema, table);
  }
}