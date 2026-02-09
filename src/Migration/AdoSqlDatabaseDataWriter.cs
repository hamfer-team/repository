using System.Reflection;
using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.Ado;
using Hamfer.Repository.Entity;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Migration;

public sealed class AdoSqlDatabaseDataWriter
{
  readonly SqlGeneralRepository repository;
  
  public AdoSqlDatabaseDataWriter(string connectionString)
  {
    this.repository = new SqlGeneralRepository(connectionString);
  }

  public async Task seed(string[]? args)
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
        SqlCommand? preCommand = (SqlCommand?)type.GetProperty(nameof(IRepositorySeedData.preCommand))?.GetValue(type);
        SqlCommand? postCommand = (SqlCommand?)type.GetProperty(nameof(IRepositorySeedData.postCommand))?.GetValue(type);
        IEnumerable<IRepositoryEntity>? seedEntities = (IEnumerable<IRepositoryEntity>?)type.GetProperty(nameof(IRepositorySeedData.SeedEntities))?.GetValue(type);

        if (preCommand != null)
        {
          this.repository.execute(preCommand);
        }

        if (seedEntities != null)
        {
          SqlCommand[]? seedCommand = SqlCommandTextGenerator.GenerateSeedCommandFor(seedEntities);
          if (seedCommand != null)
          {
            // TODO
          }
        }

        if (postCommand != null)
        {
          this.repository.execute(postCommand);
        }
      }
    }
  }
}