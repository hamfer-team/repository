using Microsoft.Data.SqlClient;
using Hamfer.Repository.Data;
using Hamfer.Repository.Entity;
using Hamfer.Repository.Models;
using Hamfer.Repository.Services;
using Hamfer.Repository.Errors;

namespace Hamfer.Repository.Duow;

public abstract class DatabaseUnitOfWorkBase<TEntity> : IDatabaseUnitOfWork<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  protected SqlConnection _connection { get; }
  protected string databaseName { get; }
  protected string schemaName { get; }
  protected string tableName { get; }

  protected Func<SqlDataReader, TEntity> _readWrapper { get; }
  protected ICollection<TEntity>? currentRecordset { get; private set; }
  protected IDictionary<Guid, DatabaseContextRecordState> currentRecordsetStates { get; private set; }
  protected DatabaseUnitOfWorkQeue<TEntity> transactionsQueue { get; private set; }

  protected DatabaseUnitOfWorkBase(string connectionString, Func<SqlDataReader, TEntity> readWrapper)
  {
    this._connection = new SqlConnection(connectionString);
    this.databaseName = this._connection.Database;
    this._readWrapper = readWrapper;

    var (schema, table) = RepositoryEntityHelper.GetSchemaAndTable<TEntity>();
    this.schemaName = schema ?? "dbo";
    this.tableName = table ?? typeof(TEntity).Name;

    this. currentRecordsetStates = new Dictionary<Guid, DatabaseContextRecordState>();
    this.transactionsQueue = new DatabaseUnitOfWorkQeue<TEntity>();
    this._connection.Open();
    getDatabaseContext();
  }

  protected abstract IEnumerable<TEntity> readFromDatabase();
  protected abstract void writeToDatabase(DatabaseUnitOfWorkQeue<TEntity> transactions);
  public abstract ICollection<TEntity>? getCurrentRecordSet(IRepositoryPaginationConfiguration<TEntity> config);
  public abstract TEntity? getCurrentRecord(Func<TEntity, bool> clause);
  protected abstract void lockDatabase();
  protected abstract void unlockDatabase();
  public abstract void Dispose();

  public virtual void refresh()
    => getDatabaseContext();

  public virtual void commit(bool withRefreshDatabaseContext = true)
  {
    lockDatabase();
    writeToDatabase(transactionsQueue);
    unlockDatabase();

    if (withRefreshDatabaseContext)
    {
      getDatabaseContext();
    }
    else
    {
      transactionsQueue.Clear();
    }
  }

  public virtual void rollBack(bool withRefreshDatabaseContext = true)
  {
    if (withRefreshDatabaseContext)
    {
      getDatabaseContext();
    }
    else
    {
      transactionsQueue.Clear();
    }
  }

  public virtual DatabaseContextRecordState getEntityState(TEntity entity)
    => getEntityState(entity.id);

  public virtual DatabaseContextRecordState getEntityState(Guid entityId)
  {
    var state = DatabaseContextRecordState.Unknown;
    if ( currentRecordsetStates != null && !currentRecordsetStates.TryGetValue(entityId, out state))
    {
      state = DatabaseContextRecordState.Unknown;
    }

    return state;
  }

  public void registerDeleted(Guid entityId)
  {
    var state = getEntityState(entityId);
    switch (state)
    {
      case DatabaseContextRecordState.Unchanged:
      case DatabaseContextRecordState.Modified:
        {
          currentRecordsetStates[entityId] = DatabaseContextRecordState.Deleted;

          var entity = currentRecordset?.SingleOrDefault(w => w.id.Equals(entityId));
          //Note: do not remove entity from CurrentRecordset
          
          //TODO: check and optimize
          this.addToQueue(entity, DatabaseContextRecordState.Deleted);
          break;
        }
      case DatabaseContextRecordState.Added:
      case DatabaseContextRecordState.AddedThenModified:
        {
          currentRecordsetStates.Remove(entityId);

          var entity = currentRecordset?.SingleOrDefault(w => w.id.Equals(entityId));
          if (entity != null)
          {
            currentRecordset?.Remove(entity);
          }

          this.addToQueue(entity, DatabaseContextRecordState.Deleted);
          break;
        }
      case DatabaseContextRecordState.Deleted:
          throw new RepositoryEntityDeletedError<Guid>(entityId);
      case DatabaseContextRecordState.Unknown:
          throw new RepositoryEntityNotFoundError<Guid>(entityId);
      default:
          throw new ArgumentOutOfRangeException(nameof(state), state, null);
    }
  }

  public void registerModified(TEntity entity)
  {
      var state = getEntityState(entity);
      switch (state)
      {
          case DatabaseContextRecordState.Unchanged:
          case DatabaseContextRecordState.Modified:
              {
                  currentRecordsetStates[entity.id] = DatabaseContextRecordState.Modified;
                  break;
              }
          case DatabaseContextRecordState.Added:
          case DatabaseContextRecordState.AddedThenModified:
              {
                  currentRecordsetStates[entity.id] = DatabaseContextRecordState.AddedThenModified;
                  break;
              }
          case DatabaseContextRecordState.Deleted:
              throw new RepositoryEntityDeletedError<Guid>(entity.id);
          case DatabaseContextRecordState.Unknown:
              throw new RepositoryEntityNotFoundError<Guid>(entity.id);
          default:
              throw new ArgumentOutOfRangeException(nameof(state), state, null);
      }

      //TODO: check
      var record = currentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
      record = entity;
      this.addToQueue(entity, DatabaseContextRecordState.Modified);
  }

  public void registerNew(TEntity entity)
  {
      var state = getEntityState(entity);
      switch (state)
      {
          case DatabaseContextRecordState.Unchanged:
          case DatabaseContextRecordState.Added:
          case DatabaseContextRecordState.AddedThenModified:
          case DatabaseContextRecordState.Modified:
              throw new RepositoryEntityAlreadyExistsError<Guid>(entity.id);

          case DatabaseContextRecordState.Deleted:
              {
                  var record = currentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
                  if (record != null)
                  {
                      currentRecordsetStates[entity.id] = DatabaseContextRecordState.Added;
                      record = entity;

                      this.addToQueue(entity, DatabaseContextRecordState.Added);
                      return;
                  }

                  currentRecordsetStates.Remove(entity.id);
                  break;
              }
          case DatabaseContextRecordState.Unknown:
              break;
          default:
              throw new ArgumentOutOfRangeException(nameof(state), state, null);
      }

      currentRecordsetStates.Add(entity.id, DatabaseContextRecordState.Added);
      currentRecordset?.Add(entity);
      
      this.addToQueue(entity, DatabaseContextRecordState.Added);
  }

  public void registerNewOrModified(TEntity entity)
  {
      var isNew = false;
      var state = getEntityState(entity);
      switch (state)
      {
          case DatabaseContextRecordState.Unchanged:
          case DatabaseContextRecordState.Modified:
              {
                  currentRecordsetStates[entity.id] = DatabaseContextRecordState.Modified;
                  break;
              }
          case DatabaseContextRecordState.Added:
          case DatabaseContextRecordState.AddedThenModified:
              {
                  currentRecordsetStates[entity.id] = DatabaseContextRecordState.AddedThenModified;
                  break;
              }
          case DatabaseContextRecordState.Deleted:
              {
                  var record = currentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
                  if (record != null)
                  {
                      currentRecordsetStates[entity.id] = DatabaseContextRecordState.Added;
                      record = entity;

                      this.addToQueue(entity, DatabaseContextRecordState.Added);
                      return;
                  }

                  currentRecordsetStates.Remove(entity.id);
                  isNew = true;

                  break;
              }
          case DatabaseContextRecordState.Unknown:
              isNew = true;
              break;
          default:
              throw new ArgumentOutOfRangeException(nameof(state), state, null);
      }
      if (isNew)
      {
          currentRecordsetStates.Add(entity.id, DatabaseContextRecordState.Added);
          currentRecordset?.Add(entity);

          this.addToQueue(entity, DatabaseContextRecordState.Added);
      }
      else
      {
          var record = currentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
          record = entity;

          this.addToQueue(entity, DatabaseContextRecordState.Modified);
      }
  }

  private void getDatabaseContext()
  {
      var entities = readFromDatabase();

      currentRecordset = [];
      currentRecordsetStates = new Dictionary<Guid, DatabaseContextRecordState>();
      foreach (var entity in entities)
      {
          currentRecordset.Add(entity);
          currentRecordsetStates.Add(entity.id, DatabaseContextRecordState.Unchanged);
      }

      transactionsQueue.Clear();
  }

  private void addToQueue(TEntity? entity, DatabaseContextRecordState state)
  {
      var transaction = new DatabaseUnitOfWorkTransaction<TEntity>(entity, state);
      transactionsQueue.Enqueue(transaction);
  }
}