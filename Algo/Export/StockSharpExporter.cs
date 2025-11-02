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

	private Task<(int, DateTime?)> Export(IEnumerable<Message> messages, CancellationToken cancellationToken)
	{
		var count = 0;
		var lastTime = default(DateTime?);

		foreach (var batch in messages.Chunk(BatchSize))
		{
			foreach (var group in batch.GroupBy(m => m.TryGetSecurityId()))
			{
				var b = group.ToArray();

				var storage = _storageRegistry.GetStorage(group.Key ?? default, DataType, _drive, format);

				cancellationToken.ThrowIfCancellationRequested();

				storage.Save(b);

				count += b.Length;

				if (b.LastOrDefault() is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;
			}
		}

		return (count, lastTime).FromResult();
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportOrderLog(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTicks(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTransactions(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<QuoteChangeMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<Level1ChangeMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<PositionChangeMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<IndicatorValue> values, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<CandleMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<NewsMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<SecurityMessage> messages, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<BoardStateMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<BoardMessage> messages, CancellationToken cancellationToken)
		=> Export(messages, cancellationToken);
}