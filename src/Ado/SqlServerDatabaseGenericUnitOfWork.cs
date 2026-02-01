using Hamfer.Repository.Entity;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Ado;

public sealed class SqlServerDatabaseGenericUnitOfWork<TEntity> : SqlServerDatabaseUnitOfWorkBase<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public SqlServerDatabaseGenericUnitOfWork(string connectionString, Func<SqlDataReader, TEntity> readWrapper) : base(connectionString, readWrapper)
  {
  }
}
