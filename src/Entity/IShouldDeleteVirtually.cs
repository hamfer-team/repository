namespace Hamfer.Repository.Entity;

public interface IShouldDeleteVirtually
{
  public bool isDeleted { get; set; }

  public DateTime? deletedAt { get; set; }

  public Guid? deletedBy { get; set; }
}