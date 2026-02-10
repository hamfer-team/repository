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
    Console.WriteLine($"🌱 Seed started ...");
    Type[] types = assembly.GetTypes();

    foreach (Type type in types)
    {
      if (ReferenceTypeHelper.IsDerivedOfGenericInterface(type, typeof(IRepositorySeedData)))
      {
        Console.Write($"➕ Seed-Data found: {type.Name}");
        dynamic? seedData = Activator.CreateInstance(type);

        SqlCommand? preCommand = seedData?.preCommand;
        SqlCommand? postCommand = seedData?.postCommand;
        IEnumerable<IRepositoryEntity>? seedEntities = (IEnumerable<IRepositoryEntity>?)type.GetField(nameof(IRepositorySeedData.SeedEntities))?.GetValue(seedData);

        if (preCommand != null)
        {
          this.unitOfWork.addToQueue(preCommand);
          Console.Write(" + Pre-Command");
        }

        if (seedEntities != null)
        {
          SqlCommand[]? seedCommands = SqlCommandTextGenerator.GenerateSeedCommandFor(seedEntities);
          if (seedCommands != null)
          {
            this.unitOfWork.addToQueue(seedCommands);
            Console.Write($" related to [{seedCommands.Length}] Entities");
          }
        }

        if (postCommand != null)
        {
          this.unitOfWork.addToQueue(postCommand);
          Console.Write(" + Post-Command");
        }

        Console.WriteLine(".");
      }
    }

    try
    {
      Console.WriteLine($"⏳ Trying to update database ...");
      await this.unitOfWork.commit();
      Console.WriteLine();
      Console.WriteLine($"🌱✅ Seed completed successfully.");
    }
    catch (Exception)
    {
      Console.WriteLine();
      ConsoleColor defColor = Console.ForegroundColor;
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine($"🌱💥 Seed abourted! and will roll-back.");
      Console.ForegroundColor = defColor;
      await this.unitOfWork.rollBack();
      throw;
    }
  }
}