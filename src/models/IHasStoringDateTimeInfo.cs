using Hamfer.Repository.data;
using Hamfer.Repository.utils;

namespace Hamfer.Repository.models;

public interface IHasStoringDateTimeInfo
{
  [RepositoryColumn(SqlColumnParam.Is_Not_Nullable)]
  [RepositoryColumn(SqlColumnParam.Set_FractionalSecondScale_int, "7")]
  public DateTime RegisterTime { get; set; }

  [RepositoryColumn(SqlColumnParam.Is_Not_Nullable)]
  [RepositoryColumn(SqlColumnParam.Set_FractionalSecondScale_int, "7")]
  public DateTime? ModificationTime { get; set; }
}