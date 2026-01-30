using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.data;
using Hamfer.Repository.models;
using Hamfer.Repository.utils;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace Hamfer.Repository.services;

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
      switch (att.Param)
      {
        case SqlTableParam.Set_Name:
          tableName = att.Value;
          break;
        case SqlTableParam.Set_Schema:
          schema = att.Value;
          break;
        case SqlTableParam.Is_Ignored:
          ValueTypeHelper.TryParse(att.Value, out ignored);
          break;
        case SqlTableParam.Set_Description:
          description = att.Value;
          break;
        case SqlTableParam.Set_PrimaryKey_commaSeparatedString:
          tablePrimarykey = att.Value;
          break;
        case SqlTableParam.With_UniqueConstraints_commaSeparatedString:
          unique = att.Value;
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
          switch (patt.Param)
          {
              case SqlColumnParam.With_FixedLength:
                  ValueTypeHelper.TryParse(patt.Value, out fixedLength);
                  break;
              case SqlColumnParam.Set_StorageSize_int:
                  ValueTypeHelper.TryParse(patt.Value, out storageSize);
                  break;
              case SqlColumnParam.Is_DateOnly:
                  ValueTypeHelper.TryParse(patt.Value, out dateOnly);
                  break;
              case SqlColumnParam.Set_FractionalSecondScale_int:
                  ValueTypeHelper.TryParse(patt.Value, out fractionalSecondScale);
                  break;
              case SqlColumnParam.Set_Precision_int:
                  ValueTypeHelper.TryParse(patt.Value, out precision);
                  break;
              case SqlColumnParam.Set_Scale_int:
                  ValueTypeHelper.TryParse(patt.Value, out scale);
                  break;
              case SqlColumnParam.With_SupprtsUnicode:
                  ValueTypeHelper.TryParse(patt.Value, out supprtsUnicode);
                  break;
              case SqlColumnParam.With_AutomaticGeneration:
                  ValueTypeHelper.TryParse(patt.Value, out automaticGeneration);
                  break;
              case SqlColumnParam.Is_Money:
                  ValueTypeHelper.TryParse(patt.Value, out isMoney);
                  break;
              case SqlColumnParam.Is_SmallMoney:
                  ValueTypeHelper.TryParse(patt.Value, out isSmallMoney);
                  break;
              case SqlColumnParam.With_MaxSize:
                  ValueTypeHelper.TryParse(patt.Value, out maxSize);
                  break;
              case SqlColumnParam.Is_Identity_With_Seed_int:
                  ValueTypeHelper.TryParse(patt.Value, out identitySeed);
                  break;
              case SqlColumnParam.Is_Identity_With_Increment_int:
                  ValueTypeHelper.TryParse(patt.Value, out identityIncrement);
                  break;
              case SqlColumnParam.Is_PrimaryKey:
                  ValueTypeHelper.TryParse(patt.Value, out isPrimaryKey);
                  break;
              case SqlColumnParam.Is_Nullable:
                  ValueTypeHelper.TryParse(patt.Value, out isNullable);
                  break;
              case SqlColumnParam.Is_Not_Nullable:
                  ValueTypeHelper.TryParse(patt.Value, out isNotNullable);
                  break;
              case SqlColumnParam.Is_Ignored:
                  ValueTypeHelper.TryParse(patt.Value, out ignored);
                  break;
              case SqlColumnParam.With_DefaultValue_string:
                  defaultValue = patt.Value;
                  //ToDo : change to property type
                  break;
              case SqlColumnParam.Set_Name:
                  colName = patt.Value;
                  break;
              case SqlColumnParam.Set_Description:
                  colDesc = patt.Value;
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
      if (SqlColumnInfoBuilder.simpleTypeHelper.TryGetValue(ptype, out MidDataType midType))
      {
        var isIdentitySuit = false;

        switch (midType)
        {
          case MidDataType.BigInt:
            cibuilder.IsInteger(8);
            cibuilder.IsNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.Binary:
            cibuilder.IsBinary(fixedLength ?? true, storageSize ?? 30);
            cibuilder.IsNullable();
            break;
          case MidDataType.Bit:
            cibuilder.IsBoolean();
            cibuilder.IsNotNullable();
            break;
          case MidDataType.DateTime:
            cibuilder.IsDate();
            if (!(dateOnly ?? false))
            {
              cibuilder.WithTime(fractionalSecondScale ?? 3);
            }
            cibuilder.IsNotNullable();
            break;
          case MidDataType.DateTimeOffset:
            cibuilder.IsDate();
            cibuilder.WithTime(fractionalSecondScale ?? 7);
            cibuilder.WithTimeZone();
            cibuilder.IsNotNullable();
            break;
          case MidDataType.Decimal:
            cibuilder.IsDecimal(precision ?? 19, scale ?? 5);
            cibuilder.IsNotNullable();
            break;
          case MidDataType.Float:
            cibuilder.IsFloatingPoint();
            cibuilder.IsNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.Int:
            cibuilder.IsInteger(4);
            cibuilder.IsNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.Numeric20:
            cibuilder.IsDecimal(20, 0);
            cibuilder.IsNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.Real:
            cibuilder.IsFloatingPoint(24);
            cibuilder.IsNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.SmallInt:
            cibuilder.IsInteger(2);
            cibuilder.IsNotNullable();
            isIdentitySuit = true;
            break;
          case MidDataType.String:
            cibuilder.IsString(supprtsUnicode ?? true, fixedLength ?? true, storageSize ?? 30);
            cibuilder.IsNullable();
            break;
          case MidDataType.String1:
            cibuilder.IsString(supprtsUnicode ?? true, false, 1);
            cibuilder.IsNotNullable();
            break;
          case MidDataType.Time:
            cibuilder.WithTime(fractionalSecondScale ?? 7);
            cibuilder.IsNotNullable();
            break;
          case MidDataType.TinyInt:
            cibuilder.IsInteger(1);
            cibuilder.IsNotNullable();
            break;
          case MidDataType.UID:
            cibuilder.IsUID(automaticGeneration ?? false);
            if (automaticGeneration ?? false) {
              cibuilder.IsNotNullable();
            } else {
              cibuilder.IsNullable();
            }
            break;
          default:
            break;
        }

        // Supporint Money Types
        if (isSmallMoney ?? false)
        {
          cibuilder.IsMoney(true);
        }

        if (isMoney ?? false)
        {
          cibuilder.IsMoney();
        }

        // Applying Max-Size
        if (maxSize ?? false)
        {
          cibuilder.WithMaxSize();
        }

        // Applying IDENTITY only on suit types
        if (isIdentitySuit)
        {
          cibuilder.WithIdentity(identitySeed ?? 1, identityIncrement ?? 1);
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
        cibuilder.IsNullable();
      }
      if (isNotNullable ?? false)
      {
        cibuilder.IsNotNullable();
      }

      // IS_PRIMERYKEY
      if (isPrimaryKey ?? false)
      {
        cibuilder.IsNotNullable(); // Ooops! I should ignore some settings ;)
        primarykey = primarykey.AppendWith(columnName);
      }

      cibuilder.WithDescription(colDesc);

      var column = cibuilder.Build();
      columns.Add(column);
    }

    // I can't create a Table without any column 8o
    if (columns.Count < 1)
    {
      return null;
    }

    var result = new SqlTableInfo
    {
        Schema = RemoveEscapeCharacters(schema ?? "dbo"),
        Name = RemoveEscapeCharacters(tableName ?? type.Name),
        Columns = columns,
        PrimaryKey = primarykey ?? tablePrimarykey,
        Description = description,
        UniqueConstraints = [.. uniques]
    };

    return result;
  }

  public static SqlCommand GenerateTableCommandBy(SqlTableInfo sti)
  {
    // https://docs.microsoft.com/en-us/sql/t-sql/statements/create-table-transact-sql?view=sql-server-ver15

    sti.Verify();

    var palinColumnText = ", [{0}] {1} {2}";
    var sb = new StringBuilder();
    foreach (var sci in sti.Columns!)
    {
      sb.Append(string.Format(palinColumnText, sci.Name, sci.SqlDbTypeText, sci.IsNullable ? "NULL" : "NOT NULL"));
    }

    var columnsString = sb.ToString();

    var plainCreateCommand = "CREATE TABLE [{0}].[{1}] ( [Id] UNIQUEIDENTIFIER CONSTRAINT Guid_Default DEFAULT NEWSEQUENTIALID() {2} CONSTRAINT [{0}_{1}_Id_PK] PRIMARY KEY ([Id])); ";
    var sqlcmd = string.Format(plainCreateCommand, sti.Schema, sti.Name, columnsString);

    return new SqlCommand(sqlcmd);
  }

  private static string RemoveEscapeCharacters(string text)
      => text.Replace("'", "").Replace("[", "").Replace("]", "");
}