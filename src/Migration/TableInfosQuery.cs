using Hamfer.Repository.Ado;
using Hamfer.Repository.Utils;

namespace Hamfer.Repository.Migration
{
  public sealed class TableInfosQuery : SqlQueryBase<TableInfoQueryResult>
  {
    public TableInfosQuery() : base(reader =>
    {
      return new TableInfoQueryResult(
        reader.Get<string?>("schema"),
        reader.Get<string?>("table"),
        reader.Get<string?>("description"),
        reader.Get<string?>("colJson"),
        reader.Get<string?>("ixJson")
      );
    })
    {
      this.query = "select s.[name] [schema], o.[name] [table], e.[value] [description], " +
  	    "(select c.[name], t.[name] [type], object_definition(c.default_object_id) [def], c.is_nullable, c.max_length, c.[precision], c.scale, c.is_identity, ic.seed_value, ic.increment_value, e.[value] [description] " +
  	    "from [sys].[columns] c " +
  		    "join [sys].[types] t on c.user_type_id = t.user_type_id " +
  		    "left join [sys].[identity_columns] ic on c.[object_id] = ic.[object_id] and c.column_id = ic.column_id " +
  		    "left join [sys].[extended_properties] e on e.major_id = c.[object_id] and e.minor_id = c.column_id and e.[name] = 'Description' " +
  	    "where c.[object_id] = o.[object_id] " +
  	    "for json path) colJson, " +
  	    "(select i.[name] [key], i.is_primary_key, c.[name] [column] " +
  	    "from [sys].[indexes] i " +
          "join [sys].[index_columns] ic on i.[object_id] = ic.[object_id] and i.index_id = ic.index_id " +
  		    "join [sys].[columns] c on ic.[object_id] = c.[object_id] and ic.column_id = c.column_id " +
        "where i.is_unique = 1 and i.[object_id] = o.[object_id] " +
  	    "for json path) ixJson " +
        "from [sys].[objects] o " +
  	      "join [sys].[schemas] s on o.[schema_id] = s.[schema_id] " +
  	      "left join [sys].[extended_properties] e on e.major_id = o.[object_id] and e.minor_id = 0 and e.[name] = 'Description' " +
        $"where o.[type] = 'U'";
    }
  }
}