using Hamfer.Repository.Data;
using Hamfer.Repository.Entity;
using Hamfer.Repository.Errors;
using Hamfer.Repository.Models;
using Hamfer.Repository.Services;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Duow;

public abstract class RepositoryEntityUnitOfWorkBase<TEntity> : IRepositoryEntityUnitOfWork<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  protected SqlConnection connection { get; }
  protected string databaseName { get; }
  protected string schemaName { get; }
  protected string tableName { get; }

  protected Func<SqlDataReader, TEntity> readWrapper { get; }
  protected ICollection<TEntity>? currentRecordset { get; private set; }
  protected IDictionary<Guid, RepositoryEntityRecordState> currentRecordsetStates { get; private set; }
  protected RepositoryEntityUnitOfWorkQeue<TEntity> transactionsQueue { get; private set; }

  protected RepositoryEntityUnitOfWorkBase(string connectionString, Func<SqlDataReader, TEntity> readWrapper)
  {
    this.connection = new SqlConnection(connectionString);
    this.databaseName = this.connection.Database;
    this.readWrapper = readWrapper;

    (string? schema, string? table) = RepositoryEntityHelper.GetSchemaAndTable<TEntity>(true);
    this.schemaName = schema!;
    this.tableName = table!;
    // Console.WriteLine($"💛 {this.schemaName}.{this.tableName}");

    this. currentRecordsetStates = new Dictionary<Guid, RepositoryEntityRecordState>();
    this.transactionsQueue = new RepositoryEntityUnitOfWorkQeue<TEntity>();
    this.connection.Open();
    getDatabaseContext().Wait();
  }

  protected abstract Task<IEnumerable<TEntity>> readFromDatabase();
  protected abstract Task writeToDatabase(RepositoryEntityUnitOfWorkQeue<TEntity> transactions);
  public abstract ICollection<TEntity>? getCurrentRecordSet(IRepositoryPaginationConfiguration<TEntity> config);
  public abstract TEntity? getCurrentRecord(Func<TEntity, bool> clause);
  protected abstract void lockDatabase();
  protected abstract void unlockDatabase();
  public abstract void Dispose();

  public virtual async Task refresh()
    => await getDatabaseContext();

  public virtual async Task commit(bool withRefreshDatabaseContext = true)
  {
    lockDatabase();
    await writeToDatabase(transactionsQueue);
    unlockDatabase();

    if (withRefreshDatabaseContext)
    {
      await getDatabaseContext();
    }
    else
    {
      transactionsQueue.Clear();
    }
  }

  public virtual async Task rollBack(bool withRefreshDatabaseContext = true)
  {
    if (withRefreshDatabaseContext)
    {
      await getDatabaseContext();
    }
    else
    {
      transactionsQueue.Clear();
    }
  }

  public virtual RepositoryEntityRecordState getEntityState(TEntity entity)
    => getEntityState(entity.id);

  public virtual RepositoryEntityRecordState getEntityState(Guid entityId)
  {
    RepositoryEntityRecordState state = RepositoryEntityRecordState.Unknown;
    if ( currentRecordsetStates != null && !currentRecordsetStates.TryGetValue(entityId, out state))
    {
      state = RepositoryEntityRecordState.Unknown;
    }

    return state;
  }

  public void registerDeleted(Guid entityId)
  {
    var state = getEntityState(entityId);
    switch (state)
    {
      case RepositoryEntityRecordState.Unchanged:
      case RepositoryEntityRecordState.Modified:
        {
          currentRecordsetStates[entityId] = RepositoryEntityRecordState.Deleted;

          var entity = currentRecordset?.SingleOrDefault(w => w.id.Equals(entityId));
          //Note: do not remove entity from CurrentRecordset
          
          //TODO: check and optimize
          this.addToQueue(entity, RepositoryEntityRecordState.Deleted);
          break;
        }
      case RepositoryEntityRecordState.Added:
      case RepositoryEntityRecordState.AddedThenModified:
        {
          currentRecordsetStates.Remove(entityId);

          var entity = currentRecordset?.SingleOrDefault(w => w.id.Equals(entityId));
          if (entity != null)
          {
            currentRecordset?.Remove(entity);
          }

          this.addToQueue(entity, RepositoryEntityRecordState.Deleted);
          break;
        }
      case RepositoryEntityRecordState.Deleted:
          throw new RepositoryEntityDeletedError<Guid>(entityId);
      case RepositoryEntityRecordState.Unknown:
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
          case RepositoryEntityRecordState.Unchanged:
          case RepositoryEntityRecordState.Modified:
              {
                  currentRecordsetStates[entity.id] = RepositoryEntityRecordState.Modified;
                  break;
              }
          case RepositoryEntityRecordState.Added:
          case RepositoryEntityRecordState.AddedThenModified:
              {
                  currentRecordsetStates[entity.id] = RepositoryEntityRecordState.AddedThenModified;
                  break;
              }
          case RepositoryEntityRecordState.Deleted:
              throw new RepositoryEntityDeletedError<Guid>(entity.id);
          case RepositoryEntityRecordState.Unknown:
              throw new RepositoryEntityNotFoundError<Guid>(entity.id);
          default:
              throw new ArgumentOutOfRangeException(nameof(state), state, null);
      }

      //TODO: check
      var record = currentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
      record = entity;
      this.addToQueue(entity, RepositoryEntityRecordState.Modified);
  }

  public void registerNew(TEntity entity)
  {
      var state = getEntityState(entity);
      switch (state)
      {
          case RepositoryEntityRecordState.Unchanged:
          case RepositoryEntityRecordState.Added:
          case RepositoryEntityRecordState.AddedThenModified:
          case RepositoryEntityRecordState.Modified:
              throw new RepositoryEntityAlreadyExistsError<Guid>(entity.id);

          case RepositoryEntityRecordState.Deleted:
              {
                  var record = currentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
                  if (record != null)
                  {
                      currentRecordsetStates[entity.id] = RepositoryEntityRecordState.Added;
                      record = entity;

                      this.addToQueue(entity, RepositoryEntityRecordState.Added);
                      return;
                  }

                  currentRecordsetStates.Remove(entity.id);
                  break;
              }
          case RepositoryEntityRecordState.Unknown:
              break;
          default:
              throw new ArgumentOutOfRangeException(nameof(state), state, null);
      }

      currentRecordsetStates.Add(entity.id, RepositoryEntityRecordState.Added);
      currentRecordset?.Add(entity);
      
      this.addToQueue(entity, RepositoryEntityRecordState.Added);
  }

  public void registerNewOrModified(TEntity entity)
  {
      var isNew = false;
      var state = getEntityState(entity);
      switch (state)
      {
          case RepositoryEntityRecordState.Unchanged:
          case RepositoryEntityRecordState.Modified:
              {
                  currentRecordsetStates[entity.id] = RepositoryEntityRecordState.Modified;
                  break;
              }
          case RepositoryEntityRecordState.Added:
          case RepositoryEntityRecordState.AddedThenModified:
              {
                  currentRecordsetStates[entity.id] = RepositoryEntityRecordState.AddedThenModified;
                  break;
              }
          case RepositoryEntityRecordState.Deleted:
              {
                  var record = currentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
                  if (record != null)
                  {
                      currentRecordsetStates[entity.id] = RepositoryEntityRecordState.Added;
                      record = entity;

                      this.addToQueue(entity, RepositoryEntityRecordState.Added);
                      return;
                  }

                  currentRecordsetStates.Remove(entity.id);
                  isNew = true;

                  break;
              }
          case RepositoryEntityRecordState.Unknown:
              isNew = true;
              break;
          default:
              throw new ArgumentOutOfRangeException(nameof(state), state, null);
      }
      if (isNew)
      {
          currentRecordsetStates.Add(entity.id, RepositoryEntityRecordState.Added);
          currentRecordset?.Add(entity);

          this.addToQueue(entity, RepositoryEntityRecordState.Added);
      }
      else
      {
          var record = currentRecordset?.SingleOrDefault(x => x.id.Equals(entity.id));
          record = entity;

          this.addToQueue(entity, RepositoryEntityRecordState.Modified);
      }
  }

  private async Task getDatabaseContext()
  {
      IEnumerable<TEntity> entities = await readFromDatabase();

      currentRecordset = [];
      currentRecordsetStates = new Dictionary<Guid, RepositoryEntityRecordState>();
      foreach (TEntity entity in entities)
      {
          currentRecordset.Add(entity);
          currentRecordsetStates.Add(entity.id, RepositoryEntityRecordState.Unchanged);
      }

      transactionsQueue.Clear();
  }

  private void addToQueue(TEntity? entity, RepositoryEntityRecordState state)
  {
      var transaction = new RepositoryEntityUnitOfWorkTransaction<TEntity>(entity, state);
      transactionsQueue.Enqueue(transaction);
  }
}