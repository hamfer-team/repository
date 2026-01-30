using Microsoft.Data.SqlClient;
using Hamfer.Kernel.Errors;

namespace Hamfer.Repository.models;

public abstract class AdoSqlServerRepositoryBase<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  protected AdoSqlServerDatabaseUnitOfWorkBase<TEntity> DatabaseContext { get; private set; }

  protected AdoSqlServerRepositoryBase(AdoSqlServerDatabaseUnitOfWorkBase<TEntity> databaseContext)
  {
    DatabaseContext = databaseContext;
  }

  protected AdoSqlServerRepositoryBase(string connectionString, Func<SqlDataReader, TEntity> readWrapper)
  {
    DatabaseContext = new AdoSqlServerDatabaseGenericUnitOfWork<TEntity>(connectionString, readWrapper);
  }

  public virtual IEnumerable<TEntity>? FindManyBy(IRepositoryPaginationConfiguration<TEntity>? config = null)
    => DatabaseContext.GetCurrentRecordSet(config);

  public virtual IEnumerable<TEntity>? FindManyBy(Func<TEntity, bool> clause , bool applyPagination = false)
  {
    RepositoryPaginationConfiguration<TEntity> config = new() { WhereClause = clause, PageSize = applyPagination ? 0 : -1 };
    return DatabaseContext.GetCurrentRecordSet(config);
  }

  public virtual TEntity? FindOne(Guid entityId)
    => DatabaseContext.GetCurrentRecord(w => w.id.Equals(entityId));

  public virtual TEntity? FindOneBy(Func<TEntity, bool> clause)
    => DatabaseContext.GetCurrentRecord(clause);

  public virtual void InsertLater(TEntity entity)
  {
    if (entity.id.Equals(Guid.Empty))
    {
      entity.id = Guid.NewGuid();
    }

    DatabaseContext.RegisterNew(entity);
  }

  public virtual void UpdateLater(TEntity entity)
  {
    if (entity.id.Equals(Guid.Empty))
    {
      throw new RepositoryError("شناسه موجودیت مورد نظر معتبر نمی‌باشد!");
    }

    DatabaseContext.RegisterModified(entity);
  }

  public virtual void UpsertLater(TEntity entity)
  {
    if (entity.id.Equals(Guid.Empty))
    {
      entity.id = Guid.NewGuid();
    }

    DatabaseContext.RegisterNewOrModified(entity);
  }

  public virtual void DeleteLater(Guid entityId)
    => DatabaseContext.RegisterDeleted(entityId);

  public virtual void DeleteManyLater(params Guid[] entityIds)
  {
    foreach (Guid entityId in entityIds)
    {
      DatabaseContext.RegisterDeleted(entityId);
    }
  }

  public virtual void Commit()
    => DatabaseContext.Commit();

  public virtual void RollBack()
    => DatabaseContext.RollBack();

  public virtual bool TryDelete(Guid entityId, out RepositoryError? error)
    => TryAction(DeleteLater, entityId, out error);

  public virtual bool TryInsert(TEntity entity, out RepositoryError? error)
      => TryAction(InsertLater, entity, out error);

  public virtual bool TryUpdate(TEntity entity, out RepositoryError? error)
      => TryAction(UpdateLater, entity, out error);

  public virtual bool TryUpsert(TEntity entity, out RepositoryError? error)
      => TryAction(UpsertLater, entity, out error);

  private bool TryAction<TArguman>(Action<TArguman> action, TArguman input, out RepositoryError? error)
  {
    error = null;
    try
    {
      action.Invoke(input);
      Commit();

      return true;
    }
    catch (RepositoryError err)
    {
      error = err;
      return false;
    }
    catch (Exception err)
    {
      error = new RepositoryError("An unhandled exception occured!", err);
      return false;
    }
  }
}