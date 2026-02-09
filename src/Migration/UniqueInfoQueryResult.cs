namespace Hamfer.Repository.Migration
{
  internal sealed class UniqueInfoQueryResult
  {
    public string? key;
    public bool? is_primary_key;
    public string? column;

    public UniqueInfoQueryResult() { }
    public UniqueInfoQueryResult(string key, string column, bool isPrimaryKey)
    {
      this.key = key;
      this.column = column;
      this.is_primary_key = isPrimaryKey;
    }
  }
}