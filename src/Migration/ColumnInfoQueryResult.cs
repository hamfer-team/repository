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

    public ColumnInfoQueryResult(string? name, string? type, string? def, bool? is_nullable, int? max_length, int? precision, int? scale, bool? is_identity, int? seed_value, int? increment_value, string? description)
    {
      this.name = name;
      this.type = type;
      this.def = def;
      this.is_nullable = is_nullable;
      this.max_length = max_length;
      this.precision = precision;
      this.scale = scale;
      this.is_identity = is_identity;
      this.seed_value = seed_value;
      this.increment_value = increment_value;
      this.description = description;
    }
  }
}