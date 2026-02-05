using Hamfer.Verification.Models;
using Hamfer.Verification.Services;

namespace Hamfer.Repository.Models;

public class SqlTableInfo: VerifiableModelBase<SqlTableInfo>
{
  public string? schema { get; set; }
  public string? name { get; set; }
  public List<SqlColumnInfo>? columns { get; set; }
  public string[]? primaryKeys { get; set; }
  public string[]? uniqueConstraints { get; set; }
  public string? description { get; set; }
  public List<SqlRelationInfo>? relations { get; set; }

  public override void verify(string? name = null)
  {
    LetsVerify.On(this, name)
      .Assert(this.schema, "اسکیمای جدول").NotNullOrEmpty().Match(@"^[A-Za-z][A-Za-z0-9]*$")
      .Assert(this.name, "نام جدول").NotNullOrEmpty().LengthMax(128).Match(@"^[A-Za-z][A-Za-z0-9]*$")
      .Assert(this.columns, "ستون‌ها").NotNullOrEmpty().VerifyAll()
      .Assert(this.primaryKeys, "فهرست کلیدهای اصلی").NotNullOrEmpty().ForEachBy(item =>
          item.NotNullOrEmpty().IsMemeberOf(this.columns?.Select(c => c.name))
        )
      .Assert(this.uniqueConstraints, "فهرست ستون‌های یکتا").ForEachBy(item =>
        item.CsvRow(out string[]? ucParts)
          .Assert(ucParts, "ستون‌ یکتا").NotNullOrEmpty().IsMemeberOf(this.columns?.Select(c => c.name))
      )
      .ThenThrowErrors();
  }
}