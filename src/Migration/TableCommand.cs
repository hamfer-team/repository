using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Migration;

internal sealed class TableCommand
{
  public string tableName { get; set; }
  public SqlCommand? createSchema { get; set; }
  public SqlCommand? createTable { get; set; }
  public ICollection<SqlCommand> updateColumns { get; set; }
  public ICollection<SqlCommand> updateConstraints { get; set; }
  public ICollection<SqlCommand> dropConstraints { get; set; }
  public ICollection<SqlCommand> createRelations { get; set; }
  public ICollection<SqlCommand> setDescriptions { get; set; }

  public TableCommand(string tableName)
  {
    this.tableName = tableName;
    this.updateColumns = [];
    this.updateConstraints = [];
    this.dropConstraints = [];
    this.createRelations = [];
    this.setDescriptions = [];
  }
  public static readonly SqlCommand[] PreparingCommands = [ new SqlCommand("SET ANSI_NULLS ON;"), new SqlCommand("SET QUOTED_IDENTIFIER ON;") ];
}