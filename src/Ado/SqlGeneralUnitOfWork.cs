namespace Hamfer.Repository.Ado;

public sealed class SqlGeneralUnitOfWork : SqlServerDatabaseUnitOfWorkBase
{
  public SqlGeneralUnitOfWork(string connectionString) : base(connectionString)
  {
  }
}