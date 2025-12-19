namespace StockSharp.Algo.Storages;

using System.Threading.Channels;

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

		var channel = Channel.CreateBounded<Message>(new BoundedChannelOptions(1024)
		{
			SingleReader = true,
			SingleWriter = true,
			FullMode = BoundedChannelFullMode.Wait,
			AllowSynchronousContinuations = true,
		});

		using var producerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		var producerToken = producerCts.Token;

		var producerTask = Task.Run(() =>
		{
			try
			{
				void Write(Message outMsg)
					=> channel.Writer.WriteAsync(outMsg, producerToken).AsTask().GetAwaiter().GetResult();

				MarketDataMessage forwardMessage = message;

				if (message.IsSubscribe)
				{
					if (message.SecurityId == default)
					{
					}
					else
					{
						var transactionId = message.TransactionId;

						var t = Settings.LoadMessages(CandleBuilderProvider, message, Write);

						if (message.To != null && t != null && (message.To <= t.Value.lastDate || t.Value.left == 0))
						{
							_fullyProcessedSubscriptions.Add(transactionId);
							Write(new SubscriptionFinishedMessage { OriginalTransactionId = transactionId });
							forwardMessage = null;
						}
						else if (t != null)
						{
							if (!(message.DataType2 == DataType.MarketDepth && message.From == null && message.To == null))
							{
								var clone = message.TypedClone();
								clone.From = t.Value.lastDate;
								clone.Count = t.Value.left;
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
						Write(new SubscriptionResponseMessage
						{
							OriginalTransactionId = message.TransactionId,
						});

						forwardMessage = null;
					}
				}

				if (forwardMessage != null)
					Write(forwardMessage);

				channel.Writer.TryComplete();
			}
			catch (Exception ex)
			{
				channel.Writer.TryComplete(ex);
			}
		}, producerToken);

		try
		{
			await foreach (var outMsg in channel.Reader.ReadAllAsync(cancellationToken))
				yield return outMsg;
		}
		finally
		{
			producerCts.Cancel();

			try
			{
				await producerTask;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}
		}
	}
}
