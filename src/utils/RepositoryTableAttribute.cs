using Hamfer.Repository.data;

namespace Hamfer.Repository.utils;

[AttributeUsage(AttributeTargets.All)]
public class RepositoryTableAttribute : Attribute
{

  public RepositoryTableAttribute(SqlTableParam param, string value)
  {
    Param = param;
    Value = value;
  }

  public SqlTableParam Param { get; }
  public string Value { get; }

}
