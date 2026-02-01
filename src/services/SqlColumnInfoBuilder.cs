using Hamfer.Repository.Data;
using Hamfer.Repository.Errors;
using Hamfer.Repository.Models;
using System.Data;

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

  private string _name;
  private SqlDbType _dbType;
  private int? _charMaxLength;
  private int? _numericPrecision;
  private int? _numericScale;
  private int? _timeScale;
  private bool _isNullable;
  private dynamic? _defaultValue;
  private string? _description;
  private int? _identitySeed;
  private int? _identityIncrement;
  private string? _sqlDbTypeText;

  public SqlColumnInfoBuilder(string name)
  {
    this._name = name;
    this._dbType = SqlDbType.NVarChar;
    this._charMaxLength = 25;
    this._isNullable = true;
    this._sqlDbTypeText = null;
  }

  public SqlColumnInfoBuilder isNullable()
  {
    this._isNullable = true;
    return this;
  }

  public SqlColumnInfoBuilder isNotNullable()
  {
    this._isNullable = false;
    return this;
  }

  public SqlColumnInfoBuilder withDefaultValue(dynamic defaultValue)
  {
    this._defaultValue = defaultValue;
    return this;
  }

  public SqlColumnInfoBuilder isString(bool supprtsUnicode = true, bool variableLength = true, int storageSize = 30)
  {
    if (storageSize < 1)
    {
      throw new RepositorySqlColumnBuilderError(_name, $"حداقل طول فیلد متنی {_name} باید یک حرف باشد!");
    }

    if (supprtsUnicode)
    {
      if (storageSize > 4000)
      {
        throw new RepositorySqlColumnBuilderError(_name, $"حداکثر فضای ذخیره‌سازی فیلد متنی {_name} می‌تواند 4000 بایت باشد!");
      }

      _dbType = variableLength ? SqlDbType.NVarChar : SqlDbType.NChar;
      _charMaxLength = storageSize;
    }
    else
    {
      if (storageSize > 8000)
      {
        throw new RepositorySqlColumnBuilderError(_name, $"حداکثر فضای ذخیره‌سازی فیلد متنی {_name} می تواند 8000 بایت باشد!");
      }

      _dbType = variableLength ? SqlDbType.VarChar : SqlDbType.Char;
      _charMaxLength = storageSize;
    }

    _sqlDbTypeText = $"{_dbType}({storageSize})";
    return this;
  }

  public SqlColumnInfoBuilder isBinary(bool variableLength = true, int storageSize = 30)
  {
    if (storageSize < 1 || storageSize > 8000)
    {
      throw new RepositorySqlColumnBuilderError(_name, $"مقدار فضای ذخیره‌سازی فیلد {_name} باید بین 1 تا 8000 بایت باشد!");
    }

    _dbType = variableLength ? SqlDbType.VarBinary : SqlDbType.Binary;
    _charMaxLength = storageSize; // TODO: 2x

    _sqlDbTypeText = $"{_dbType}({storageSize})";
    return this;
  }

  public SqlColumnInfoBuilder isDecimal(int precision = 18, int scale = 0)
  {
    if (precision < 1 || precision > 38)
    {
      throw new RepositorySqlColumnBuilderError(_name, $"تعداد کل ارقام {_name} باید بین 1 تا 38 رقم باشد!");
    }

    var p = 5 + 4 * Math.Floor(precision / 9.6);
    if (scale < 0 || scale > p)
    {
      throw new RepositorySqlColumnBuilderError(_name, $"تعداد ارقام اعشاری {_name} باید بین 0 تا {p} رقم باشد!");
    }

    _dbType = SqlDbType.Decimal;
    _numericPrecision = precision;
    _numericScale = scale;

    _sqlDbTypeText = $"{_dbType}({_numericPrecision},{_numericScale})";
    return this;
  }

  public SqlColumnInfoBuilder isFloatingPoint(int mantissaBits = 53)
  {
    if (mantissaBits < 1 || mantissaBits > 53)
    {
      throw new RepositorySqlColumnBuilderError(_name, $"تعداد بیت مانتیس {_name} باید بین 1 تا 53 بیت باشد!");
    }

    _dbType = mantissaBits < 25 ? SqlDbType.Real : SqlDbType.Float;

    return this;
  }

  public SqlColumnInfoBuilder isInteger(int storageSize = 4)
  {
    if (storageSize < 1 || storageSize > 8)
    {
      throw new RepositorySqlColumnBuilderError(_name, $"مقدار فضای ذخیره‌سازی فیلد {_name} باید بین 1 تا 8 بایت باشد!");
    }

    if (storageSize > 4) {
      _dbType = SqlDbType.BigInt;
    } else if (storageSize > 2)
    {
      _dbType = SqlDbType.Int;
    } else if (storageSize > 1)
    {
      _dbType = SqlDbType.SmallInt;
    } else {
      _dbType = SqlDbType.TinyInt;
    }

    return this;
  }

  public SqlColumnInfoBuilder isBoolean()
  {
    _dbType = SqlDbType.Bit;
    return this;
  }

  public SqlColumnInfoBuilder isMoney(bool isSmall = false)
  {
    _dbType = isSmall ? SqlDbType.SmallMoney : SqlDbType.Money;
    return this;
  }

  public SqlColumnInfoBuilder isDate()
  {
    _dbType = SqlDbType.Date;
    return this;
  }

  public SqlColumnInfoBuilder isUid(bool automaticGeneration = false)
  {
    _dbType = automaticGeneration ? SqlDbType.Timestamp : SqlDbType.UniqueIdentifier;

    return this;
  }

  public SqlColumnInfoBuilder isObject()
  {
    _dbType = SqlDbType.Variant;
    return this;
  }

  public SqlColumnInfoBuilder withTime(int fractionalSecondScale = 3)
  {
    // TODO: check
    if (_dbType.ToString().Contains("Date", StringComparison.InvariantCultureIgnoreCase))
    {
      if (fractionalSecondScale < 0 || fractionalSecondScale > 7)
      {
        throw new RepositorySqlColumnBuilderError(_name, $"تعداد رقم خردشده ثانیه در {_name} باید بین 0 تا 7 رقم باشد!");
      }

      if (fractionalSecondScale > 3)
      {
        _dbType = SqlDbType.DateTime2;
        _timeScale = fractionalSecondScale;
        _sqlDbTypeText = $"{_dbType}({_timeScale})";
      }
      else if (fractionalSecondScale > 0)
      {
        _dbType = SqlDbType.DateTime;
      }
      else
      {
        _dbType = SqlDbType.SmallDateTime;
      }

      _sqlDbTypeText ??= _dbType.ToString();
      _timeScale = fractionalSecondScale;
    }
    else
    {
      if (fractionalSecondScale < 1 || fractionalSecondScale > 7)
      {
        throw new RepositorySqlColumnBuilderError(_name, $"تعداد رقم خردشده ثانیه در {_name} باید بین 1 تا 7 رقم باشد!");
      }

      _dbType = SqlDbType.Time;
      _timeScale = fractionalSecondScale;
      _sqlDbTypeText = $"{_dbType}({_timeScale})";
    }
    
    return this;
  }

  public SqlColumnInfoBuilder withTimeZone()
  {
    _dbType = SqlDbType.DateTimeOffset;
    return this;
  }

  public SqlColumnInfoBuilder withMaxSize()
  {
    var hadIdentity = _sqlDbTypeText?.IndexOf("IDENTITY") >= 0;
    _sqlDbTypeText = null;

    switch (_dbType)
    {
      case SqlDbType.BigInt:
      case SqlDbType.Int:
      case SqlDbType.SmallInt:
      case SqlDbType.TinyInt:
        _dbType = SqlDbType.BigInt;
        break;

      case SqlDbType.Binary:
      case SqlDbType.Char:
      case SqlDbType.VarBinary:
      case SqlDbType.VarChar:
        _charMaxLength = 8000;
        _sqlDbTypeText = $"{_dbType}(MAX)";
        break;

      case SqlDbType.NChar:
      case SqlDbType.NVarChar:
        _charMaxLength = 4000;
        _sqlDbTypeText = $"{_dbType}(MAX)";
        break;

      case SqlDbType.Decimal:
        _numericPrecision = 38;
        _numericScale = 10;
        _sqlDbTypeText = $"{_dbType}({_numericPrecision},{_numericScale})";
        break;

      case SqlDbType.Float:
      case SqlDbType.Real:
        _dbType = SqlDbType.Float;
        break;

      case SqlDbType.SmallMoney:
      case SqlDbType.Money:
        _dbType = SqlDbType.Money;
        break;

      case SqlDbType.SmallDateTime:
      case SqlDbType.DateTime:
      case SqlDbType.DateTime2:
        _dbType = SqlDbType.DateTime2;
        _timeScale = 7;
        _sqlDbTypeText = $"{_dbType}({_timeScale})";
        break;

      case SqlDbType.Time:
        _timeScale = 7;
        _sqlDbTypeText = $"{_dbType}({_timeScale})";
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

    _sqlDbTypeText = (_sqlDbTypeText ?? _dbType.ToString()) +
      (hadIdentity ? $" IDENTITY({_identitySeed},{_identityIncrement}) " : "");
    return this;
  }

  public SqlColumnInfoBuilder withDescription(string? description)
  {
    _description = description;
    return this;
  }

  public SqlColumnInfoBuilder withIdentity(int seed = 1, int increment = 1)
  {
    _identitySeed = seed;
    _identityIncrement = increment;

    _sqlDbTypeText = $"{_sqlDbTypeText} IDENTITY({_identitySeed},{_identityIncrement})";
    return this;
  }

  public SqlColumnInfo build()
  {
    return new SqlColumnInfo
    {
      name = this._name,
      dbType = this._dbType,
      charMaxLength = this._charMaxLength,
      numericPrecision = this._numericPrecision,
      numericScale = this._numericScale,
      timeScale = this._timeScale,
      isNullable = this._isNullable,
      defaultValue = this._defaultValue,
      description = this._description,
      identitySeed = this._identitySeed,
      identityIncrement = this._identityIncrement,
      sqlDbTypeText = this._sqlDbTypeText ?? this._dbType.ToString(),
    };
  }
}