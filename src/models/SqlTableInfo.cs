using Hamfer.Verification.Models;
using Hamfer.Verification.Services;

namespace Hamfer.Repository.Models;

public class SqlTableInfo: VerifiableModelBase<SqlTableInfo>
{
  public string? schema { get; set; }
  public string? name { get; set; }
  public List<SqlColumnInfo>? columns { get; set; }
  public string[]? primaryKeys { get; set; }
  public Dictionary<string, string[]>? uniqueConstraints { get; set; }
  public string? description { get; set; }
  public List<SqlRelationInfo>? relations { get; set; }

  public override void verify(string? name = null)
  {
    LetsVerify.On(this, name)
      .Assert(this.schema, "اسکیمای جدول").NotNullOrEmpty().Match(@"^[A-Za-z][A-Za-z0-9]*$")
      .Assert(this.name, "نام جدول").NotNullOrEmpty().LengthMax(128).Match(@"^[A-Za-z][A-Za-z0-9]*$")
      .Assert(this.columns, "ستون‌ها").NotNullOrEmpty().VerifyAll()
      .Assert(this.primaryKeys, "فهرست کلیدهای اصلی").NotNullOrEmpty().ForEachBy<string>((res, item) =>
        res
          .Assert(item, "کلید اصلی").NotNullOrEmpty().IsMemeberOf(this.columns?.Select(c => c.name))
      )
      .Assert(this.uniqueConstraints, "فهرست ستون‌های یکتا").ForEachBy<KeyValuePair<string, string[]>>((res, item) =>
        res
          .Assert(item.Key, "عنوان گروه ستون‌‌های یکتا").NotNullOrEmpty().Match(@"^[A-Za-z][A-Za-z0-9_]+$")
          .Assert(item.Value, "ستون‌های یکتا").ForEachBy<string>((res, item) => 
            res.Assert(item, "ستون یکتا").NotNullOrEmpty().IsMemeberOf(this.columns?.Select(c => c.name))
          )
      )
      .ThenThrowErrors();
  }
}