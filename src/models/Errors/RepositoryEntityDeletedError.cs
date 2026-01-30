using Hamfer.Kernel.Errors;

namespace Hamfer.Repository.models.Errors;

public class RepositoryEntityDeletedError<TId> : RepositoryError
{
  private const string MessagePattern = "An entity with Id='{0}' is already deleted!, you can commit changes to delete it from database.";

  public RepositoryEntityDeletedError(TId myProperty) : base(string.Format(MessagePattern, myProperty))
  {
      MyProperty = myProperty;
  }

  public RepositoryEntityDeletedError(TId myProperty, string message) : base(message)
  {
    MyProperty = myProperty;
  }

  public RepositoryEntityDeletedError(TId myProperty, string message, Exception innerError) : base(message, innerError)
  {
    MyProperty = myProperty;
  }

  public TId MyProperty { get; }
}