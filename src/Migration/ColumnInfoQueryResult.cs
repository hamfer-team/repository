namespace Hamfer.Repository.Migration
{
  internal sealed class ColumnInfoQueryResult
  {
    public string? name;
    public string? type;
    public string? def;
    public bool? is_nullable;
    public int? max_length;
    public int? precision;
    public int? scale;
    public bool? is_identity;
    public int? seed_value;
    public int? increment_value;
    public string? description;
  }
}