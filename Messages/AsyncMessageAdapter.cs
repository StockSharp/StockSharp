namespace StockSharp.Messages;

/// <summary>
/// Message adapter with async processing support.
/// </summary>
public abstract class AsyncMessageAdapter : MessageAdapter, IAsyncMessageAdapter
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

	/// <inheritdoc />
	[Browsable(false)]
	public virtual TimeSpan DisconnectTimeout { get; } = TimeSpan.FromSeconds(5);

	private int _maxParallelMessages = 5;

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
	public virtual ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		SendOutMessage(new ConnectMessage());
		return default;
	}

	/// <inheritdoc />
	public virtual ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		SendOutMessage(new DisconnectMessage());
		return default;
	}

	/// <inheritdoc />
	public virtual ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		SendOutMessage(new ResetMessage());
		return default;
	}

	/// <inheritdoc />
	public virtual ValueTask ChangePasswordAsync(ChangePasswordMessage pwdMsg, CancellationToken cancellationToken)
		=> SendInMessageAsync(pwdMsg, cancellationToken);

	/// <inheritdoc />
	public virtual ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> SendInMessageAsync(lookupMsg, cancellationToken);

	/// <inheritdoc />
	public virtual ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> SendInMessageAsync(lookupMsg, cancellationToken);

	/// <inheritdoc />
	public virtual ValueTask BoardLookupAsync(BoardLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> SendInMessageAsync(lookupMsg, cancellationToken);

	/// <inheritdoc />
	public virtual ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
		=> SendInMessageAsync(statusMsg, cancellationToken);

	/// <inheritdoc />
	public virtual ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
		=> SendInMessageAsync(regMsg, cancellationToken);

	/// <inheritdoc />
	public virtual ValueTask ReplaceOrderAsync(OrderReplaceMessage replaceMsg, CancellationToken cancellationToken)
		=> SendInMessageAsync(replaceMsg, cancellationToken);

	/// <inheritdoc />
	public virtual ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
		=> SendInMessageAsync(cancelMsg, cancellationToken);

	/// <inheritdoc />
	public virtual ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
		=> SendInMessageAsync(cancelMsg, cancellationToken);

	/// <inheritdoc />
	public virtual ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
		=> SendInMessageAsync(timeMsg, cancellationToken);

	/// <inheritdoc />
	public virtual ValueTask SendInMessageAsync(Message msg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	public virtual async ValueTask MarketDataAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		if (mdMsg.IsSubscribe)
		{
			var now = DateTime.UtcNow;

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
	/// Handles subscription request for news data.
	/// Override to provide implementation for news subscription processing.
	/// The default implementation throws <see cref="SubscriptionResponseMessage.NotSupported"/>.
	/// </summary>
	/// <param name="mdMsg">Market data subscription message.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
	protected virtual ValueTask OnNewsSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// Handles subscription request for level1 data.
	/// Override to provide implementation for level1 subscription processing.
	/// The default implementation throws <see cref="SubscriptionResponseMessage.NotSupported"/>.
	/// </summary>
	/// <param name="mdMsg">Market data subscription message.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
	protected virtual ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// Handles subscription request for ticks data.
	/// Override to provide implementation for ticks subscription processing.
	/// The default implementation throws <see cref="SubscriptionResponseMessage.NotSupported"/>.
	/// </summary>
	/// <param name="mdMsg">Market data subscription message.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
	protected virtual ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// Handles subscription request for market depth data.
	/// Override to provide implementation for market depth subscription processing.
	/// The default implementation throws <see cref="SubscriptionResponseMessage.NotSupported"/>.
	/// </summary>
	/// <param name="mdMsg">Market data subscription message.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
	protected virtual ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// Handles subscription request for order log (trades/transactions) data.
	/// Override to provide implementation for order log subscription processing.
	/// The default implementation throws <see cref="SubscriptionResponseMessage.NotSupported"/>.
	/// </summary>
	/// <param name="mdMsg">Market data subscription message.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
	protected virtual ValueTask OnOrderLogSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// Handles subscription request for time-frame candles (TF candles) data.
	/// Override to provide implementation for TF candles subscription processing.
	/// The default implementation throws <see cref="SubscriptionResponseMessage.NotSupported"/>.
	/// </summary>
	/// <param name="mdMsg">Market data subscription message.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
	protected virtual ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// Handles subscription request for candles data.
	/// Override to provide implementation for candles subscription processing.
	/// The default implementation throws <see cref="SubscriptionResponseMessage.NotSupported"/>.
	/// </summary>
	/// <param name="mdMsg">Market data subscription message.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
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