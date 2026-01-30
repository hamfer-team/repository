using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;

namespace Hamfer.Repository.models;

public class AdoSqlQueryBase<TResult>
  where TResult : class
{
  private const string SelectWord = "SELECT";
  private const string FromWord = "FROM";
  private const string WhereWord = "Where";
  private const string GroupByWord = "GROUP BY";
  private const string OrderByWord = "ORDER BY";
  private const string CteWord = "WITH";

  private string? _cteStatement;
  private string? _selectStatement;
  private string? _fromStatement;
  private string? _whereStatement;
  private string? _groupbyStatement;
  private string? _orderbyStatement;

  public AdoSqlQueryBase(Func<SqlDataReader, TResult> readWrapper)
  {
      ReadWrapper = readWrapper;
  }

  public AdoSqlQueryBase<TResult> AddSelect(string selectStatement)
  {
    _selectStatement = AddMissedStartingWord(selectStatement, SelectWord);
    return this;
  }

  public AdoSqlQueryBase<TResult> AddFrom(string fromStatement)
  {
    _fromStatement = AddMissedStartingWord(fromStatement, FromWord);
    return this;
  }

  public AdoSqlQueryBase<TResult> AddWhere(string whereStatement)
  {
    _whereStatement = AddMissedStartingWord(whereStatement, WhereWord);
    return this;
  }

  public AdoSqlQueryBase<TResult> AddGroupBy(string groupbyStatement)
  {
    _groupbyStatement = AddMissedStartingWord(groupbyStatement, GroupByWord);
    return this;
  }

  public AdoSqlQueryBase<TResult> AddOrderBy(string orderbyStatement)
  {
    _orderbyStatement = AddMissedStartingWord(orderbyStatement, OrderByWord);
    return this;
  }

  public AdoSqlQueryBase<TResult> AddCte(string cteStatement)
  {
    _cteStatement = AddMissedStartingWord(cteStatement, CteWord);
    return this;
  }

  public string Query
  {
    get 
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

      return $"{sb.ToString().Trim()};";
    }
  }

  public Func<SqlDataReader, TResult> ReadWrapper { get; }

  private string AddMissedStartingWord(string statement, string startingWord)
  {
    if (!Regex.IsMatch(statement, @$"\s*{startingWord}", RegexOptions.IgnoreCase))
    {
      statement = $"{startingWord} {statement.Trim()}";
    }

    return statement;
  }
}
