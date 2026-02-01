using Hamfer.Repository.Attributes;

namespace Hamfer.Repository.Entity;

public interface IHasModificationInfo: IHasCreationInfo
{
  [RepositoryColumn(SqlColumnParam.Is_Nullable)]
  [RepositoryColumn(SqlColumnParam.Set_FractionalSecondScale_int, "7")]
  public DateTime? modifiedAt { get; set; }

  [RepositoryColumn(SqlColumnParam.Is_Nullable)]
  public Guid? modifiedBy { get; set; }
}