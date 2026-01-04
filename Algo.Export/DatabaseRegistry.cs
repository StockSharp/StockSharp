namespace StockSharp.Algo.Export;

using Ecng.Configuration;
using Ecng.Data;

/// <summary>
/// Database services registry.
/// </summary>
public static class DatabaseRegistry
{
	/// <summary>
	/// <see cref="IDatabaseBatchInserterProvider"/>
	/// </summary>
	public static IDatabaseBatchInserterProvider Provider => ConfigManager.GetService<IDatabaseBatchInserterProvider>();

	/// <summary>
	/// Cache.
	/// </summary>
	public static DatabaseConnectionCache Storage => ConfigManager.GetService<DatabaseConnectionCache>();
}