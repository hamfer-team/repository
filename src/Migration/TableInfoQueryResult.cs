using System.Data;
using System.Text.Json;
using Hamfer.Repository.Models;

namespace Hamfer.Repository.Migration
{
  internal sealed class TableInfoQueryResult : SqlTableInfo
  {
    public TableInfoQueryResult(string? schema, string? table, string? description, string? colJson, string? ixJson)
    {
      this.schema = schema;
      this.name = table;
      this.description = description;
      ColumnInfoQueryResult[]? colResults = colJson != null ? JsonSerializer.Deserialize<ColumnInfoQueryResult[]>(colJson) : null;
      this.columns = colResults?.Select(c => new SqlColumnInfo()
      {
        name = c.name,
        dbType = c.type != null ? Enum.Parse<SqlDbType>(c.type) : null,
        charMaxLength = c.max_length,
        defaultValue = c.def,
        defaultValueText = c.def,
        description = c.description,
        identitySeed = c.seed_value,
        identityIncrement = c.increment_value,
        isNullable = c.is_nullable ?? true,
        numericPrecision = c.precision,
        numericScale = c.scale,
        timeScale = c.scale,
      }).ToList();

      UniqueInfoQueryResult[]? uniqueResults = ixJson != null ? JsonSerializer.Deserialize<UniqueInfoQueryResult[]>(ixJson) : null;
      this.primaryKeys = uniqueResults?.Where(w=> w.is_primary_key ?? false && w.column != null).Select(c => c.column!).ToArray();

      UniqueInfoQueryResult[]? uniques = uniqueResults?.Where(w => w.is_primary_key ?? false && w.column != null).ToArray();
      if (uniques != null) {
        this.uniqueConstraints ??= [];
        foreach (UniqueInfoQueryResult unique in uniques)
        {
          if (this.uniqueConstraints.ContainsKey(unique.key!))
          {
            if(this.uniqueConstraints.TryGetValue(unique.key!, out string[]?value))
            {
              this.uniqueConstraints.Remove(unique.key!);
            }
            List<string>? values = value?.ToList();
            values?.Add(unique.column!);
            this.uniqueConstraints.Add(unique.key!, values?.ToArray() ?? []);
          } else
          {
            this.uniqueConstraints.Add(unique.key!, [unique.column!]);
          }
        }
      }
    }
  }
}