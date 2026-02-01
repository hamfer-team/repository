using Hamfer.Repository.Attributes;

namespace Hamfer.Repository.Entity;

public interface IHasCreationInfo
{
  [RepositoryColumn(SqlColumnParam.Is_Not_Nullable)]
  [RepositoryColumn(SqlColumnParam.Set_FractionalSecondScale_int, "7")]
  public DateTime createdAt { get; set; }

  [RepositoryColumn(SqlColumnParam.Is_Not_Nullable)]
  public Guid createdBy { get; set; }
}