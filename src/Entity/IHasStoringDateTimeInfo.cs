namespace Hamfer.Repository.Entity;

public interface IHasStoringDateTimeInfo
{
  public DateTime registerTime { get; set; }

  public DateTime? modificationTime { get; set; }
}