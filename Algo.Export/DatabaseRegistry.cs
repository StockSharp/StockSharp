namespace StockSharp.Algo.Export;

using Ecng.Configuration;
using Ecng.Data;

/// <summary>
/// Database services registry.
/// </summary>
public static class DatabaseRegistry
{
	/// <summary>
	/// <see cref="IDatabaseProvider"/>
	/// </summary>
	public static IDatabaseProvider Provider => ConfigManager.GetService<IDatabaseProvider>();

	/// <summary>
	/// Cache.
	/// </summary>
	public static DatabaseConnectionCache Storage => ConfigManager.GetService<DatabaseConnectionCache>();
}