using Hamfer.Kernel.Errors;

namespace Hamfer.Repository.Errors;

public class RepositoryConnectionError : RepositoryError
{
  public RepositoryConnectionError(string connectionString, string message, Exception? innerError = null) : base(message, innerError)
  {
    this.connectionString = connectionString;
  }

  public string connectionString { get; }
}