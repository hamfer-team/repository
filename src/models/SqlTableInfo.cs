using Hamfer.Verification.Models;
using Hamfer.Verification.Services;

namespace Hamfer.Repository.models;

public class SqlTableInfo: VerifiableModelBase<SqlTableInfo>
{
  public string? Schema { get; set; }
  public string? Name { get; set; }
  public List<SqlColumnInfo>? Columns { get; set; }
  public string? PrimaryKey { get; set; }
  public string[]? UniqueConstraints { get; set; }
  public string? Description { get; set; }
  public List<SqlRelationInfo>? Relations { get; set; }

  public override void Verify()
  {
    LetsVerify.On(this)
      .For(this.Schema, "اسکیمای جدول").AssertNotNullOrEmpty().AssertMatch(@"^[A-Za-z][A-Za-z0-9]*$")
      .For(this.Name, "نام جدول").AssertNotNullOrEmpty().AssertMaxLength(128).AssertMatch(@"^[A-Za-z][A-Za-z0-9]*$")
      .For(this.Columns, "ستون‌ها").AssertNotNullOrEmpty().VerifyAllItems()
      .For(this.PrimaryKey, "کلید اصلی").AssertNotNullOrEmpty()
        .AssertCsvRow(out string[]? pkParts)
          .For(pkParts, "فهرست ستون‌های کلید اصلی").AssertNotNullOrEmpty().AssertIsMemeberOf(this.Columns?.Select(c => c.Name))
      .For(this.UniqueConstraints, "ستون‌های یکتا").AssertForEachBy(item =>
        item.AssertCsvRow(out string[]? ucParts)
          .For(ucParts, "فهرست ستون‌های یکتا").AssertNotNullOrEmpty().AssertIsMemeberOf(this.Columns?.Select(c => c.Name))
      )
      .ThenThrowErrors();
  }
}