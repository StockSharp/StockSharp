namespace StockSharp.Algo.Storages;

/// <summary>
/// The storage based provider of stocks and trade boards.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StorageExchangeInfoProvider"/>.
/// </remarks>
/// <param name="entityRegistry">The storage of trade objects.</param>
public class StorageExchangeInfoProvider(IEntityRegistry entityRegistry) : InMemoryExchangeInfoProvider
{
	private readonly IEntityRegistry _entityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));

	/// <summary>
	/// Initializes a new instance of the <see cref="StorageExchangeInfoProvider"/>.
	/// </summary>
	/// <param name="entityRegistry">The storage of trade objects.</param>
	/// <param name="autoInit">Invoke <see cref="InitAsync"/> method.</param>
	[Obsolete("Use constructor without 'autoInit' parameter.")]	
	public StorageExchangeInfoProvider(IEntityRegistry entityRegistry, bool autoInit)
		: this(entityRegistry)
	{
		_entityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));

		if (autoInit)
			AsyncHelper.Run(() => InitAsync(default));
	}

	/// <inheritdoc />
	public override ValueTask InitAsync(CancellationToken cancellationToken)
	{
		var boardCodes = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		boardCodes.AddRange(_entityRegistry.ExchangeBoards.Select(b => b.Code));

		var boards = Boards.Where(b => !boardCodes.Contains(b.Code)).ToArray();

		if (boards.Length > 0)
		{
			boards
				.Select(b => b.Exchange)
				.Distinct()
				.ForEach(Save);

			boards
				.ForEach(Save);
		}

		_entityRegistry.Exchanges.ForEach(base.Save);
		_entityRegistry.ExchangeBoards.ForEach(base.Save);

		return base.InitAsync(cancellationToken);
	}

	/// <inheritdoc />
	public override void Save(ExchangeBoard board)
	{
		if (board == null)
			throw new ArgumentNullException(nameof(board));

		_entityRegistry.ExchangeBoards.Save(board);

		base.Save(board);
	}

	/// <inheritdoc />
	public override void Save(Exchange exchange)
	{
		if (exchange == null)
			throw new ArgumentNullException(nameof(exchange));

		_entityRegistry.Exchanges.Save(exchange);

		base.Save(exchange);
	}
}