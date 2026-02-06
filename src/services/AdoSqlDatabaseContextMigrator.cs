using System.Data;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Hamfer.Kernel.Errors;
using Hamfer.Kernel.Utils;
using Hamfer.Repository.Ado;
using Hamfer.Repository.Entity;
using Hamfer.Repository.Errors;
using Hamfer.Repository.Models;
using Hamfer.Repository.Utils;
using Hamfer.Verification.Services;
using Microsoft.Data.SqlClient;

namespace Hamfer.Repository.Services;

public sealed class AdoSqlDatabaseContextMigrator
{
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

  public async Task generateMigration(Assembly assembly, string? title = null, string? path = null, bool removeOldDb = false)
  {
    // Create Database if not exists
    await this.createDatabase(removeOldDb);

    var sqlQuery = new TableInfosQuery();
    IEnumerable<TableInfoQueryResult> queryResults = this.repository.query(sqlQuery);

    // Gather Table infoes from models of the assembly
    #region Gather current models info from assembly and database
    Type[] types = assembly.GetTypes();
    ICollection<TableCommand> tableCommands = [];

    List<SqlTableInfo> tableInfos = [];
    foreach (Type type in types)
    {
      if (ReferenceTypeHelper.IsDerivedOfGenericInterface(type, typeof(IRepositoryEntity<>)))
      {
        SqlTableInfo? tiEntity = SqlDatabaseCommandHelper.GatherTableInfoBy(type);
        if (tiEntity == null)
        {
          continue;
        }

        tableInfos.Add(tiEntity);

        TableInfoQueryResult? dbEntity = queryResults.SingleOrDefault(w =>
          (w.schema?.Equals(tiEntity.schema, StringComparison.InvariantCultureIgnoreCase) ?? false) &&
          (w.name?.Equals(tiEntity.name, StringComparison.InvariantCultureIgnoreCase) ?? false));
        
        tableCommands.Add(SqlDatabaseCommandHelper.GenerateTableCommandsBy(tiEntity, type.Name, dbEntity));
      }
    }
    #endregion

    // Create migrations folder
    # region folder & raw files
    string assemblyCodePath = Path.GetDirectoryName(assembly.Location) ?? "";
    string pathSep = Path.DirectorySeparatorChar.ToString();
    int binIx = assemblyCodePath.IndexOf($@"{pathSep}bin{pathSep}");
    if (binIx > 0)
    {
      assemblyCodePath = assemblyCodePath[..binIx];
    }

    string migrationsPath = Path.Join(assemblyCodePath, path ?? "migrations");
    string migrationsRawPath = Path.Join(migrationsPath, "raw");
    if (!Path.Exists(migrationsPath))
    {
      Directory.CreateDirectory(migrationsPath);
    }
    if (!Path.Exists(migrationsRawPath))
    {
      DirectoryInfo di = Directory.CreateDirectory(migrationsRawPath);
      di.Attributes |= FileAttributes.Hidden;
    }

    // Create raw file
    string? timePart = DateTime.Now.ToPersianString("{0:0000}{1:00}{2:00}{3:00}{4:00}{5:00}{6:0000}");
    string snapshotFileName = $"{timePart}_{title ?? ".raw"}";
    string snapshotFile = Path.Join(migrationsRawPath, snapshotFileName);
    using (StreamWriter ssw = new(snapshotFile, true))
    {
      foreach (SqlTableInfo tableInfo in tableInfos)
      {
        string uglyfied = JsonSerializer.Serialize(tableInfo)
          .Replace("\"schema\":","s:").Replace("\"name\":","n:").Replace("\"columns\":","c:")
          .Replace("\"defaultValue\":","v:").Replace("\"defaultValueText\":","w:").Replace("\"dbType\":","t:").Replace("\"sqlDbTypeText\":","T:")
          .Replace("\"charMaxLength\":","x:").Replace("\"numericPrecision\":","p:").Replace("\"numericScale\":","l:").Replace("\"timeScale\":","m:")
          .Replace("\"isNullable\":","_:").Replace("\"identitySeed\":","q:").Replace("\"identityIncrement\":","i:").Replace("\"description\":","d:")
          .Replace("\"primaryKeys\":","k:").Replace("\"uniqueConstraints\":","u:").Replace("\"relations\":","r:");
        ssw.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(uglyfied)));
      }
    }

    #endregion
    
    // Create migration file
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

    Console.WriteLine($"📄✅ Script-file ${fileName} created successfully ${migrationsPath}");
  }

  public async Task updateDatabase()
  {
    // TODO
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
      if (!updateDatabase)
      {
        await this.generateMigration(assembly, migrationTitle, migrationPath, removeOldDb);
      }

      if (!generateOnly)
      {
        await this.updateDatabase();
      }
    }
    else
    {
      throw new RepositoryError("در فراخوانی اسمبلی درخواست‌کننده مشکلی وجود دارد!");
    }
  }

  private static void WriteCommand(StreamWriter sw, string commandText, string? comment = null)
  {
    sw.WriteLine();
    if (comment != null) { sw.WriteLine($"-- {comment}"); }
    sw.WriteLine(commandText);
  }
}

sealed class TableInfoQueryResult : SqlTableInfo
{
  public TableInfoQueryResult(string? schema, string? table, string? description, string? colJson, string? ixJson)
  {
    this.schema = schema;
    this.name = table;
    this.description = description;
    ColumnInfoQueryResult[]? colResults = colJson != null ? JsonSerializer.Deserialize<ColumnInfoQueryResult[]>(colJson) : null;
    this.columns = colResults?.Select(c => new SqlColumnInfo()
    {
      name = c.name,
      dbType = c.type != null ? Enum.Parse<SqlDbType>(c.type) : null,
      charMaxLength = c.max_length,
      defaultValue = c.def,
      defaultValueText = c.def,
      description = c.description,
      identitySeed = c.seed_value,
      identityIncrement = c.increment_value,
      isNullable = c.is_nullable ?? true,
      numericPrecision = c.precision,
      numericScale = c.scale,
      timeScale = c.scale,
    }).ToList();

    UniqueInfoQueryResult[]? uniqueResults = ixJson != null ? JsonSerializer.Deserialize<UniqueInfoQueryResult[]>(ixJson) : null;
    this.primaryKeys = uniqueResults?.Where(w=> w.is_primary_key ?? false && w.column != null).Select(c => c.column!).ToArray();

    UniqueInfoQueryResult[]? uniques = uniqueResults?.Where(w => w.is_primary_key ?? false && w.column != null).ToArray();
    if (uniques != null) {
      this.uniqueConstraints ??= [];
      foreach (UniqueInfoQueryResult unique in uniques)
      {
        if (this.uniqueConstraints.ContainsKey(unique.key!))
        {
          if(this.uniqueConstraints.TryGetValue(unique.key!, out string[]?value))
          {
            this.uniqueConstraints.Remove(unique.key!);
          }
          List<string>? values = value?.ToList();
          values?.Add(unique.column!);
          this.uniqueConstraints.Add(unique.key!, values?.ToArray() ?? []);
        } else
        {
          this.uniqueConstraints.Add(unique.key!, [unique.column!]);
        }
      }
    }
  }
}

sealed class ColumnInfoQueryResult
{
  public string? name;
  public string? type;
  public string? def;
  public bool? is_nullable;
  public int? max_length;
  public int? precision;
  public int? scale;
  public bool? is_identity;
  public int? seed_value;
  public int? increment_value;
  public string? description;
}

sealed class UniqueInfoQueryResult
{
  public string? key;
  public bool? is_primary_key;
  public string? column;
}

sealed class TableInfosQuery : SqlQueryBase<TableInfoQueryResult>
{
  public TableInfosQuery() : base(reader =>
  {
    return new TableInfoQueryResult(
      reader.Get<string?>("schema"),
      reader.Get<string?>("table"),
      reader.Get<string?>("description"),
      reader.Get<string?>("colJson"),
      reader.Get<string?>("ixJson")
    );
  })
  {
    this.query = "select s.[name] [schema], o.[name] [table], e.[value] [description], " +
	    "(select c.[name], t.[name] [type], object_definition(c.default_object_id) [def], c.is_nullable, c.max_length, c.[precision], c.scale, c.is_identity, ic.seed_value, ic.increment_value, e.[value] [description] " +
	    "from [sys].[columns] c " +
		    "join [sys].[types] t on c.user_type_id = t.user_type_id " +
		    "left join [sys].[identity_columns] ic on c.[object_id] = ic.[object_id] and c.column_id = ic.column_id" +
		    "left join [sys].[extended_properties] e on e.major_id = c.[object_id] and e.minor_id = c.column_id and e.[name] = 'Description' " +
	    "where c.[object_id] = o.[object_id] " +
	    "for json path) colJson, " +
	    "(select i.[name] [key], i.is_primary_key, c.[name] [column] " +
	    "from [sys].[indexes] i " +
        "join [sys].[index_columns] ic on i.[object_id] = ic.[object_id] and i.index_id = ic.index_id " +
		    "join [sys].[columns] c on ic.[object_id] = c.[object_id] and ic.column_id = c.column_id " +
      "where i.is_unique = 1 and i.[object_id] = o.[object_id] " +
	    "for json path) ixJson " +
      "from [sys].[objects] o " +
	      "join [sys].[schemas] s on o.[schema_id] = s.[schema_id] " +
	      "left join [sys].[extended_properties] e on e.major_id = o.[object_id] and e.minor_id = 0 and e.[name] = 'Description' " +
      $"where o.[type] = 'U'";
  }
}