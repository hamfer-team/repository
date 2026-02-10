using Hamfer.Repository.Data;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Duow;

public class DatabaseUnitOfWorkTransaction
{
  public SqlCommand SqlCommand { get; }
  public DatabaseTransactionState state { get; private set; }

  public DatabaseUnitOfWorkTransaction(SqlCommand sqlCommand, DatabaseTransactionState state)
  {
    this.SqlCommand = sqlCommand;
    this.state = state;
  }

  public async Task execute()
  {
    this.state = DatabaseTransactionState.Executed;
    try
    {
      // Console.WriteLine($"🤎 {this.SqlCommand.CommandText}");
      // for (int i = 0; i < this.SqlCommand.Parameters.Count; i++) { Console.WriteLine($"🤎 {this.SqlCommand.Parameters[i]}: {this.SqlCommand.Parameters[i].SqlValue}"); }

      await this.SqlCommand.ExecuteNonQueryAsync();
      this.state = DatabaseTransactionState.Succeed;
    }
    catch (Exception)
    {
      this.state = DatabaseTransactionState.Faild;
      throw;
    }
  }
}