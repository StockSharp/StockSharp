namespace StockSharp.Algo.Export;

/// <summary>
/// The export into the StockSharp format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StockSharpExporter"/>.
/// </remarks>
/// <param name="dataType">Data type info.</param>
/// <param name="storageRegistry">The storage of market data.</param>
/// <param name="drive">Storage.</param>
/// <param name="format">Format type.</param>
public class StockSharpExporter(DataType dataType, IStorageRegistry storageRegistry, IMarketDataDrive drive, StorageFormats format) : BaseExporter(dataType)
{
	private readonly IStorageRegistry _storageRegistry = storageRegistry ?? throw new ArgumentNullException(nameof(storageRegistry));
	private readonly IMarketDataDrive _drive = drive ?? throw new ArgumentNullException(nameof(drive));
	private int _batchSize = 50;

	/// <summary>
	/// The size of transmitted data package. The default is 50 elements.
	/// </summary>
	public int BatchSize
	{
		get => _batchSize;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_batchSize = value;
		}
	}

	private new async Task<(int, DateTime?)> Export<TMessage>(IAsyncEnumerable<TMessage> messages, CancellationToken cancellationToken)
		where TMessage : Message
	{
		var count = 0;
		var lastTime = default(DateTime?);

		await foreach (var batch in messages.Chunk(BatchSize).WithCancellation(cancellationToken))
		{
			foreach (var group in batch.GroupBy(m => m.TryGetSecurityId()))
			{
				var b = group.ToArray();

				var storage = _storageRegistry.GetStorage(group.Key ?? default, DataType, _drive, format);

				await storage.SaveAsync(b, cancellationToken);

				count += b.Length;

				if (b.LastOrDefault() is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;
			}
		}

		return (count, lastTime);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportOrderLogAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTicksAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTransactionsAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<QuoteChangeMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<Level1ChangeMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<PositionChangeMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<IndicatorValue> values, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<CandleMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<NewsMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<SecurityMessage> messages, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<BoardStateMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<BoardMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);
}