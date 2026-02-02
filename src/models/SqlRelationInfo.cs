using Hamfer.Verification.Models;

namespace Hamfer.Repository.Models;

public class SqlRelationInfo : VerifiableModelBase<SqlRelationInfo>
{
  public string? parentPropertyName { get; set; }
  public bool parentHasMany { get; set; }
  public bool parentHasOne => !parentHasMany;
  public Type? childType { get; set; }

  public override void verify(string? name = null)
  {
    //TODO
  }
}
