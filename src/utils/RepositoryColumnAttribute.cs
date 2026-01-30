using Hamfer.Repository.data;

namespace Hamfer.Repository.utils;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class RepositoryColumnAttribute : Attribute
{

  public RepositoryColumnAttribute(SqlColumnParam param, string? value = null)
  {
      Param = param;
      Value = value;
  }

  public SqlColumnParam Param { get; }
  public string? Value { get; }

}
