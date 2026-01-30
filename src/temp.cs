// namespace Microsoft.Data.SqlClient;

// public class SqlConnection: IDisposable
// {
//   public ConnectionState State { get; set; }
//   public string Database { get; internal set; }

//   public SqlConnection(string _connectionString)
//   {
//     throw new NotImplementedException();
//   }

//   public void Open()
//   {
//     throw new NotImplementedException();
//   }

//   public void Close()
//   {
//     throw new NotImplementedException();
//   }

//   public void Dispose()
//   {
//     throw new NotImplementedException();
//   }

//   public SqlTransaction? BeginTransaction(IsolationLevel isolationLevel, string name)
//   {
//     throw new NotImplementedException();
//   }
// }

// public class SqlDataReader : IDisposable
// {
//   public bool HasRows { get; set; }
//   public void Dispose()
//   {
//     throw new NotImplementedException();
//   }

//   public bool Read()
//   {
//     throw new NotImplementedException();
//   }

//   public TValue GetFieldValue<TValue>(object ord)
//   {
//     throw new NotImplementedException();
//   }

//   public object GetOrdinal(string column)
//   {
//     throw new NotImplementedException();
//   }

//   public bool IsDBNull(object ord)
//   {
//     throw new NotImplementedException();
//   }
// }

// public class SqlCommand
// {
//   public SqlParameter Parameters { get; set; }
//   public string CommandText { get; internal set; }
//   public SqlConnection Connection { get; set; }
//   public SqlTransaction? Transaction { get; set; }

//   public SqlCommand(string _command)
//   {
//     throw new NotImplementedException();
//   }

//   public SqlCommand(string _command, SqlConnection _connection)
//   {
//     throw new NotImplementedException();
//   }

//   public SqlCommand()
//   {
//     throw new NotImplementedException();
//   }

//   public SqlDataReader ExecuteReader()
//   {
//     return new SqlDataReader();
//   }

//   public void ExecuteNonQuery()
//   {
//     throw new NotImplementedException();
//   }
// }

// public class SqlParameter
// {
//   public void AddWithValue(string name, object? value)
//   {
//     throw new NotImplementedException();
//   }

//   public void Clear()
//   {
//     throw new NotImplementedException();
//   }
// }

// public class SqlTransaction
// {
//   public void Commit()
//   {
//     throw new NotImplementedException();
//   }

//   public void Rollback()
//   {
//     throw new NotImplementedException();
//   }
// }
