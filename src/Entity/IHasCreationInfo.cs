namespace Hamfer.Repository.Entity;

public interface IHasCreationInfo
{
  public DateTime createdAt { get; set; }

  public Guid createdBy { get; set; }
}