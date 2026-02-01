using Hamfer.Repository.Attributes;

namespace Hamfer.Repository.Entity;

public interface IHasStoringDateTimeInfo
{
  [RepositoryColumn(SqlColumnParam.Is_Not_Nullable)]
  [RepositoryColumn(SqlColumnParam.Set_FractionalSecondScale_int, "7")]
  public DateTime registerTime { get; set; }

  [RepositoryColumn(SqlColumnParam.Is_Not_Nullable)]
  [RepositoryColumn(SqlColumnParam.Set_FractionalSecondScale_int, "7")]
  public DateTime? modificationTime { get; set; }
}