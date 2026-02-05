using System.Data;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.Data;
using Hamfer.Repository.Errors;
using Hamfer.Repository.Models;

namespace Hamfer.Repository.Services;

public class SqlColumnInfoBuilder
{
  public static readonly Dictionary<Type, MidDataType> SimpleTypeHelper = new()
  {
    { typeof(ulong), MidDataType.Numeric20 },
    { typeof(long), MidDataType.BigInt },
    { typeof(uint), MidDataType.BigInt },
    { typeof(int), MidDataType.Int },
    { typeof(ushort), MidDataType.Int },
    { typeof(short), MidDataType.SmallInt },
    { typeof(sbyte), MidDataType.SmallInt },
    { typeof(byte), MidDataType.TinyInt },
    { typeof(bool), MidDataType.Bit },
    { typeof(decimal), MidDataType.Decimal },   //TODO: check
    { typeof(float), MidDataType.Real },        //TODO: check
    { typeof(double), MidDataType.Float },      //TODO: check
    { typeof(string), MidDataType.String },
    { typeof(char), MidDataType.String1 },
    { typeof(DateTime), MidDataType.DateTime }, //TODO: check
    { typeof(DateTimeOffset), MidDataType.DateTimeOffset },
    { typeof(byte[]), MidDataType.Binary },
    { typeof(TimeSpan), MidDataType.Time },     //TODO: check
    { typeof(Guid), MidDataType.Uid }
  };

  private readonly string name;
  private SqlDbType? dbType;
  private int? charMaxLength;
  private int? numericPrecision;
  private int? numericScale;
  private int? timeScale;
  private bool nullable;
  private string? defaultValue;
  private string? description;
  private int? identitySeed;
  private int? identityIncrement;
  private string? sqlDbTypeText;
  private string? defaultValueText;

  public SqlColumnInfoBuilder(string name)
  {
    this.name = name;
    this.dbType = null;
    this.charMaxLength = null;
    this.nullable = true;
    this.sqlDbTypeText = null;
    this.defaultValueText = null;
  }

  public SqlColumnInfoBuilder isNullable()
  {
    this.nullable = true;
    return this;
  }

  public SqlColumnInfoBuilder isNotNullable()
  {
    this.nullable = false;
    return this;
  }

  public SqlColumnInfoBuilder withDefaultValue(string defaultValue)
  {
    this.defaultValue = defaultValue;

    if (this.dbType != null)
    {
      this.defaultValueText = SqlCommandTextHelper.getValueText(defaultValue, this.dbType.Value);
    }

    return this;
  }

  public SqlColumnInfoBuilder isString(bool supprtsUnicode = true, bool variableLength = true, int storageSize = 30)
  {
    if (storageSize < 1)
    {
      throw new RepositorySqlColumnBuilderError(this.name, $"حداقل طول فیلد متنی {this.name} باید یک حرف باشد!");
    }

    if (supprtsUnicode)
    {
      if (storageSize > 4000)
      {
        throw new RepositorySqlColumnBuilderError(this.name, $"حداکثر فضای ذخیره‌سازی فیلد متنی {this.name} می‌تواند 4000 بایت باشد!");
      }

      this.dbType = variableLength ? SqlDbType.NVarChar : SqlDbType.NChar;
      this.charMaxLength = storageSize;
    }
    else
    {
      if (storageSize > 8000)
      {
        throw new RepositorySqlColumnBuilderError(name, $"حداکثر فضای ذخیره‌سازی فیلد متنی {name} می تواند 8000 بایت باشد!");
      }

      this.dbType = variableLength ? SqlDbType.VarChar : SqlDbType.Char;
      this.charMaxLength = storageSize;
    }

    this.sqlDbTypeText = $"[{this.dbType?.ToString().ToLowerInvariant()}]({storageSize})";
    this.defaultValueText = SqlCommandTextHelper.getValueText(this.defaultValue, this.dbType!.Value);
    return this;
  }

  public SqlColumnInfoBuilder isBinary(bool variableLength = true, int storageSize = 30)
  {
    if (storageSize < 1 || storageSize > 8000)
    {
      throw new RepositorySqlColumnBuilderError(name, $"مقدار فضای ذخیره‌سازی فیلد {name} باید بین 1 تا 8000 بایت باشد!");
    }

    this.dbType = variableLength ? SqlDbType.VarBinary : SqlDbType.Binary;
    this.charMaxLength = storageSize; // TODO: 2x

    this.sqlDbTypeText = $"[{this.dbType?.ToString().ToLowerInvariant()}]({storageSize})";
    this.defaultValueText = SqlCommandTextHelper.getValueText(this.defaultValue, this.dbType!.Value);
    return this;
  }

  public SqlColumnInfoBuilder isDecimal(int precision = 18, int scale = 0)
  {
    if (precision < 1 || precision > 38)
    {
      throw new RepositorySqlColumnBuilderError(this.name, $"تعداد کل ارقام {this.name} باید بین 1 تا 38 رقم باشد!");
    }

    double p = 5 + 4 * Math.Floor(precision / 9.6);
    if (scale < 0 || scale > p)
    {
      throw new RepositorySqlColumnBuilderError(this.name, $"تعداد ارقام اعشاری {this.name} باید بین 0 تا {p} رقم باشد!");
    }

    this.dbType = SqlDbType.Decimal;
    this.numericPrecision = precision;
    this.numericScale = scale;

    this.sqlDbTypeText = $"[{this.dbType?.ToString().ToLowerInvariant()}]({this.numericPrecision},{this.numericScale})";
    this.defaultValueText = SqlCommandTextHelper.getValueText(this.defaultValue, this.dbType!.Value);
    return this;
  }

  public SqlColumnInfoBuilder isFloatingPoint(int mantissaBits = 53)
  {
    if (mantissaBits < 1 || mantissaBits > 53)
    {
      throw new RepositorySqlColumnBuilderError(name, $"تعداد بیت مانتیس {name} باید بین 1 تا 53 بیت باشد!");
    }

    this.dbType = mantissaBits < 25 ? SqlDbType.Real : SqlDbType.Float;
    this.defaultValueText = SqlCommandTextHelper.getValueText(this.defaultValue, this.dbType!.Value);
    return this;
  }

  public SqlColumnInfoBuilder isInteger(int storageSize = 4)
  {
    if (storageSize < 1 || storageSize > 8)
    {
      throw new RepositorySqlColumnBuilderError(name, $"مقدار فضای ذخیره‌سازی فیلد {name} باید بین 1 تا 8 بایت باشد!");
    }

    if (storageSize > 4) {
     this.dbType = SqlDbType.BigInt;
    } else if (storageSize > 2)
    {
     this.dbType = SqlDbType.Int;
    } else if (storageSize > 1)
    {
     this.dbType = SqlDbType.SmallInt;
    } else {
     this.dbType = SqlDbType.TinyInt;
    }

    this.defaultValueText = SqlCommandTextHelper.getValueText(this.defaultValue, this.dbType!.Value);
    return this;
  }

  public SqlColumnInfoBuilder isBoolean()
  {
    this.dbType = SqlDbType.Bit;
    this.defaultValueText = SqlCommandTextHelper.getValueText(this.defaultValue, this.dbType!.Value);
    return this;
  }

  public SqlColumnInfoBuilder isMoney(bool isSmall = false)
  {
    this.dbType = isSmall ? SqlDbType.SmallMoney : SqlDbType.Money;
    decimal? defaultValue = TypeHelper.ChangeTypeTo<decimal?>(this.defaultValue);
    this.defaultValueText = this.defaultValue != null ? $"({defaultValue})" : "({0})";
    this.defaultValueType = valueTexterType.numeric;
    return this;
  }

  public SqlColumnInfoBuilder isDate()
  {
    this.dbType = SqlDbType.Date;
    this.defaultValueText = SqlCommandTextHelper.getValueText(this.defaultValue, this.dbType!.Value);
    return this;
  }

  public SqlColumnInfoBuilder isUid(bool automaticGeneration = false)
  {
    this.dbType = automaticGeneration ? SqlDbType.Timestamp : SqlDbType.UniqueIdentifier;
    this.defaultValueText = SqlCommandTextHelper.getValueText(this.defaultValue, this.dbType!.Value);
    return this;
  }

  public SqlColumnInfoBuilder isObject()
  {
    this.dbType = SqlDbType.Json;
    this.defaultValueText = SqlCommandTextHelper.getValueText(this.defaultValue, this.dbType!.Value);
    return this;
  }

  public SqlColumnInfoBuilder withTime(int fractionalSecondScale = 3)
  {
    if (this.dbType != null && this.dbType.ToString()!.Contains("Date", StringComparison.InvariantCultureIgnoreCase))
    {
      if (fractionalSecondScale < 0 || fractionalSecondScale > 7)
      {
        throw new RepositorySqlColumnBuilderError(this.name, $"تعداد رقم خردشده ثانیه در {this.name} باید بین 0 تا 7 رقم باشد!");
      }

      if (fractionalSecondScale > 3)
      {
        this.dbType = SqlDbType.DateTime2;
        this.timeScale = fractionalSecondScale;
      }
      else if (fractionalSecondScale > 1)
      {
        this.dbType = SqlDbType.DateTime;
      }
      else
      {
        this.dbType = SqlDbType.SmallDateTime;
      }

      this.sqlDbTypeText ??= this.dbType?.ToString().ToLowerInvariant();
      this.timeScale = fractionalSecondScale;
      this.defaultValueText = SqlCommandTextHelper.getValueText(this.defaultValue, this.dbType!.Value);
    }
    else
    {
      if (fractionalSecondScale < 1 || fractionalSecondScale > 7)
      {
        throw new RepositorySqlColumnBuilderError(name, $"تعداد رقم خردشده ثانیه در {name} باید بین 1 تا 7 رقم باشد!");
      }

      this.dbType = SqlDbType.Time;
      this.timeScale = fractionalSecondScale;
      this.sqlDbTypeText = $"[{this.dbType?.ToString().ToLowerInvariant()}]({timeScale})";
    }
    
    return this;
  }

  public SqlColumnInfoBuilder withTimeZone()
  {
    this.dbType = SqlDbType.DateTimeOffset;
    this.defaultValueText = SqlCommandTextHelper.getValueText(this.defaultValue, this.dbType!.Value);
    return this;
  }

  public SqlColumnInfoBuilder withMaxSize()
  {
    bool hadIdentity = sqlDbTypeText?.IndexOf("IDENTITY", StringComparison.InvariantCultureIgnoreCase) >= 0;
    sqlDbTypeText = null;

    switch (this.dbType)
    {
      case SqlDbType.BigInt:
      case SqlDbType.Int:
      case SqlDbType.SmallInt:
      case SqlDbType.TinyInt:
       this.dbType = SqlDbType.BigInt;
        break;

      case SqlDbType.Binary:
      case SqlDbType.Char:
      case SqlDbType.VarBinary:
      case SqlDbType.VarChar:
        this.charMaxLength = 8000;
        this.sqlDbTypeText = $"[{this.dbType?.ToString().ToLowerInvariant()}](max)";
        break;

      case SqlDbType.NChar:
      case SqlDbType.NVarChar:
        this.charMaxLength = 4000;
        this.sqlDbTypeText = $"[{this.dbType?.ToString().ToLowerInvariant()}](max)";
        break;

      case SqlDbType.Decimal:
        this.numericPrecision = 38;
        this.numericScale = 10;
        this.sqlDbTypeText = $"[{this.dbType?.ToString().ToLowerInvariant()}]({numericPrecision},{numericScale})";
        break;

      case SqlDbType.Float:
      case SqlDbType.Real:
       this.dbType = SqlDbType.Float;
        break;

      case SqlDbType.SmallMoney:
      case SqlDbType.Money:
       this.dbType = SqlDbType.Money;
        break;

      case SqlDbType.SmallDateTime:
      case SqlDbType.DateTime:
      case SqlDbType.DateTime2:
       this.dbType = SqlDbType.DateTime2;
        this.timeScale = 7;
        this.sqlDbTypeText = $"[{this.dbType?.ToString().ToLowerInvariant()}]({timeScale})";
        break;

      case SqlDbType.Time:
        this.timeScale = 7;
        this.sqlDbTypeText = $"[{this.dbType?.ToString().ToLowerInvariant()}]({timeScale})";
        break;

      case SqlDbType.Bit:
      case SqlDbType.Date:
      case SqlDbType.DateTimeOffset:
      case SqlDbType.Structured: // Is a specifying structured data contained in table-valued parameters
      case SqlDbType.Timestamp:
      case SqlDbType.Udt: // Is a User-Defined Type
      case SqlDbType.UniqueIdentifier:
      case SqlDbType.Variant:
      case SqlDbType.Xml: // Is a run-time Type from string ;)
        // Ignored
        break;

      default:
        // Deprecated types ???
        break;
    }

    this.sqlDbTypeText = (this.sqlDbTypeText ?? this.dbType?.ToString().ToLowerInvariant()) +
      (hadIdentity ? $" IDENTITY({this.identitySeed},{this.identityIncrement}) " : "");
    return this;
  }

  public SqlColumnInfoBuilder withDescription(string? description)
  {
    this.description = description;
    return this;
  }

  public SqlColumnInfoBuilder withIdentity(int seed = 1, int increment = 1)
  {
    this.identitySeed = seed;
    this.identityIncrement = increment;

    this.sqlDbTypeText = $"{this.sqlDbTypeText} IDENTITY({this.identitySeed},{this.identityIncrement})";
    return this;
  }

  public SqlColumnInfo build()
  {
    return new SqlColumnInfo
    {
      name = this.name,
      dbType = this.dbType,
      charMaxLength = this.charMaxLength,
      numericPrecision = this.numericPrecision,
      numericScale = this.numericScale,
      timeScale = this.timeScale,
      isNullable = this.nullable,
      defaultValue = this.defaultValue,
      description = this.description,
      identitySeed = this.identitySeed,
      identityIncrement = this.identityIncrement,
      sqlDbTypeText = this.sqlDbTypeText ?? this.dbType?.ToString().ToLowerInvariant(),
    };
  }
}