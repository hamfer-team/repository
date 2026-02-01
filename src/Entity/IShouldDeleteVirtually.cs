using Hamfer.Repository.Attributes;

namespace Hamfer.Repository.Entity;

public interface IShouldDeleteVirtually
{
  [RepositoryColumn(SqlColumnParam.Is_Not_Nullable)]
  [RepositoryColumn(SqlColumnParam.With_DefaultValue_string, "false")]
  public bool isDeleted { get; set; }

  [RepositoryColumn(SqlColumnParam.Is_Nullable)]
  [RepositoryColumn(SqlColumnParam.Set_FractionalSecondScale_int, "7")]
  public DateTime? deletedAt { get; set; }

  [RepositoryColumn(SqlColumnParam.Is_Nullable)]
  public Guid? deletedBy { get; set; }
}