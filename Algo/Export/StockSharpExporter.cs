namespace StockSharp.Algo.Export;

/// <summary>
/// The export into the StockSharp format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StockSharpExporter"/>.
/// </remarks>
/// <param name="dataType">Data type info.</param>
/// <param name="isCancelled">The processor, returning process interruption sign.</param>
/// <param name="storageRegistry">The storage of market data.</param>
/// <param name="drive">Storage.</param>
/// <param name="format">Format type.</param>
public class StockSharpExporter(DataType dataType, Func<int, bool> isCancelled, IStorageRegistry storageRegistry, IMarketDataDrive drive, StorageFormats format) : BaseExporter(dataType, isCancelled, drive.CheckOnNull(nameof(drive)).Path)
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

	private (int, DateTimeOffset?) Export(IEnumerable<Message> messages)
	{
		var count = 0;
		var lastTime = default(DateTimeOffset?);

		foreach (var batch in messages.Chunk(BatchSize))
		{
			foreach (var group in batch.GroupBy(m => m.TryGetSecurityId()))
			{
				var b = group.ToArray();

				var storage = _storageRegistry.GetStorage(group.Key ?? default, DataType.MessageType, DataType.Arg, _drive, format);

				if (!CanProcess(b.Length))
					break;

				storage.Save(b);

				count += b.Length;

				if (b.LastOrDefault() is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;
			}
		}

		return (count, lastTime);
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportOrderLog(IEnumerable<ExecutionMessage> messages)
		=> Export(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTicks(IEnumerable<ExecutionMessage> messages)
		=> Export(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTransactions(IEnumerable<ExecutionMessage> messages)
		=> Export(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<QuoteChangeMessage> messages)
		=> Export(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<Level1ChangeMessage> messages)
		=> Export(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<PositionChangeMessage> messages)
		=> Export(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<IndicatorValue> values) => throw new NotSupportedException();

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<CandleMessage> messages)
		=> Export(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<NewsMessage> messages)
		=> Export(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<SecurityMessage> messages) => throw new NotSupportedException();

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardStateMessage> messages)
		=> Export(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardMessage> messages)
		=> Export(messages);
}