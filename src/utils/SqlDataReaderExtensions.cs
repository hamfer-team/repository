using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.utils;

public static class SqlDataReaderExtensions
{
  public static TValue? Get<TValue>(this SqlDataReader reader, string column)
  {
    var ord = reader.GetOrdinal(column);
    if (reader.IsDBNull(ord))
    {
      return default;
    }

    var value = reader.GetFieldValue<TValue>(ord);
    return value;
  }
}
