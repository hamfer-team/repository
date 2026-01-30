using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.models;

public class AdoSqlGeneralRepository<TResult>
  where TResult: class
{
  protected SqlConnection _connection { get; }
  protected AdoSqlQueryBase<TResult> _sqlQuery { get; }

  public AdoSqlGeneralRepository(string connectionString, AdoSqlQueryBase<TResult> sqlQuery)
  {
    _connection = new SqlConnection(connectionString);
    _sqlQuery = sqlQuery;
}

  public IEnumerable<TResult> ExecuteQuery(params object[] inputParams)
  {
    if (_connection.State != System.Data.ConnectionState.Open)
    {
      _connection.Open();
    }

    var sqlCommand = new SqlCommand(_sqlQuery.Query, _connection);
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
        var record = _sqlQuery.ReadWrapper(reader);
        result.Add(record);
      }
    }

    return result;
  }
}
