namespace Hamfer.Repository.Duow;

public interface IDatabaseUnitOfWork: IDisposable
{
  Task commit();
  Task rollBack();
}