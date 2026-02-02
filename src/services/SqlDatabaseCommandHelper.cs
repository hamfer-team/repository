using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.Attributes;
using Hamfer.Repository.Data;
using Hamfer.Repository.Entity;
using Hamfer.Repository.Models;
using Hamfer.Verification.Errors;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace Hamfer.Repository.Services;

public static class SqlDatabaseCommandHelper
{
  private static readonly List<string> RepositoryEntityIgnoreColumns = ["Id"];

  public static SqlTableInfo? GatherTableInfoBy(Type type)
  {
    #region Get Table Meta Data
    string? schema = null;
    string? tableName = null;
    bool? ignored = null;
    string? description = null;
    string? unique = null;
    string? tablePrimarykey = null;
    string? primarykey = null;
    List<string> uniques = [];

    var atts = type.GetCustomAttributes<RepositoryTableAttribute>(true);
    foreach (var att in atts)
    {
      switch (att.param)
      {
        case SqlTableParam.Set_Name:
          tableName = att.value;
          break;
        case SqlTableParam.Set_Schema:
          schema = att.value;
          break;
        case SqlTableParam.Is_Ignored:
          ValueTypeHelper.TryParse(att.value, out ignored);
          break;
        case SqlTableParam.Set_Description:
          description = att.value;
          break;
        case SqlTableParam.Set_PrimaryKey_commaSeparatedString:
          tablePrimarykey = att.value;
          break;
        case SqlTableParam.With_UniqueConstraints_commaSeparatedString:
          unique = att.value;
          uniques.Add(unique);
          break;
        default:
          break;
      }
    }
    #endregion

    // ok, Ok, OK! I'll ignore this.
    if (ignored ?? false)
    {
      return null;
    }

    var columns = new List<SqlColumnInfo>();
    foreach (PropertyInfo prop in type.GetProperties())
    {
      // we don't need to add some columns like Id in SqlCommand :D
      if (RepositoryEntityIgnoreColumns.Contains(prop.Name))
      {
        continue;
      }

      var ptype =  prop.PropertyType;

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
      bool? isNullable = null;
      bool? isNotNullable = null;
      dynamic? defaultValue;
      string? colDesc = null;

      var patts = prop.GetCustomAttributes<RepositoryColumnAttribute>(true);
      foreach (var patt in patts)
      {
          switch (patt.param)
          {
              case SqlColumnParam.With_FixedLength:
                  ValueTypeHelper.TryParse(patt.value, out fixedLength);
                  break;
              case SqlColumnParam.Set_StorageSize_int:
                  ValueTypeHelper.TryParse(patt.value, out storageSize);
                  break;
              case SqlColumnParam.Is_DateOnly:
                  ValueTypeHelper.TryParse(patt.value, out dateOnly);
                  break;
              case SqlColumnParam.Set_FractionalSecondScale_int:
                  ValueTypeHelper.TryParse(patt.value, out fractionalSecondScale);
                  break;
              case SqlColumnParam.Set_Precision_int:
                  ValueTypeHelper.TryParse(patt.value, out precision);
                  break;
              case SqlColumnParam.Set_Scale_int:
                  ValueTypeHelper.TryParse(patt.value, out scale);
                  break;
              case SqlColumnParam.With_SupprtsUnicode:
                  ValueTypeHelper.TryParse(patt.value, out supprtsUnicode);
                  break;
              case SqlColumnParam.With_AutomaticGeneration:
                  ValueTypeHelper.TryParse(patt.value, out automaticGeneration);
                  break;
              case SqlColumnParam.Is_Money:
                  ValueTypeHelper.TryParse(patt.value, out isMoney);
                  break;
              case SqlColumnParam.Is_SmallMoney:
                  ValueTypeHelper.TryParse(patt.value, out isSmallMoney);
                  break;
              case SqlColumnParam.With_MaxSize:
                  ValueTypeHelper.TryParse(patt.value, out maxSize);
                  break;
              case SqlColumnParam.Is_Identity_With_Seed_int:
                  ValueTypeHelper.TryParse(patt.value, out identitySeed);
                  break;
              case SqlColumnParam.Is_Identity_With_Increment_int:
                  ValueTypeHelper.TryParse(patt.value, out identityIncrement);
                  break;
              case SqlColumnParam.Is_PrimaryKey:
                  ValueTypeHelper.TryParse(patt.value, out isPrimaryKey);
                  break;
              case SqlColumnParam.Is_Nullable:
                  ValueTypeHelper.TryParse(patt.value, out isNullable);
                  break;
              case SqlColumnParam.Is_Not_Nullable:
                  ValueTypeHelper.TryParse(patt.value, out isNotNullable);
                  break;
              case SqlColumnParam.Is_Ignored:
                  ValueTypeHelper.TryParse(patt.value, out ignored);
                  break;
              case SqlColumnParam.With_DefaultValue_string:
                  defaultValue = patt.value;
                  //ToDo : change to property type
                  break;
              case SqlColumnParam.Set_Name:
                  colName = patt.value;
                  break;
              case SqlColumnParam.Set_Description:
                  colDesc = patt.value;
                  break;
              default:
                  break;
          }
      }
      #endregion

      // Ignored property detected!, So skip to next ;)
      if (ignored ?? false)
      {
        continue;
      }

      //TODO enum ?
      //TODO struct ?

      var columnName = RemoveEscapeCharacters(colName ?? prop.Name);
      var cibuilder = new SqlColumnInfoBuilder(columnName);
      if (SqlColumnInfoBuilder.SimpleTypeHelper.TryGetValue(ptype, out MidDataType midType))
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
          case MidDataType.String1:
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
      else
      {
        if (ReferenceTypeHelper.IsDerivedOfGenericInterface(ptype, typeof(IRepositoryEntity<>)))
        {
          throw new RepositoryError("NotIMPLEMENTED Yet!", new NotImplementedException());
        }
        else
        {
          throw new RepositoryError("NotIMPLEMENTED Yet!", new NotImplementedException());
        }
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
        primarykey = primarykey.AppendWith(columnName);
      }

      cibuilder.withDescription(colDesc);

      var column = cibuilder.build();
      columns.Add(column);
    }

    // I can't create a Table without any column 8o
    if (columns.Count < 1)
    {
      return null;
    }

    var result = new SqlTableInfo
    {
        schema = RemoveEscapeCharacters(schema ?? "dbo"),
        name = RemoveEscapeCharacters(tableName ?? type.Name),
        columns = columns,
        primaryKey = primarykey ?? tablePrimarykey,
        description = description,
        uniqueConstraints = [.. uniques]
    };

    return result;
  }

  public static SqlCommand GenerateTableCommandBy(SqlTableInfo sti, string? name = null)
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

    var palinColumnText = ", [{0}] {1} {2}";
    var sb = new StringBuilder();
    foreach (var sci in sti.columns!)
    {
      sb.Append(string.Format(palinColumnText, sci.name, sci.sqlDbTypeText, sci.isNullable ? "NULL" : "NOT NULL"));
    }

    var columnsString = sb.ToString();

    var plainCreateCommand = "CREATE TABLE [{0}].[{1}] ( [Id] UNIQUEIDENTIFIER CONSTRAINT Guid_Default DEFAULT NEWSEQUENTIALID() {2} CONSTRAINT [{0}_{1}_Id_PK] PRIMARY KEY ([Id])); ";
    var sqlcmd = string.Format(plainCreateCommand, sti.schema, sti.name, columnsString);

    return new SqlCommand(sqlcmd);
  }

  private static string RemoveEscapeCharacters(string text)
      => text.Replace("'", "").Replace("[", "").Replace("]", "");
}