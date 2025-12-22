namespace StockSharp.Algo;

/// <summary>
/// The messages adapter build order book and tick data from order log flow.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderLogMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Underlying adapter.</param>
public class OrderLogMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	private class SubscriptionInfo
	{
		public readonly AsyncLock Lock = new();

		public SubscriptionInfo(MarketDataMessage origin)
		{
			Origin = origin ?? throw new ArgumentNullException(nameof(origin));
			IsTicks = Origin.DataType2 == DataType.Ticks;
		}

		public MarketDataMessage Origin { get; }

		public readonly bool IsTicks;

		public IOrderLogMarketDepthBuilder Builder { get; set; }
		public SubscriptionStates State { get; set; }
	}

	private readonly SynchronizedDictionary<long, SubscriptionInfo> _subscriptionIds = [];

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				_subscriptionIds.Clear();
				break;

			case MessageTypes.MarketData:
				message = ProcessMarketDataRequest((MarketDataMessage)message);
				break;
		}

		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	private MarketDataMessage ProcessMarketDataRequest(MarketDataMessage message)
	{
		if (message.IsSubscribe)
		{
			if (message.SecurityId == default || !InnerAdapter.IsMarketDataTypeSupported(DataType.OrderLog))
				return message;

			var isBuild = message.BuildMode == MarketDataBuildModes.Build && message.BuildFrom == DataType.OrderLog;

			if (message.DataType2 == DataType.MarketDepth)
			{
				if (isBuild || !InnerAdapter.IsMarketDataTypeSupported(message.DataType2))
				{
					var builder = message.DepthBuilder ?? InnerAdapter.CreateOrderLogMarketDepthBuilder(message.SecurityId);

					_subscriptionIds.Add(message.TransactionId, new SubscriptionInfo(message.TypedClone()) { Builder = builder });

					message = message.TypedClone();
					message.DataType2 = DataType.OrderLog;

					LogInfo("OL->MD subscribed {0}/{1}.", message.SecurityId, message.TransactionId);
				}
			}
			else if (message.DataType2 == DataType.Ticks)
			{
				if (isBuild || !InnerAdapter.IsMarketDataTypeSupported(message.DataType2))
				{
					_subscriptionIds.Add(message.TransactionId, new SubscriptionInfo(message.TypedClone()));

					message = message.TypedClone();
					message.DataType2 = DataType.OrderLog;

					LogInfo("OL->TICK subscribed {0}/{1}.", message.SecurityId, message.TransactionId);
				}
			}
		}
		else
			TryRemoveSubscription(message.OriginalTransactionId, out _);

		return message;
	}

	private bool TryRemoveSubscription(long id, out SubscriptionInfo info)
	{
		if (!_subscriptionIds.TryGetAndRemove(id, out info))
			return false;

		LogInfo("OL->{0} unsubscribed {1}/{2}.", info.IsTicks ? "MD" : "TICK", info.Origin.SecurityId, info.Origin.TransactionId);
		return true;
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.SubscriptionResponse:
			{
				var responseMsg = (SubscriptionResponseMessage)message;
				var id = responseMsg.OriginalTransactionId;

				if (!responseMsg.IsOk())
				{
					if (TryRemoveSubscription(id, out var info))
					{
						using (await info.Lock.LockAsync(cancellationToken))
							info.State = info.State.ChangeSubscriptionState(SubscriptionStates.Error, id, this);
					}
				}
				else if (_subscriptionIds.TryGetValue(id, out var info) && info.State != SubscriptionStates.Online)
				{
					QuoteChangeMessage snapshot = null;

					using (await info.Lock.LockAsync(cancellationToken))
					{
						info.State = info.State.ChangeSubscriptionState(SubscriptionStates.Online, id, this);

						if (!info.IsTicks)
						{
							snapshot = info.Builder.GetSnapshot(responseMsg.LocalTime)
								?? throw new InvalidOperationException(LocalizedStrings.MarketDepthIsEmpty);
						}

						if (snapshot != null)
							snapshot.SetSubscriptionIds(subscriptionId: id);
					}

					if (snapshot != null)
						await base.OnInnerAdapterNewOutMessageAsync(snapshot, cancellationToken);
				}

				break;
			}
			case MessageTypes.SubscriptionFinished:
			{
				var id = ((SubscriptionFinishedMessage)message).OriginalTransactionId;

				if (TryRemoveSubscription(id, out var info))
				{
					using (await info.Lock.LockAsync(cancellationToken))
						info.State = info.State.ChangeSubscriptionState(SubscriptionStates.Finished, id, this);
				}

				break;
			}
			case MessageTypes.Execution:
			{
				if (_subscriptionIds.Count == 0)
					break;

				var execMsg = (ExecutionMessage)message;

				if (execMsg.DataType == DataType.OrderLog && execMsg.IsSystem != false)
					message = await ProcessBuildersAsync(execMsg, cancellationToken);

				break;
			}
		}

		if (message != null)
			await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	private async ValueTask<Message> ProcessBuildersAsync(ExecutionMessage execMsg, CancellationToken cancellationToken)
	{
		List<long> nonBuilderIds = null;

		foreach (var subscriptionId in execMsg.GetSubscriptionIds())
		{
			if (!_subscriptionIds.TryGetValue(subscriptionId, out var info))
			{
				nonBuilderIds ??= [];

				nonBuilderIds.Add(subscriptionId);

				// can be non OL->MB subscription
				//LogDebug("OL processing {0}/{1} not found.", execMsg.SecurityId, subscriptionId);
				continue;
			}

			if (!info.IsTicks)
			{
				try
				{
					QuoteChangeMessage depth;

					using (await info.Lock.LockAsync(cancellationToken))
					{
						depth = info.Builder.Update(execMsg);

						if (info.State != SubscriptionStates.Online)
							depth = null;
						else
							depth = depth?.TypedClone();
					}

					LogDebug("OL->MD processing {0}={1}.", execMsg.SecurityId, depth != null);

					if (depth != null)
					{
						depth.SetSubscriptionIds(subscriptionId: subscriptionId);
						await base.OnInnerAdapterNewOutMessageAsync(depth, cancellationToken);
					}
				}
				catch (Exception ex)
				{
					// если ОЛ поврежден, то не нарушаем весь цикл обработки сообщения
					// а только выводим сообщение в лог
					await base.OnInnerAdapterNewOutMessageAsync(ex.ToErrorMessage(), cancellationToken);
				}
			}
			else
			{
				LogDebug("OL->TICK processing {0}.", execMsg.SecurityId);
				await base.OnInnerAdapterNewOutMessageAsync(execMsg.ToTick(), cancellationToken);
			}
		}

		if (nonBuilderIds is null)
			return null;

		execMsg.SetSubscriptionIds([.. nonBuilderIds]);
		return execMsg;
	}

	/// <summary>
	/// Create a copy of <see cref="OrderLogMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone() => new OrderLogMessageAdapter(InnerAdapter.TypedClone());
}