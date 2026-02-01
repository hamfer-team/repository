namespace Hamfer.Repository.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class RepositoryColumnAttribute : Attribute
{

  public RepositoryColumnAttribute(SqlColumnParam param, string? value = null)
  {
      this.param = param;
      this.value = value;
  }

  public SqlColumnParam param { get; }
  public string? value { get; }

}
