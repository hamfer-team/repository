using System.Data;
using Hamfer.Verification.Models;
using Hamfer.Verification.Services;

namespace Hamfer.Repository.Models;

public class SqlColumnInfo : VerifiableModelBase<SqlColumnInfo>
{
  public string? name { get; set; }
  //public int OrdinalPosition { get; set; }
  
  public dynamic? defaultValue { get; set; }
  public bool isNullable { get; set; }
  public SqlDbType? dbType { get; set; }
  public int? charMaxLength { get; set; }
  public int? numericPrecision { get; set; }
  public int? numericScale { get; set; }
  public int? timeScale { get; set; }
  //public string CharSetName { get; set; }
  //public string CollationName { get; set; }
  public int? identitySeed { get; set; }
  public int? identityIncrement { get; set; }

  public string? description { get; set; }
  public string? sqlDbTypeText { get; set; }

  public string? defaultValueText { get; set; }

  public override void verify(string? name = null)
  {
    LetsVerify.On(this, name)
      .Assert(this.name, "نام ستون").NotNullOrEmpty().LengthMax(128).Match(@"^[A-Za-z][A-Za-z0-9]*$")
      .ThenThrowErrors();
  }
}
