namespace StockSharp.Messages;

/// <summary>
/// Wrapping based adapter.
/// </summary>
public interface IMessageAdapterWrapper : IMessageAdapter
{
	/// <summary>
	/// Underlying adapter.
	/// </summary>
	IMessageAdapter InnerAdapter { get; set; }
}

/// <summary>
/// Base implementation of <see cref="IMessageAdapterWrapper"/>.
/// </summary>
public abstract class MessageAdapterWrapper : Cloneable<IMessageAdapter>, IMessageAdapterWrapper
{
	private IMessageAdapter _innerAdapter;

	/// <summary>
	/// Initialize <see cref="MessageAdapterWrapper"/>.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	protected MessageAdapterWrapper(IMessageAdapter innerAdapter)
	{
		InnerAdapter = innerAdapter ?? throw new ArgumentNullException(nameof(innerAdapter));

		_innerAdapterName = GetUnderlyingAdapter(InnerAdapter).Name;
	}

	private static IMessageAdapter GetUnderlyingAdapter(IMessageAdapter adapter)
	{
		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		if (adapter is IMessageAdapterWrapper wrapper)
			return GetUnderlyingAdapter(wrapper.InnerAdapter);

		return adapter;
	}

	/// <inheritdoc />
	public IMessageAdapter InnerAdapter
	{
		get => _innerAdapter;
		set
		{
			if (_innerAdapter == value)
				return;

			if (_innerAdapter != null)
				_innerAdapter.NewOutMessageAsync -= InnerAdapterNewOutMessageAsync;

			_innerAdapter = value;

			if (_innerAdapter != null)
				_innerAdapter.NewOutMessageAsync += InnerAdapterNewOutMessageAsync;
		}
	}

	/// <summary>
	/// Control <see cref="InnerAdapter"/> lifetime.
	/// </summary>
	public bool OwnInnerAdapter { get; set; }

	/// <summary>
	/// Process <see cref="InnerAdapter"/> output message.
	/// </summary>
	/// <param name="message">The message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected virtual ValueTask InnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message.IsBack())
			return RaiseNewOutMessageAsync(message, cancellationToken);
		else
			return OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Process <see cref="InnerAdapter"/> output message.
	/// </summary>
	/// <param name="message">The message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected virtual ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		return RaiseNewOutMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// To call the event <see cref="NewOutMessageAsync"/>.
	/// </summary>
	/// <param name="message">The message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected async ValueTask RaiseNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var handler = NewOutMessageAsync;
		if (handler is not null)
			await handler(message, cancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
		NewOutMessage?.Invoke(message);
#pragma warning restore CS0618
	}

	/// <summary>
	/// To call the event <see cref="NewOutMessage"/>.
	/// </summary>
	/// <param name="message">The message.</param>
	[Obsolete("Use RaiseNewOutMessageAsync method.")]
	protected void RaiseNewOutMessage(Message message)
	{
#pragma warning disable CS0618 // Type or member is obsolete
		NewOutMessage?.Invoke(message);
#pragma warning restore CS0618
	}

	/// <summary>
	/// Auto send <see cref="Message.BackMode"/> messages to <see cref="InnerAdapter"/>.
	/// </summary>
	protected virtual bool SendInBackFurther => true;

	/// <inheritdoc />
	public virtual ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message.IsBack())
		{
			if (message.Adapter == this)
			{
				message.UndoBack();
			}
			else
			{
				if (SendInBackFurther)
				{
					await InnerAdapter.SendInMessageAsync(message, cancellationToken);
					return;
				}
			}
		}

		try
		{
			await OnSendInMessageAsync(message, cancellationToken);
		}
		catch (Exception ex)
		{
			await RaiseNewOutMessageAsync(message.CreateErrorResponse(ex, this), cancellationToken);
			throw;
		}
	}

	/// <summary>
	/// Send message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	protected virtual ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		return InnerAdapter.SendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	public event Action<Message> NewOutMessage;

	/// <inheritdoc />
	public event Func<Message, CancellationToken, ValueTask> NewOutMessageAsync;

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		InnerAdapter.Load(storage);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		InnerAdapter.Save(storage);
	}

	Guid ILogSource.Id => InnerAdapter.Id;

	private readonly string _innerAdapterName;

	string ILogSource.Name
	{
		get => _innerAdapterName + $" ({GetType().Name.Remove(nameof(MessageAdapter))})";
		set { }
	}

	/// <inheritdoc />
	public virtual ILogSource Parent
	{
		get => InnerAdapter.Parent;
		set => InnerAdapter.Parent = value;
	}

	/// <inheritdoc />
	public event Action<ILogSource> ParentRemoved
	{
		add { }
		remove { }
	}

	LogLevels ILogSource.LogLevel
	{
		get => InnerAdapter.LogLevel;
		set => InnerAdapter.LogLevel = value;
	}

	/// <inheritdoc />
	public DateTime CurrentTimeUtc => InnerAdapter.CurrentTimeUtc;

	DateTimeOffset ILogSource.CurrentTime => CurrentTimeUtc;

	bool ILogSource.IsRoot => InnerAdapter.IsRoot;

	event Action<LogMessage> ILogSource.Log
	{
		add => InnerAdapter.Log += value;
		remove => InnerAdapter.Log -= value;
	}

	void ILogReceiver.AddLog(LogMessage message)
	{
		InnerAdapter.AddLog(message);
	}

	/// <inheritdoc />
	public bool CheckTimeFrameByRequest => InnerAdapter.CheckTimeFrameByRequest;

	/// <inheritdoc />
	public ReConnectionSettings ReConnectionSettings => InnerAdapter.ReConnectionSettings;

	/// <inheritdoc />
	public IdGenerator TransactionIdGenerator => InnerAdapter.TransactionIdGenerator;

	/// <inheritdoc />
	public virtual IEnumerable<MessageTypeInfo> PossibleSupportedMessages => InnerAdapter.PossibleSupportedMessages;

	/// <inheritdoc />
	public virtual IEnumerable<MessageTypes> SupportedInMessages
	{
		get => InnerAdapter.SupportedInMessages;
		set => InnerAdapter.SupportedInMessages = value;
	}

	/// <inheritdoc />
	public virtual IEnumerable<MessageTypes> NotSupportedResultMessages => InnerAdapter.NotSupportedResultMessages;

	/// <inheritdoc />
	public virtual IEnumerable<DataType> GetSupportedMarketDataTypes(SecurityId securityId, DateTime? from, DateTime? to)
		=> InnerAdapter.GetSupportedMarketDataTypes(securityId, from, to);

	/// <inheritdoc />
	public TimeSpan HeartbeatInterval
	{
		get => InnerAdapter.HeartbeatInterval;
		set => InnerAdapter.HeartbeatInterval = value;
	}

	/// <inheritdoc />
	public string StorageName => InnerAdapter.StorageName;

	/// <inheritdoc />
	public virtual bool IsNativeIdentifiersPersistable => InnerAdapter.IsNativeIdentifiersPersistable;

	/// <inheritdoc />
	public virtual bool IsNativeIdentifiers => InnerAdapter.IsNativeIdentifiers;

	/// <inheritdoc />
	public virtual bool IsFullCandlesOnly => InnerAdapter.IsFullCandlesOnly;

	/// <inheritdoc />
	public virtual bool IsSupportSubscriptions => InnerAdapter.IsSupportSubscriptions;

	/// <inheritdoc />
	public virtual bool IsSupportCandlesUpdates(MarketDataMessage subscription) => InnerAdapter.IsSupportCandlesUpdates(subscription);

	/// <inheritdoc />
	public virtual bool IsSupportCandlesPriceLevels(MarketDataMessage subscription) => InnerAdapter.IsSupportCandlesPriceLevels(subscription);

	/// <inheritdoc />
	public virtual MessageAdapterCategories Categories => InnerAdapter.Categories;

	IEnumerable<(string, Type)> IMessageAdapter.SecurityExtendedFields => InnerAdapter.SecurityExtendedFields;

	/// <inheritdoc />
	public virtual IEnumerable<int> SupportedOrderBookDepths => InnerAdapter.SupportedOrderBookDepths;

	/// <inheritdoc />
	public virtual bool IsSupportOrderBookIncrements => InnerAdapter.IsSupportOrderBookIncrements;

	/// <inheritdoc />
	public virtual bool IsSupportExecutionsPnL => InnerAdapter.IsSupportExecutionsPnL;

	/// <inheritdoc />
	public virtual bool IsSecurityNewsOnly => InnerAdapter.IsSecurityNewsOnly;

	/// <inheritdoc />
	public IEnumerable<Level1Fields> CandlesBuildFrom => InnerAdapter.CandlesBuildFrom;

	/// <inheritdoc />
	public virtual bool IsSupportTransactionLog => InnerAdapter.IsSupportTransactionLog;

	Type IMessageAdapter.OrderConditionType => InnerAdapter.OrderConditionType;

	bool IMessageAdapter.HeartbeatBeforConnect => InnerAdapter.HeartbeatBeforConnect;

	Uri IMessageAdapter.Icon => InnerAdapter.Icon;

	bool IMessageAdapter.IsAutoReplyOnTransactonalUnsubscription => InnerAdapter.IsAutoReplyOnTransactonalUnsubscription;

	bool IMessageAdapter.IsReplaceCommandEditCurrent => InnerAdapter.IsReplaceCommandEditCurrent;

	bool IMessageAdapter.EnqueueSubscriptions
	{
		get => InnerAdapter.EnqueueSubscriptions;
		set => InnerAdapter.EnqueueSubscriptions = value;
	}

	bool IMessageAdapter.UseInChannel => InnerAdapter.UseInChannel;
	bool IMessageAdapter.UseOutChannel => InnerAdapter.UseOutChannel;

	TimeSpan IMessageAdapter.IterationInterval => InnerAdapter.IterationInterval;

	TimeSpan? IMessageAdapter.LookupTimeout => InnerAdapter.LookupTimeout;

	string IMessageAdapter.FeatureName => InnerAdapter.FeatureName;

	bool IMessageAdapter.ExtraSetup => InnerAdapter.ExtraSetup;

	/// <inheritdoc />
	public virtual bool? IsPositionsEmulationRequired => InnerAdapter.IsPositionsEmulationRequired;

	string[] IMessageAdapter.AssociatedBoards => InnerAdapter.AssociatedBoards;

	TimeSpan IMessageAdapter.DisconnectTimeout => InnerAdapter.DisconnectTimeout;

	int IMessageAdapter.MaxParallelMessages { get => InnerAdapter.MaxParallelMessages; set => InnerAdapter.MaxParallelMessages = value; }
	TimeSpan IMessageAdapter.FaultDelay { get => InnerAdapter.FaultDelay; set => InnerAdapter.FaultDelay = value; }

	ValueTask IMessageAdapter.SendInMessageAsync(Message message, CancellationToken cancellationToken)
		=> InnerAdapter.SendInMessageAsync(message, cancellationToken);

	IOrderLogMarketDepthBuilder IMessageAdapter.CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		=> InnerAdapter.CreateOrderLogMarketDepthBuilder(securityId);

	/// <inheritdoc />
	public virtual bool IsAllDownloadingSupported(DataType dataType)
		=> InnerAdapter.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	public virtual bool IsSecurityRequired(DataType dataType)
		=> InnerAdapter.IsSecurityRequired(dataType);

#pragma warning disable CS0618 // Type or member is obsolete
	void IMessageAdapter.SendOutMessage(Message message)
		=> InnerAdapter.SendOutMessage(message);
#pragma warning restore CS0618 // Type or member is obsolete

    ValueTask IMessageAdapter.SendOutMessageAsync(Message message, CancellationToken cancellationToken)
		=> InnerAdapter.SendOutMessageAsync(message, cancellationToken);

	/// <inheritdoc />
	public virtual void Dispose()
	{
		if (InnerAdapter is null)
			return;

		InnerAdapter.NewOutMessageAsync -= InnerAdapterNewOutMessageAsync;

		if (OwnInnerAdapter)
			InnerAdapter.Dispose();

		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	public override string ToString() => InnerAdapter.ToString();

	/// <inheritdoc />
	public void LogVerbose(string message, params object[] args)
		=> this.AddVerboseLog(message, args);
	
	/// <inheritdoc />
	public void LogDebug(string message, params object[] args)
		=> this.AddDebugLog(message, args);

	/// <inheritdoc />
	public void LogInfo(string message, params object[] args)
		=> this.AddInfoLog(message, args);
	
	/// <inheritdoc />
	public void LogWarning(string message, params object[] args)
		=> this.AddWarningLog(message, args);
	
	/// <inheritdoc />
	public void LogError(string message, params object[] args)
		=> this.AddErrorLog(message, args);
}