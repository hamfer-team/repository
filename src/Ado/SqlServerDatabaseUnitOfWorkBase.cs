using System.Data;
using Hamfer.Kernel.Errors;
using Hamfer.Repository.Duow;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Ado;

public abstract class SqlServerDatabaseUnitOfWorkBase: DatabaseUnitOfWorkBase
{
  protected SqlTransaction? sqlTransaction { get; private set; }

  public SqlServerDatabaseUnitOfWorkBase(string connectionString): base(connectionString)
  {
  }

  public override void Dispose()
  {
    if (this.connection.State != ConnectionState.Closed)
    {
      this.connection.Close();
    }

    this.connection?.Dispose();
  }

  protected override async Task writeToDatabase(DatabaseUnitOfWorkQeue transactions)
  {
    this.sqlTransaction = connection.BeginTransaction(IsolationLevel.Serializable, "UnitOfworkTransaction");

    try
    {
      int count = transactions.Count;
      for (int i = 0; i < count; i++)
      {
        DatabaseUnitOfWorkTransaction transaction = transactions.Dequeue();

        SqlCommand command = transaction.SqlCommand;
        command.Transaction = this.sqlTransaction;

        await transaction.execute();
      }

      this.sqlTransaction?.Commit();
    }
    catch (Exception commitException)
    {
      try
      {
        this.sqlTransaction?.Rollback();
      }
      catch (Exception rollbackException)
      {
        AggregateException ex = new([commitException, rollbackException]);
        throw new RepositoryError("Rollback after failed commit also failed!!", ex);
      }

      throw new RepositoryError("Commit SQL Transaction failed!", commitException);
    }
  }

  protected override void lockDatabase()
  {
    // ignored: Not needed yet!
  }

  protected override void unlockDatabase()
  {
    // ignored: Not needed yet!
  }
}