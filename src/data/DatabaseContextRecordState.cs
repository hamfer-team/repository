namespace Hamfer.Repository.data;

public enum DatabaseContextRecordState
{
  Unknown = -1,
  Unchanged = 0,
  Added = 1,
  Modified = 2,
  AddedThenModified = 3,
  Deleted = 4
}
