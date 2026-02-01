using Hamfer.Kernel.Errors;

namespace Hamfer.Repository.Errors;

public class RepositorySqlColumnBuilderError : RepositoryError
{
  public RepositorySqlColumnBuilderError(string columnName, string message, Exception? innerError = null) : base(message, innerError)
  {
    this.columnName = columnName;
  }

  public string columnName { get; }
}