using Microsoft.Data.SqlClient;
using Hamfer.Repository.data;
using Hamfer.Repository.models.Errors;
using Hamfer.Repository.services;

namespace Hamfer.Repository.models;

public abstract class DatabaseUnitOfWorkBase<TEntity> : IDatabaseUnitOfWork<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  protected SqlConnection _connection { get; }
  protected string DatabaseName { get; }
  protected string SchemaName { get; }
  protected string TableName { get; }

  protected Func<SqlDataReader, TEntity> _readWrapper { get; }
  protected ICollection<TEntity>? CurrentRecordset { get; private set; }
  protected IDictionary<Guid, DatabaseContextRecordState> CurrentRecordsetStates { get; private set; }
  protected DatabaseUnitOfWorkQeue<TEntity> TransactionsQueue { get; private set; }

  protected DatabaseUnitOfWorkBase(string connectionString, Func<SqlDataReader, TEntity> readWrapper)
  {
    this._connection = new SqlConnection(connectionString);
    this.DatabaseName = this._connection.Database;
    this._readWrapper = readWrapper;

    var st = IRepositoryEntityHelper.GetSchemaAndTable<TEntity>();
    this.SchemaName = st.Schema ?? "dbo";
    this.TableName = st.Table ?? typeof(TEntity).Name;

    this. CurrentRecordsetStates = new Dictionary<Guid, DatabaseContextRecordState>();
    this.TransactionsQueue = new DatabaseUnitOfWorkQeue<TEntity>();
    this._connection.Open();
    GetDatabaseContext();
  }

  protected abstract IEnumerable<TEntity> ReadFromDeatabse();
  protected abstract void WriteToDeatabse(DatabaseUnitOfWorkQeue<TEntity> transactions);
  public abstract ICollection<TEntity>? GetCurrentRecordSet(IRepositoryPaginationConfiguration<TEntity> config);
  public abstract TEntity? GetCurrentRecord(Func<TEntity, bool> clause);
  protected abstract void LockDatabase();
  protected abstract void UnlockDatabase();
  public abstract void Dispose();

  public virtual void Refresh()
    => GetDatabaseContext();

  public virtual void Commit(bool withRefreshDatabaseContext = true)
  {
    LockDatabase();
    WriteToDeatabse(TransactionsQueue);
    UnlockDatabase();

    if (withRefreshDatabaseContext)
    {
      GetDatabaseContext();
    }
    else
    {
      TransactionsQueue.Clear();
    }
  }

  public virtual void RollBack(bool withRefreshDatabaseContext = true)
  {
    if (withRefreshDatabaseContext)
    {
      GetDatabaseContext();
    }
    else
    {
      TransactionsQueue.Clear();
    }
  }

  public virtual DatabaseContextRecordState GetEntityState(TEntity entity)
    => GetEntityState(entity.id);

  public virtual DatabaseContextRecordState GetEntityState(Guid entityId)
  {
    var state = DatabaseContextRecordState.Unknown;
    if ( CurrentRecordsetStates != null && !CurrentRecordsetStates.TryGetValue(entityId, out state))
    {
      state = DatabaseContextRecordState.Unknown;
    }

    return state;
  }

  public void RegisterDeleted(Guid entityId)
  {
    var state = GetEntityState(entityId);
    switch (state)
    {
      case DatabaseContextRecordState.Unchanged:
      case DatabaseContextRecordState.Modified:
        {
          CurrentRecordsetStates[entityId] = DatabaseContextRecordState.Deleted;

          var entity = CurrentRecordset?.SingleOrDefault(w => w.id.Equals(entityId));
          //Note: do not remove entity from CurrentRecordset
          
          //TODO: check and optimize
          this.AddToQueue(entity, DatabaseContextRecordState.Deleted);
          break;
        }
      case DatabaseContextRecordState.Added:
      case DatabaseContextRecordState.AddedThenModified:
        {
          CurrentRecordsetStates.Remove(entityId);

          var entity = CurrentRecordset?.SingleOrDefault(w => w.id.Equals(entityId));
          if (entity != null)
          {
            CurrentRecordset?.Remove(entity);
          }

          this.AddToQueue(entity, DatabaseContextRecordState.Deleted);
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

  public void RegisterModified(TEntity entity)
  {
      var state = GetEntityState(entity);
      switch (state)
      {
          case DatabaseContextRecordState.Unchanged:
          case DatabaseContextRecordState.Modified:
              {
                  CurrentRecordsetStates[entity.id] = DatabaseContextRecordState.Modified;
                  break;
              }
          case DatabaseContextRecordState.Added:
          case DatabaseContextRecordState.AddedThenModified:
              {
                  CurrentRecordsetStates[entity.id] = DatabaseContextRecordState.AddedThenModified;
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
      var record = CurrentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
      record = entity;
      this.AddToQueue(entity, DatabaseContextRecordState.Modified);
  }

  public void RegisterNew(TEntity entity)
  {
      var state = GetEntityState(entity);
      switch (state)
      {
          case DatabaseContextRecordState.Unchanged:
          case DatabaseContextRecordState.Added:
          case DatabaseContextRecordState.AddedThenModified:
          case DatabaseContextRecordState.Modified:
              throw new RepositoryEntityAlreadyExistsError<Guid>(entity.id);

          case DatabaseContextRecordState.Deleted:
              {
                  var record = CurrentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
                  if (record != null)
                  {
                      CurrentRecordsetStates[entity.id] = DatabaseContextRecordState.Added;
                      record = entity;

                      this.AddToQueue(entity, DatabaseContextRecordState.Added);
                      return;
                  }

                  CurrentRecordsetStates.Remove(entity.id);
                  break;
              }
          case DatabaseContextRecordState.Unknown:
              break;
          default:
              throw new ArgumentOutOfRangeException(nameof(state), state, null);
      }

      CurrentRecordsetStates.Add(entity.id, DatabaseContextRecordState.Added);
      CurrentRecordset?.Add(entity);
      
      this.AddToQueue(entity, DatabaseContextRecordState.Added);
  }

  public void RegisterNewOrModified(TEntity entity)
  {
      var isNew = false;
      var state = GetEntityState(entity);
      switch (state)
      {
          case DatabaseContextRecordState.Unchanged:
          case DatabaseContextRecordState.Modified:
              {
                  CurrentRecordsetStates[entity.id] = DatabaseContextRecordState.Modified;
                  break;
              }
          case DatabaseContextRecordState.Added:
          case DatabaseContextRecordState.AddedThenModified:
              {
                  CurrentRecordsetStates[entity.id] = DatabaseContextRecordState.AddedThenModified;
                  break;
              }
          case DatabaseContextRecordState.Deleted:
              {
                  var record = CurrentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
                  if (record != null)
                  {
                      CurrentRecordsetStates[entity.id] = DatabaseContextRecordState.Added;
                      record = entity;

                      this.AddToQueue(entity, DatabaseContextRecordState.Added);
                      return;
                  }

                  CurrentRecordsetStates.Remove(entity.id);
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
          CurrentRecordsetStates.Add(entity.id, DatabaseContextRecordState.Added);
          CurrentRecordset?.Add(entity);

          this.AddToQueue(entity, DatabaseContextRecordState.Added);
      }
      else
      {
          var record = CurrentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
          record = entity;

          this.AddToQueue(entity, DatabaseContextRecordState.Modified);
      }
  }

  private void GetDatabaseContext()
  {
      var entities = ReadFromDeatabse();

      CurrentRecordset = [];
      CurrentRecordsetStates = new Dictionary<Guid, DatabaseContextRecordState>();
      foreach (var entity in entities)
      {
          CurrentRecordset.Add(entity);
          CurrentRecordsetStates.Add(entity.id, DatabaseContextRecordState.Unchanged);
      }

      TransactionsQueue.Clear();
  }

  private void AddToQueue(TEntity? entity, DatabaseContextRecordState state)
  {
      var transaction = new DatabaseUnitOfWorkTransaction<TEntity>(entity, state);
      TransactionsQueue.Enqueue(transaction);
  }
}