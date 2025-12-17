namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface for storage buffer.
/// </summary>
public interface IStorageBuffer : IPersistable
{
	/// <summary>
	/// Save data only for subscriptions.
	/// </summary>
	bool FilterSubscription { get; set; }

	/// <summary>
	/// Enable storage.
	/// </summary>
	bool Enabled { get; set; }

	/// <summary>
	/// Enable level1 storage.
	/// </summary>
	bool EnabledLevel1 { get; set; }

	/// <summary>
	/// Enable order book storage.
	/// </summary>
	bool EnabledOrderBook { get; set; }

	/// <summary>
	/// Enable positions storage.
	/// </summary>
	bool EnabledPositions { get; set; }

	/// <summary>
	/// Enable transactions storage.
	/// </summary>
	bool EnabledTransactions { get; set; }

	/// <summary>
	/// <see cref="BufferMessageAdapter.StartStorageTimer"/>.
	/// </summary>
	bool DisableStorageTimer { get; set; }

	/// <summary>
	/// Ignore messages with <see cref="IGeneratedMessage.BuildFrom"/> is not <see langword="null"/>.
	/// </summary>
	ISet<DataType> IgnoreGenerated { get; }

	/// <summary>
	/// Get accumulated <see cref="DataType.Ticks"/>.
	/// </summary>
	/// <returns>Ticks.</returns>
	IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTicks();

	/// <summary>
	/// Get accumulated <see cref="DataType.OrderLog"/>.
	/// </summary>
	/// <returns>Order log.</returns>
	IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetOrderLog();

	/// <summary>
	/// Get accumulated <see cref="DataType.Transactions"/>.
	/// </summary>
	/// <returns>Transactions.</returns>
	IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTransactions();

	/// <summary>
	/// Get accumulated <see cref="CandleMessage"/>.
	/// </summary>
	/// <returns>Candles.</returns>
	IDictionary<(SecurityId secId, DataType dataType), IEnumerable<CandleMessage>> GetCandles();

	/// <summary>
	/// Get accumulated <see cref="Level1ChangeMessage"/>.
	/// </summary>
	/// <returns>Level1.</returns>
	IDictionary<SecurityId, IEnumerable<Level1ChangeMessage>> GetLevel1();

	/// <summary>
	/// Get accumulated <see cref="PositionChangeMessage"/>.
	/// </summary>
	/// <returns>Position changes.</returns>
	IDictionary<SecurityId, IEnumerable<PositionChangeMessage>> GetPositionChanges();

	/// <summary>
	/// Get accumulated <see cref="QuoteChangeMessage"/>.
	/// </summary>
	/// <returns>Order books.</returns>
	IDictionary<SecurityId, IEnumerable<QuoteChangeMessage>> GetOrderBooks();

	/// <summary>
	/// Get accumulated <see cref="NewsMessage"/>.
	/// </summary>
	/// <returns>News.</returns>
	IEnumerable<NewsMessage> GetNews();

	/// <summary>
	/// Get accumulated <see cref="BoardStateMessage"/>.
	/// </summary>
	/// <returns>States.</returns>
	IEnumerable<BoardStateMessage> GetBoardStates();

	/// <summary>
	/// Process incoming message.
	/// </summary>
	/// <param name="message">Message.</param>
	void ProcessInMessage(Message message);

	/// <summary>
	/// Process outgoing message.
	/// </summary>
	/// <param name="message">Message.</param>
	void ProcessOutMessage(Message message);
}