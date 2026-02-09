using System.Reflection;
using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.Ado;
using Hamfer.Repository.Entity;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Migration;

public sealed class AdoSqlDatabaseDataWriter
{
  private readonly SqlGeneralUnitOfWork unitOfWork;
  
  public AdoSqlDatabaseDataWriter(string connectionString)
  {
    this.unitOfWork = new SqlGeneralUnitOfWork(connectionString);
  }

  public async Task seed()
  {
    Assembly? assembly = Assembly.GetEntryAssembly();

    if (assembly != null)
    {
      await this.seed(assembly);
    }
    else
    {
      throw new RepositoryError("در فراخوانی اسمبلی درخواست‌کننده مشکلی وجود دارد!");
    }
  }

  private async Task seed(Assembly assembly)
  {
    Type[] types = assembly.GetTypes();

    foreach (Type type in types)
    {
      if (ReferenceTypeHelper.IsDerivedOfGenericInterface(type, typeof(IRepositorySeedData)))
      {
        dynamic? seedData = Activator.CreateInstance(type);

        SqlCommand? preCommand = seedData?.preCommand;
        SqlCommand? postCommand = seedData?.postCommand;
        IEnumerable<IRepositoryEntity>? seedEntities = (IEnumerable<IRepositoryEntity>?)type.GetField(nameof(IRepositorySeedData.SeedEntities))?.GetValue(seedData);

        if (preCommand != null)
        {
          this.unitOfWork.addToQueue(preCommand);
        }

        if (seedEntities != null)
        {
          SqlCommand[]? seedCommands = SqlCommandTextGenerator.GenerateSeedCommandFor(seedEntities);
          if (seedCommands != null)
          {
            this.unitOfWork.addToQueue(seedCommands);
          }
        }

        if (postCommand != null)
        {
          this.unitOfWork.addToQueue(postCommand);
        }
      }
    }

    try
    {
      await this.unitOfWork.commit();
    }
    catch (Exception)
    {
      await this.unitOfWork.rollBack();
      throw;
    }
  }
}