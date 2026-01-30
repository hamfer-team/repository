using System.Data;
using Hamfer.Verification.Models;
using Hamfer.Verification.Services;

namespace Hamfer.Repository.models;

public class SqlColumnInfo : VerifiableModelBase<SqlColumnInfo>
{
  public string? Name { get; set; }
  //public int OrdinalPosition { get; set; }
  public dynamic? Default { get; set; }
  public bool IsNullable { get; set; }
  public SqlDbType? DbType { get; set; }
  public int? CharMaxLength { get; set; }
  public int? NumericPrecision { get; set; }
  public int? NumericScale { get; set; }
  public int? TimeScale { get; set; }
  //public string CharSetName { get; set; }
  //public string CollationName { get; set; }
  public string? Description { get; set; }
  public int? IdentitySeed { get; set; }
  public int? IdentityIncrement { get; set; }

  public string? SqlDbTypeText { get; set; }

  public override void Verify()
  {
    LetsVerify.On(this)
      .For(this.Name, "نام ستون").AssertNotNullOrEmpty().AssertMaxLength(128).AssertMatch(@"^[A-Za-z][A-Za-z0-9]*$")
      //.For(this.CharMaxLength, "حداکثر اندازه").WhenIsNotNull(). TODO
      .ThenThrowErrors();
  }
}
