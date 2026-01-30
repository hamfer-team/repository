using Hamfer.Kernel.Errors;

namespace Hamfer.Repository.models.Errors;

public class RepositoryEntityAlreadyExistsError<TId> : RepositoryError
{
  private const string MessagePattern = "An entity with Id='{0}' already exists!, you can't add it again.";

  public RepositoryEntityAlreadyExistsError(TId myProperty) : base(string.Format(MessagePattern, myProperty))
  {
    MyProperty = myProperty;
  }

  public RepositoryEntityAlreadyExistsError(TId myProperty, string message) : base(message)
  {
    MyProperty = myProperty;
  }

  public RepositoryEntityAlreadyExistsError(TId myProperty, string message, Exception innerError) : base(message, innerError)
  {
    MyProperty = myProperty;
  }

  public TId MyProperty { get; }
}