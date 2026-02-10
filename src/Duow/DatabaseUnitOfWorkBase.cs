using Hamfer.Repository.Data;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Duow;

public abstract class DatabaseUnitOfWorkBase : IDatabaseUnitOfWork
{
  protected SqlConnection connection { get; }
  protected string databaseName { get; }

  protected DatabaseUnitOfWorkQeue transactionsQueue { get; private set; }

  protected DatabaseUnitOfWorkBase(string connectionString)
  {
    this.connection = new SqlConnection(connectionString);
    this.databaseName = this.connection.Database;

    this.transactionsQueue = new DatabaseUnitOfWorkQeue();
    this.connection.Open();
  }

  protected abstract Task writeToDatabase(DatabaseUnitOfWorkQeue transactions);
  protected abstract void lockDatabase();
  protected abstract void unlockDatabase();
  public abstract void Dispose();

  public virtual async Task commit()
  {
    this.lockDatabase();
    await this.writeToDatabase(transactionsQueue);
    this.unlockDatabase();

    this.transactionsQueue.Clear();
  }

  public virtual async Task rollBack()
  {
    this.transactionsQueue.Clear();
  }

  public void addToQueue(SqlCommand sqlCommand, DatabaseTransactionState state = DatabaseTransactionState.Initiated)
  {
    DatabaseUnitOfWorkTransaction transaction = new(sqlCommand, state);
    this.transactionsQueue.Enqueue(transaction);
  }

  public void addToQueue(SqlCommand[] sqlCommands, DatabaseTransactionState state = DatabaseTransactionState.Initiated)
  {
    foreach (SqlCommand sqlCommand in sqlCommands)
    {
      this.addToQueue(sqlCommand);
      // Console.WriteLine($"🧡 Transaction[{this.transactionsQueue.Count}]: {sqlCommand.CommandText}");
    }
  }
}