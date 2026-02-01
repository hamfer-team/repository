using Microsoft.Data.SqlClient;
using Hamfer.Kernel.Errors;
using Hamfer.Repository.Entity;
using Hamfer.Repository.Models;

namespace Hamfer.Repository.Ado;

public abstract class SqlServerRepositoryBase<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  protected SqlServerDatabaseUnitOfWorkBase<TEntity> databaseContext { get; private set; }

  protected SqlServerRepositoryBase(SqlServerDatabaseUnitOfWorkBase<TEntity> databaseContext)
  {
    this.databaseContext = databaseContext;
  }

  protected SqlServerRepositoryBase(string connectionString, Func<SqlDataReader, TEntity> readWrapper)
  {
    databaseContext = new SqlServerDatabaseGenericUnitOfWork<TEntity>(connectionString, readWrapper);
  }

  public virtual IEnumerable<TEntity>? findManyBy(IRepositoryPaginationConfiguration<TEntity>? config = null)
    => databaseContext.getCurrentRecordSet(config);

  public virtual IEnumerable<TEntity>? findManyBy(Func<TEntity, bool> clause , bool applyPagination = false)
  {
    RepositoryPaginationConfiguration<TEntity> config = new() { where = clause, pageSize = applyPagination ? 0 : -1 };
    return databaseContext.getCurrentRecordSet(config);
  }

  public virtual TEntity? findOne(Guid entityId)
    => databaseContext.getCurrentRecord(w => w.id.Equals(entityId));

  public virtual TEntity? findOneBy(Func<TEntity, bool> clause)
    => databaseContext.getCurrentRecord(clause);

  public virtual void insertLater(TEntity entity)
  {
    if (entity.id.Equals(Guid.Empty))
    {
      entity.id = Guid.NewGuid();
    }

    databaseContext.registerNew(entity);
  }

  public virtual void updateLater(TEntity entity)
  {
    if (entity.id.Equals(Guid.Empty))
    {
      throw new RepositoryError("شناسه موجودیت مورد نظر معتبر نمی‌باشد!");
    }

    databaseContext.registerModified(entity);
  }

  public virtual void upsertLater(TEntity entity)
  {
    if (entity.id.Equals(Guid.Empty))
    {
      entity.id = Guid.NewGuid();
    }

    databaseContext.registerNewOrModified(entity);
  }

  public virtual void deleteLater(Guid entityId)
    => databaseContext.registerDeleted(entityId);

  public virtual void deleteManyLater(params Guid[] entityIds)
  {
    foreach (Guid entityId in entityIds)
    {
      databaseContext.registerDeleted(entityId);
    }
  }

  public virtual void Commit()
    => databaseContext.commit();

  public virtual void rollBack()
    => databaseContext.rollBack();

  public virtual bool tryDelete(Guid entityId, out RepositoryError? error)
    => tryAction(deleteLater, entityId, out error);

  public virtual bool tryInsert(TEntity entity, out RepositoryError? error)
      => tryAction(insertLater, entity, out error);

  public virtual bool tryUpdate(TEntity entity, out RepositoryError? error)
      => tryAction(updateLater, entity, out error);

  public virtual bool tryUpsert(TEntity entity, out RepositoryError? error)
      => tryAction(upsertLater, entity, out error);

  private bool tryAction<TArguman>(Action<TArguman> action, TArguman input, out RepositoryError? error)
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