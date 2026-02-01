using Hamfer.Kernel.Errors;

namespace Hamfer.Repository.Errors;

public class RepositoryEntityNotFoundError<TId> : RepositoryError
{
  private const string MESSAGE_PATTERN = "An entity with Id='{0}' not found!";

  public RepositoryEntityNotFoundError(TId myProperty) : base(string.Format(MESSAGE_PATTERN, myProperty))
  {
    this.myProperty = myProperty;
  }

  public RepositoryEntityNotFoundError(TId myProperty, string message) : base(message)
  {
    this.myProperty = myProperty;
  }

  public RepositoryEntityNotFoundError(TId myProperty, string message, Exception innerError) : base(message, innerError)
  {
    this.myProperty = myProperty;
  }

  public TId myProperty { get; }
}