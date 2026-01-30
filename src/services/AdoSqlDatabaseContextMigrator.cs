using Hamfer.Kernel.Utils;
using Hamfer.Repository.models;
using Microsoft.Data.SqlClient;
using System.Reflection;

namespace Hamfer.Repository.services;

public static class AdoSqlDatabaseContextMigrator
{
  public static void MigrateCurrentEntitiesFrom(Assembly assembly)
  {
    var types = assembly.GetTypes();
    var commands = new List<SqlCommand>();
    foreach (var type in types)
    {
      if (ReferenceTypeHelper.IsDerivedOfGenericInterface(type, typeof(IRepositoryEntity<>)))
      {
        SqlTableInfo? tiEntity = SqlDatabaseCommandHelper.GatherTableInfoBy(type);
        if (tiEntity == null)
        {
          continue;
        }

        SqlCommand sqlCmd = SqlDatabaseCommandHelper.GenerateTableCommandBy(tiEntity);
        commands.Add(sqlCmd);
      }
    }

    //TODO start a sql transaction to create or alter tables
  }

  public static void MigrateEntitiesFromAssembly<TEntity>()
    where TEntity: class, IRepositoryEntity<TEntity>
  {
    var assembly = Assembly.GetAssembly(typeof(TEntity));
    if (assembly != null)
    {
      MigrateCurrentEntitiesFrom(assembly);
    }
  }
}