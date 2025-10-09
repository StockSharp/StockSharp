namespace StockSharp.Configuration;

using Ecng.Reflection;

/// <summary>
/// Extension class.
/// </summary>
public static class Extensions
{
	/// <summary>
	/// </summary>
	public static long? TryGetProductId()
		=> Paths
			.EntryAssembly?
			.GetAttribute<ProductIdAttribute>()?
			.ProductId;

	private static readonly HashSet<string> _nonAdapters = new(StringComparer.InvariantCultureIgnoreCase)
	{
		"StockSharp.Alerts",
		"StockSharp.Alerts.Interfaces",
		"StockSharp.Algo",
		"StockSharp.Algo.Export",
		"StockSharp.BusinessEntities",
		"StockSharp.Charting.Interfaces",
		"StockSharp.Configuration",
		"StockSharp.Configuration.Adapters",
		"StockSharp.Diagram.Core",
		"StockSharp.Fix.Core",
		"StockSharp.Licensing",
		"StockSharp.Localization",
		"StockSharp.Media",
		"StockSharp.Media.Names",
		"StockSharp.Messages",
		"StockSharp.Xaml",
		"StockSharp.Xaml.CodeEditor",
		"StockSharp.Xaml.Charting",
		"StockSharp.Xaml.Diagram",
		"StockSharp.Studio.Controls",
		"StockSharp.Studio.Core",
		"StockSharp.Studio.Nuget",
		"StockSharp.Studio.WebApi",
		"StockSharp.Studio.WebApi.UI",
		"StockSharp.QuikLua",
		"StockSharp.QuikLua32",
		"StockSharp.MT4",
		"StockSharp.MT5",
		"StockSharp.Server.Database",
		"StockSharp.Server.Core",
		"StockSharp.Server.Fix",
		"StockSharp.Server.Utils",
	};


	/// <summary>
	/// Finds and returns adapter types in the specified directory.
	/// Scans for assemblies starting with "StockSharp." (except a known exclusion list),
	/// loads them, and collects types implementing <see cref="IMessageAdapter"/> (excluding dialects).
	/// </summary>
	/// <param name="dir">The directory path to scan for adapter assemblies (.dll files).</param>
	/// <param name="errorHandler">An action to handle exceptions that occur during assembly loading.</param>
	/// <returns>An enumeration of adapter <see cref="Type"/>s found in the directory.</returns>
	public static IEnumerable<Type> FindAdapters(this string dir, Action<Exception> errorHandler)
	{
		var adapters = new List<Type>();

		try
		{
			var assemblies = Directory.GetFiles(dir, "*.dll").Where(p =>
			{
				var name = Path.GetFileNameWithoutExtension(p);

				if (!name.StartsWithIgnoreCase("StockSharp."))
					return false;

				if (_nonAdapters.Contains(name))
					return false;

				return true;
			});

			foreach (var assembly in assemblies)
			{
				if (!assembly.IsAssembly())
					continue;

				try
				{
					var asm = Assembly.Load(AssemblyName.GetAssemblyName(assembly));

					adapters.AddRange(asm.FindImplementations<IMessageAdapter>(extraFilter: t => !t.Name.EndsWith("Dialect")));
				}
				catch (Exception e)
				{
					errorHandler?.Invoke(e);
				}
			}
		}
		catch (Exception e)
		{
			errorHandler?.Invoke(e);
		}

		return adapters;
	}

	/// <summary>
	/// Builds an absolute path from a potentially relative <paramref name="relative"/> path.
	/// </summary>
	/// <param name="relative">A relative or absolute path.</param>
	/// <param name="baseDir">The base directory used when <paramref name="relative"/> is not rooted.</param>
	/// <returns>
	/// An absolute path. If <paramref name="relative"/> is already rooted, the same value is returned;
	/// otherwise the path combined with <paramref name="baseDir"/> is returned.
	/// </returns>
	public static string MakeFullPath(this string relative, string baseDir)
		=> Path.IsPathRooted(relative) ? relative : Path.Combine(baseDir, relative);
}