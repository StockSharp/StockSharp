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
	/// To export values asynchronously.
	/// </summary>
	/// <typeparam name="T">The type of values.</typeparam>
	/// <param name="values">Value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	public Task<(int, DateTime?)> Export<T>(IAsyncEnumerable<T> values, CancellationToken cancellationToken)
	{
		if (values == null)
			throw new ArgumentNullException(nameof(values));

		return Do.InvariantAsync(() =>
		{
			if (DataType == DataType.MarketDepth)
				return Export(values.Cast(m => m.To<QuoteChangeMessage>()), cancellationToken);
			else if (DataType == DataType.Level1)
				return Export(values.Cast(m => m.To<Level1ChangeMessage>()), cancellationToken);
			else if (DataType == DataType.Ticks)
				return ExportTicksAsync(values.Cast(m => m.To<ExecutionMessage>()), cancellationToken);
			else if (DataType == DataType.OrderLog)
				return ExportOrderLogAsync(values.Cast(m => m.To<ExecutionMessage>()), cancellationToken);
			else if (DataType == DataType.Transactions)
				return ExportTransactionsAsync(values.Cast(m => m.To<ExecutionMessage>()), cancellationToken);
			else if (DataType.IsCandles)
				return Export(values.Cast(m => m.To<CandleMessage>()), cancellationToken);
			else if (DataType == DataType.News)
				return Export(values.Cast(m => m.To<NewsMessage>()), cancellationToken);
			else if (DataType == DataType.Securities)
				return Export(values.Cast(m => m.To<SecurityMessage>()), cancellationToken);
			else if (DataType == DataType.PositionChanges)
				return Export(values.Cast(m => m.To<PositionChangeMessage>()), cancellationToken);
			else if (DataType == TraderHelper.IndicatorValue)
				return Export(values.Cast(m => m.To<IndicatorValue>()), cancellationToken);
			else if (DataType == DataType.BoardState)
				return Export(values.Cast(m => m.To<BoardStateMessage>()), cancellationToken);
			else if (DataType == DataType.Board)
				return Export(values.Cast(m => m.To<BoardMessage>()), cancellationToken);
			else
				throw new InvalidOperationException(DataType.ToString());
		});
	}

	/// <summary>
	/// To export <see cref="QuoteChangeMessage"/> asynchronously.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> Export(IAsyncEnumerable<QuoteChangeMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="Level1ChangeMessage"/> asynchronously.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> Export(IAsyncEnumerable<Level1ChangeMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="DataType.Ticks"/> asynchronously.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> ExportTicksAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="DataType.OrderLog"/> asynchronously.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> ExportOrderLogAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="DataType.Transactions"/> asynchronously.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> ExportTransactionsAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="CandleMessage"/> asynchronously.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> Export(IAsyncEnumerable<CandleMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="NewsMessage"/> asynchronously.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> Export(IAsyncEnumerable<NewsMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="SecurityMessage"/> asynchronously.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> Export(IAsyncEnumerable<SecurityMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="PositionChangeMessage"/> asynchronously.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> Export(IAsyncEnumerable<PositionChangeMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="IndicatorValue"/> asynchronously.
	/// </summary>
	/// <param name="values">Values.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> Export(IAsyncEnumerable<IndicatorValue> values, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="BoardStateMessage"/> asynchronously.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> Export(IAsyncEnumerable<BoardStateMessage> messages, CancellationToken cancellationToken);

	/// <summary>
	/// To export <see cref="BoardMessage"/> asynchronously.
	/// </summary>
	/// <param name="messages">Messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	protected abstract Task<(int, DateTime?)> Export(IAsyncEnumerable<BoardMessage> messages, CancellationToken cancellationToken);
}