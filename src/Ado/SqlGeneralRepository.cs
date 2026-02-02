using Hamfer.Verification.Services;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Ado;

public class SqlGeneralRepository
{
  protected SqlConnection _connection { get; }

  public SqlGeneralRepository(string? connectionString)
  {
    LetsVerify.On().Assert(connectionString, "متن ارتباط با پایگاه داده").NotNullOrEmpty().ThenThrowErrors();

    _connection = new SqlConnection(connectionString);
  }

  public bool validate()
  {
    if (_connection.State != System.Data.ConnectionState.Open)
    {
      try
      {
        _connection.Open();
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

  public IEnumerable<TResult> query<TResult>(SqlQueryBase<TResult> sqlQuery, params object[] inputParams) where TResult: class
  {
    if (_connection.State != System.Data.ConnectionState.Open)
    {
      _connection.Open();
    }

    var sqlCommand = new SqlCommand(sqlQuery.query, _connection);
    for (int i = 0; i < inputParams.Length; i++)
    {
      var inp = inputParams[i];
      sqlCommand.Parameters.AddWithValue($"@P{i + 1}", inp);
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
}
