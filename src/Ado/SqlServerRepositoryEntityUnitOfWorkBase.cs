using System.Data;
using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.Data;
using Hamfer.Repository.Duow;
using Hamfer.Repository.Entity;
using Hamfer.Repository.Models;
using Hamfer.Repository.Services;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Ado;

public abstract class SqlServerRepositoryEntityUnitOfWorkBase<TEntity> : RepositoryEntityUnitOfWorkBase<TEntity>
where TEntity : class, IRepositoryEntity<TEntity>
{
  protected SqlTransaction? _transaction { get; private set; }
  protected RepositorySqlCommandHelper<TEntity> _CommandHelper { get; }

  public SqlServerRepositoryEntityUnitOfWorkBase(string connectionString, Func<SqlDataReader, TEntity> readWrapper)
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

  public override ICollection<TEntity>? getCurrentRecordSet(IRepositoryPaginationConfiguration<TEntity>? config)
  {
    bool applyPagination = config?.pageSize != -1;
    // Handling Configuration
    var defaultconfig = new RepositoryPaginationDefaultConfiguration<TEntity>();
    if (config == null)
    {
      config = defaultconfig;
    }
    else
    {
      config.pageSize = config.pageSize < 1 ? defaultconfig.pageSize : config.pageSize;
      config.pageNo = config.pageNo < 1 ? defaultconfig.pageNo : config.pageNo;
      config.where ??= defaultconfig.where;
      if (config.sort == null || config.sort.Length < 1)
      {
        config.sort = defaultconfig.sort;
      }
    }

    // Apply filter
    var result = config.where != null ? base.currentRecordset?.Where(config.where) : base.currentRecordset;

    // Apply sort
    if (config.sort != null) {
      for (int i = config.sort.Length; i > 0; i--)
      {
        var sortItem = config.sort[i - 1];

        if (sortItem.sortOrder == SortOrderBy.Ascending)
        {
          result = result?.OrderBy(x => ReferenceTypeHelper.GetPropertValueByName(x, sortItem.propertyName));
        }

        if (sortItem.sortOrder == SortOrderBy.Descending)
        {
          result = result?.OrderBy(x => ReferenceTypeHelper.GetPropertValueByName(x, sortItem.propertyName));
        }
      }
    }

    // Apply pagination
    if (applyPagination)
    {
      int skipCount = config.pageSize * (config.pageNo - 1);
      result = result?.Skip(skipCount).Take(config.pageSize);
    }

    return result?.ToList();
  }

  public override TEntity? getCurrentRecord(Func<TEntity, bool> clause)
    => base.currentRecordset?.SingleOrDefault(clause);

  protected override async Task<IEnumerable<TEntity>> readFromDatabase()
    => await callReadCommad();

  protected override async Task writeToDatabase(RepositoryEntityUnitOfWorkQeue<TEntity> transactions)
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

        switch (transaction.state)
        {
          case RepositoryEntityRecordState.Unknown:
            // TODO check WHY?
            break;
          case RepositoryEntityRecordState.Unchanged:
            // ignored
            break;
          case RepositoryEntityRecordState.Added:
          case RepositoryEntityRecordState.AddedThenModified:
            await this.callCreateCommandBy(transaction.entity, command);
            break;
          case RepositoryEntityRecordState.Modified:
            await this.callUpdateCommandBy(transaction.entity, command);
            break;
          case RepositoryEntityRecordState.Deleted:
            await this.callDeleteCommandBy(transaction.entity, command);
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

  protected override void lockDatabase()
  {
    // ignored: Not needed yet!
  }

  protected override void unlockDatabase()
  {
    // ignored: Not needed yet!
  }

  // C: Create
  private async Task callCreateCommandBy(TEntity? entity, SqlCommand command)
  {
    // TODO
    var fields = _CommandHelper.generateFieldValuesPattern();
    var values = fields.Replace("]", "").Replace('[', '@');
    command.CommandText = $"INSERT INTO [{base.schemaName}].[{base.tableName}] ({fields}) VALUES ({values.ToLower()});";

    _CommandHelper.applyFieldParameters(command, entity);

    await command.ExecuteNonQueryAsync();
  }

  // R: Read
  private async Task<IEnumerable<TEntity>> callReadCommad()
  {
    SqlCommand command = new($"SELECT * FROM [{base.schemaName}].[{base.tableName}]", _connection);

    ICollection<TEntity> result = [];
    using (SqlDataReader reader = await command.ExecuteReaderAsync())
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
  private async Task callUpdateCommandBy(TEntity? entity, SqlCommand command)
  {
    // TODO
    var fieldAndValuesPattern = _CommandHelper.generateFieldAndValuesPattern([nameof(IRepositoryEntity<>.id)]);
    command.CommandText = $"UPDATE [{base.schemaName}].[{base.tableName}] SET {fieldAndValuesPattern} WHERE Id=@id;";
    
    _CommandHelper.applyFieldParameters(command, entity);

    await command.ExecuteNonQueryAsync();
  }

  // D: Delete
  private async Task callDeleteCommandBy(TEntity? entity, SqlCommand command)
  {
    // TODO
    command.CommandText = $"DELETE FROM [{base.schemaName}].[{base.tableName}] WHERE Id=@id;";
    command.Parameters.AddWithValue("@id", entity?.id);

    await command.ExecuteNonQueryAsync();
  }
}