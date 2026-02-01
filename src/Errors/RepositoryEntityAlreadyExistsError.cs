using Hamfer.Kernel.Errors;

namespace Hamfer.Repository.Errors;

public class RepositoryEntityAlreadyExistsError<TId> : RepositoryError
{
  private const string MESSAGE_PATTERN = "An entity with Id='{0}' already exists!, you can't add it again.";

  public RepositoryEntityAlreadyExistsError(TId myProperty) : base(string.Format(MESSAGE_PATTERN, myProperty))
  {
    this.myProperty = myProperty;
  }

  public RepositoryEntityAlreadyExistsError(TId myProperty, string message) : base(message)
  {
    this.myProperty = myProperty;
  }

  public RepositoryEntityAlreadyExistsError(TId myProperty, string message, Exception innerError) : base(message, innerError)
  {
    this.myProperty = myProperty;
  }

  public TId myProperty { get; }
}