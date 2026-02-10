using System.Text.RegularExpressions;
using Hamfer.Repository.Services;

namespace Hamfer.Repository.Utils;

public static class SqlCommandTools
{
  public static bool IsSame(string? a, string? b)
    => (a == null && b == null) || (a?.Equals(b, StringComparison.InvariantCultureIgnoreCase) ?? false);

  public static string RemoveEscapeCharacters(string text)
    => text.Replace("'", "").Replace("[", "").Replace("]", "").Replace("-", "_");

  public static string? RemoveDefaultValueCharacters(string? text)
    => text != null ? Regex.Replace(text, @"(^\(\((?<value>.+)\)\)$|^\((?<value>.+)\)$)", match => match.Groups["value"].ToString()) : null;

  public static string PrepareTableName(string? schema, string tableName) => $"[{schema ?? RepositoryEntityHelper.DEFAULT_SCHEMA}].[{tableName}]";

  public static string PrepareParamName(string name, string? paramPostfix = null) => RemoveEscapeCharacters($"@{name}{paramPostfix ?? ""}".ToLowerInvariant());

  public static string DbKeyForDefaultValue(string schema, string table, string column) => $"[DF_{schema}_{table}_{column}]";

  public static string DbKeyForPrimaryKey(string schema, string table) => $"[PK_{schema}_{table}]";
  
  public static string DbKeyForUnique(string schema, string table, string key) => $"[IX_{schema}_{table}{(key == "" ? "" : "_" + key)}]";

  public static string RemovedDataModelPostfix(string typeName)
  {
    Match? match = new Regex(@"^(?<name>.+?)(Model|DataModel|Entity|EntityModel|Table|TableModel)$", RegexOptions.IgnoreCase).Match(typeName);
    return match != null && match.Success ? match.Groups["name"].ToString() : typeName;
  }
}