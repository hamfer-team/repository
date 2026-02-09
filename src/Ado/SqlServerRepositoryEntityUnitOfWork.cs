using Hamfer.Repository.Entity;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Ado;

public sealed class SqlServerRepositoryEntityUnitOfWork<TEntity> : SqlServerRepositoryEntityUnitOfWorkBase<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  public SqlServerRepositoryEntityUnitOfWork(string connectionString, Func<SqlDataReader, TEntity> readWrapper) : base(connectionString, readWrapper)
  {
  }
}
