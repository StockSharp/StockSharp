namespace StockSharp.Algo.Storages;

using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Storage processor.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StorageProcessor"/>.
/// </remarks>
/// <param name="settings">Storage settings.</param>
/// <param name="candleBuilderProvider">Candle builders provider.</param>
public class StorageProcessor(StorageCoreSettings settings, CandleBuilderProvider candleBuilderProvider)
{
	private readonly SynchronizedSet<long> _fullyProcessedSubscriptions = [];

	/// <summary>
	/// Storage settings.
	/// </summary>
	public StorageCoreSettings Settings { get; } = settings ?? throw new ArgumentNullException(nameof(settings));

	/// <summary>
	/// Candle builders provider.
	/// </summary>
	public CandleBuilderProvider CandleBuilderProvider { get; } = candleBuilderProvider ?? throw new ArgumentNullException(nameof(candleBuilderProvider));

	/// <summary>
	/// To reset the state.
	/// </summary>
	public void Reset()
	{
		_fullyProcessedSubscriptions.Clear();
	}

	/// <summary>
	/// Process <see cref="MarketDataMessage"/>.
	/// </summary>
	/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
	/// <param name="newOutMessage">New message event.</param>
	/// <returns>Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</returns>
	public MarketDataMessage ProcessMarketData(MarketDataMessage message, Action<Message> newOutMessage)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (newOutMessage is null)
			throw new ArgumentNullException(nameof(newOutMessage));

		if (message.From == null /*&& Settings.DaysLoad == TimeSpan.Zero*/)
			return message;

		if (message.IsSubscribe)
		{
			if (message.SecurityId == default)
				return message;

			var transactionId = message.TransactionId;

			var t = Settings.LoadMessages(CandleBuilderProvider, message, newOutMessage);

			if (message.To != null && t != null && (message.To <= t.Value.lastDate || t.Value.left == 0))
			{
				_fullyProcessedSubscriptions.Add(transactionId);
				newOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = transactionId });

				return null;
			}

			if (t != null)
			{
				if (!(message.DataType2 == DataType.MarketDepth && message.From == null && message.To == null))
				{
					var clone = message.TypedClone();
					clone.From = t.Value.lastDate;
					clone.Count = t.Value.left;
					message = clone;
					message.ValidateBounds();
				}
			}
		}
		else
		{
			if (_fullyProcessedSubscriptions.Remove(message.OriginalTransactionId))
			{
				newOutMessage(new SubscriptionResponseMessage
				{
					OriginalTransactionId = message.TransactionId,
				});

				return null;
			}
		}

		return message;
	}
}