namespace StockSharp.Messages;

/// <summary>
/// Message adapter with async processing support.
/// </summary>
public abstract class AsyncMessageAdapter : MessageAdapter
{
	private readonly AsyncMessageProcessor _asyncMessageProcessor;

	/// <summary>
	/// Initialize <see cref="AsyncMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	protected AsyncMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		_asyncMessageProcessor = new(this);
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_asyncMessageProcessor.Dispose();
		base.DisposeManaged();
	}

	/// <inheritdoc />
	[Browsable(false)]
	public override bool IsSupportPartialDownloading => false;

	/// <summary>
	/// Disconnect timeout.
	/// </summary>
	[Browsable(false)]
	public virtual TimeSpan DisconnectTimeout { get; } = TimeSpan.FromSeconds(5);

	private int _maxParallelMessages = 5;

	/// <summary>
	/// Max number of parallel (non-control) messages processing.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ParallelKey,
		Description = LocalizedStrings.ParallelDescKey,
		GroupName = LocalizedStrings.AdaptersKey,
		Order = 310)]
	public int MaxParallelMessages
	{
		get => _maxParallelMessages;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_maxParallelMessages = value;
		}
	}

	private TimeSpan _faultDelay = TimeSpan.FromSeconds(2);

	/// <summary>
	/// Delay between faulted iterations.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FaultDelayKey,
		Description = LocalizedStrings.FaultDelayDescKey,
		GroupName = LocalizedStrings.AdaptersKey,
		Order = 310)]
	public TimeSpan FaultDelay
	{
		get => _faultDelay;
		set
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_faultDelay = value;
		}
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
		=> _asyncMessageProcessor.EnqueueMessage(message);

	/// <summary>
	/// Process <see cref="ConnectMessage"/>.
	/// </summary>
	/// <param name="connectMsg"><see cref="ConnectMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		SendOutMessage(new ConnectMessage());
		return default;
	}

	/// <summary>
	/// Process <see cref="DisconnectMessage"/>.
	/// </summary>
	/// <param name="disconnectMsg"><see cref="DisconnectMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		SendOutMessage(new DisconnectMessage());
		return default;
	}

	/// <summary>
	/// Process <see cref="ResetMessage"/>.
	/// </summary>
	/// <remarks>
	/// Must NOT throw.
	/// </remarks>
	/// <param name="resetMsg"><see cref="ResetMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		SendOutMessage(new ResetMessage());
		return default;
	}

	/// <summary>
	/// Process <see cref="ChangePasswordMessage"/>.
	/// </summary>
	/// <param name="pwdMsg"><see cref="ChangePasswordMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask ChangePasswordAsync(ChangePasswordMessage pwdMsg, CancellationToken cancellationToken)
		=> ProcessMessageAsync(pwdMsg, cancellationToken);

	/// <summary>
	/// Process <see cref="SecurityLookupMessage"/>.
	/// </summary>
	/// <param name="lookupMsg"><see cref="SecurityLookupMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> ProcessMessageAsync(lookupMsg, cancellationToken);

	/// <summary>
	/// Process <see cref="PortfolioLookupMessage"/>.
	/// </summary>
	/// <param name="lookupMsg"><see cref="PortfolioLookupMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> ProcessMessageAsync(lookupMsg, cancellationToken);

	/// <summary>
	/// Process <see cref="BoardLookupMessage"/>.
	/// </summary>
	/// <param name="lookupMsg"><see cref="BoardLookupMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask BoardLookupAsync(BoardLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> ProcessMessageAsync(lookupMsg, cancellationToken);

	/// <summary>
	/// Process <see cref="OrderStatusMessage"/>.
	/// </summary>
	/// <param name="statusMsg"><see cref="OrderStatusMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
		=> ProcessMessageAsync(statusMsg, cancellationToken);

	/// <summary>
	/// Process <see cref="OrderRegisterMessage"/>.
	/// </summary>
	/// <param name="regMsg"><see cref="OrderRegisterMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
		=> ProcessMessageAsync(regMsg, cancellationToken);

	/// <summary>
	/// Process <see cref="OrderReplaceMessage"/>.
	/// </summary>
	/// <param name="replaceMsg"><see cref="OrderReplaceMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask ReplaceOrderAsync(OrderReplaceMessage replaceMsg, CancellationToken cancellationToken)
		=> ProcessMessageAsync(replaceMsg, cancellationToken);

	/// <summary>
	/// Process <see cref="OrderCancelMessage"/>.
	/// </summary>
	/// <param name="cancelMsg"><see cref="OrderCancelMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
		=> ProcessMessageAsync(cancelMsg, cancellationToken);

	/// <summary>
	/// Process <see cref="OrderGroupCancelMessage"/>.
	/// </summary>
	/// <param name="cancelMsg"><see cref="OrderGroupCancelMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
		=> ProcessMessageAsync(cancelMsg, cancellationToken);

	/// <summary>
	/// Process <see cref="TimeMessage"/>.
	/// </summary>
	/// <param name="timeMsg"><see cref="TimeMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
		=> ProcessMessageAsync(timeMsg, cancellationToken);

	/// <summary>
	/// Process <see cref="Message"/>.
	/// </summary>
	/// <param name="msg"><see cref="Message"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual ValueTask ProcessMessageAsync(Message msg, CancellationToken cancellationToken)
		=> default;

	/// <summary>
	/// Process <see cref="MarketDataMessage"/>.
	/// </summary>
	/// <param name="mdMsg"><see cref="MarketDataMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public virtual async ValueTask MarketDataAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		if (mdMsg.IsSubscribe)
		{
			var now = DateTimeOffset.UtcNow;

			var from = mdMsg.From;
			var to = mdMsg.To;

			if ((from > now && mdMsg.IsHistoryOnly()) || from > to)
			{
				SendSubscriptionResult(mdMsg);
				return;
			}
		}

		var dataType = mdMsg.DataType2;

		var task =
				dataType == DataType.News 		? OnNewsSubscriptionAsync(mdMsg, cancellationToken)
			: dataType == DataType.Level1 		? OnLevel1SubscriptionAsync(mdMsg, cancellationToken)
			: dataType == DataType.Ticks 		? OnTicksSubscriptionAsync(mdMsg, cancellationToken)
			: dataType == DataType.MarketDepth 	? OnMarketDepthSubscriptionAsync(mdMsg, cancellationToken)
			: dataType == DataType.OrderLog 	? OnOrderLogSubscriptionAsync(mdMsg, cancellationToken)
			: dataType.IsTFCandles 				? OnTFCandlesSubscriptionAsync(mdMsg, cancellationToken)
			: dataType.IsCandles 				? OnCandlesSubscriptionAsync(mdMsg, cancellationToken)
			: throw SubscriptionResponseMessage.NotSupported;

		await task;
	}

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnNewsSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnOrderLogSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	public override TimeSpan GetHistoryStepSize(SecurityId securityId, DataType dataType, out TimeSpan iterationInterval)
	{
		iterationInterval = TimeSpan.Zero;
		return TimeSpan.MaxValue;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
			.Set(nameof(MaxParallelMessages), MaxParallelMessages)
			.Set(nameof(FaultDelay), FaultDelay)
		;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		MaxParallelMessages = storage.GetValue(nameof(MaxParallelMessages), MaxParallelMessages);
		FaultDelay = storage.GetValue(nameof(FaultDelay), FaultDelay);
	}
}