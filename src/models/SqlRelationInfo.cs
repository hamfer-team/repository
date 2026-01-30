using Hamfer.Verification.Models;

namespace Hamfer.Repository.models;

public class SqlRelationInfo : VerifiableModelBase<SqlRelationInfo>
{
  public string? ParentPropertyName { get; set; }
  public bool ParentHasMany { get; set; }
  public bool ParentHasOne => !ParentHasMany;
  public Type? ChildType { get; set; }

  public override void Verify()
  {
    //TODO
  }
}
