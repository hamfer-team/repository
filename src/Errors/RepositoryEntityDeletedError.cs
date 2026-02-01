using Hamfer.Kernel.Errors;

namespace Hamfer.Repository.Errors;

public class RepositoryEntityDeletedError<TId> : RepositoryError
{
  private const string MESSAGE_PATTERN = "An entity with Id='{0}' is already deleted!, you can commit changes to delete it from database.";

  public RepositoryEntityDeletedError(TId myProperty) : base(string.Format(MESSAGE_PATTERN, myProperty))
  {
      this.myProperty = myProperty;
  }

  public RepositoryEntityDeletedError(TId myProperty, string message) : base(message)
  {
    this.myProperty = myProperty;
  }

  public RepositoryEntityDeletedError(TId myProperty, string message, Exception innerError) : base(message, innerError)
  {
    this.myProperty = myProperty;
  }

  public TId myProperty { get; }
}