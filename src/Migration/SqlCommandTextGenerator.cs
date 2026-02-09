using Hamfer.Repository.Entity;
using Hamfer.Repository.Models;
using Hamfer.Verification.Errors;
using Microsoft.Data.SqlClient;
using static Hamfer.Repository.Utils.SqlCommandTools;

namespace Hamfer.Repository.Migration;

internal sealed class SqlCommandTextGenerator
{
  internal static TableCommand GenerateTableCommandsBy(SqlTableInfo sti, string? name = null, SqlTableInfo? dbti = null)
  {
    // https://docs.microsoft.com/en-us/sql/t-sql/statements/create-table-transact-sql?view=sql-server-ver15

    VerifyTableInfo(sti, name);

    string nl = Environment.NewLine;
    string schema = RemoveEscapeCharacters(sti.schema!);
    string table = RemoveEscapeCharacters(sti.name!);
    string tableFullName = $"[{schema}].[{table}]";

    bool tableExists = false;
    if (dbti != null && IsSame(sti.schema, dbti?.schema) && IsSame(sti.name, dbti?.name))
    {
      // Console.Write($"💛 {tableFullName} exists:");
      tableExists = true;
    }

    List<string> columnStrings = [];
    TableCommand tableCommand = new(tableFullName);

    foreach (SqlColumnInfo sci in sti.columns!)
    {
      string column = RemoveEscapeCharacters(sci.name!);
      string columnTypeString = $"[{column}] {sci.sqlDbTypeText} {(sci.isNullable ? "NULL" : "NOT NULL")}";
      string columnString = columnTypeString;
      SqlColumnInfo? dbci = tableExists ? dbti?.columns?.SingleOrDefault(w => IsSame(column, w.name)) : null;
      if (sci.defaultValue != null)
      {
        columnString += $" CONSTRAINT {DbKeyForDefaultValue(schema, table, column)} DEFAULT {sci.defaultValueText}";
      }

      // Column description Creation or updating is same
      if (sci.description != null && !IsSame(sci.description, dbci?.description))
      {
        tableCommand.setDescriptions.Add(new SqlCommand($"EXEC sys.sp_addextendedproperty @name=N'Description', @value=N'{sti.description}', @level0type=N'SCHEMA',@level0name=N'{schema}', @level1type=N'TABLE',@level1name=N'{table}', @level2type=N'COLUMN',@level2name=N'{column}';"));
      }

      if (tableExists) {
        bool columnExists = dbci != null;

        if (columnExists) {
          bool columnTypeIsSame = columnExists && IsSame(sci.sqlDbTypeText, dbci?.defaultValueText) && (sci.isNullable == dbci!.isNullable);
          bool columnDefIsSame = columnExists && IsSame(RemoveDefaultValueCharacters(sci.defaultValueText), RemoveDefaultValueCharacters(dbci?.defaultValueText));
          /*
          // TODO: DROP STATISTICS before alter column: https://learn.microsoft.com/en-us/sql/t-sql/statements/alter-table-transact-sql?view=sql-server-ver17

          +------------+--------------+------------------+-----------------+-------------------------------+---------------------------------------------+
          |tableExists | columnExists | columnTypeIsSame | columnDefIsSame | dbci.defaultValueText != null |  TODO                                       |
          +------------+--------------+------------------+-----------------+-------------------------------+---------------------------------------------+
          |     ✅    |      ✅      |        ✅        |       ✅       |              💢               |  [Do nothing / Continue to next column]    |
          |     ✅    |      ✅      |        ✅        |       ❌       |              ✅               |  Drop Default + Add Default                |
          |     ✅    |      ✅      |        ✅        |       ❌       |              ❌               |  Add Default                               |
          |     ✅    |      ✅      |        ❌        |       ✅       |              ✅               |  Drop Default + Alter Column + Add Default |
          |     ✅    |      ✅      |        ❌        |       ✅       |              ❌               |  Alter Column                              |
          |     ✅    |      ✅      |        ❌        |       ❌       |              ✅               |  Drop Default + Alter Column + Add Default |
          |     ✅    |      ✅      |        ❌        |       ❌       |              ❌               |  Alter Column + Add Default                |
          |     ✅    |      ❌      |        💢        |       💢       |              💢               |  [Add Column]                              |
          |     ❌    |      💢      |        💢        |       💢       |              💢               |  [Add Table]                               |
          +------------+--------------+------------------+-----------------+-------------------------------+---------------------------------------------+
          >> tableExists : ✅
            >> columnExists : ✅
              >> dbci.defaultValueText != null : ✅ => Drop Default
              >> columnTypeIsSame : ❌ => Alter Column
              >> !(columnTypeIsSame : ❌ && columnDefIsSame : ✅ && dbci.defaultValueText != null : ❌) => Add Default
                = columnTypeIsSame : ✅ || columnDefIsSame : ❌ || dbci.defaultValueText != null : ✅ => Add Default
            >> columnExists : ❌ : Add Column
          >> tableExists : ❌ : Add Table
          */

          if (columnTypeIsSame && columnDefIsSame)
          {
            // Do nothing / Continue to next column
            continue;
          }

          if (dbci?.defaultValueText != null)
          {
            // Drop Default
            tableCommand.dropConstraints.Add(new SqlCommand($"ALTER TABLE {tableFullName} DROP CONSTRAINT {DbKeyForDefaultValue(schema,table,column)};"));
          }

          if (columnTypeIsSame || !columnDefIsSame || dbci?.defaultValueText != null)
          {
            // Add Default
            tableCommand.updateConstraints.Add(new SqlCommand($"ALTER TABLE {tableFullName} ADD CONSTRAINT {DbKeyForDefaultValue(schema,table,column)} DEFAULT ({sci.defaultValueText}) FOR [{column}];"));
          }

          if (!columnTypeIsSame)
          {
            // Alter Column
            tableCommand.updateColumns.Add(new SqlCommand($"ALTER TABLE {tableFullName} ALTER COLUMN {columnTypeString};"));
          }
          continue;
        }

        // Add Column
        tableCommand.updateColumns.Add(new SqlCommand($"ALTER TABLE {tableFullName} ADD COLUMN {columnString};"));

        continue;
      }

      // Add Table
      columnStrings.Add(columnString);
    }
    
    if (tableExists)
    {
      // TODO PKs and uniques
    }
    else
    {
      string columnsString = columnStrings.Aggregate((a, b) => $"{a},{nl}\t{b}");

      string primaryKeysString = sti.primaryKeys!.Select(spk => $"[{RemoveEscapeCharacters(spk)}]").Aggregate((a, b) => $"{a}, {b}");
      string primaryKeyString = $",{nl}\tCONSTRAINT {DbKeyForPrimaryKey(schema, table)} PRIMARY KEY CLUSTERED ({primaryKeysString})";

      
      string uniqueConstraintString = "";
      if (sti.uniqueConstraints != null) {
        foreach (KeyValuePair<string, string[]> unique in sti.uniqueConstraints)
        {
          string uniquesString = unique.Value.Select(u => $"[{RemoveEscapeCharacters(u)}]").Aggregate((a, b) => $"{a}, {b}");
          uniqueConstraintString = $",{nl}\tCONSTRAINT {DbKeyForUnique(schema, table, unique.Key)} UNIQUE NONCLUSTERED ({uniquesString})";
        }
      }

      tableCommand.createTable = new($"CREATE TABLE {tableFullName} ({nl}\t{columnsString}{primaryKeyString}{uniqueConstraintString}{nl});");
    }

    SqlCommand? descriptionCommand = sti.description != null 
      ? new($"EXEC sys.sp_addextendedproperty @name=N'Description', @value=N'{sti.description}', @level0type=N'SCHEMA',@level0name=N'{schema}', @level1type=N'TABLE',@level1name=N'{table}';")
      : null;
    if (descriptionCommand != null)
    {
      tableCommand.setDescriptions.Add(descriptionCommand);
    }

    // TODO: add relations

    return tableCommand;
  }

  internal static SqlCommand[]? GenerateSeedCommandFor(IEnumerable<IRepositoryEntity> seedEntities)
  {
    Console.WriteLine($"🧡 ");

    return null;
    // throw new NotImplementedException();
  }

  private static void VerifyTableInfo(SqlTableInfo sti, string? name = null)
  {
    try
    {
      sti.verify(name);
    }
    catch (LetsVerifyAggregateError err)
    {
      err.writeMessages();
      throw;
    }
    catch (LetsVerifyError)
    {
      throw;
    }
  }
}