using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.models;

public sealed class AdoSqlServerDatabaseGenericUnitOfWork<TEntity> : AdoSqlServerDatabaseUnitOfWorkBase<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public AdoSqlServerDatabaseGenericUnitOfWork(string connectionString, Func<SqlDataReader, TEntity> readWrapper) : base(connectionString, readWrapper)
  {
  }
}
