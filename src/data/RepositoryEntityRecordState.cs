namespace Hamfer.Repository.Data;

public enum RepositoryEntityRecordState
{
  Unknown = -1,
  Unchanged = 0,
  Added = 1,
  Modified = 2,
  AddedThenModified = 3,
  Deleted = 4
}
