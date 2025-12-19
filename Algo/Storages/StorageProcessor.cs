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

	async IAsyncEnumerable<Message> IStorageProcessor.ProcessMarketData(MarketDataMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (message.From == null /*&& Settings.DaysLoad == TimeSpan.Zero*/)
		{
			yield return message;
			yield break;
		}

		MarketDataMessage forwardMessage = message;

		if (message.IsSubscribe)
		{
			if (message.SecurityId != default)
			{
				var transactionId = message.TransactionId;
				var context = new StorageLoadContext();

				await foreach (var outMsg in Settings.LoadMessagesAsync(CandleBuilderProvider, message, context, cancellationToken))
					yield return outMsg;

				if (message.To != null && context.HasData && (message.To <= context.LastDate || context.Left == 0))
				{
					_fullyProcessedSubscriptions.Add(transactionId);
					yield return new SubscriptionFinishedMessage { OriginalTransactionId = transactionId };
					forwardMessage = null;
				}
				else if (context.HasData)
				{
					if (!(message.DataType2 == DataType.MarketDepth && message.From == null && message.To == null))
					{
						var clone = message.TypedClone();
						clone.From = context.LastDate;
						clone.Count = context.Left;
						forwardMessage = clone;
						forwardMessage.ValidateBounds();
					}
				}
			}
		}
		else
		{
			if (_fullyProcessedSubscriptions.Remove(message.OriginalTransactionId))
			{
				yield return new SubscriptionResponseMessage
				{
					OriginalTransactionId = message.TransactionId,
				};

				forwardMessage = null;
			}
		}

		if (forwardMessage != null)
			yield return forwardMessage;
	}
}
