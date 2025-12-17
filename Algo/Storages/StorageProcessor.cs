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
public class StorageProcessor(StorageCoreSettings settings, CandleBuilderProvider candleBuilderProvider) : IStorageProcessor
{
	private readonly SynchronizedSet<long> _fullyProcessedSubscriptions = [];

	/// <inheritdoc/>
	public StorageCoreSettings Settings { get; } = settings ?? throw new ArgumentNullException(nameof(settings));

	/// <inheritdoc/>
	public CandleBuilderProvider CandleBuilderProvider { get; } = candleBuilderProvider ?? throw new ArgumentNullException(nameof(candleBuilderProvider));

	void IStorageProcessor.Reset()
	{
		_fullyProcessedSubscriptions.Clear();
	}

	MarketDataMessage IStorageProcessor.ProcessMarketData(MarketDataMessage message, Action<Message> newOutMessage)
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