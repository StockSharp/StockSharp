namespace StockSharp.Configuration;

using Ecng.Reflection;

/// <summary>
/// In memory configuration message adapter's provider.
/// </summary>
public class InMemoryMessageAdapterProvider : IMessageAdapterProvider
{
	static InMemoryMessageAdapterProvider()
	{
		AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
	}

	private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
	{
		var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		var assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
		
		if (!File.Exists(assemblyPath))
			return null;

		return Assembly.LoadFrom(assemblyPath);
	}

	private readonly Type _transportAdapter;

	/// <summary>
	/// Initialize <see cref="InMemoryMessageAdapterProvider"/>.
	/// </summary>
	/// <param name="currentAdapters">All currently available adapters.</param>
	/// <param name="transportAdapter"><see cref="CreateTransportAdapter"/></param>
	public InMemoryMessageAdapterProvider(IEnumerable<IMessageAdapter> currentAdapters, Type transportAdapter = null)
	{
		CurrentAdapters = currentAdapters ?? throw new ArgumentNullException(nameof(currentAdapters));
		_transportAdapter = transportAdapter;

		var idGenerator = new IncrementalIdGenerator();
		PossibleAdapters = [.. GetAdapters().Select(t =>
		{
			try
			{
				return t.CreateAdapter(idGenerator);
			}
			catch (Exception ex)
			{
				ex.LogError();
				return null;
			}
		}).WhereNotNull()];
	}

	/// <inheritdoc />
	public virtual IEnumerable<IMessageAdapter> CurrentAdapters { get; }

	/// <inheritdoc />
	public virtual IEnumerable<IMessageAdapter> PossibleAdapters { get; }

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
		"StockSharp.Server.Core",
		"StockSharp.Server.Fix",
		"StockSharp.Server.Utils",
	};
	
	private IEnumerable<Type> GetAdapters()
	{
		var adapters = new List<Type>();

		try
		{
			var assemblies = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll").Where(p =>
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
					e.LogError();
				}
			}
		}
		catch (Exception e)
		{
			e.LogError();
		}

		return adapters;
	}

	/// <inheritdoc />
	public virtual IEnumerable<IMessageAdapter> CreateStockSharpAdapters(IdGenerator transactionIdGenerator, string login, SecureString password) => [];

	/// <inheritdoc />
	public virtual IMessageAdapter CreateTransportAdapter(IdGenerator transactionIdGenerator)
	{
		if (_transportAdapter is null)
			throw new NotSupportedException();

		return _transportAdapter.CreateInstance<IMessageAdapter>(transactionIdGenerator);
	}
}