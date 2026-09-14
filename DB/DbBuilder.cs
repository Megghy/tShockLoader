using System.Data;
using System.Diagnostics;
using System.IO;
using System.Data.SQLite;
using MySqlConnector;
using Npgsql;
using TerrariaApi.Server;
using TShockAPI.Configuration;

namespace TShockAPI.DB;

/// <summary>
/// Provides logic to build a DB connection.
/// </summary>
public sealed class DbBuilder
{
	private readonly TShock _caller;
	private readonly TShockConfig _config;
	private readonly string _savePath;

	/// <summary>
	/// Initializes a new instance of the <see cref="DbBuilder"/> class.
	/// </summary>
	/// <param name="caller">The TShock instance calling this DbBuilder.</param>
	/// <param name="config">The TShock configuration, supplied by <see cref="TShock.Config" /> at init.</param>
	/// <param name="savePath">The savePath registered by TShock. See <see cref="TShock.SavePath" />.</param>
	public DbBuilder(TShock caller, TShockConfig config, string savePath)
	{
		_caller = caller;
		_config = config;
		_savePath = savePath;
	}

	/// <summary>
	/// Builds a DB connection based on the provided configuration.
	/// </summary>
	/// <param name="config">The TShock configuration.</param>
	/// <remarks>
	/// Default settings will result in a local sqlite database file named "tshock.db" in the current directory to be used as server DB.
	/// </remarks>
	public IDbConnection BuildDbConnection()
	{
		string dbType = _config.Settings.StorageType.ToLowerInvariant();

		return dbType switch
		{
			"sqlite" => BuildSqliteConnection(),
			"mysql" => BuildMySqlConnection(),
			"postgres" => BuildPostgresConnection(),
			_ => throw new("Invalid storage type")
		};
	}

	private SQLiteConnection BuildSqliteConnection()
	{
		string dbFilePath = Path.Combine(_savePath, _config.Settings.SqliteDBPath);

		if (Path.GetDirectoryName(dbFilePath) is not { } dbDirPath)
		{
			throw new DirectoryNotFoundException($"The SQLite database path '{dbFilePath}' could not be found.");
		}

		Directory.CreateDirectory(dbDirPath);

		return new($"Data Source={dbFilePath}");
	}

	private MySqlConnection BuildMySqlConnection()
	{
		try
		{
			string[] hostport = _config.Settings.MySqlHost.Split(':');

			MySqlConnectionStringBuilder connStrBuilder = new()
			{
				Server = hostport[0],
				Port = hostport.Length > 1 ? uint.Parse(hostport[1]) : 3306,
				Database = _config.Settings.MySqlDbName,
				UserID = _config.Settings.MySqlUsername,
				Password = _config.Settings.MySqlPassword
			};

			return new(connStrBuilder.ToString());
		}
		catch (MySqlException e)
		{
			ServerApi.LogWriter.PluginWriteLine(_caller, e.ToString(), TraceLevel.Error);
			throw new("MySql not setup correctly", e);
		}
	}

	private NpgsqlConnection BuildPostgresConnection()
	{
		try
		{
			string[] hostport = _config.Settings.PostgresHost.Split(':');

			NpgsqlConnectionStringBuilder connStrBuilder = new()
			{
				Host = hostport[0],
				Port = hostport.Length > 1 ? int.Parse(hostport[1]) : 5432,
				Database = _config.Settings.PostgresDbName,
				Username = _config.Settings.PostgresUsername,
				Password = _config.Settings.PostgresPassword
			};

			return new(connStrBuilder.ToString());
		}
		catch (NpgsqlException e)
		{
			ServerApi.LogWriter.PluginWriteLine(_caller, e.ToString(), TraceLevel.Error);
			throw new("Postgres not setup correctly", e);
		}
	}
}
