using Hamfer.Kernel.Errors;

namespace Hamfer.Repository.models.Errors;

public class RepositorySqlColumnBuilderError : RepositoryError
{
  public RepositorySqlColumnBuilderError(string columnName, string message, Exception? innerError = null) : base(message, innerError)
  {
    ColumnName = columnName;
  }

  public string ColumnName { get; }
}