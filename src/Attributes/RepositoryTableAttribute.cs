namespace Hamfer.Repository.Attributes;

[AttributeUsage(AttributeTargets.All)]
public class RepositoryTableAttribute : Attribute
{

  public RepositoryTableAttribute(SqlTableParam param, string value)
  {
    this.param = param;
    this.value = value;
  }

  public SqlTableParam param { get; }
  public string value { get; }

}
