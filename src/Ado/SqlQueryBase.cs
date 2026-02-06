using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Ado;

public class SqlQueryBase<TResult>
  where TResult : class
{
  private const string SELECT_WORD = "SELECT";
  private const string FROM_WORD = "FROM";
  private const string WHERE_WORD = "Where";
  private const string GROUP_BY_WORD = "GROUP BY";
  private const string ORDER_BY_WORD = "ORDER BY";
  private const string CTE_WORD = "WITH";

  private string? _cteStatement;
  private string? _selectStatement;
  private string? _fromStatement;
  private string? _whereStatement;
  private string? _groupbyStatement;
  private string? _orderbyStatement;
  private string? _queryString;

  public SqlQueryBase(Func<SqlDataReader, TResult> readWrapper)
  {
      this.readWrapper = readWrapper;
  }

  public SqlQueryBase<TResult> addSelect(string selectStatement)
  {
    _selectStatement = addMissedStartingWord(selectStatement, SELECT_WORD);
    return this;
  }

  public SqlQueryBase<TResult> addFrom(string fromStatement)
  {
    _fromStatement = addMissedStartingWord(fromStatement, FROM_WORD);
    return this;
  }

  public SqlQueryBase<TResult> addWhere(string whereStatement)
  {
    _whereStatement = addMissedStartingWord(whereStatement, WHERE_WORD);
    return this;
  }

  public SqlQueryBase<TResult> addGroupBy(string groupbyStatement)
  {
    _groupbyStatement = addMissedStartingWord(groupbyStatement, GROUP_BY_WORD);
    return this;
  }

  public SqlQueryBase<TResult> addOrderBy(string orderbyStatement)
  {
    _orderbyStatement = addMissedStartingWord(orderbyStatement, ORDER_BY_WORD);
    return this;
  }

  public SqlQueryBase<TResult> addCte(string cteStatement)
  {
    _cteStatement = addMissedStartingWord(cteStatement, CTE_WORD);
    return this;
  }

  public string query
  {
    get
    {
      if (_queryString == null)
      {
        StringBuilder sb = new();

        sb.Append(_cteStatement)
          .Append(' ')
          .Append(_selectStatement)
          .Append(' ')
          .Append(_fromStatement)
          .Append(' ')
          .Append(_whereStatement)
          .Append(' ')
          .Append(_groupbyStatement)
          .Append(' ')
          .Append(_orderbyStatement);

        this._queryString = $"{sb.ToString().Trim()};";
      }
      return this._queryString;
    }

    set => this._queryString = value;
  }

  public Func<SqlDataReader, TResult> readWrapper { get; }

  private string addMissedStartingWord(string statement, string startingWord)
  {
    if (!Regex.IsMatch(statement, @$"\s*{startingWord}", RegexOptions.IgnoreCase))
    {
      statement = $"{startingWord} {statement.Trim()}";
    }

    return statement;
  }
}
