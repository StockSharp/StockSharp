namespace StockSharp.Algo.Export;

/// <summary>
/// The base class of export.
/// </summary>
/// <remarks>
/// Initialize <see cref="BaseExporter"/>.
/// </remarks>
/// <param name="dataType">Data type info.</param>
public abstract class BaseExporter(DataType dataType)
{
	/// <summary>
	/// Data type info.
	/// </summary>
	public DataType DataType { get; } = dataType ?? throw new ArgumentNullException(nameof(dataType));

	private Encoding _encoding = Encoding.UTF8;

	/// <summary>
	/// Encoding.
	/// </summary>
	public Encoding Encoding
	{
		get => _encoding;
		set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// To export values.
	/// </summary>
	/// <param name="values">Value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	public Task<(int, DateTimeOffset?)> Export(IEnumerable values, CancellationToken cancellationToken)
	{
		if (values == null)
			throw new ArgumentNullException(nameof(values));

		return Do.InvariantAsync(() =>
		{
			if (DataType == DataType.MarketDepth)
				return Export((IEnumerable<QuoteChangeMessage>)values, cancellationToken);
			else if (DataType == DataType.Level1)
				return Export((IEnumerable<Level1ChangeMessage>)values, cancellationToken);
			else if (DataType == DataType.Ticks)
				return ExportTicks((IEnumerable<ExecutionMessage>)values, cancellationToken);
			else if (DataType == DataType.OrderLog)
				return ExportOrderLog((IEnumerable<ExecutionMessage>)values, cancellationToken);
			else if (DataType == DataType.Transactions)
				return ExportTransactions((IEnumerable<ExecutionMessage>)values, cancellationToken);
			else if (DataType.IsCandles)
				return Export((IEnumerable<CandleMessage>)values, cancellationToken);
			else if (DataType == DataType.News)
				return Export((IEnumerable<NewsMessage>)values, cancellationToken);
			else if (DataType == DataType.Securities)
				return Export((IEnumerable<SecurityMessage>)values, cancellationToken);
			else if (DataType == DataType.PositionChanges)
				return Export((IEnumerable<PositionChangeMessage>)values, cancellationToken);
			else if (DataType == TraderHelper.IndicatorValue)
				return Export((IEnumerable<IndicatorValue>)values, cancellationToken);
			else if (DataType == DataType.BoardState)
				return Export((IEnumerable<BoardStateMessage>)values, cancellationToken);
			else if (DataType == DataType.Board)
				return Export((IEnumerable<BoardMessage>)values, cancellationToken);
			else
				throw new InvalidOperationException(DataType.ToString());
		});
	}

	/// <summary>
	/// To export <see cref="QuoteChangeMessage"/>.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> Export(IEnumerable<QuoteChangeMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="Level1ChangeMessage"/>.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> Export(IEnumerable<Level1ChangeMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="DataType.Ticks"/>.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> ExportTicks(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="DataType.OrderLog"/>.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> ExportOrderLog(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="DataType.Transactions"/>.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> ExportTransactions(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="CandleMessage"/>.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> Export(IEnumerable<CandleMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="NewsMessage"/>.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> Export(IEnumerable<NewsMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="SecurityMessage"/>.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> Export(IEnumerable<SecurityMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="PositionChangeMessage"/>.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> Export(IEnumerable<PositionChangeMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="IndicatorValue"/>.
	/// </summary>
	/// <param name="values">Values.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> Export(IEnumerable<IndicatorValue> values, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="BoardStateMessage"/>.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> Export(IEnumerable<BoardStateMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="BoardMessage"/> and its derived types.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTimeOffset?)> Export(IEnumerable<BoardMessage> messages, CancellationToken cancellationToken);
}