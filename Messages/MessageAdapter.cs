namespace StockSharp.Messages;

using System.Runtime.CompilerServices;

/// <summary>
/// The base adapter converts messages <see cref="Message"/> to the command of the trading system and back.
/// </summary>
public abstract class MessageAdapter : BaseLogReceiver, IMessageAdapter, INotifyPropertyChanged
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
	public virtual IEnumerable<DataType> GetSupportedMarketDataTypes(SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to)
		=> SupportedMarketDataTypes;

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
	public virtual bool IsSupportPartialDownloading => true;

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
	public virtual IEnumerable<Tuple<string, Type>> SecurityExtendedFields { get; } = [];

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
	public virtual bool HeartbeatBeforConnect => false;

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
	public event Action<Message> NewOutMessage;

	ChannelStates IMessageChannel.State => ChannelStates.Started;

	void IMessageChannel.Open()
	{
	}

	void IMessageChannel.Close()
	{
	}

	void IMessageChannel.Suspend()
	{
	}

	void IMessageChannel.Resume()
	{
	}

	void IMessageChannel.Clear()
	{
	}

	event Action IMessageChannel.StateChanged
	{
		add { }
		remove { }
	}

	/// <inheritdoc />
	[Browsable(false)]
	public virtual string[] AssociatedBoards => [];

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
	public bool SendInMessage(Message message)
	{
		if (message.Type == MessageTypes.Connect)
		{
			if (!Platform.IsCompatible())
			{
				SendOutMessage(new ConnectMessage
				{
					Error = new InvalidOperationException(LocalizedStrings.BitSystemIncompatible.Put(GetType().Name, Platform))
				});

				return true;
			}
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
					SendOutMessage(mdMsg.TransactionId.CreateSubscriptionResponse(new NotSupportedException(LocalizedStrings.WrongSecurityBoard.Put(secId, boardCode, $"{secId.SecurityCode}@{boardCode}"))));
					return false;
				}
			}

			var result = OnSendInMessage(message);

			if (IsAutoReplyOnTransactonalUnsubscription)
			{
				switch (message.Type)
				{
					case MessageTypes.PortfolioLookup:
					case MessageTypes.OrderStatus:
					{
						var subscrMsg = (ISubscriptionMessage)message;

						if (!subscrMsg.IsSubscribe)
							SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = subscrMsg.TransactionId });

						break;
					}
				}
			}

			return result;
		}
		catch (Exception ex)
		{
			SendOutMessage(message.CreateErrorResponse(ex, this));
			return false;
		}
	}

	/// <summary>
	/// Send message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns><see langword="true"/> if the specified message was processed successfully, otherwise, <see langword="false"/>.</returns>
	protected abstract bool OnSendInMessage(Message message);

	/// <summary>
	/// Send outgoing message and raise <see cref="NewOutMessage"/> event.
	/// </summary>
	/// <param name="message">Message.</param>
	protected internal virtual void SendOutMessage(Message message)
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

		NewOutMessage?.Invoke(message);
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
				posMsg.ServerTime = CurrentTime;
				break;
			case ExecutionMessage execMsg when execMsg.DataType == DataType.Transactions && execMsg.ServerTime == default:
				execMsg.ServerTime = CurrentTime;
				break;
		}
	}

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> disconnect message.
	/// </summary>
	/// <param name="expected">Is disconnect expected.</param>
	protected void SendOutDisconnectMessage(bool expected)
	{
		SendOutDisconnectMessage(expected ? null : new InvalidOperationException(LocalizedStrings.UnexpectedDisconnection));
	}

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> disconnect message.
	/// </summary>
	/// <param name="error">Error info. Can be <see langword="null"/>.</param>
	protected void SendOutDisconnectMessage(Exception error)
	{
		SendOutMessage(error == null ? new DisconnectMessage() : new ConnectMessage
		{
			Error = error
		});
	}

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> connection state message.
	/// </summary>
	/// <param name="state"><see cref="ConnectionStates"/></param>
	protected void SendOutConnectionState(ConnectionStates state)
	{
		if (state.ToMessage() is Message msg)
			SendOutMessage(msg);
	}

	/// <summary>
	/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="description">Error details.</param>
	protected void SendOutError(string description)
	{
		SendOutError(new InvalidOperationException(description));
	}

	/// <summary>
	/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="error">Error details.</param>
	protected void SendOutError(Exception error)
	{
		SendOutMessage(error.ToErrorMessage());
	}

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionResponseMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="error">Subscribe or unsubscribe error info. To be set if the answer.</param>
	protected void SendSubscriptionReply(long originalTransactionId, Exception error = null)
	{
		SendOutMessage(originalTransactionId.CreateSubscriptionResponse(error));
	}

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionResponseMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	protected void SendSubscriptionNotSupported(long originalTransactionId)
	{
		SendOutMessage(originalTransactionId.CreateNotSupported());
	}

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionFinishedMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="nextFrom"><see cref="SubscriptionFinishedMessage.NextFrom"/>.</param>
	protected void SendSubscriptionFinished(long originalTransactionId, DateTimeOffset? nextFrom = null)
	{
		SendOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = originalTransactionId, NextFrom = nextFrom });
	}

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionOnlineMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	protected void SendSubscriptionOnline(long originalTransactionId)
	{
		SendOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = originalTransactionId });
	}

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionOnlineMessage"/> or <see cref="SubscriptionFinishedMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="message">Subscription.</param>
	protected void SendSubscriptionResult(ISubscriptionMessage message)
	{
		SendOutMessage(message.CreateResult());
	}

	/// <inheritdoc />
	public virtual IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		=> new OrderLogMarketDepthBuilder(securityId);

	/// <inheritdoc />
	public virtual TimeSpan GetHistoryStepSize(SecurityId securityId, DataType dataType, out TimeSpan iterationInterval)
		=> Extensions.GetHistoryStepSize(this, securityId, dataType, out iterationInterval);

	/// <inheritdoc />
	public virtual int? GetMaxCount(DataType dataType) => dataType.GetDefaultMaxCount();

	/// <inheritdoc />
	public virtual bool IsAllDownloadingSupported(DataType dataType) => false;

	/// <inheritdoc />
	public virtual bool IsSecurityRequired(DataType dataType) => dataType.IsSecurityRequired;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool UseInChannel => false;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual bool UseOutChannel => true;

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

		base.Load(storage);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Id), Id);
		storage.SetValue(nameof(HeartbeatInterval), HeartbeatInterval);
		storage.SetValue(nameof(SupportedInMessages), Do.Invariant(() => SupportedInMessages.Select(t => t.To<string>()).ToArray()));
		storage.SetValue(nameof(ReConnectionSettings), ReConnectionSettings.Save());
		storage.SetValue(nameof(EnqueueSubscriptions), EnqueueSubscriptions);
		storage.SetValue(nameof(IterationInterval), IterationInterval);

		base.Save(storage);
	}

	/// <summary>
	/// Create a copy of <see cref="MessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public virtual IMessageChannel Clone()
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

/// <summary>
/// Special adapter, which transmits directly to the output of all incoming messages.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PassThroughMessageAdapter"/>.
/// </remarks>
/// <param name="transactionIdGenerator">Transaction id generator.</param>
public class PassThroughMessageAdapter(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
{
	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		SendOutMessage(message);
		return true;
	}
}