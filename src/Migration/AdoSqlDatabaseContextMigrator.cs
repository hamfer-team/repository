using System.Reflection;
using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.Ado;
using Hamfer.Repository.Entity;
using Hamfer.Repository.Errors;
using Hamfer.Repository.Models;
using Hamfer.Repository.Services;
using Hamfer.Verification.Services;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Migration
{
  public sealed class AdoSqlDatabaseContextMigrator
  {
    const string MIGRATION_FILES_FOLDER_NAME_DEFAULT = "migrations";

    readonly string connectionString;
    readonly SqlConnection serverConnection;
    readonly bool serverIsValid;
    readonly SqlGeneralRepository repository;
    readonly bool dbIsValid;

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

    public async Task generateMigration(Assembly assembly, string? title = null, string? path = null)
    {
      ICollection<TableCommand> tableCommands = [];

      TableInfosQuery sqlQuery = new();
      string?[] schemas = [.. this.repository.query(new SchemasQuery())];
      IEnumerable<TableInfoQueryResult> queryResults = this.repository.query(sqlQuery);

      // Gather Table infoes from models of the assembly
      #region Gather current models info from assembly and database
      Type[] types = assembly.GetTypes();

      foreach (Type type in types)
      {
        if (ReferenceTypeHelper.IsDerivedOfGenericInterface(type, typeof(IRepositoryEntity<>)))
        {
          SqlTableInfo? tiEntity = SqlDatabaseCommandHelper.GatherTableInfoBy(type);
          if (tiEntity == null)
          {
            continue;
          }

          SqlCommand? createSchemaCommand = null;
          if (tiEntity.schema != null && schemas.IndexOf(tiEntity.schema) < 0)
          {
            createSchemaCommand = new SqlCommand($"CREATE SCHEMA [{tiEntity.schema}];");
          }

          TableInfoQueryResult? dbEntity = queryResults.SingleOrDefault(w =>
            (w.schema?.Equals(tiEntity.schema, StringComparison.InvariantCultureIgnoreCase) ?? false) &&
            (w.name?.Equals(tiEntity.name, StringComparison.InvariantCultureIgnoreCase) ?? false));
        
          TableCommand tableCommand = SqlCommandTextGenerator.GenerateTableCommandsBy(tiEntity, type.Name, dbEntity);
          tableCommand.createSchema = createSchemaCommand;
          tableCommands.Add(tableCommand);
        }
      }
      #endregion

      // Create migration file
      string migrationsPath = prepareMigrationFolder(assembly, path);

      string? timePart = DateTime.Now.ToPersianString("{0:0000}{1:00}{2:00}{3:00}{4:00}{5:00}{6:0000}");
      string fileName = $"{timePart}_{title ?? "migration.sql"}";
      string file = Path.Join(migrationsPath, fileName);

      using StreamWriter sw = new(file, true);

      sw.WriteLine("BEGIN TRY");
      sw.WriteLine();
      sw.WriteLine("BEGIN TRAN;");

      foreach (SqlCommand command in TableCommand.PreparingCommands)
      {
        WriteCommand(sw, command.CommandText);
      }

      foreach (TableCommand tableCommand in tableCommands)
      {
        if (tableCommand.createSchema != null)
        {
          WriteCommand(sw, tableCommand.createSchema.CommandText, $"Create Schema command");
        }

        foreach (SqlCommand command in tableCommand.dropConstraints)
        {
          WriteCommand(sw, command.CommandText, $"Drop Constraints command");
        }

        if (tableCommand.createTable != null)
        {
          WriteCommand(sw, tableCommand.createTable.CommandText, $"Create {tableCommand.tableName} Table command");
        }

        foreach (SqlCommand command in tableCommand.updateColumns)
        {
          WriteCommand(sw, command.CommandText, $"Update Column command");
        }

        foreach (SqlCommand command in tableCommand.updateConstraints)
        {
          WriteCommand(sw, command.CommandText, $"Update Constraint command");
        }

        foreach (SqlCommand command in tableCommand.createRelations)
        {
          WriteCommand(sw, command.CommandText, $"Create Relation command");
        }

        foreach (SqlCommand command in tableCommand.setDescriptions)
        {
          WriteCommand(sw, command.CommandText, $"Set description command");
        }
      }

      sw.WriteLine();
      sw.WriteLine("COMMIT TRAN;");
      sw.WriteLine();
      sw.WriteLine("END TRY");
      sw.WriteLine();
      sw.WriteLine("BEGIN CATCH");
      sw.WriteLine();
      sw.WriteLine("IF @@TRANCOUNT > 0");
      sw.WriteLine("BEGIN");
      sw.WriteLine("\tROLLBACK TRAN;");
      sw.WriteLine("END;");
      sw.WriteLine();
      sw.WriteLine("THROW");
      sw.WriteLine();
      sw.WriteLine("END CATCH");

      Console.WriteLine($"📄✅ Script-file {fileName} created successfully {migrationsPath}");
    }

    public async Task updateDatabase(Assembly assembly, string? path = null)
    {
      string lastMigration = findLastMigration(assembly, path) 
        ?? throw new RepositoryError($"Migration file not found!");
      string sqlCommandText = await File.ReadAllTextAsync(lastMigration);

      repository.execute(sqlCommandText);
    }

    public async Task migrate(string[]? args)
    {
      string? migrationPath = null;
      string? migrationTitle = null;
      bool removeOldDb = false;
      bool generateOnly = false;
      bool updateDatabase = false;
      if (args != null) {
        for (int i = 1; i < args.Length; i++)
        {
          string arg = args[i - 1];
          string nextArg = args[i];
          if (arg == "-t" || arg == "-title")
          {
            LetsVerify.On().Assert(nextArg, "Migration-Title").NotNullOrEmpty().Match(@"^[A-Za-z][A-Za-z0-9_]*$").ThenThrowErrors();
            migrationTitle = nextArg;
          }

          if (arg == "-p" || arg == "-path")
          {
            LetsVerify.On().Assert(nextArg, "Migration-Path").NotNullOrEmpty().PathExists().ThenThrowErrors();
            migrationPath = nextArg;
          }

          if (arg == "-rod" || arg == "-removeOldDb")
          {
            LetsVerify.On().Assert(nextArg, "Migration-Path").NotNullOrEmpty().Equals<string>("true").ThenThrowErrors();
            removeOldDb = true;
          }

          if (arg == "generat-only" || arg == "generateOnly" || nextArg == "generat-only" || nextArg == "generateOnly")
          {
            generateOnly = true;
          }

          if (arg == "update-database" || arg == "updateDatabase" || nextArg == "update-database" || nextArg == "updateDatabase")
          {
            updateDatabase = true;
          }
        }
      }

      Assembly? assembly = Assembly.GetEntryAssembly();
    
      if (assembly != null)
      {
        await this.createDatabase(removeOldDb);

        if (!updateDatabase)
        {
          await this.generateMigration(assembly, migrationTitle, migrationPath);
        }

        if (!generateOnly)
        {
          await this.updateDatabase(assembly, migrationPath);
        }
      }
      else
      {
        throw new RepositoryError("در فراخوانی اسمبلی درخواست‌کننده مشکلی وجود دارد!");
      }
    }

    private string? findLastMigration(Assembly assembly, string? path = null)
    {
      string migrationPath = prepareMigrationFolder(assembly, path, false);
      List<string> fileNames = [.. Directory.GetFiles(migrationPath)];
      return fileNames.OrderDescending().FirstOrDefault();
    }

    private static string prepareMigrationFolder(Assembly assembly, string? path = null, bool withCreate = true)
    {
      string assemblyCodePath = Path.GetDirectoryName(assembly.Location) ?? "";
      string pathSep = Path.DirectorySeparatorChar.ToString();
      int binIx = assemblyCodePath.IndexOf($@"{pathSep}bin{pathSep}");
      if (binIx > 0)
      {
        assemblyCodePath = assemblyCodePath[..binIx];
      }

      string migrationsPath = Path.Join(assemblyCodePath, path ?? MIGRATION_FILES_FOLDER_NAME_DEFAULT);
      if (!Path.Exists(migrationsPath) && withCreate)
      {
        Directory.CreateDirectory(migrationsPath);
      }

      return migrationsPath;
    }

    private static void WriteCommand(StreamWriter sw, string commandText, string? comment = null)
    {
      sw.WriteLine();
      if (comment != null) { sw.WriteLine($"-- {comment}"); }
      sw.WriteLine(commandText);
    }
  }
}