using System.Reflection;
using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.Ado;
using Hamfer.Repository.Entity;
using Hamfer.Repository.Errors;
using Hamfer.Repository.Models;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Services;

public sealed class AdoSqlDatabaseContextMigrator
{
  readonly string connectionString;
  readonly SqlConnection serverConnection;
  readonly bool serverIsValid;
  SqlGeneralRepository repository;
  bool dbIsValid;

  public AdoSqlDatabaseContextMigrator(string connectionString)
  {
    this.connectionString = connectionString;
    this.repository = new SqlGeneralRepository(connectionString);
    this.serverIsValid = this.repository.validateServer(out this.serverConnection);
    this.dbIsValid = this.repository.validate();
  }

  public async Task createDatabase(bool removeOldDb = false)
  {
    if (!this.serverIsValid)
    {
      throw new RepositoryConnectionError(this.serverConnection.ConnectionString, "در زمان اتصال به سرور پایگاه داده خطایی رخ داده است!");
    }

    if (this.serverConnection.State != System.Data.ConnectionState.Open)
    {
      await this.serverConnection.OpenAsync();
    }

    if (!this.dbIsValid || removeOldDb)
    {
      string dbName = new SqlConnectionStringBuilder(){ ConnectionString = this.connectionString }.InitialCatalog;
      SqlCommand command = this.serverConnection.CreateCommand();
      command.CommandText = $"CREATE DATABASE {dbName};";
      await command.ExecuteNonQueryAsync();
      Console.WriteLine("➕✅ Database created successfully!");
    }
  }

  public async Task migrate(Assembly assembly, string? title = null, string? path = null, bool removeOldDb = false)
  {
    // Create Database if not exists
    await this.createDatabase(removeOldDb);

    // Gather Create Table commands from assembly
    Type[] types = assembly.GetTypes();
    List<SqlCommand> createTableCommands = [];
    List<SqlCommand> relationCommands = [];
    List<SqlCommand> defaultValueCommands = [];
    List<SqlCommand> extendedCommands = [];
    foreach (var type in types)
    {
      if (ReferenceTypeHelper.IsDerivedOfGenericInterface(type, typeof(IRepositoryEntity<>)))
      {
        SqlTableInfo? tiEntity = SqlDatabaseCommandHelper.GatherTableInfoBy(type);
        if (tiEntity == null)
        {
          continue;
        }

        TableCommand? tableCmds = SqlDatabaseCommandHelper.GenerateTableCommandsBy(tiEntity, type.Name);
        if (tableCmds != null)
        {
          createTableCommands.Add(tableCmds.create);
          if (tableCmds.relations != null) relationCommands.AddRange(tableCmds.relations);
          if (tableCmds.defaulValues != null) defaultValueCommands.AddRange(tableCmds.defaulValues);
          if (tableCmds.description != null) extendedCommands.Add(tableCmds.description);
          if (tableCmds.columnDescriptions != null) extendedCommands.AddRange(tableCmds.columnDescriptions);
        }
      }
    }

    // Create migrations folder
    string assemblyCodePath = Path.GetDirectoryName(assembly.Location) ?? "";
    string pathSep = Path.DirectorySeparatorChar.ToString();
    int binIx = assemblyCodePath.IndexOf($@"{pathSep}bin{pathSep}");
    if (binIx > 0)
    {
      assemblyCodePath = assemblyCodePath[..binIx];
    }

    string migrationsPath = Path.Join(assemblyCodePath, path ?? "migrations");
    if (!Path.Exists(migrationsPath))
    {
      Directory.CreateDirectory(migrationsPath);
    }

    // Create migration file
    string fileName = $"{DateTime.Now.ToPersianString("{0:0000}{1:00}{2:00}{3:00}{4:00}{5:00}{6:0000}")}_{title ?? "migration.sql"}";
    string file = Path.Join(migrationsPath, fileName);
    using StreamWriter sw = new(file, true);

    sw.WriteLine("BEGIN TRY");
    sw.WriteLine("BEGIN TRAN;");
    foreach (SqlCommand command in TableCommand.PreparingCommands)
    {
      sw.WriteLine();
      sw.Write(command.CommandText);
      sw.WriteLine(";");
    }

    foreach (SqlCommand command in createTableCommands)
    {
      sw.WriteLine();
      sw.WriteLine($"-- Create-Table command");
      sw.Write(command.CommandText);
      sw.WriteLine(";");
    }

    //TODO start a sql transaction to create or alter tables

    sw.WriteLine();
    sw.WriteLine("COMMIT TRAN;");
    sw.WriteLine("END TRY");
    sw.WriteLine("BEGIN CATCH");
    sw.WriteLine("IF @@TRANCOUNT > 0");
    sw.WriteLine("BEGIN");
    sw.WriteLine("\tROLLBACK TRAN;");
    sw.WriteLine("END;");
    sw.WriteLine("THROW");
    sw.WriteLine("END CATCH");
  }

  public async Task migrate(string? title = null, string? path = null, bool removeOldDb = false)
  {
    Assembly? assembly = Assembly.GetEntryAssembly();
    
    if (assembly != null)
    {
      await this.migrate(assembly, title, path, removeOldDb);
    }
    else
    {
      throw new RepositoryError("در فراخوانی اسمبلی درخواست‌کننده مشکلی وجود دارد!");
    }
  }
}