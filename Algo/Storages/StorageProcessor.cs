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
	private readonly SynchronizedSet<long> _servedSubscriptions = [];

	/// <inheritdoc/>
	public StorageCoreSettings Settings { get; } = settings ?? throw new ArgumentNullException(nameof(settings));

	/// <inheritdoc/>
	public CandleBuilderProvider CandleBuilderProvider { get; } = candleBuilderProvider ?? throw new ArgumentNullException(nameof(candleBuilderProvider));

	void IStorageProcessor.Reset()
	{
		_fullyProcessedSubscriptions.Clear();
		_servedSubscriptions.Clear();
	}

	async IAsyncEnumerable<Message> IStorageProcessor.ProcessMarketData(MarketDataMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		// No start date and no count — forward upstream, nothing to serve from storage.
		// A count request without From is a "last Count records" query and is served
		// from the tail of the storage (resolved in StorageHelper.GetRangeAsync).
		// An unsubscribe carries no range of its own and is answered below by the subscription it ends.
		if (message.IsSubscribe && message.From == null && !(message.Count > 0) /*&& Settings.DaysLoad == TimeSpan.Zero*/)
		{
			yield return message;
			yield break;
		}

		MarketDataMessage forwardMessage = message;

		if (message.IsSubscribe)
		{
			// One processor can sit in more than one wrapper of the same pipeline, and history a
			// subscriber asked for once has to arrive once: the second pass only passes the request on.
			if (message.SecurityId != default && _servedSubscriptions.TryAdd(message.TransactionId))
			{
				var transactionId = message.TransactionId;
				var context = new StorageLoadContext();

				await foreach (var outMsg in Settings.LoadMessagesAsync(CandleBuilderProvider, message, context, cancellationToken))
					yield return outMsg;

				// The request is complete when storage reached the end of the range, and equally when
				// the asked-for Count ran out - a remainder of an exhausted Count is a request for nothing.
				if (context.HasData && (context.Left == 0 || (message.To != null && message.To <= context.LastDate)))
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
			_servedSubscriptions.Remove(message.OriginalTransactionId);

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
