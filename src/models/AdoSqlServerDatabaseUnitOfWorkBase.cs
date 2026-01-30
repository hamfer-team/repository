using System.Data;
using Microsoft.Data.SqlClient;
using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.data;
using Hamfer.Repository.services;

namespace Hamfer.Repository.models;

public abstract class AdoSqlServerDatabaseUnitOfWorkBase<TEntity> : DatabaseUnitOfWorkBase<TEntity>
where TEntity : class, IRepositoryEntity<TEntity>
{
  protected SqlTransaction? _transaction { get; private set; }
  protected RepositorySqlCommandHelper<TEntity> _CommandHelper { get; }

  public AdoSqlServerDatabaseUnitOfWorkBase(string connectionString, Func<SqlDataReader, TEntity> readWrapper)
    : base(connectionString, readWrapper)
  {
    this._CommandHelper = new RepositorySqlCommandHelper<TEntity>();
  }

  public override void Dispose()
  {
    if (this._connection.State != ConnectionState.Closed)
    {
      this._connection.Close();
    }

    this._connection?.Dispose();
  }

  public override ICollection<TEntity>? GetCurrentRecordSet(IRepositoryPaginationConfiguration<TEntity>? config)
  {
    bool applyPagination = config?.PageSize != -1;
    // Handling Configuration
    var defaultconfig = new RepositoryPaginationDefaultConfiguration<TEntity>();
    if (config == null)
    {
      config = defaultconfig;
    }
    else
    {
      config.PageSize = config.PageSize < 1 ? defaultconfig.PageSize : config.PageSize;
      config.PageNo = config.PageNo < 1 ? defaultconfig.PageNo : config.PageNo;
      config.WhereClause ??= defaultconfig.WhereClause;
      if (config.Sort == null || config.Sort.Length < 1)
      {
        config.Sort = defaultconfig.Sort;
      }
    }

    // Apply filter
    var result = config.WhereClause != null ? base.CurrentRecordset?.Where(config.WhereClause) : base.CurrentRecordset;

    // Apply sort
    if (config.Sort != null) {
      for (int i = config.Sort.Length; i > 0; i--)
      {
        var sortItem = config.Sort[i - 1];

        if (sortItem.SortOrder == SortOrderBy.Ascending)
        {
          result = result?.OrderBy(x => ReferenceTypeHelper.GetPropertValueByName(x, sortItem.PropertyName));
        }

        if (sortItem.SortOrder == SortOrderBy.Descending)
        {
          result = result?.OrderBy(x => ReferenceTypeHelper.GetPropertValueByName(x, sortItem.PropertyName));
        }
      }
    }

    // Apply pagination
    if (applyPagination)
    {
      int skipCount = config.PageSize * (config.PageNo - 1);
      result = result?.Skip(skipCount).Take(config.PageSize);
    }

    return result?.ToList();
  }

  public override TEntity? GetCurrentRecord(Func<TEntity, bool> clause)
    => base.CurrentRecordset?.SingleOrDefault(clause);

  protected override IEnumerable<TEntity> ReadFromDeatabse()
    => CallReadCommad();

  protected override void WriteToDeatabse(DatabaseUnitOfWorkQeue<TEntity> transactions)
  {
    _transaction = _connection.BeginTransaction(IsolationLevel.Serializable, "UnitOfworkTransaction");
    SqlCommand command = new()
    { 
      Connection = _connection,
      Transaction = _transaction
    };

    try
    {
      var count = transactions.Count;
      for (int i = 0; i < count; i++)
      {
        var transaction = transactions.Dequeue();
        command.Parameters.Clear();

        switch (transaction.State)
        {
          case DatabaseContextRecordState.Unknown:
            // TODO check WHY?
            break;
          case DatabaseContextRecordState.Unchanged:
            // ignored
            break;
          case DatabaseContextRecordState.Added:
          case DatabaseContextRecordState.AddedThenModified:
            this.CallCreateCommandBy(transaction.Entity, command);
            break;
          case DatabaseContextRecordState.Modified:
            this.CallUpdateCommandBy(transaction.Entity, command);
            break;
          case DatabaseContextRecordState.Deleted:
            this.CallDeleteCommandBy(transaction.Entity, command);
            break;
          default:
            break;
        }
      }

      _transaction?.Commit();
    }
    catch (Exception commitException)
    {
      try
      {
        _transaction?.Rollback();
      }
      catch (Exception rollbackException)
      {
        var ex = new AggregateException([commitException, rollbackException]);
        throw new RepositoryError("Rollback after failed commit alse failed!!", ex);
      }

      throw new RepositoryError("Commit SQL Transaction failed!", commitException);
    }
  }

  protected override void LockDatabase()
  {
    // ignored: Not needed yet!
  }

  protected override void UnlockDatabase()
  {
    // ignored: Not needed yet!
  }

  // C: Create
  private void CallCreateCommandBy(TEntity? entity, SqlCommand command)
  {
    // TODO
    var fields = _CommandHelper.GenerateFieldValuesPattern();
    var values = fields.Replace("]", "").Replace('[', '@');
    command.CommandText = $"INSERT INTO [{base.SchemaName}].[{base.TableName}] ({fields}) VALUES ({values.ToLower()});";

    _CommandHelper.ApplyFieldParameters(command, entity);

    command.ExecuteNonQuery();
  }

  // R: Read
  private IEnumerable<TEntity> CallReadCommad()
  {
    SqlCommand command = new($"SELECT * FROM [{base.SchemaName}].[{base.TableName}]", _connection);

    ICollection<TEntity> result = [];
    using (SqlDataReader reader = command.ExecuteReader())
    {
      if (reader.HasRows)
      {
        while (reader.Read())
          {
            TEntity record = _readWrapper(reader);
            result.Add(record);
          }
      }
    }

    return result;
  }

  // U: Update
  private void CallUpdateCommandBy(TEntity? entity, SqlCommand command)
  {
    // TODO
    var fieldAndValuesPattern = _CommandHelper.GenerateFieldAndValuesPattern([nameof(IRepositoryEntity<>.id)]);
    command.CommandText = $"UPDATE [{base.SchemaName}].[{base.TableName}] SET {fieldAndValuesPattern} WHERE Id=@id;";
    
    _CommandHelper.ApplyFieldParameters(command, entity);

    command.ExecuteNonQuery();
  }

  // D: Delete
  private void CallDeleteCommandBy(TEntity? entity, SqlCommand command)
  {
    // TODO
    command.CommandText = $"DELETE FROM [{base.SchemaName}].[{base.TableName}] WHERE Id=@id;";
    command.Parameters.AddWithValue("@id", entity?.id);

    command.ExecuteNonQuery();
  }
}