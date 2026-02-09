namespace Hamfer.Repository.Entity;

public interface IHasRegisterInfo
{
  public DateTime registerTime { get; set; }

  public Guid registerantId { get; set; }
}