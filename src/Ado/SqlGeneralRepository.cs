using Hamfer.Verification.Services;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Ado;

public class SqlGeneralRepository
{
  protected SqlConnection connection { get; private set; }

  public SqlGeneralRepository(string? connectionString)
  {
    LetsVerify.On().Assert(connectionString, "متن ارتباط با پایگاه داده").NotNullOrEmpty().ThenThrowErrors();

    connection = new SqlConnection(connectionString);
  }

  public bool validate()
  {
    if (connection.State != System.Data.ConnectionState.Open)
    {
      try
      {
        connection.Open();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        return false;
      }
    }

    Console.WriteLine("🔗✅ Database validated successfully!");
    return true;
  }

  public bool validateServer(out SqlConnection connection)
  {
    connection = this.connection;
    if (this.connection.State != System.Data.ConnectionState.Open)
    {
      try
      {
        SqlConnectionStringBuilder scsb = new() { ConnectionString = this.connection.ConnectionString };
          
        scsb.InitialCatalog = "master";
        connection = new (scsb.ToString());

        connection.Open();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        return false;
      }
    }

    Console.WriteLine("🔗✅ Database Server validated successfully!");
    return true;
  }

  public IEnumerable<TResult> query<TResult>(SqlQueryBase<TResult> sqlQuery, params object[]? inputParams)
  {
    if (connection.State != System.Data.ConnectionState.Open)
    {
      connection.Open();
    }

    SqlCommand sqlCommand = new(sqlQuery.query, connection);
    if (inputParams != null) {
      for (int i = 0; i < inputParams.Length; i++)
      {
        object inp = inputParams[i];
        sqlCommand.Parameters.AddWithValue($"@P{i + 1}", inp);
      }
    }

    var result = new List<TResult>();
    using var reader = sqlCommand.ExecuteReader();

    if (reader.HasRows)
    {
      while (reader.Read())
      {
        var record = sqlQuery.readWrapper(reader);
        result.Add(record);
      }
    }

    return result;
  }

  public void execute(string sqlCommandText, params object[]? inputParams)
  {
    if (connection.State != System.Data.ConnectionState.Open)
    {
      connection.Open();
    }

    SqlCommand sqlCommand = new(sqlCommandText, connection);
    if (inputParams != null) {
      for (int i = 0; i < inputParams.Length; i++)
      {
        object inp = inputParams[i];
        sqlCommand.Parameters.AddWithValue($"@P{i + 1}", inp);
      }
    }

    sqlCommand.ExecuteNonQuery();
  }

  public void execute(SqlCommand sqlCommand, params object[]? inputParams) => execute(sqlCommand.CommandText, inputParams);
}
