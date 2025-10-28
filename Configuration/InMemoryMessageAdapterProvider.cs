namespace StockSharp.Configuration;

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
		
		return File.Exists(assemblyPath)
			? Assembly.LoadFrom(assemblyPath)
			: null;
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
		PossibleAdapters = [.. Directory.GetCurrentDirectory().FindAdapters(ex => ex.LogError()).Select(t =>
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

	/// <inheritdoc />
	public virtual IEnumerable<IMessageAdapter> CreateStockSharpAdapters(IdGenerator transactionIdGenerator, string login, SecureString password) => [];

	/// <inheritdoc />
	public virtual IAsyncMessageAdapter CreateTransportAdapter(IdGenerator transactionIdGenerator)
	{
		if (_transportAdapter is null)
			throw new NotSupportedException();

		return _transportAdapter.CreateInstance<IAsyncMessageAdapter>(transactionIdGenerator);
	}
}