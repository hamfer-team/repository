using System.Data;
using System.Text.Json;
using Hamfer.Kernel.Utils;

namespace Hamfer.Repository.Services;

public static class SqlCommandTextHelper {
  public static string? getValueText(dynamic? value, SqlDbType type)
  {
    const string NULL = "Null";
    static string safe(string value) { return value.Replace("'", "''"); }

    if (value == null) return null;

    switch (type)
    {
      case SqlDbType.BigInt:
      case SqlDbType.Decimal:
      case SqlDbType.Float:
      case SqlDbType.Int:
      case SqlDbType.Money:
      case SqlDbType.Real:
      case SqlDbType.SmallMoney:
      case SqlDbType.SmallInt:
      case SqlDbType.TinyInt:
      case SqlDbType.Timestamp:
        {
          decimal? dv = TypeHelper.ChangeTypeTo<decimal?>(value);
          return dv != null ? $"({dv})" : NULL;
        }
      case SqlDbType.Char:
      case SqlDbType.NChar:
      case SqlDbType.VarChar:
      case SqlDbType.NVarChar:
      case SqlDbType.Text:
      case SqlDbType.NText:
      case SqlDbType.Binary:
      case SqlDbType.VarBinary:
      case SqlDbType.Image:
        {
          return value != null ? $"'{safe(value)}'" : NULL;
        }
      case SqlDbType.Bit:
        {
          bool? dv = TypeHelper.ChangeTypeTo<bool?>(value);
          return dv != null ? (dv.Value ? "1": "0") : NULL;
        }
      case SqlDbType.UniqueIdentifier:
        {
          Guid? dv = TypeHelper.ChangeTypeTo<Guid?>(value);
          return dv != null ? $"'{dv}'" : NULL;
        }
      case SqlDbType.Date:
        {
          DateTime? dv = TypeHelper.ChangeTypeTo<DateTime>(value);
          return dv != null ? $"'{dv?.ToString("yyyy-MM-dd")}'" : NULL;
        }
      case SqlDbType.SmallDateTime:
        {
          DateTime? dv = TypeHelper.ChangeTypeTo<DateTime>(value);
          return dv != null ? $"'{dv?.ToString("yyyy-MM-dd hh:mm")}'" : NULL;
        }
      case SqlDbType.DateTime:
        {
          DateTime? dv = TypeHelper.ChangeTypeTo<DateTime>(value);
          return dv != null ? $"'{dv?.ToString("yyyy-MM-dd hh:mm:ss.fff")}'" : NULL;
        }
      case SqlDbType.DateTime2:
        {
          DateTime? dv = TypeHelper.ChangeTypeTo<DateTime>(value);
          return dv != null ? $"'{dv?.ToString("yyyy-MM-dd hh:mm:ss.fffffff")}'" : NULL;
        }
      case SqlDbType.DateTimeOffset:
        {
          DateTime? dv = TypeHelper.ChangeTypeTo<DateTime>(value);
          return dv != null ? $"'{dv?.ToString("yyyy-MM-dd hh:mm:ss.fffffff zzz")}'" : NULL;
        }
      case SqlDbType.Time:
        {
          DateTime? dv = TypeHelper.ChangeTypeTo<DateTime>(value);
          return dv != null ? $"'{dv?.ToString("hh:mm:ss.fffffff")}'" : NULL;
        }
      case SqlDbType.Json:
        {
          object? dv = TypeHelper.ChangeTypeTo<object?>(value);
          return dv != null ? $"'{safe(JsonSerializer.Serialize(dv))}'" : NULL;
        }
      default:
        return null;
    }
  }
}