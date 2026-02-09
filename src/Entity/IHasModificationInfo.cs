namespace Hamfer.Repository.Entity;

public interface IHasModificationInfo: IHasCreationInfo
{
  public DateTime? modifiedAt { get; set; }

  public Guid? modifiedBy { get; set; }
}