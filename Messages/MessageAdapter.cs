namespace StockSharp.Messages;

using System.Runtime.CompilerServices;

/// <summary>
/// The base adapter converts messages <see cref="Message"/> to the command of the trading system and back.
/// </summary>
public abstract partial class MessageAdapter : BaseLogReceiver, IMessageAdapter, INotifyPropertyChanged
{
	/// <summary>
	/// Initialize <see cref="MessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	protected MessageAdapter(IdGenerator transactionIdGenerator)
	{
		TransactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));

		StorageName = GetType().Namespace.Remove(nameof(StockSharp)).Remove(".");

		Platform = GetType().GetPlatform();

		var attr = GetType().GetAttribute<MessageAdapterCategoryAttribute>();
		if (attr != null)
			Categories = attr.Categories;
	}

	private IEnumerable<MessageTypes> CheckDuplicate(IEnumerable<MessageTypes> value, string propName)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		var arr = value.ToArray();

		var duplicate = arr.GroupBy(m => m).FirstOrDefault(g => g.Count() > 1);
		if (duplicate != null)
			throw new ArgumentException(LocalizedStrings.HasDuplicates.Put(duplicate.Key), nameof(value));

		OnPropertyChanged(propName);

		return arr;
	}

	private IEnumerable<MessageTypes> _supportedInMessages = [];

	/// <inheritdoc />
	[Browsable(false)]
	public virtual IEnumerable<MessageTypes> SupportedInMessages
	{
		get => _supportedInMessages;
		set => _supportedInMessages = CheckDuplicate(value, nameof(SupportedInMessages));
	}

	private IEnumerable<MessageTypes> _notSupportedResultMessages = [];

	/// <inheritdoc />
	[Browsable(false)]
	public virtual IEnumerable<MessageTypes> NotSupportedResultMessages
	{
		get => _notSupportedResultMessages;
		set => _notSupportedResultMessages = CheckDuplicate(value, nameof(NotSupportedResultMessages));
	}

	private readonly CachedSynchronizedSet<MessageTypeInfo> _possibleSupportedMessages = [];

	/// <inheritdoc />
	[Browsable(false)]
	public virtual IEnumerable<MessageTypeInfo> PossibleSupportedMessages
	{
		get => _possibleSupportedMessages.Cache;
		set
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var duplicate = value.GroupBy(m => m.Type).FirstOrDefault(g => g.Count() > 1);
			if (duplicate != null)
				throw new ArgumentException(LocalizedStrings.HasDuplicates.Put(duplicate.Key), nameof(value));

			_possibleSupportedMessages.Clear();
			_possibleSupportedMessages.AddRange(value);

			OnPropertyChanged();

			SupportedInMessages = [.. value.Select(t => t.Type)];
		}
	}

	private readonly CachedSynchronizedSet<DataType> _supportedMarketDataTypes = [];

	/// <inheritdoc />
	[Browsable(false)]
	internal IEnumerable<DataType> SupportedMarketDataTypes
	{
		get => _supportedMarketDataTypes.Cache;
		set
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var duplicate = value.GroupBy(m => m).FirstOrDefault(g => g.Count() > 1);
			if (duplicate != null)
				throw new ArgumentException(LocalizedStrings.HasDuplicates.Put(duplicate.Key), nameof(value));

			_supportedMarketDataTypes.Clear();
			_supportedMarketDataTypes.AddRange(value);

			OnPropertyChanged();
		}
	}

	/// <inheritdoc />
	public virtual IAsyncEnumerable<DataType> GetSupportedMarketDataTypesAsync(SecurityId securityId, DateTime? from, DateTime? to)
		=> SupportedMarketDataTypes.ToAsyncEnumerable();

	/// <inheritdoc />
	[Browsable(false)]
	public virtual IEnumerable<Level1Fields> CandlesBuildFrom => [];

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool CheckTimeFrameByRequest => false;

	private TimeSpan _heartbeatInterval = TimeSpan.Zero;

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.HeartBeatKey,
		Description = LocalizedStrings.HeartBeatDescKey,
		GroupName = LocalizedStrings.AdaptersKey,
		Order = 300)]
	public TimeSpan HeartbeatInterval
	{
		get => _heartbeatInterval;
		set
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_heartbeatInterval = value;
		}
	}

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool IsNativeIdentifiersPersistable => true;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool IsNativeIdentifiers => false;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool IsFullCandlesOnly => true;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool IsSupportSubscriptions => true;

	/// <inheritdoc />
	public virtual bool IsSupportCandlesUpdates(MarketDataMessage subscription) => false;

	/// <inheritdoc />
	public virtual bool IsSupportCandlesPriceLevels(MarketDataMessage subscription) => false;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual MessageAdapterCategories Categories { get; }

	/// <inheritdoc />
	[Browsable(false)]
	public virtual string StorageName { get; }

	/// <summary>
	/// Bit process, which can run the adapter.
	/// </summary>
	[Browsable(false)]
	public Platforms Platform { get; protected set; }

	/// <summary>
	/// Feature name.
	/// </summary>
	[Browsable(false)]
	public virtual string FeatureName => string.Empty;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual IEnumerable<(string, Type)> SecurityExtendedFields { get; } = [];

	/// <inheritdoc />
	[Browsable(false)]
	public virtual IEnumerable<int> SupportedOrderBookDepths => [];

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool IsSupportOrderBookIncrements => false;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool IsSupportExecutionsPnL => false;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool IsSecurityNewsOnly => false;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual Type OrderConditionType => GetType()
		.GetAttribute<OrderConditionAttribute>()?
		.ConditionType;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool HeartbeatBeforeConnect => false;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual Uri Icon => GetType().TryGetIconUrl();

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool IsAutoReplyOnTransactonalUnsubscription => false;

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.EnqueueSubscriptionsKey,
		Description = LocalizedStrings.EnqueueSubscriptionsDescKey,
		GroupName = LocalizedStrings.AdaptersKey,
		Order = 301)]
	public virtual bool EnqueueSubscriptions { get; set; }

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool IsSupportTransactionLog => false;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool IsReplaceCommandEditCurrent => false;

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ReConnectionSettingsKey,
		Description = LocalizedStrings.ReConnectionDescKey,
		GroupName = LocalizedStrings.ConnectionKey)]
	public ReConnectionSettings ReConnectionSettings { get; } = new ReConnectionSettings();

	private IdGenerator _transactionIdGenerator;

	/// <inheritdoc />
	[Browsable(false)]
	public IdGenerator TransactionIdGenerator
	{
		get => _transactionIdGenerator;
		set => _transactionIdGenerator = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool ExtraSetup => false;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool UseInChannel => true;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool UseOutChannel => true;

	private Func<Message, CancellationToken, ValueTask> _newOutMessageAsync;

	/// <inheritdoc />
	public event Func<Message, CancellationToken, ValueTask> NewOutMessageAsync
	{
		add => _newOutMessageAsync += value;
		remove => _newOutMessageAsync -= value;
	}

	/// <inheritdoc />
	[Browsable(false)]
	public virtual string[] AssociatedBoards => [];

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

	private IFileSystem _fileSystem = Extensions.DefaultFileSystem;

	/// <summary>
	/// <see cref="IFileSystem"/>
	/// </summary>
	[Browsable(false)]
	public IFileSystem FileSystem
	{
		get => _fileSystem;
		set => _fileSystem = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Validate the specified security id is supported by the adapter and subscription can be done.
	/// </summary>
	/// <param name="secId"><see cref="SecurityId"/>.</param>
	/// <returns>Check result.</returns>
	protected virtual bool ValidateSecurityId(SecurityId secId)
	{
		if (secId == SecurityId.News)
			return SupportedMarketDataTypes.Contains(DataType.News);

		var boards = AssociatedBoards;

		if (boards.Length > 0)
			return boards.Any(b => secId.IsAssociated(b));

		return false;
	}

	/// <inheritdoc />
	public virtual async ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message.Type == MessageTypes.Connect && !Platform.IsCompatible())
		{
			await SendOutMessageAsync(new ConnectMessage
			{
				Error = new InvalidOperationException(LocalizedStrings.BitSystemIncompatible.Put(GetType().Name, Platform))
			}, cancellationToken);

			return;
		}

		InitMessageLocalTime(message);

		try
		{
			if (message.Type == MessageTypes.MarketData && AssociatedBoards.Length > 0)
			{
				var mdMsg = (MarketDataMessage)message;
				var secId = mdMsg.SecurityId;

				if (!ValidateSecurityId(secId))
				{
					var boardCode = AssociatedBoards.First();
					await SendOutMessageAsync(mdMsg.TransactionId.CreateSubscriptionResponse(new NotSupportedException(LocalizedStrings.WrongSecurityBoard.Put(secId, boardCode, $"{secId.SecurityCode}@{boardCode}"))), cancellationToken);
					return;
				}
			}

			await OnSendInMessageAsync(message, cancellationToken);

			if (IsAutoReplyOnTransactonalUnsubscription)
			{
				switch (message.Type)
				{
					case MessageTypes.PortfolioLookup:
					case MessageTypes.OrderStatus:
					{
						var subscrMsg = (ISubscriptionMessage)message;

						if (!subscrMsg.IsSubscribe)
							await SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = subscrMsg.TransactionId }, cancellationToken);

						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			await SendOutMessageAsync(message.CreateErrorResponse(ex, this), cancellationToken);
		}
	}

	/// <summary>
	/// Send in message handler.
	/// </summary>
	/// <param name="message"><see cref="Message"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	protected virtual ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		return message.Type switch
		{
			MessageTypes.Connect => ConnectAsync((ConnectMessage)message, cancellationToken),
			MessageTypes.Disconnect => DisconnectAsync((DisconnectMessage)message, cancellationToken),
			MessageTypes.Reset => ResetAsync((ResetMessage)message, cancellationToken),
			MessageTypes.ChangePassword => ChangePasswordAsync((ChangePasswordMessage)message, cancellationToken),
			MessageTypes.SecurityLookup => SecurityLookupAsync((SecurityLookupMessage)message, cancellationToken),
			MessageTypes.PortfolioLookup => PortfolioLookupAsync((PortfolioLookupMessage)message, cancellationToken),
			MessageTypes.BoardLookup => BoardLookupAsync((BoardLookupMessage)message, cancellationToken),
			MessageTypes.OrderStatus => OrderStatusAsync((OrderStatusMessage)message, cancellationToken),
			MessageTypes.OrderRegister => RegisterOrderAsync((OrderRegisterMessage)message, cancellationToken),
			MessageTypes.OrderReplace => ReplaceOrderAsync((OrderReplaceMessage)message, cancellationToken),
			MessageTypes.OrderCancel => CancelOrderAsync((OrderCancelMessage)message, cancellationToken),
			MessageTypes.OrderGroupCancel => CancelOrderGroupAsync((OrderGroupCancelMessage)message, cancellationToken),
			MessageTypes.Time => TimeAsync((TimeMessage)message, cancellationToken),
			MessageTypes.MarketData => MarketDataAsync((MarketDataMessage)message, cancellationToken),
			_ => throw SubscriptionResponseMessage.NotSupported,
		};
	}

	/// <inheritdoc />
	protected virtual ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ConnectMessage(), cancellationToken);
	}

	/// <inheritdoc />
	protected virtual ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new DisconnectMessage(), cancellationToken);
	}

	/// <inheritdoc />
	protected virtual ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ResetMessage(), cancellationToken);
	}

	/// <inheritdoc />
	protected virtual ValueTask ChangePasswordAsync(ChangePasswordMessage pwdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	protected virtual ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	protected virtual ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	protected virtual ValueTask BoardLookupAsync(BoardLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	protected virtual ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	protected virtual ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	protected virtual ValueTask ReplaceOrderAsync(OrderReplaceMessage replaceMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	protected virtual ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	protected virtual ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	protected virtual ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <inheritdoc />
	protected virtual async ValueTask MarketDataAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
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
	public virtual void SendOutMessage(Message message)
		=> SendOutMessageAsync(message, default);

	/// <inheritdoc />
	public virtual ValueTask SendOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		//// do not process empty change msgs
		//if (!message.IsBack)
		//{
		//	if (message is Level1ChangeMessage l1Msg && !l1Msg.HasChanges())
		//		return;
		//	else if (message is BasePositionChangeMessage posMsg && !posMsg.HasChanges())
		//		return;
		//}

		InitMessageLocalTime(message);

		message.Adapter ??= this;

		if (message is DataTypeInfoMessage dtim && dtim.FileDataType is DataType dt && dt.IsMarketData)
			this.AddSupportedMarketDataType(dt);

		return _newOutMessageAsync?.Invoke(message, cancellationToken) ?? default;
	}

	/// <summary>
	/// Initialize local timestamp <see cref="Message"/>.
	/// </summary>
	/// <param name="message">Message.</param>
	private void InitMessageLocalTime(Message message)
	{
		message.TryInitLocalTime(this);

		switch (message)
		{
			case PositionChangeMessage posMsg when posMsg.ServerTime == default:
				posMsg.ServerTime = CurrentTimeUtc;
				break;
			case ExecutionMessage execMsg when execMsg.DataType == DataType.Transactions && execMsg.ServerTime == default:
				execMsg.ServerTime = CurrentTimeUtc;
				break;
		}
	}

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> disconnect message.
	/// </summary>
	/// <param name="expected">Is disconnect expected.</param>
	protected void SendOutDisconnectMessage(bool expected)
		=> SendOutDisconnectMessageAsync(expected, default);

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> disconnect message.
	/// </summary>
	/// <param name="error">Error info. Can be <see langword="null"/>.</param>
	protected void SendOutDisconnectMessage(Exception error)
		=> SendOutDisconnectMessageAsync(error, default);

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> disconnect message.
	/// </summary>
	/// <param name="expected">Is disconnect expected.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected ValueTask SendOutDisconnectMessageAsync(bool expected, CancellationToken cancellationToken)
	{
		return SendOutDisconnectMessageAsync(expected ? null : new InvalidOperationException(LocalizedStrings.UnexpectedDisconnection), cancellationToken);
	}

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> disconnect message.
	/// </summary>
	/// <param name="error">Error info. Can be <see langword="null"/>.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected ValueTask SendOutDisconnectMessageAsync(Exception error, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(error == null ? new DisconnectMessage() : new ConnectMessage
		{
			Error = error
		}, cancellationToken);
	}

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> connection state message.
	/// </summary>
	/// <param name="state"><see cref="ConnectionStates"/></param>
	protected void SendOutConnectionState(ConnectionStates state)
		=> SendOutConnectionStateAsync(state, default);

	/// <summary>
	/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="description">Error details.</param>
	protected void SendOutError(string description)
		=> SendOutErrorAsync(description, default);

	/// <summary>
	/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="error">Error details.</param>
	protected void SendOutError(Exception error)
		=> SendOutErrorAsync(error, default);

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionResponseMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="error">Subscribe or unsubscribe error info. To be set if the answer.</param>
	protected void SendSubscriptionReply(long originalTransactionId, Exception error = null)
		=> SendSubscriptionReplyAsync(originalTransactionId, default, error);

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionResponseMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	protected void SendSubscriptionNotSupported(long originalTransactionId)
		=> SendSubscriptionNotSupportedAsync(originalTransactionId, default);

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionFinishedMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="nextFrom"><see cref="SubscriptionFinishedMessage.NextFrom"/>.</param>
	protected void SendSubscriptionFinished(long originalTransactionId, DateTime? nextFrom = null)
		=> SendSubscriptionFinishedAsync(originalTransactionId, default, nextFrom);

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionOnlineMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	protected void SendSubscriptionOnline(long originalTransactionId)
		=> SendSubscriptionOnlineAsync(originalTransactionId, default);

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionOnlineMessage"/> or <see cref="SubscriptionFinishedMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="message">Subscription.</param>
	protected void SendSubscriptionResult(ISubscriptionMessage message)
		=> SendSubscriptionResultAsync(message, default);

	/// <summary>
	/// Send to <see cref="SendOutMessageAsync"/> connection state message.
	/// </summary>
	/// <param name="state"><see cref="ConnectionStates"/></param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected ValueTask SendOutConnectionStateAsync(ConnectionStates state, CancellationToken cancellationToken)
	{
		if (state.ToMessage() is Message msg)
			return SendOutMessageAsync(msg, cancellationToken);

		return default;
	}

	/// <summary>
	/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessageAsync"/>.
	/// </summary>
	/// <param name="description">Error details.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected ValueTask SendOutErrorAsync(string description, CancellationToken cancellationToken)
	{
		return SendOutErrorAsync(new InvalidOperationException(description), cancellationToken);
	}

	/// <summary>
	/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessageAsync"/>.
	/// </summary>
	/// <param name="error">Error details.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected ValueTask SendOutErrorAsync(Exception error, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(error.ToErrorMessage(), cancellationToken);
	}

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionResponseMessage"/> and pass it to the method <see cref="SendOutMessageAsync"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <param name="error">Subscribe or unsubscribe error info. To be set if the answer.</param>
	protected ValueTask SendSubscriptionReplyAsync(long originalTransactionId, CancellationToken cancellationToken, Exception error = null)
	{
		return SendOutMessageAsync(originalTransactionId.CreateSubscriptionResponse(error), cancellationToken);
	}

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionResponseMessage"/> and pass it to the method <see cref="SendOutMessageAsync"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected ValueTask SendSubscriptionNotSupportedAsync(long originalTransactionId, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(originalTransactionId.CreateNotSupported(), cancellationToken);
	}

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionFinishedMessage"/> and pass it to the method <see cref="SendOutMessageAsync"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <param name="nextFrom"><see cref="SubscriptionFinishedMessage.NextFrom"/>.</param>
	protected ValueTask SendSubscriptionFinishedAsync(long originalTransactionId, CancellationToken cancellationToken, DateTime? nextFrom = null)
	{
		return SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = originalTransactionId, NextFrom = nextFrom }, cancellationToken);
	}

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionOnlineMessage"/> and pass it to the method <see cref="SendOutMessageAsync"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected ValueTask SendSubscriptionOnlineAsync(long originalTransactionId, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = originalTransactionId }, cancellationToken);
	}

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionOnlineMessage"/> or <see cref="SubscriptionFinishedMessage"/> and pass it to the method <see cref="SendOutMessageAsync"/>.
	/// </summary>
	/// <param name="message">Subscription.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected ValueTask SendSubscriptionResultAsync(ISubscriptionMessage message, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(message.CreateResult(), cancellationToken);
	}

	/// <inheritdoc />
	public virtual IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		=> new OrderLogMarketDepthBuilder(securityId);

	/// <inheritdoc />
	public virtual bool IsAllDownloadingSupported(DataType dataType) => false;

	/// <inheritdoc />
	public virtual bool IsSecurityRequired(DataType dataType) => dataType.IsSecurityRequired;

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IterationsKey,
		Description = LocalizedStrings.IterationIntervalKey,
		GroupName = LocalizedStrings.AdaptersKey,
		Order = 304)]
	public virtual TimeSpan IterationInterval { get; set; } = TimeSpan.FromSeconds(2);

	/// <inheritdoc />
	[Browsable(false)]
	public virtual TimeSpan? LookupTimeout => null;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool? IsPositionsEmulationRequired => null;

	/// <inheritdoc />
	[ReadOnly(false)]
	public override string Name
	{
		get => base.Name;
		set => base.Name = value;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		Id = storage.GetValue(nameof(Id), Id);
		HeartbeatInterval = storage.GetValue<TimeSpan>(nameof(HeartbeatInterval));

		if (storage.ContainsKey(nameof(SupportedInMessages)))
		{
			SupportedInMessages = Do.Invariant(() => storage.GetValue<string[]>(nameof(SupportedInMessages))
				.Select(i =>
				{
					// TODO Remove few releases later 2024-07-20
					i = i.Length > 1 && i[0] == 1564 ? i.Substring(1) : i;

					return i.ToMessageType();
				}).ToArray());
		}

		if (storage.ContainsKey(nameof(ReConnectionSettings)))
			ReConnectionSettings.Load(storage, nameof(ReConnectionSettings));

		EnqueueSubscriptions = storage.GetValue(nameof(EnqueueSubscriptions), EnqueueSubscriptions);
		IterationInterval = storage.GetValue(nameof(IterationInterval), IterationInterval);

		MaxParallelMessages = storage.GetValue(nameof(MaxParallelMessages), MaxParallelMessages);
		FaultDelay = storage.GetValue(nameof(FaultDelay), FaultDelay);

		base.Load(storage);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Id), Id)
			.Set(nameof(HeartbeatInterval), HeartbeatInterval)
			.Set(nameof(SupportedInMessages), Do.Invariant(() => SupportedInMessages.Select(t => t.To<string>()).ToArray()))
			.Set(nameof(ReConnectionSettings), ReConnectionSettings.Save())
			.Set(nameof(EnqueueSubscriptions), EnqueueSubscriptions)
			.Set(nameof(IterationInterval), IterationInterval)
			.Set(nameof(MaxParallelMessages), MaxParallelMessages)
			.Set(nameof(FaultDelay), FaultDelay)
		;

		base.Save(storage);
	}

	/// <summary>
	/// Create a copy of <see cref="MessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public virtual IMessageAdapter Clone()
	{
		var clone = GetType().CreateInstance<MessageAdapter>(TransactionIdGenerator);
		clone.Load(this.Save());
		return clone;
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	private PropertyChangedEventHandler _propertyChanged;

	event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
	{
		add => _propertyChanged += value;
		remove => _propertyChanged -= value;
	}

	/// <summary>
	/// Raise <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
	/// </summary>
	/// <param name="propertyName">The name of the property that changed.</param>
	protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
	{
		_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}