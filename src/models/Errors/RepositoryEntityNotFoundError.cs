using Hamfer.Kernel.Errors;

namespace Hamfer.Repository.models.Errors;

public class RepositoryEntityNotFoundError<TId> : RepositoryError
{
  private const string MessagePattern = "An entity with Id='{0}' not found!";

  public RepositoryEntityNotFoundError(TId myProperty) : base(string.Format(MessagePattern, myProperty))
  {
    MyProperty = myProperty;
  }

  public RepositoryEntityNotFoundError(TId myProperty, string message) : base(message)
  {
    MyProperty = myProperty;
  }

  public RepositoryEntityNotFoundError(TId myProperty, string message, Exception innerError) : base(message, innerError)
  {
    MyProperty = myProperty;
  }

  public TId MyProperty { get; }
}