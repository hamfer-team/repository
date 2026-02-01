namespace Hamfer.Repository.Data;

public enum MidDataType
{
  BigInt,         // bigint, Int64, long, UInt32, uint
  Binary,         // binary, byte[], Byte[], image, text, ntext
  Bit,            // bit, bool, Boolean
  DateTime,       // datetime, DateTime
  DateTimeOffset, // dateTimeOffset, DateTimeOffset, datetime(Zone)
  Decimal,        // decimal, Decimal
  Float,          // float, double, Double
  Int,            // int, Int32, ushort, UInt16
  Numeric20,      // numeric(20,0), ulong, UInt64
  Real,           // real, float, Float
  SmallInt,       // smallint, short, Int16, sbyte, SByte
  String,         // string, varchar(1+)
  String1,        // string(1), char, Char
  Time,           // time, TimeSpan
  TinyInt,        // tinyint, byte, Byte
  Uid             // uniqueidentifier, GUID
}