namespace StockSharp.Fix.Dialects;

using System.Security;
using System.Text.RegularExpressions;

using FixExtensions = StockSharp.Fix.Native.Extensions;

/// <summary>
/// Base class describing the dialect of the FIX protocol.
/// </summary>
public abstract partial class BaseFixDialect : BaseLogReceiver, IFixDialect
{
	private readonly IdGenerator _transactionIdGenerator;
	private IFixWriter _bodyWriter;
	private IFixWriter _bodyWriter2;
	private readonly SynchronizedDictionary<long, SecurityId> _requestSecurities = [];
	private readonly SynchronizedDictionary<long, long> _orderRegisterTransactions = [];
	private readonly SynchronizedPairSet<long, string> _clOrdIds = [];
	private bool _disconnecting;

	/// <summary>
	/// </summary>
	protected long LastResendRequest { get; private set; }

	/// <summary>
	/// Initialize <see cref="BaseFixDialect"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	/// <param name="encoding">Encoding.</param>
	/// <param name="version">FIX version.</param>
	protected BaseFixDialect(IdGenerator transactionIdGenerator, Encoding encoding, string version = FixVersions.Fix44)
	{
		if (version.IsEmpty())
			throw new ArgumentNullException(nameof(version));

		Encoding = encoding;
		_transactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
		Version = version;

		_depthBuilder = new FixDepthBuilder(this);

		var attr = GetType().GetAttribute<MessageAdapterCategoryAttribute>();
		if (attr != null)
			Categories = attr.Categories;
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $": Sender {SenderCompId} Target {TargetCompId}";

	/// <inheritdoc />
	public string Version { get; }

	private Encoding _encoding;

	/// <inheritdoc />
	public Encoding Encoding
	{
		get => _encoding;
		set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	public string Login { get; set; }

	/// <inheritdoc />
	public SecureString Password { get; set; }

	/// <inheritdoc />
	public string SenderCompId { get; set; }

	/// <inheritdoc />
	public string TargetCompId { get; set; }

	/// <inheritdoc />
	public bool IsDemo { get; set; }

	private FastDateTimeParser _timeStampParser = new(FixExtensions.TimeStampFormat);

	/// <inheritdoc />
	public FastDateTimeParser TimeStampParser
	{
		get => _timeStampParser;
		set => _timeStampParser = value ?? throw new ArgumentNullException(nameof(value));
	}

	private FastTimeSpanParser _timeParser = new(FixExtensions.TimeFormat);

	/// <inheritdoc />
	public FastTimeSpanParser TimeParser
	{
		get => _timeParser;
		set => _timeParser = value ?? throw new ArgumentNullException(nameof(value));
	}

	private FastDateTimeParser _dateParser = new(FixExtensions.DateFormat);

	/// <inheritdoc />
	public FastDateTimeParser DateParser
	{
		get => _dateParser;
		set => _dateParser = value ?? throw new ArgumentNullException(nameof(value));
	}

	private FastDateTimeParser _yearMonthParser = new(FixExtensions.YearMonthFormat);

	/// <inheritdoc />
	public FastDateTimeParser YearMonthParser
	{
		get => _yearMonthParser;
		set => _yearMonthParser = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	public bool IsResetCounter { get; set; } = true;

	private TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(60);

	/// <inheritdoc />
	public ReConnectionSettings ReConnectionSettings { get; } = new ReConnectionSettings();

	/// <inheritdoc />
	public TimeSpan HeartbeatInterval
	{
		get => _heartbeatInterval;
		set
		{
			if ((int)value.TotalSeconds <= 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_heartbeatInterval = value;
		}
	}

	private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;

	/// <inheritdoc />
	public TimeZoneInfo TimeZone
	{
		get => _timeZone;
		set => _timeZone = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	public virtual TimeSpan DisconnectTimeout { get; } = TimeSpan.FromSeconds(5);

	private int _maxParallelMessages = 5;

	/// <inheritdoc />
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
	public string ExchangeBoard { get; set; }

	/// <inheritdoc />
	public string ClientCode { get; set; }

	/// <inheritdoc />
	public bool DoNotSendAccount { get; set; }

	/// <inheritdoc />
	public string ClientVersion { get; set; }

	/// <inheritdoc />
	public string Accounts { get; set; }

	/// <inheritdoc />
	public string[] AssociatedBoards => [];

	private IFixWriter _writer;
	IFixWriter IFixDialect.Writer => _writer;

	private IFixReader _reader;
	IFixReader IFixDialect.Reader => _reader;

	/// <summary>
	/// Translate tick data as <see cref="Level1ChangeMessage"/> or <see cref="ExecutionMessage"/>.
	/// </summary>
	public bool TickAsLevel1 { get; set; }

	/// <summary>
	/// Translate quote data as <see cref="Level1ChangeMessage"/> or <see cref="QuoteChangeMessage"/>.
	/// </summary>
	public bool QuotesAsLevel1 { get; set; }

	/// <summary>
	/// Use <see cref="Login"/> as portfolio name.
	/// </summary>
	protected virtual bool LoginAsPortfolioName => false;

	/// <inheritdoc />
	public bool OverrideExecIdByNative { get; set; }

	/// <inheritdoc />
	public virtual MessageAdapterCategories Categories { get; }

	/// <inheritdoc />
	public virtual string StorageName => Name;

	/// <inheritdoc />
	public virtual bool IsNativeIdentifiers => false;

	/// <summary>
	/// Support market-data response.
	/// </summary>
	protected virtual bool IsSupportMarketDataResponse => false;

	/// <inheritdoc />
	public virtual IEnumerable<(string, Type)> SecurityExtendedFields { get; } = [];

	/// <inheritdoc />
	public virtual bool IsNativeIdentifiersPersistable => true;

	/// <inheritdoc />
	public virtual bool SupportUnknownExecutions { get; set; }

	/// <inheritdoc />
	public virtual IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		=> new OrderLogMarketDepthBuilder(securityId);

	private readonly DataType[] _supportedMarketDataTypes =
	[
		DataType.Level1,
		DataType.News,
		DataType.MarketDepth,
		DataType.Ticks,
	];

	/// <inheritdoc />
	public virtual IAsyncEnumerable<DataType> GetSupportedMarketDataTypesAsync(SecurityId securityId, DateTime? from, DateTime? to)
		=> _supportedMarketDataTypes.ToAsyncEnumerable();

	/// <inheritdoc />
	public virtual bool IsSupportCandlesUpdates(MarketDataMessage subscription) => false;

	/// <inheritdoc />
	public virtual bool IsSupportCandlesPriceLevels(MarketDataMessage subscription) => false;

	/// <summary>
	/// Reply errors for messages of type <see cref="FixMessages.NewOrderSingle"/> transfers via <see cref="FixMessages.Reject"/>.
	/// </summary>
	protected virtual bool NewOrderSingleErrorsAsReject => true;

	/// <inheritdoc />
	public virtual bool CheckTimeFrameByRequest => false;

	/// <inheritdoc />
	public virtual IEnumerable<int> SupportedOrderBookDepths => [];

	/// <inheritdoc />
	public virtual bool IsSupportOrderBookIncrements => true;

	/// <inheritdoc />
	public virtual bool IsSupportExecutionsPnL => false;

	/// <inheritdoc />
	public virtual bool IsSecurityNewsOnly => false;

	/// <inheritdoc />
	public virtual Type OrderConditionType => typeof(FixOrderCondition);

	/// <inheritdoc />
	public virtual bool HeartbeatBeforeConnect => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual IdGenerator TransactionIdGenerator => _transactionIdGenerator;

	/// <inheritdoc />
	public virtual IEnumerable<MessageTypeInfo> PossibleSupportedMessages { get; } =
	[
		MessageTypes.MarketData.ToInfo(),
		MessageTypes.SecurityLookup.ToInfo(),

		MessageTypes.PortfolioLookup.ToInfo(),
		MessageTypes.OrderRegister.ToInfo(),
		MessageTypes.OrderReplace.ToInfo(),
		MessageTypes.OrderCancel.ToInfo(),
		MessageTypes.OrderGroupCancel.ToInfo(),
		MessageTypes.OrderStatus.ToInfo(),

		MessageTypes.ChangePassword.ToInfo(),

		FixMessageTypes.SeqReset.ToInfo(),
		FixMessageTypes.ResendRequest.ToInfo(),
	];

	/// <inheritdoc />
	public virtual IEnumerable<MessageTypes> SupportedInMessages { get; set; } = [];

	/// <inheritdoc />
	public virtual IEnumerable<MessageTypes> NotSupportedResultMessages { get; set; } =
	[
		MessageTypes.OrderStatus, MessageTypes.PortfolioLookup,
	];

	/// <inheritdoc />
	public virtual bool CancelOnDisconnect { get; set; }

	/// <inheritdoc />
	public virtual Uri Icon => GetType().TryGetIconUrl();

	/// <inheritdoc />
	public virtual bool IsAutoReplyOnTransactonalUnsubscription => true;

	/// <inheritdoc />
	public virtual bool IsFullCandlesOnly => true;

	/// <inheritdoc />
	public virtual bool IsSupportSubscriptions => true;

	/// <inheritdoc />
	public virtual IEnumerable<Level1Fields> CandlesBuildFrom => [];

	/// <inheritdoc />
	public virtual bool EnqueueSubscriptions { get; set; }

	/// <inheritdoc />
	public virtual bool IsSupportTransactionLog => !IsResetCounter;

	/// <inheritdoc />
	public virtual bool UseInChannel => false;

	/// <inheritdoc />
	public virtual bool UseOutChannel => true;

	/// <inheritdoc />
	public virtual TimeSpan IterationInterval => default;

	/// <inheritdoc />
	public virtual TimeSpan? LookupTimeout => default;

	/// <inheritdoc />
	public virtual string FeatureName => string.Empty;

	/// <inheritdoc />
	public virtual bool? IsPositionsEmulationRequired => null;

	/// <inheritdoc />
	public virtual bool IsReplaceCommandEditCurrent => false;

	private readonly IncrementalIdGenerator _idGenerator = new();

	/// <inheritdoc />
	public virtual long CurrentCounter
	{
		get => _idGenerator.Current;
		set => _idGenerator.Current = value;
	}

	/// <summary>
	/// Server address.
	/// </summary>
	protected EndPoint Address { get; private set; }

	/// <inheritdoc />
	public virtual bool SupportLicensing => false;

	/// <inheritdoc />
	public virtual bool ExtraSetup => false;

	/// <inheritdoc />
	void IFixDialect.Init(IFixWriter writer, IFixReader reader, EndPoint address)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
		_reader = reader ?? throw new ArgumentNullException(nameof(reader));
		Address = address ?? throw new ArgumentNullException(nameof(address));

		_bodyWriter = new TextFixWriter(new MemoryStream(), Encoding, ownsStream: true);
		_bodyWriter2 = new TextFixWriter(new MemoryStream(), Encoding, ownsStream: true);
	}

	private readonly Regex _parseSeqNum = new(@"MsgSeqNum too (low|high), expecting (?<expected>(\d+))");

	/// <inheritdoc />
	public virtual long? TryParseNextMsqSeqNum(string errorMessage)
	{
		var match = _parseSeqNum.Match(errorMessage);

		if (!match.Success)
			return null;

		return match.Groups["expected"].Value.To<long>();
	}

	/// <summary>
	/// Reset state.
	/// </summary>
	protected virtual void OnReset()
	{
		_depthBuilder.Reset();
		_totalSecCountByRequestId.Clear();
		_orderRegisterTransactions.Clear();
		_clOrdIds.Clear();
		_requestSecurities.Clear();

		LastResendRequest = 0;
		_disconnecting = false;
	}

	private Func<Message, CancellationToken, ValueTask> _newOutMessageAsync;

	event Func<Message, CancellationToken, ValueTask> IMessageTransport.NewOutMessageAsync
	{
		add => _newOutMessageAsync += value;
		remove => _newOutMessageAsync -= value;
	}

	/// <summary>
	/// Raise <see cref="IMessageTransport.NewOutMessageAsync"/>
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	protected ValueTask RaiseNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		return _newOutMessageAsync?.Invoke(message, cancellationToken) ?? default;
	}

	/// <summary>
	/// Check state before connect.
	/// </summary>
	protected virtual void CheckState()
	{
		if (SenderCompId.IsEmpty())
			throw new InvalidOperationException(LocalizedStrings.SenderIdNotSet);

		if (TargetCompId.IsEmpty())
			throw new InvalidOperationException(LocalizedStrings.TargetIdNotSet);
	}

	/// <inheritdoc />
	public virtual ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		return message.Type switch
		{
			MessageTypes.Connect => ConnectAsync((ConnectMessage)message, cancellationToken),
			MessageTypes.Disconnect => DisconnectAsync((DisconnectMessage)message, cancellationToken),
			MessageTypes.Reset => ResetAsync((ResetMessage)message, cancellationToken),
			MessageTypes.ChangePassword => ChangePasswordAsync((ChangePasswordMessage)message, cancellationToken),
			MessageTypes.SecurityLookup => SecurityLookupAsync((SecurityLookupMessage)message, cancellationToken),
			MessageTypes.PortfolioLookup => PortfolioLookupAsync((PortfolioLookupMessage)message, cancellationToken),
			MessageTypes.OrderStatus => OrderStatusAsync((OrderStatusMessage)message, cancellationToken),
			_ => WriteMessageAsync(message, cancellationToken),
		};
	}

	/// <inheritdoc />
	protected virtual ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		CheckState();

		return WriteMessageAsync(connectMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected virtual ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		_disconnecting = true;
		return WriteMessageAsync(disconnectMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected virtual ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		OnReset();
		return default;
	}

	/// <inheritdoc />
	protected virtual ValueTask ChangePasswordAsync(ChangePasswordMessage pwdMsg, CancellationToken cancellationToken)
	{
		CheckState();

		return WriteMessageAsync(pwdMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected virtual ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		return WriteMessageAsync(lookupMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected virtual async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		if (lookupMsg.IsSubscribe)
		{
			if (LoginAsPortfolioName)
			{
				await RaiseNewOutMessageAsync(new PortfolioMessage
				{
					PortfolioName = GetSyntheticPortfolioName(),
					OriginalTransactionId = lookupMsg.TransactionId
				}, cancellationToken);

				await RaiseNewOutMessageAsync(lookupMsg.CreateResult(), cancellationToken);
				return;
			}
		}
		else
		{
			if (IsAutoReplyOnTransactonalUnsubscription)
			{
				await RaiseNewOutMessageAsync(lookupMsg.CreateResult(), cancellationToken);
				return;
			}
		}

		await WriteMessageAsync(lookupMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected virtual async ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		Message message = statusMsg;

		if (statusMsg.IsSubscribe)
		{
			if (!IsResetCounter && !PossibleSupportedMessages.Any(p => p.Type == MessageTypes.OrderStatus))
			{
				LastResendRequest = statusMsg.TransactionId;

				message = new FixResendRequestMessage
				{
					BeginSeqNo = statusMsg.Skip ?? 1,
					EndSeqNo = (statusMsg.Skip + statusMsg.Count) ?? 0,
				};
			}
		}
		else
		{
			if (IsAutoReplyOnTransactonalUnsubscription)
			{
				await RaiseNewOutMessageAsync(statusMsg.CreateResult(), cancellationToken);
				return;
			}
		}

		await WriteMessageAsync(message, cancellationToken);
	}

	private async ValueTask WriteMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var writing = false;

		try
		{
			_writer.FlushDump();
			_writer.ClearState();

			var msgType = await OnWriteAsync(_bodyWriter2, message, cancellationToken);

			if (msgType == null)
			{
				_bodyWriter2.Stream.Position = 0;
				return;
			}

			var seqNum = _idGenerator.GetNextId();

			if (NewOrderSingleErrorsAsReject && message is OrderRegisterMessage regMsg)
				_orderRegisterTransactions.Add(seqNum, regMsg.TransactionId);

			var mdMsg = message as MarketDataMessage;

			if (mdMsg != null)
			{
				if (!mdMsg.SecurityId.SecurityCode.IsEmpty())
					_requestSecurities.Add(mdMsg.TransactionId, mdMsg.SecurityId);
			}

			writing = true;
			await _writer.WriteFixMessageAsync(_bodyWriter, Version, msgType, SenderCompId, TargetCompId, TimeStampParser, seqNum, async (writer, ct) =>
			{
				await writer.WriteStreamAsync(_bodyWriter2, ct);
			}, cancellationToken);

			if (mdMsg != null && !IsSupportMarketDataResponse)
				await RaiseNewOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
		}
		finally
		{
			if (writing && _writer.IsDump)
				LogDebug("To server: {0}", _writer.FlushDump());
		}
	}

	async IAsyncEnumerable<Message> IFixDialect.ReadAsync([EnumeratorCancellation]CancellationToken cancellationToken)
	{
		var messageType = await _reader.ReadHeaderAsync(Version, cancellationToken);

		if (messageType == null)
			yield break;

		await foreach (var msg in OnReadAsync(_reader, messageType, cancellationToken).WithEnforcedCancellation(cancellationToken))
		{
			yield return msg;
		}
	}

	/// <inheritdoc />
	public virtual bool IsAllDownloadingSupported(DataType dataType) => dataType == DataType.Securities || dataType == DataType.Transactions || dataType == DataType.PositionChanges;

	/// <inheritdoc />
	public virtual bool IsSecurityRequired(DataType dataType) => dataType.IsSecurityRequired;

	/// <summary>
	/// Read next message from FIX protocol.
	/// </summary>
	/// <param name="reader">The reader of data recorded in the FIX protocol format.</param>
	/// <param name="msgType">Message type.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The sequence of messages.</returns>
	protected virtual IAsyncEnumerable<Message> OnReadAsync(IFixReader reader, string msgType, CancellationToken cancellationToken)
	{
		async IAsyncEnumerable<Message> readUnk()
		{
			await Task.Yield();
			LogWarning("Unknown message type: {0}", msgType);
			yield break;
		}

		return msgType switch
		{
			FixMessages.Logon => ReadLogonAsync(reader, cancellationToken),
			FixMessages.Logout => ReadLogoutAsync(reader, cancellationToken),
			FixMessages.TradingSessionStatus => ReadTradingSessionStatusAsync(reader, cancellationToken),
			FixMessages.Heartbeat => ReadHeartbeatAsync(reader, cancellationToken),
			FixMessages.TestRequest => ReadTestRequestAsync(reader, cancellationToken),
			FixMessages.BusinessMessageReject => ReadBusinessMessageRejectAsync(reader, cancellationToken),
			FixMessages.Reject => ReadRejectAsync(reader, cancellationToken),
			FixMessages.ResendRequest => ReadResendRequestAsync(reader, cancellationToken),
			FixMessages.SequenceReset => ReadSequenceResetAsync(reader, cancellationToken),
			FixMessages.UserResponse => ReadUserResponseAsync(reader, cancellationToken),
			FixMessages.QuoteRequestReject => ReadQuoteRequestRejectAsync(reader, cancellationToken),
			FixMessages.QuoteStatusReport => ReadQuoteStatusReportAsync(reader, cancellationToken),
			FixMessages.Quote => _depthBuilder.ProcessQuoteAsync(reader, cancellationToken),
			FixMessages.MarketDataSnapshotFullRefresh => ReadMarketDataRefreshAsync(reader, true, default),
			FixMessages.MarketDataIncrementalRefresh => ReadMarketDataRefreshAsync(reader, false, default),
			FixMessages.MarketDataRequestReject => ReadMarketDataRequestRejectAsync(reader, cancellationToken),
			FixMessages.News => reader.ReadNewsAsync(TimeStampParser, cancellationToken),
			FixMessages.SecurityDefinition or FixMessages.SecurityList => ReadSecurityMessageAsync(reader, cancellationToken),
			FixMessages.ExecutionReport => ReadExecutionReportAsync(reader, cancellationToken),
			FixMessages.OrderCancelReject => ReadOrderCancelRejectAsync(reader, cancellationToken),
			FixMessages.OrderMassCancelReport => ReadOrderMassCancelReportAsync(reader, cancellationToken),
			FixMessages.RequestForPositionsAck => ReadRequestForPositionAckAsync(reader, cancellationToken),
			FixMessages.PositionReport => ReadPositionReportAsync(reader, cancellationToken),
			FixMessages.SecurityStatus => ReadSecurityStatusAsync(reader, cancellationToken),
			_ => readUnk(),
		};
	}

	private async IAsyncEnumerable<Message> ReadLogonAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		bool? resetSeqNumFlag = null;
		int? nextExpectedMsgSeqNum = null;
		string sessionId = null;
		string language = null;
		string licenseFeatureId = null;
		DateTime? componentTimestamp = null;
		var supportLicensing = false;
		var isDemo = false;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.ResetSeqNumFlag:
					resetSeqNumFlag = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.NextExpectedMsgSeqNum:
					nextExpectedMsgSeqNum = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.TradingSessionID:
					sessionId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Language:
					language = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.LicenseFeatureId:
					licenseFeatureId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.ComponentTimestamp:
					componentTimestamp = await reader.ReadUtcAsync(TimeStampParser, ct);
					return true;
				case FixTags.SupportLicensing:
					supportLicensing = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.DemoOnly:
					isDemo = await reader.ReadBoolAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		if (resetSeqNumFlag == null || !resetSeqNumFlag.Value)
		{
			if (nextExpectedMsgSeqNum != null)
				CurrentCounter = nextExpectedMsgSeqNum.Value;
		}

		//if (_resendMaxNum > 0)
		//{
		//	_writer.WriteResendRequest(2, _resendMaxNum);
		//	_resendMaxNum = 0;
		//}

		yield return new FixLogonMessage
		{
			SessionId = sessionId,
			Language = language,
			SupportLicensing = supportLicensing,
			IsDemo = isDemo,
			LicenseFeatureId = licenseFeatureId,
			LicenseComponentTimestamp = componentTimestamp,
		};
	}

	private async IAsyncEnumerable<Message> ReadHeartbeatAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string requestId = null;
		bool? possDupFlag = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.TestReqID:
					requestId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.PossDupFlag:
					possDupFlag = await reader.ReadBoolAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		if (possDupFlag == true)
			yield break;

		if (!requestId.IsEmpty())
			yield return new TimeMessage { OriginalTransactionId = requestId };
	}

	private async IAsyncEnumerable<Message> ReadTestRequestAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string requestId = null;
		bool? possDupFlag = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.TestReqID:
					requestId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.PossDupFlag:
					possDupFlag = await reader.ReadBoolAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		if (possDupFlag == true)
			yield break;

		yield return new TimeMessage
		{
			TransactionId = _transactionIdGenerator.GetNextId(),
			OriginalTransactionId = requestId,
			BackMode = MessageBackModes.Direct
		};
	}

	private async IAsyncEnumerable<Message> ReadBusinessMessageRejectAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		long? refSeqNum = null;
		string refMsgType = null;
		int? rejectReason = null;
		string text = null;
		int? refTagId = null;
		DateTime? sendingTime = null;
		long? responseId = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.SendingTime:
					sendingTime = await reader.ReadUtcAsync(TimeStampParser, ct);
					return true;
				case FixTags.RefSeqNum:
					refSeqNum = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.RefMsgType:
					refMsgType = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.BusinessRejectReason:
					rejectReason = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.Text:
					text = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.RefTagID:
					refTagId = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.MDResponseID:
					responseId = await reader.ReadLongAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		var fullText = LocalizedStrings.MessageNotProcessedByFix.Put(refSeqNum, refMsgType, rejectReason, text, refTagId);

		if (NewOrderSingleErrorsAsReject && refSeqNum != null && _orderRegisterTransactions.TryGetValue(refSeqNum.Value, out var transId))
		{
			yield return new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				ServerTime = sendingTime ?? DateTime.UtcNow,
				OriginalTransactionId = transId,
				Error = new InvalidOperationException(fullText),
				OrderState = OrderStates.Failed,
			};
		}
		else
		{
			var errorMsg = fullText.ToErrorMessage();
			errorMsg.OriginalTransactionId = responseId ?? default;
			yield return errorMsg;
		}
	}

	private async IAsyncEnumerable<Message> ReadRejectAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		long? refSeqNum = null;
		string refMsgType = null;
		int? rejectReason = null;
		string text = null;
		int? refTagId = null;
		DateTime? sendingTime = null;
		long? responseId = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.SendingTime:
					sendingTime = await reader.ReadUtcAsync(TimeStampParser, ct);
					return true;
				case FixTags.RefSeqNum:
					refSeqNum = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.RefMsgType:
					refMsgType = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.SessionRejectReason:
					rejectReason = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.Text:
					text = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.RefTagID:
					refTagId = await reader.ReadIntAsync(ct);
					return true;
				case FixTags.MDResponseID:
					responseId = await reader.ReadLongAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		var fullText = LocalizedStrings.MessageNotProcessedByFix.Put(refSeqNum, refMsgType, rejectReason, text, refTagId);

		if (NewOrderSingleErrorsAsReject && refSeqNum != null && _orderRegisterTransactions.TryGetValue(refSeqNum.Value, out var transId))
		{
			yield return new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				ServerTime = sendingTime ?? DateTime.UtcNow,
				OriginalTransactionId = transId,
				Error = new InvalidOperationException(fullText),
				OrderState = OrderStates.Failed,
			};
		}
		else
		{
			var errorMsg = fullText.ToErrorMessage();
			errorMsg.OriginalTransactionId = responseId ?? default;
			yield return errorMsg;
		}
	}

	private async IAsyncEnumerable<Message> ReadUserResponseAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string requestId = null;
		string userName = null;
		UserStatus? userStatus = null;
		string userStatusText = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.UserRequestID:
					requestId = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.Username:
					userName = await reader.ReadStringAsync(ct);
					return true;
				case FixTags.UserStatus:
					userStatus = (UserStatus)await reader.ReadIntAsync(ct);
					return true;
				case FixTags.UserStatusText:
					userStatusText = await reader.ReadStringAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		var transId = requestId.To<long?>() ?? 0;

		switch (userStatus)
		{
			case UserStatus.LoggedIn:
				yield return new UserInfoMessage
				{
					Login = userName,
					OriginalTransactionId = transId,
				};
				break;

			case UserStatus.PasswordChanged:
				yield return new ChangePasswordMessage { OriginalTransactionId = transId };
				break;

			case UserStatus.PasswordIncorrect:
				yield return new ChangePasswordMessage
				{
					OriginalTransactionId = transId,
					Error = new InvalidOperationException(userStatusText ?? LocalizedStrings.UnknownPasswordChangeError),
				};
				break;
		}
	}

	private async IAsyncEnumerable<Message> ReadResendRequestAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		long? beginSeqNo = null;
		long? endSeqNo = null;
		bool? possDupFlag = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.BeginSeqNo:
					beginSeqNo = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.EndSeqNo:
					endSeqNo = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.PossDupFlag:
					possDupFlag = await reader.ReadBoolAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		if (possDupFlag == true)
			yield break;

		//var current = endSeqNo != null && endSeqNo > 0 ? endSeqNo.Value + 1 : beginSeqNo;

		//if (current != null)
		//	CurrentCounter = current.Value;

		yield return new FixResendRequestMessage
		{
			BeginSeqNo = beginSeqNo ?? default,
			EndSeqNo = endSeqNo ?? default,
			//BackMode = MessageBackModes.Direct
		};
	}

	/// <summary>
	/// Process extra tags for <see cref="FixSeqResetMessage"/>.
	/// </summary>
	/// <param name="tag">Tag.</param>
	/// <param name="reader">The reader of data recorded in the FIX protocol format.</param>
	/// <param name="message">Sequence reset message.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Result.</returns>
	protected virtual ValueTask<bool> ProcessSequenceResetExtraTagAsync(FixTags tag, IFixReader reader, FixSeqResetMessage message, CancellationToken cancellationToken)
		=> new(false);

	private async IAsyncEnumerable<Message> ReadSequenceResetAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		var message = new FixSeqResetMessage();

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.GapFillFlag:
					message.GapFill = await reader.ReadBoolAsync(ct);
					return true;
				case FixTags.NewSeqNo:
					message.NewSeqNo = await reader.ReadLongAsync(ct);
					return true;
				case FixTags.MsgSeqNum:
					message.SeqNum = await reader.ReadLongAsync(ct);
					return true;
				default:
					return await ProcessSequenceResetExtraTagAsync(tag, reader, message, ct);
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		yield return message;
	}

	/// <summary>
	/// Check <see cref="FixMessages.Logout"/> contains error message.
	/// </summary>
	/// <param name="text">Text message.</param>
	/// <returns><see langword="true"/> if the specified text contains error message, otherwise, <see langword="false"/>.</returns>
	protected virtual bool IsLogoutError(string text)
	{
		return !text.IsEmpty();
	}

	private async IAsyncEnumerable<Message> ReadLogoutAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		string text = null;

		var isOk = await reader.ReadMessageAsync(async (tag, ct) =>
		{
			switch (tag)
			{
				case FixTags.Text:
					text = await reader.ReadStringAsync(ct);
					return true;
				default:
					return false;
			}
		}, cancellationToken);

		if (!isOk)
			yield break;

		if (IsLogoutError(text))
		{
			var msg = _disconnecting ? (BaseConnectionMessage)new DisconnectMessage() : new ConnectMessage();
			msg.Error = new InvalidOperationException(text);

			yield return msg;
		}
	}

	/// <summary>
	/// Write the specified message into FIX protocol.
	/// </summary>
	/// <param name="writer">The recorder of data in the FIX protocol format.</param>
	/// <param name="message">The message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="FixMessages"/> value.</returns>
	protected virtual async ValueTask<string> OnWriteAsync(IFixWriter writer, Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Connect:
			{
				return await WriteLogonRequestAsync(writer, (ConnectMessage)message, cancellationToken);
			}

			case MessageTypes.Disconnect:
			{
				return await WriteLogoutRequestAsync(writer, cancellationToken);
			}

			case MessageTypes.Time:
			{
				return await WriteTimeMessageAsync(writer, (TimeMessage)message, cancellationToken);
			}

			case FixMessageTypes.SeqReset:
			{
				var resetMsg = (FixSeqResetMessage)message;
				return await WriteSequenceResetAsync(writer, resetMsg.GapFill ?? default, resetMsg.NewSeqNo, cancellationToken);
			}

			case FixMessageTypes.ResendRequest:
			{
				var resendMsg = (FixResendRequestMessage)message;
				return await WriteResendRequestAsync(writer, resendMsg.BeginSeqNo, resendMsg.EndSeqNo, cancellationToken);
			}

			case MessageTypes.ChangePassword:
			{
				return await WriteUserRequestChangePasswordAsync(writer, (ChangePasswordMessage)message, cancellationToken);
			}

			default:
				return null;
		}
	}

	/// <summary>
	/// To record the <see cref="FixMessages.Logon"/> message (request).
	/// </summary>
	/// <param name="writer">The recorder of data in the FIX protocol format.</param>
	/// <param name="message"><see cref="ConnectMessage"/>.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <param name="extra">Write extra parameters for <see cref="FixMessages.Logon"/> message.</param>
	/// <returns><see cref="FixMessages"/> value.</returns>
	protected async ValueTask<string> WriteLogonRequestAsync(IFixWriter writer, ConnectMessage message, CancellationToken cancellationToken, Func<IFixWriter, CancellationToken, ValueTask> extra = null)
	{
		if (writer is null)		throw new ArgumentNullException(nameof(writer));
		if (message is null)	throw new ArgumentNullException(nameof(message));

		await writer.WriteAsync(FixTags.EncryptMethod, cancellationToken);
		await writer.WriteAsync((int)EncryptMethod.None, cancellationToken);

		await writer.WriteAsync(FixTags.HeartBtInt, cancellationToken);
		await writer.WriteAsync((int)HeartbeatInterval.TotalSeconds, cancellationToken);

		await writer.WriteAsync(FixTags.ResetSeqNumFlag, cancellationToken);
		await writer.WriteAsync(IsResetCounter, cancellationToken);

		if (!Login.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Username, cancellationToken);
			await writer.WriteAsync(Login, cancellationToken);
		}

		if (!Password.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Password, cancellationToken);
			await writer.WriteAsync(Password.UnSecure(), cancellationToken);
		}

		if (!message.ClientVersion.IsEmpty())
		{
			await writer.WriteAsync(FixTags.DefaultApplVerID, cancellationToken);
			await writer.WriteAsync(message.ClientVersion, cancellationToken);
		}

		if (!message.Language.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Language, cancellationToken);
			await writer.WriteAsync(message.Language, cancellationToken);
		}

		if (SupportLicensing)
		{
			await writer.WriteAsync(FixTags.SupportLicensing, cancellationToken);
			await writer.WriteAsync(true, cancellationToken);

			await writer.WriteAsync(FixTags.DemoOnly, cancellationToken);
			await writer.WriteAsync(IsDemo, cancellationToken);
		}

		if (extra != null)
			await extra(writer, cancellationToken);

		return FixMessages.Logon;
	}

	/// <summary>
	/// To record the <see cref="FixMessages.Logout"/> message (request).
	/// </summary>
	/// <param name="writer">The recorder of data in the FIX protocol format.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <param name="text">The text of reason.</param>
	/// <returns><see cref="FixMessages"/> value.</returns>
	protected static async ValueTask<string> WriteLogoutRequestAsync(IFixWriter writer, CancellationToken cancellationToken, string text = null)
	{
		if (!text.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Text, cancellationToken);
			await writer.WriteAsync(text, cancellationToken);
		}

		return FixMessages.Logout;
	}

	/// <summary>
	/// To record the <see cref="FixMessages.SequenceReset"/> message (request).
	/// </summary>
	/// <param name="writer">The recorder of data in the FIX protocol format.</param>
	/// <param name="gapFill">Gap fill.</param>
	/// <param name="newSeqNo">New sequence number.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="FixMessages"/> value.</returns>
	protected async ValueTask<string> WriteSequenceResetAsync(IFixWriter writer, bool gapFill, long newSeqNo, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.GapFillFlag, cancellationToken);
		await writer.WriteAsync(gapFill, cancellationToken);

		await writer.WriteAsync(FixTags.NewSeqNo, cancellationToken);
		await writer.WriteAsync(newSeqNo, cancellationToken);

		return FixMessages.SequenceReset;
	}

	/// <summary>
	/// To record the <see cref="FixMessages.ResendRequest"/> message.
	/// </summary>
	/// <param name="writer">The recorder of data in the FIX protocol format.</param>
	/// <param name="beginSeqNo">The original message identifier.</param>
	/// <param name="endSeqNo">The last message identifier.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="FixMessages"/> value.</returns>
	protected static async ValueTask<string> WriteResendRequestAsync(IFixWriter writer, long beginSeqNo, long endSeqNo, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.BeginSeqNo, cancellationToken);
		await writer.WriteAsync(beginSeqNo, cancellationToken);

		await writer.WriteAsync(FixTags.EndSeqNo, cancellationToken);
		await writer.WriteAsync(endSeqNo, cancellationToken);

		return FixMessages.ResendRequest;
	}

	/// <summary>
	/// To record the <see cref="FixMessages.UserRequest"/> message.
	/// </summary>
	/// <param name="writer">The recorder of data in the FIX protocol format.</param>
	/// <param name="message">Password change message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <param name="userName">Current user name.</param>
	/// <param name="password">Current password.</param>
	/// <returns><see cref="FixMessages"/> value.</returns>
	protected async ValueTask<string> WriteUserRequestChangePasswordAsync(IFixWriter writer, ChangePasswordMessage message, CancellationToken cancellationToken, string userName = null, string password = null)
	{
		await writer.WriteAsync(FixTags.UserRequestID, cancellationToken);
		await writer.WriteAsync(message.TransactionId.To<string>(), cancellationToken);

		await writer.WriteAsync(FixTags.UserRequestType, cancellationToken);
		await writer.WriteAsync((int)UserRequestType.ChangePasswordForUser, cancellationToken);

		await writer.WriteAsync(FixTags.NewPassword, cancellationToken);
		await writer.WriteAsync(message.NewPassword.UnSecure(), cancellationToken);

		if (userName != null)
		{
			await writer.WriteAsync(FixTags.Username, cancellationToken);
			await writer.WriteAsync(userName, cancellationToken);
		}

		if (password != null)
		{
			await writer.WriteAsync(FixTags.Password, cancellationToken);
			await writer.WriteAsync(password, cancellationToken);
		}

		return FixMessages.UserRequest;
	}

	/// <summary>
	/// To record the <see cref="FixMessages.Heartbeat"/> or <see cref="FixMessages.TestRequest"/> message.
	/// </summary>
	/// <param name="writer">The recorder of data in the FIX protocol format.</param>
	/// <param name="timeMsg">Heartbeat message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="FixMessages"/> value.</returns>
	protected static ValueTask<string> WriteTimeMessageAsync(IFixWriter writer, TimeMessage timeMsg, CancellationToken cancellationToken)
	{
		return !timeMsg.OriginalTransactionId.IsEmpty()
			? WriteHeartbeatAsync(writer, timeMsg.OriginalTransactionId, cancellationToken)
			: WriteTestRequestAsync(writer, timeMsg.TransactionId, cancellationToken);
	}

	private static async ValueTask<string> WriteHeartbeatAsync(IFixWriter writer, string requestId, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.TestReqID, cancellationToken);
		await writer.WriteAsync(requestId.To<string>(), cancellationToken);

		return FixMessages.Heartbeat;
	}

	private static async ValueTask<string> WriteTestRequestAsync(IFixWriter writer, long requestId, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.TestReqID, cancellationToken);
		await writer.WriteAsync(requestId.To<string>(), cancellationToken);

		return FixMessages.TestRequest;
	}

	/// <summary>
	/// Get board code.
	/// </summary>
	/// <param name="destination"><see cref="FixTags.ExDestination"/>.</param>
	/// <param name="exchange"><see cref="FixTags.SecurityGroup"/>.</param>
	/// <param name="tradingSession"><see cref="FixTags.TradingSessionID"/>.</param>
	/// <returns>Board code.</returns>
	protected virtual string GetBoardCode(string destination, string exchange, string tradingSession)
	{
		if (!destination.IsEmpty())
			return destination;

		if (!exchange.IsEmpty())
			return exchange;

		if (!tradingSession.IsEmpty())
			return tradingSession;

		return ExchangeBoard;
	}

	/// <summary>
	/// Write <see cref="FixTags.ClOrdID"/>.
	/// </summary>
	/// <param name="writer">Writer.</param>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected ValueTask WriteClOrdIdAsync(IFixWriter writer, long transactionId, CancellationToken cancellationToken)
	{
		if (SupportUnknownExecutions && _clOrdIds.TryGetValue(transactionId, out var clOrdId))
			return writer.WriteAsync(clOrdId.To<string>(), cancellationToken);
		else
			return writer.WriteAsync(transactionId.To<string>(), cancellationToken);
	}

	/// <inheritdoc />
	public virtual IMessageAdapter Clone() => PersistableHelper.Clone(this);

	object ICloneable.Clone() => Clone();

	ValueTask IMessageAdapter.SendOutMessageAsync(Message message, CancellationToken cancellationToken)
		=> RaiseNewOutMessageAsync(message, cancellationToken);

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_bodyWriter?.Dispose();
		_bodyWriter2?.Dispose();

		base.DisposeManaged();
	}
}
