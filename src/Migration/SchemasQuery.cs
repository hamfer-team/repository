using Hamfer.Repository.Ado;
using Hamfer.Repository.Utils;

namespace Hamfer.Repository.Migration
{
  internal sealed class SchemasQuery : SqlQueryBase<string?>
  {
    public SchemasQuery() : base(reader =>
    {
      return reader.Get<string?>("name");
    })
    {
      this.query = "select [name] from [sys].[schemas];";
    }
  }
}