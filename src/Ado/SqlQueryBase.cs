using System.Text;
using System.Text.RegularExpressions;
using Hamfer.Repository.Entity;
using Hamfer.Repository.Services;
using static Hamfer.Repository.Utils.SqlCommandTools;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Ado;

public class SqlQueryBase<TResult>
{
  private const string SELECT_WORD = "SELECT";
  private const string FROM_WORD = "FROM";
  private const string WHERE_WORD = "WHERE";
  private const string GROUP_BY_WORD = "GROUP BY";
  private const string ORDER_BY_WORD = "ORDER BY";
  private const string CTE_WORD = "WITH";

  private string? cteStatement;
  private string? selectStatement;
  private string? fromStatement;
  private string? whereStatement;
  private string? groupbyStatement;
  private string? orderbyStatement;
  private string? queryString;
  private RepositorySqlCommandHelper commandHelper;

  public SqlQueryBase(Func<SqlDataReader, TResult> readWrapper)
  {
    this.readWrapper = readWrapper;
    this.commandHelper = new RepositorySqlCommandHelper(typeof(TResult));
  }

  public SqlQueryBase<TResult> addSelect(string selectStatement)
  {
    this.selectStatement = addMissedStartingWord(selectStatement, SELECT_WORD);
    return this;
  }
  public SqlQueryBase<TResult> addSelect()
  {
    string selectStatement = this.commandHelper.generateFieldPatternInsert();
    return this.addSelect(selectStatement);
  }

  public SqlQueryBase<TResult> addFrom(string fromStatement)
  {
    this.fromStatement = addMissedStartingWord(fromStatement, FROM_WORD);
    return this;
  }
  public SqlQueryBase<TResult> addFrom<TEntity>(string? alias = null)
    where TEntity: class, IRepositoryEntity<TEntity>
  {
    (string? schema, string? table) = RepositoryEntityHelper.GetSchemaAndTable<TEntity>();
    string aliasString = alias != null ? $" AS {alias}" : "";
    string fromStatement = PrepareTableName(schema, table ?? RemovedDataModelPostfix(typeof(TEntity).Name)) + aliasString;
    return this.addFrom(fromStatement);
  }

  public SqlQueryBase<TResult> addWhere(string whereStatement)
  {
    this.whereStatement = addMissedStartingWord(whereStatement, WHERE_WORD);
    return this;
  }

  public SqlQueryBase<TResult> addGroupBy(string groupbyStatement)
  {
    this.groupbyStatement = addMissedStartingWord(groupbyStatement, GROUP_BY_WORD);
    return this;
  }

  public SqlQueryBase<TResult> addOrderBy(string orderbyStatement)
  {
    this.orderbyStatement = addMissedStartingWord(orderbyStatement, ORDER_BY_WORD);
    return this;
  }

  public SqlQueryBase<TResult> addCte(string cteStatement)
  {
    this.cteStatement = addMissedStartingWord(cteStatement, CTE_WORD);
    return this;
  }

  public string query
  {
    get
    {
      if (queryString == null)
      {
        StringBuilder sb = new();

        sb.Append(cteStatement)
          .Append(' ')
          .Append(selectStatement)
          .Append(' ')
          .Append(fromStatement)
          .Append(' ')
          .Append(whereStatement)
          .Append(' ')
          .Append(groupbyStatement)
          .Append(' ')
          .Append(orderbyStatement);

        this.queryString = $"{sb.ToString().Trim()};";
      }
      return this.queryString;
    }

    set => this.queryString = value;
  }

  public Func<SqlDataReader, TResult> readWrapper { get; }

  private string addMissedStartingWord(string statement, string startingWord)
  {
    if (!Regex.IsMatch(statement, $@"^\s*{startingWord}", RegexOptions.IgnoreCase))
    {
      statement = $"{startingWord} {statement.Trim()}";
    }

    return statement;
  }
}
