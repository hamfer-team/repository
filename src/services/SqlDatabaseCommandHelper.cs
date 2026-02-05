using System.Reflection;
using System.Text.RegularExpressions;
using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.Attributes;
using Hamfer.Repository.Data;
using Hamfer.Repository.Entity;
using Hamfer.Repository.Errors;
using Hamfer.Repository.Models;
using Hamfer.Verification.Errors;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Services;

public static class SqlDatabaseCommandHelper
{
  public static SqlTableInfo? GatherTableInfoBy(Type type)
  {
    if (!ReferenceTypeHelper.IsDerivedOfGenericInterface(type, typeof(IRepositoryEntity<>)))
    {
      throw new RepositoryError($"The <{type.FullName}> can't be used for gathering table-info.");
    }

    #region Get Table Meta Data
    string? schema = null;
    string? tableName = null;
    bool? ignored = null;
    string? description = null;
    string? tablePrimarykeyCsv = null;
    Dictionary<string, string[]>? uniques = null;
    string? tableUniqueCsv = null;

    IEnumerable<RepositoryTableAttribute>? atts = type.GetCustomAttributes<RepositoryTableAttribute>(true);
    foreach (RepositoryTableAttribute att in atts)
    {
      switch (att.param)
      {
        case SqlTableParam.Set_Name:
          _ = ValueTypeHelper.TryParse(att.value, out tableName);
          break;
        case SqlTableParam.Set_Schema:
          _ = ValueTypeHelper.TryParse(att.value, out schema);
          break;
        case SqlTableParam.Is_Ignored:
          _ = ValueTypeHelper.TryParse(att.value, out ignored);
          break;
        case SqlTableParam.Set_Description:
          _ = ValueTypeHelper.TryParse(att.value, out description);
          break;
        case SqlTableParam.Set_PrimaryKey_commaSeparatedString:
          _ = ValueTypeHelper.TryParse(att.value, out tablePrimarykeyCsv);
          break;
        case SqlTableParam.With_UniqueConstraints_commaSeparatedString:
          _ = ValueTypeHelper.TryParse(att.value, out tableUniqueCsv);
          break;
        default:
          break;
      }
    }

    tableName = RemoveEscapeCharacters(tableName ?? type.Name);
    Match? match = new Regex(@"^(?<name>.+?)(Model|DataModel|Entity|EntityModel|Table|TableModel)$", RegexOptions.IgnoreCase).Match(tableName);
    tableName = match != null && match.Success ? match.Groups["name"].ToString() : tableName;
    
    if (tableUniqueCsv != null)
    {
      uniques ??= [];
      uniques.Add("", [.. Regex.Replace(tableUniqueCsv, @"\s+", "", RegexOptions.IgnoreCase).Split(",")]);
    }
    #endregion

    if (ignored ?? false)
    {
      // ok, Ok, OK! I'll ignore this entity.
      return null;
    }

    List<SqlColumnInfo> columns = [];
    List<string>? primaryKeys = tablePrimarykeyCsv != null ? [.. Regex.Replace(tablePrimarykeyCsv, @"\s+", "", RegexOptions.IgnoreCase).Split(",")] : null;


    foreach (PropertyInfo prop in type.GetProperties())
    {
      Type ptype =  prop.PropertyType;

      #region Get Column Meta Data
      bool? fixedLength = null;
      int? storageSize = null;
      bool? dateOnly = null;
      int? fractionalSecondScale = null;
      int? precision = null;
      int? scale = null;
      bool? supprtsUnicode = null;
      bool? automaticGeneration = null;
      bool? isMoney = null;
      bool? isSmallMoney = null;
      bool? maxSize = null;
      int? identitySeed = null;
      int? identityIncrement = null;

      string? colName = null;
      bool? isPrimaryKey = null;
      string? uniqueGroup = null;
      bool? isNullable = null;
      bool? isNotNullable = null;
      string? colDesc = null;

      IEnumerable<RepositoryColumnAttribute>? patts = prop.GetCustomAttributes<RepositoryColumnAttribute>(true);
      foreach (RepositoryColumnAttribute patt in patts)
      {
        switch (patt.param)
        {
          case SqlColumnParam.With_FixedLength:
            _ = ValueTypeHelper.TryParse(patt.value, out fixedLength);
            fixedLength ??= true;
              break;
          case SqlColumnParam.Set_StorageSize_int:
            _ = ValueTypeHelper.TryParse(patt.value, out storageSize);
            break;
          case SqlColumnParam.Is_DateOnly:
            _ = ValueTypeHelper.TryParse(patt.value, out dateOnly);
            dateOnly ??= true;
            break;
          case SqlColumnParam.Set_FractionalSecondScale_int:
            _ = ValueTypeHelper.TryParse(patt.value, out fractionalSecondScale);
            break;
          case SqlColumnParam.Set_Precision_int:
            _ = ValueTypeHelper.TryParse(patt.value, out precision);
            break;
          case SqlColumnParam.Set_Scale_int:
            _ = ValueTypeHelper.TryParse(patt.value, out scale);
            break;
          case SqlColumnParam.With_SupprtsUnicode:
            _ = ValueTypeHelper.TryParse(patt.value, out supprtsUnicode);
            supprtsUnicode ??= true;
            break;
          case SqlColumnParam.With_AutomaticGeneration:
            _ = ValueTypeHelper.TryParse(patt.value, out automaticGeneration);
            automaticGeneration ??= true;
            break;
          case SqlColumnParam.Is_Money:
            _ = ValueTypeHelper.TryParse(patt.value, out isMoney);
            isMoney ??= true;
            break;
          case SqlColumnParam.Is_SmallMoney:
            _ = ValueTypeHelper.TryParse(patt.value, out isSmallMoney);
            isSmallMoney ??= true;
            break;
          case SqlColumnParam.With_MaxSize:
            _ = ValueTypeHelper.TryParse(patt.value, out maxSize);
            maxSize ??= true;
            break;
          case SqlColumnParam.Is_Identity_With_Seed_int:
            _ = ValueTypeHelper.TryParse(patt.value, out identitySeed);
            break;
          case SqlColumnParam.Is_Identity_With_Increment_int:
            _ = ValueTypeHelper.TryParse(patt.value, out identityIncrement);
            break;
          case SqlColumnParam.Is_PrimaryKey:
            _ = ValueTypeHelper.TryParse(patt.value, out isPrimaryKey);
            isPrimaryKey ??= true;
            break;
          case SqlColumnParam.Is_Unique_With_string:
            _ = ValueTypeHelper.TryParse(patt.value, out uniqueGroup);
            break;
          case SqlColumnParam.Is_Nullable:
            _ = ValueTypeHelper.TryParse(patt.value, out isNullable);
            isNullable ??= true;
            break;
          case SqlColumnParam.Is_Not_Nullable:
            _ = ValueTypeHelper.TryParse(patt.value, out isNotNullable);
            isNotNullable ??= true;
            break;
          case SqlColumnParam.Is_Ignored:
            _ = ValueTypeHelper.TryParse(patt.value, out ignored);
            ignored ??= true;
            break;
          case SqlColumnParam.With_DefaultValue_string:
            _ = ValueTypeHelper.TryParse(patt.value, out dynamic? defaultValue);
            break;
          case SqlColumnParam.Set_Name:
            _ = ValueTypeHelper.TryParse(patt.value, out colName);
            break;
          case SqlColumnParam.Set_Description:
            _ = ValueTypeHelper.TryParse(patt.value, out colDesc);
            break;
          default:
            break;
        }
      }
      #endregion

      // Console.Write($"🧡 {type.FullName}: {schema}.{tableName}.{colName} {ptype.Name} ");
      // Ignored property detected!, So skip to next ;)
      if (ignored ?? false)
      {
        continue;
      }

      //TODO enum ?
      //TODO struct ?

      string columnName = RemoveEscapeCharacters(colName ?? prop.Name);
      SqlColumnInfoBuilder cibuilder = new(columnName);
      if (SqlColumnInfoBuilder.TypeHelper(ptype, out MidDataType? midType))
      {
        var isIdentitySuit = false;

        switch (midType)
        {
          case MidDataType.BigInt:
            cibuilder.isInteger(8);
            cibuilder.isNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.Binary:
            cibuilder.isBinary(fixedLength ?? true, storageSize ?? 30);
            cibuilder.isNullable();
            break;
          case MidDataType.Bit:
            cibuilder.isBoolean();
            cibuilder.isNotNullable();
            break;
          case MidDataType.DateTime:
            cibuilder.isDate();
            if (!(dateOnly ?? false))
            {
              cibuilder.withTime(fractionalSecondScale ?? 3);
            }
            cibuilder.isNotNullable();
            break;
          case MidDataType.DateTimeOffset:
            cibuilder.isDate();
            cibuilder.withTime(fractionalSecondScale ?? 7);
            cibuilder.withTimeZone();
            cibuilder.isNotNullable();
            break;
          case MidDataType.Decimal:
            cibuilder.isDecimal(precision ?? 19, scale ?? 5);
            cibuilder.isNotNullable();
            break;
          case MidDataType.Float:
            cibuilder.isFloatingPoint();
            cibuilder.isNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.Int:
            cibuilder.isInteger(4);
            cibuilder.isNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.Numeric20:
            cibuilder.isDecimal(20, 0);
            cibuilder.isNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.Real:
            cibuilder.isFloatingPoint(24);
            cibuilder.isNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.SmallInt:
            cibuilder.isInteger(2);
            cibuilder.isNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.String:
            cibuilder.isString(supprtsUnicode ?? true, fixedLength ?? true, storageSize ?? 30);
            cibuilder.isNullable();
            break;
          case MidDataType.Char:
            cibuilder.isString(supprtsUnicode ?? true, false, 1);
            cibuilder.isNotNullable();
            break;
          case MidDataType.Time:
            cibuilder.withTime(fractionalSecondScale ?? 7);
            cibuilder.isNotNullable();
            break;
          case MidDataType.TinyInt:
            cibuilder.isInteger(1);
            cibuilder.isNotNullable();
            break;
          case MidDataType.Uid:
            cibuilder.isUid(automaticGeneration ?? false);
            if (automaticGeneration ?? false) {
              cibuilder.isNotNullable();
            } else {
              cibuilder.isNullable();
            }
            break;
          default:
            break;
        }

        // Supporint Money Types
        if (isSmallMoney ?? false)
        {
          cibuilder.isMoney(true);
        }

        if (isMoney ?? false)
        {
          cibuilder.isMoney();
        }

        // Applying Max-Size
        if (maxSize ?? false)
        {
          cibuilder.withMaxSize();
        }

        // Applying IDENTITY only on suit types
        if (isIdentitySuit)
        {
          cibuilder.withIdentity(identitySeed ?? 1, identityIncrement ?? 1);
        }
      }
      if (midType == null)
      {
        throw new RepositoryError($"Not IMPLEMENTED for <{ptype.Name}> Yet!", new NotImplementedException());
      }

      // IS_NULLABLE
      if (isNullable ?? false)
      {
        cibuilder.isNullable();
      }
      if (isNotNullable ?? false)
      {
        cibuilder.isNotNullable();
      }

      // IS_PRIMERYKEY
      if (isPrimaryKey ?? false)
      {
        cibuilder.isNotNullable(); // Ooops! I should ignore some settings ;)
        primaryKeys ??= [];
        primaryKeys.Add(columnName);
      }

      // UNIQUE CONSTRAINT
      if (uniqueGroup != null)
      {
        uniques ??= [];
        if (uniques.ContainsKey(uniqueGroup))
        {
          List<string>? value = uniques.GetValueOrDefault(uniqueGroup)?.ToList();
          value?.Add(columnName);
          uniques.Remove(uniqueGroup);
          uniques.Add(uniqueGroup, value?.ToArray() ?? []);
        } else
        {
          uniques.Add(uniqueGroup, [columnName]);
        }
      }

      cibuilder.withDescription(colDesc);

      columns.Add(cibuilder.build());
    }

    // I can't create a Table without any column 8o.
    if (columns.Count < 1)
    {
      return null;
    }

    SqlTableInfo result = new()
    {
        schema = RemoveEscapeCharacters(schema ?? "dbo"),
        name = RemoveEscapeCharacters(tableName ?? type.Name),
        columns = columns,
        primaryKeys = primaryKeys?.ToArray(),
        description = description,
        uniqueConstraints = uniques
    };

    return result;
  }

  public static TableCommand? GenerateTableCommandsBy(SqlTableInfo sti, string? name = null)
  {
    // https://docs.microsoft.com/en-us/sql/t-sql/statements/create-table-transact-sql?view=sql-server-ver15

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

    string schema = RemoveEscapeCharacters(sti.schema!);
    string table = RemoveEscapeCharacters(sti.name!);
    string tableFullName = $"[{schema}].[{table}]";
    string nl = Environment.NewLine;

    List<string> columnStrings = [];
    List<SqlCommand> columnDescriptionCommands = [];
    foreach (SqlColumnInfo sci in sti.columns!)
    {
      string column = RemoveEscapeCharacters(sci.name!);
      string columnString = $"[{column}] {sci.sqlDbTypeText} {(sci.isNullable ? "NULL" : "NOT NULL")}";
      if (sci.defaultValue != null)
      {
        columnString += $"CONSTRAINT [DF_{schema}_{table}_{column}] DEFAULT ${sci.defaultValueText}";
      }
      columnStrings.Add(columnString);
      if (sci.description != null)
      {
        columnDescriptionCommands.Add(new SqlCommand($"EXEC sys.sp_addextendedproperty @name=N'Description', @value=N'{sti.description}', @level0type=N'SCHEMA',@level0name=N'{schema}', @level1type=N'TABLE',@level1name=N'{table}', @level2type=N'COLUMN',@level2name=N'{column}' "));
      }
    }
    string columnsString = columnStrings.Aggregate((a, b) => $"{a},{nl}\t{b}");

    string primaryKeysString = sti.primaryKeys!.Select(spk => $"[{RemoveEscapeCharacters(spk)}]").Aggregate((a, b) => $"{a}, {b}");
    string primaryKeyString = $",{nl}\tCONSTRAINT [PK_{schema}_{table}] PRIMARY KEY CLUSTERED ({primaryKeysString})";

    string uniqueConstraintString = "";
    if (sti.uniqueConstraints != null) {
      foreach (KeyValuePair<string, string[]> unique in sti.uniqueConstraints)
      {
        string uniquesString = unique.Value.Select(u => $"[{RemoveEscapeCharacters(u)}]").Aggregate((a, b) => $"{a}, {b}");
        string key = unique.Key != "" ? $"_{unique.Key}" : "";
        uniqueConstraintString = $",{nl}\tCONSTRAINT [IX_{schema}_{table}{key}] UNIQUE NONCLUSTERED ({uniquesString})";
      }
    }

    SqlCommand createCommand = new($"CREATE TABLE [{tableFullName}] ({nl}\t{columnsString}{primaryKeyString}{uniqueConstraintString}{nl})");
  
    SqlCommand? descriptionCommand = sti.description != null 
      ? new($"EXEC sys.sp_addextendedproperty @name=N'Description', @value=N'{sti.description}', @level0type=N'SCHEMA',@level0name=N'{schema}', @level1type=N'TABLE',@level1name=N'{table}'")
      : null;

    // TODO: add relations

    return new TableCommand { 
      create = createCommand,
      //relations = relationCommands,
      description = descriptionCommand,
      columnDescriptions = columnDescriptionCommands.Count > 0 ? columnDescriptionCommands?.ToArray() : null,
    };
  }

  private static string RemoveEscapeCharacters(string text)
      => text.Replace("'", "").Replace("[", "").Replace("]", "");
}

public sealed class TableCommand
{
  public required SqlCommand create { get; set; }
  public SqlCommand[]? relations { get; set; }
  public SqlCommand[]? defaulValues { get; set; }
  public SqlCommand? description { get; set; }
  public SqlCommand[]? columnDescriptions { get; set; }

  public static readonly SqlCommand[] PreparingCommands = [ new SqlCommand("SET ANSI_NULLS ON"), new SqlCommand("SET QUOTED_IDENTIFIER ON") ];
}