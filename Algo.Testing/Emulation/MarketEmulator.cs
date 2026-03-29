namespace StockSharp.Algo.Testing.Emulation;

using StockSharp.Algo.Commissions;
using StockSharp.MatchingEngine;

/// <summary>
/// Market emulator v2 with modular architecture.
/// Wraps <see cref="MatchingEngineAdapter"/> and adds emulation-specific logic
/// (random volumes, tick/L1→orderbook conversion, candle matching, commissions, price limits).
/// </summary>
public class MarketEmulator : BaseLogReceiver, IMarketEmulator
{
	private readonly Dictionary<SecurityId, SecurityEmulator> _securityEmulators = [];
	private readonly MatchingEngineAdapter _engine = new();
	private readonly ICommissionManager _commissionManager = new CommissionManager();
	private DateTime _currentTime;
	private DateTime _lastInputTime;

	private IRandomProvider _randomProvider = new DefaultRandomProvider();

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	public MarketEmulator(
		ISecurityProvider securityProvider,
		IPortfolioProvider portfolioProvider,
		IExchangeInfoProvider exchangeInfoProvider,
		IdGenerator transactionIdGenerator)
	{
		SecurityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
		PortfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
		ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
		TransactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));

		_engine.TransactionIdGenerator = transactionIdGenerator;
	}

	/// <inheritdoc />
	public ISecurityProvider SecurityProvider { get; }

	/// <inheritdoc />
	public IPortfolioProvider PortfolioProvider { get; }

	/// <inheritdoc />
	public IExchangeInfoProvider ExchangeInfoProvider { get; }

	/// <summary>
	/// Transaction id generator.
	/// </summary>
	public IdGenerator TransactionIdGenerator { get; }

	/// <inheritdoc />
	public MarketEmulatorSettings Settings { get; } = new();

	/// <inheritdoc />
	public IRandomProvider RandomProvider
	{
		get => _randomProvider;
		set => _randomProvider = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Order ID generator.
	/// </summary>
	public IncrementalIdGenerator OrderIdGenerator
	{
		get => _engine.OrderIdGenerator;
		set => _engine.OrderIdGenerator = value;
	}

	/// <summary>
	/// Trade ID generator.
	/// </summary>
	public IncrementalIdGenerator TradeIdGenerator
	{
		get => _engine.TradeIdGenerator;
		set => _engine.TradeIdGenerator = value;
	}

	/// <summary>
	/// Processed message count.
	/// </summary>
	public long ProcessedMessageCount { get; private set; }

	/// <inheritdoc />
	public override DateTime CurrentTime => _currentTime;

	/// <inheritdoc />
	public event Func<Message, CancellationToken, ValueTask> NewOutMessageAsync;

	/// <summary>
	/// Extended verification mode.
	/// </summary>
	public bool VerifyMode { get; set; }

	/// <summary>
	/// Portfolio manager for handling portfolio state.
	/// </summary>
	public IPortfolioManager PortfolioManager
	{
		get => _engine.PortfolioManager;
		set => _engine.PortfolioManager = value;
	}

	private SecurityEmulator GetEmulator(SecurityId securityId)
	{
		securityId.GetHashCode(); // force caching

		if (!_securityEmulators.TryGetValue(securityId, out var emulator))
		{
			emulator = new SecurityEmulator(this, _engine, securityId);
			_securityEmulators[securityId] = emulator;

			var sec = SecurityProvider.LookupById(securityId);
			if (sec != null)
			{
				var secMsg = sec.ToMessage();
				emulator.ProcessSecurity(secMsg);
				_engine.GetSecurityState(securityId).ProcessSecurity(secMsg);
			}
		}

		return emulator;
	}

	/// <inheritdoc />
	async ValueTask IMessageTransport.SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		var isSystem = message.Type == MessageTypes.Reset || message is BaseConnectionMessage;
		var hasTime = message.LocalTime != default;

		if (!isSystem && hasTime)
		{
			if (_lastInputTime != default && message.LocalTime < _lastInputTime)
				throw new InvalidOperationException($"Message {message.Type} time {message.LocalTime:O} is less than current emulator time {_lastInputTime:O}");

			_lastInputTime = message.LocalTime;
		}

		var results = new List<Message>();

		try
		{
			ProcessMessage(message, results);
		}
		catch (Exception ex)
		{
			results.Add(ex.ToErrorMessage());
		}

		if (message.Type != MessageTypes.Reset)
			ProcessedMessageCount++;

		var allowStore = Settings.AllowStoreGenerateMessages;

		foreach (var msg in results)
		{
			if (!allowStore)
				msg.OfflineMode = MessageOfflineModes.Ignore;

			_currentTime = msg.LocalTime;
			await SendOutMessageAsync(msg, cancellationToken);
		}
	}

	private void SyncEngineSettings()
	{
		_engine.Settings.CheckMoney = Settings.CheckMoney;
		_engine.Settings.CheckTradingState = Settings.CheckTradingState;
		_engine.Settings.IncreaseDepthVolume = Settings.IncreaseDepthVolume;
		_engine.Settings.SpreadSize = Settings.SpreadSize;
		_engine.Settings.InitialOrderId = Settings.InitialOrderId;
		_engine.Settings.InitialTradeId = Settings.InitialTradeId;
	}

	private void ProcessMessage(Message message, List<Message> results)
	{
		SyncEngineSettings();

		switch (message.Type)
		{
			case MessageTypes.Reset:
				Reset();
				results.Add(new ResetMessage());
				break;

			case MessageTypes.EmulationState:
				// Handled by MarketEmulatorAdapter before reaching the emulator.
				// Should not arrive here in normal operation.
				break;

			case MessageTypes.Time:
				// handled below in ProcessTime call
				break;

			case MessageTypes.QuoteChange:
			{
				var quoteMsg = (QuoteChangeMessage)message;
				GetEmulator(quoteMsg.SecurityId).OnQuoteChange();
				// Delegate to engine's SecurityState
				_engine.GetSecurityState(quoteMsg.SecurityId).ProcessQuoteChange(quoteMsg, results);
				break;
			}

			case MessageTypes.Level1Change:
			{
				var l1Msg = (Level1ChangeMessage)message;
				var emulator = GetEmulator(l1Msg.SecurityId);
				// Emulation: L1 → update order book with random volumes
				emulator.ProcessLevel1(l1Msg, results);
				// Check stop orders
				if (l1Msg.TryGetDecimal(Level1Fields.LastTradePrice) is { } l1LastPrice)
					_engine.CheckStopOrders(l1Msg.SecurityId, l1LastPrice, l1Msg.LocalTime, results);
				break;
			}

			case MessageTypes.Execution:
				ProcessExecution((ExecutionMessage)message, results);
				break;

			case MessageTypes.OrderRegister:
				ProcessOrderRegister((OrderRegisterMessage)message, results);
				break;

			case MessageTypes.OrderCancel:
				_engine.ProcessOrderCancel((OrderCancelMessage)message, results);
				break;

			case MessageTypes.OrderReplace:
			{
				var engineResults = new List<Message>();
				_engine.ProcessMessage(message, engineResults);
				ApplyCommissions(engineResults);
				results.AddRange(engineResults);
				break;
			}

			case MessageTypes.OrderGroupCancel:
			{
				var engineResults = new List<Message>();
				_engine.ProcessMessage(message, engineResults);
				ApplyCommissions(engineResults);
				results.AddRange(engineResults);
				break;
			}

			case MessageTypes.OrderStatus:
			{
				var engineResults = new List<Message>();
				_engine.ProcessMessage(message, engineResults);
				results.AddRange(engineResults);
				break;
			}

			case MessageTypes.PortfolioLookup:
			{
				var engineResults = new List<Message>();
				_engine.ProcessMessage(message, engineResults);
				results.AddRange(engineResults);
				break;
			}

			case MessageTypes.Security:
			{
				var secMsg = (SecurityMessage)message;
				GetEmulator(secMsg.SecurityId).ProcessSecurity(secMsg);
				_engine.GetSecurityState(secMsg.SecurityId).ProcessSecurity(secMsg);
				break;
			}

			case MessageTypes.PositionChange:
			{
				var engineResults = new List<Message>();
				_engine.ProcessMessage(message, engineResults);
				results.AddRange(engineResults);
				break;
			}

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;
				if (!mdMsg.SecurityId.IsAllSecurity())
				{
					GetEmulator(mdMsg.SecurityId).ProcessMarketData(mdMsg);
					_engine.GetSecurityState(mdMsg.SecurityId).ProcessMarketData(mdMsg);
				}
				break;
			}

			case HistoryMessageTypes.CommissionRule:
				_commissionManager.Rules.Add(((CommissionRuleMessage)message).Rule);
				break;

			default:
				if (message is CandleMessage candleMsg)
					GetEmulator(candleMsg.SecurityId).ProcessCandle(candleMsg, results);
				break;
		}

		// Process time-based events
		ProcessTime(message.LocalTime, results);
	}

	private void ProcessExecution(ExecutionMessage execMsg, List<Message> results)
	{
		var emulator = GetEmulator(execMsg.SecurityId);

		if (execMsg.DataType == DataType.Ticks)
		{
			emulator.ProcessTick(execMsg, results);
			if (execMsg.TradePrice is { } tickPrice)
				_engine.CheckStopOrders(execMsg.SecurityId, tickPrice, execMsg.LocalTime, results);

			if (emulator.HasTicksSubscription)
				results.Add(execMsg);
		}
		else if (execMsg.DataType == DataType.OrderLog)
		{
			emulator.ProcessOrderLog(execMsg, results);
		}
		else if (execMsg.DataType == DataType.Transactions)
		{
			if (execMsg.HasOrderInfo())
			{
				var regMsg = new OrderRegisterMessage
				{
					TransactionId = execMsg.TransactionId,
					SecurityId = execMsg.SecurityId,
					PortfolioName = execMsg.PortfolioName,
					Side = execMsg.Side,
					Price = execMsg.OrderPrice,
					Volume = execMsg.OrderVolume ?? 0,
					OrderType = execMsg.OrderType ?? OrderTypes.Limit,
					TimeInForce = execMsg.TimeInForce,
					TillDate = execMsg.ExpiryDate,
					PostOnly = execMsg.PostOnly,
					LocalTime = execMsg.LocalTime,
				};
				ProcessOrderRegister(regMsg, results);
			}
		}
	}

	private void ProcessOrderRegister(OrderRegisterMessage regMsg, List<Message> results)
	{
		var emulator = GetEmulator(regMsg.SecurityId);

		// Candle matching is emulation-specific
		var candle = emulator.HasCandleSubscription ? emulator.GetLastStoredCandle() : null;

		if (candle is not null)
		{
			ProcessOrderRegisterWithCandle(regMsg, emulator, candle, results);
		}
		else
		{
			// Delegate to engine for standard matching
			var beforeCount = results.Count;
			_engine.ProcessOrderRegister(regMsg, results);
			ApplyCommissions(results, beforeCount);
		}
	}

	private void ProcessOrderRegisterWithCandle(OrderRegisterMessage regMsg, SecurityEmulator emulator, CandleMessage candle, List<Message> results)
	{
		// Stop order interception
		if (regMsg.OrderType == OrderTypes.Conditional && regMsg.Condition is IStopLossOrderCondition)
		{
			_engine.ProcessOrderRegister(regMsg, results);
			return;
		}

		var state = _engine.GetSecurityState(regMsg.SecurityId);
		var portfolio = _engine.PortfolioManager.GetPortfolio(regMsg.PortfolioName);
		var serverTime = regMsg.LocalTime;

		// Validate
		if (Settings.CheckMoney)
		{
			if (_engine.PortfolioManager is EmulatedPortfolioManager epm && epm.MarginController is null)
				epm.MarginController = new MarginController();

			var error = _engine.PortfolioManager.ValidateFunds(regMsg.PortfolioName, regMsg.SecurityId, regMsg.Price, regMsg.Volume);
			if (error != null)
			{
				results.Add(CreateOrderResponse(regMsg, OrderStates.Failed, error: error));
				return;
			}
		}

		var replyMsg = new ExecutionMessage
		{
			HasOrderInfo = true,
			DataTypeEx = DataType.Transactions,
			ServerTime = serverTime,
			LocalTime = regMsg.LocalTime,
			OriginalTransactionId = regMsg.TransactionId,
		};
		results.Add(replyMsg);

		var marginPrice = 0m; // candle mode — no book

		var order = new EmulatorOrder
		{
			TransactionId = regMsg.TransactionId,
			Side = regMsg.Side,
			Price = regMsg.Price,
			Balance = regMsg.Volume,
			Volume = regMsg.Volume,
			PortfolioName = regMsg.PortfolioName,
			TimeInForce = regMsg.TimeInForce,
			OrderType = regMsg.OrderType,
			PostOnly = regMsg.PostOnly ?? false,
			ExpiryDate = regMsg.TillDate,
			ServerTime = serverTime,
			LocalTime = regMsg.LocalTime,
			MarginPrice = marginPrice,
		};

		var orderId = _engine.OrderIdGenerator.GetNextId();
		order.OrderId = orderId;

		portfolio.ProcessOrderRegistration(regMsg.SecurityId, regMsg.Side, regMsg.Volume, marginPrice);
		AddPortfolioUpdate(portfolio, regMsg.LocalTime, results);

		var matchResult = MatchOrderByCandle(order, candle);

		if (matchResult.IsRejected)
		{
			portfolio.ProcessOrderCancellation(regMsg.SecurityId, regMsg.Side, regMsg.Volume, marginPrice);

			replyMsg.OrderState = OrderStates.Done;
			replyMsg.Balance = regMsg.Volume;
			replyMsg.OrderVolume = regMsg.Volume;
			replyMsg.Error = new InvalidOperationException(matchResult.RejectionReason);
			return;
		}

		var isIOC = regMsg.TimeInForce == TimeInForce.CancelBalance;
		var isFOK = regMsg.TimeInForce == TimeInForce.MatchOrCancel;
		var hasTrades = matchResult.Trades.Count > 0;

		var marketPrice = regMsg.Side == Sides.Buy
			? state.OrderBook.BestAsk?.price
			: state.OrderBook.BestBid?.price;

		if ((isIOC || isFOK) && hasTrades)
		{
			results.Add(new ExecutionMessage
			{
				LocalTime = regMsg.LocalTime,
				SecurityId = regMsg.SecurityId,
				OrderId = orderId,
				OriginalTransactionId = regMsg.TransactionId,
				Balance = matchResult.RemainingVolume,
				OrderVolume = regMsg.Volume,
				OrderState = OrderStates.Done,
				PortfolioName = regMsg.PortfolioName,
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				ServerTime = serverTime,
			});
		}

		foreach (var trade in matchResult.Trades)
		{
			var tradeId = _engine.TradeIdGenerator.GetNextId();

			var tradeMsg = new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				SecurityId = regMsg.SecurityId,
				LocalTime = regMsg.LocalTime,
				ServerTime = serverTime,
				OriginalTransactionId = regMsg.TransactionId,
				OrderId = orderId,
				TradeId = tradeId,
				TradePrice = trade.Price,
				TradeVolume = trade.Volume,
				Side = regMsg.Side,
				MarketPrice = marketPrice,
			};

			tradeMsg.Commission = _commissionManager.Process(tradeMsg);

			var (_, _, position) = portfolio.ProcessTrade(
				regMsg.SecurityId, regMsg.Side, trade.Price, trade.Volume, tradeMsg.Commission);

			if (!isIOC && !isFOK)
			{
				results.Add(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					LocalTime = regMsg.LocalTime,
					ServerTime = serverTime,
					SecurityId = regMsg.SecurityId,
					OrderId = orderId,
					OriginalTransactionId = regMsg.TransactionId,
					Balance = matchResult.RemainingVolume,
					OrderVolume = regMsg.Volume,
					OrderState = matchResult.FinalState,
					PortfolioName = regMsg.PortfolioName,
					HasOrderInfo = true,
				});
			}

			results.Add(tradeMsg);

			results.Add(new PositionChangeMessage
			{
				SecurityId = SecurityId.Money,
				ServerTime = serverTime,
				LocalTime = regMsg.LocalTime,
				PortfolioName = regMsg.PortfolioName,
			}
			.Add(PositionChangeTypes.CurrentValue, position.CurrentValue)
			.TryAdd(PositionChangeTypes.AveragePrice, position.AveragePrice));

			AddPortfolioUpdate(portfolio, regMsg.LocalTime, results);
		}

		var isCancelled = matchResult.FinalState == OrderStates.Done && matchResult.RemainingVolume > 0;

		if ((isIOC || isFOK) && isCancelled)
		{
			if (!hasTrades)
			{
				results.Add(new ExecutionMessage
				{
					LocalTime = regMsg.LocalTime,
					SecurityId = regMsg.SecurityId,
					OrderId = orderId,
					OriginalTransactionId = regMsg.TransactionId,
					Balance = matchResult.RemainingVolume,
					OrderVolume = regMsg.Volume,
					OrderState = OrderStates.Done,
					PortfolioName = regMsg.PortfolioName,
					DataTypeEx = DataType.Transactions,
					HasOrderInfo = true,
					ServerTime = serverTime,
				});
			}

			portfolio.ProcessOrderCancellation(regMsg.SecurityId, regMsg.Side, matchResult.RemainingVolume, marginPrice);
			AddPortfolioUpdate(portfolio, regMsg.LocalTime, results);
		}
		else if (matchResult.Trades.Count == 0)
		{
			replyMsg.OrderState = matchResult.FinalState;
			if (matchResult.FinalState != OrderStates.Active)
			{
				replyMsg.Balance = matchResult.RemainingVolume;
				replyMsg.OrderVolume = regMsg.Volume;
			}
		}

		if (matchResult.ShouldPlaceInBook && matchResult.RemainingVolume > 0)
		{
			if (regMsg.TillDate.HasValue && regMsg.TillDate.Value <= regMsg.LocalTime)
			{
				replyMsg.OrderState = OrderStates.Done;
				replyMsg.Balance = matchResult.RemainingVolume;
				replyMsg.OrderVolume = regMsg.Volume;
			}
			else
			{
				order.Balance = matchResult.RemainingVolume;
				state.OrderBook.AddQuote(order);
				state.OrderManager.RegisterOrder(order, regMsg.LocalTime);
			}
		}

		if (state.HasDepthSubscription)
		{
			results.Add(state.OrderBook.ToMessage(regMsg.LocalTime, serverTime));
		}
	}

	private void ProcessTime(DateTime time, List<Message> results)
	{
		// Process expired orders via engine's security states
		foreach (var (secId, _) in _securityEmulators)
		{
			var state = _engine.GetSecurityState(secId);
			var expired = state.OrderManager.ProcessTime(time);
			foreach (var order in expired)
			{
				state.OrderBook.RemoveQuote(order.TransactionId, order.Side, order.Price);

				results.Add(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					SecurityId = state.SecurityId,
					LocalTime = time,
					ServerTime = time,
					OriginalTransactionId = order.TransactionId,
					OrderId = order.OrderId,
					OrderState = OrderStates.Done,
					Balance = order.Balance,
					OrderVolume = order.Volume,
					PortfolioName = order.PortfolioName,
					HasOrderInfo = true,
				});
			}
		}

		// Emulation handles stored candles
		foreach (var emulator in _securityEmulators.Values)
		{
			emulator.ProcessStoredCandles(time, results);
		}
	}

	/// <summary>
	/// Match order against candle data directly.
	/// </summary>
	private static MatchResult MatchOrderByCandle(EmulatorOrder order, CandleMessage candle)
	{
		var balance = order.Balance;

		var leftBalance = candle.TotalVolume == 0
			? 0
			: 0m.Max(balance - candle.TotalVolume);

		if (leftBalance > 0 && order.TimeInForce == TimeInForce.MatchOrCancel)
		{
			return new MatchResult
			{
				Order = order,
				Trades = [],
				MatchedOrders = [],
				RemainingVolume = balance,
				ShouldPlaceInBook = false,
				FinalState = OrderStates.Done,
			};
		}

		decimal execPrice;

		if (order.OrderType == OrderTypes.Market)
		{
			execPrice = (candle.HighPrice + candle.LowPrice) / 2;

			if (execPrice > candle.HighPrice || execPrice < candle.LowPrice)
				execPrice = candle.ClosePrice;
		}
		else
		{
			if (order.Price > candle.HighPrice || order.Price < candle.LowPrice)
			{
				return new MatchResult
				{
					Order = order,
					Trades = [],
					MatchedOrders = [],
					RemainingVolume = balance,
					ShouldPlaceInBook = order.TimeInForce != TimeInForce.CancelBalance,
					FinalState = order.TimeInForce == TimeInForce.CancelBalance ? OrderStates.Done : OrderStates.Active,
				};
			}

			execPrice = order.Price;
		}

		var tradeVolume = balance - leftBalance;
		var trades = new List<MatchTrade>
		{
			new(execPrice, tradeVolume, order.Side, [])
		};

		var isFullyMatched = leftBalance <= 0;
		var finalState = isFullyMatched ? OrderStates.Done : OrderStates.Active;
		var shouldPlaceInBook = !isFullyMatched && order.TimeInForce != TimeInForce.CancelBalance;

		if (order.TimeInForce == TimeInForce.CancelBalance)
			finalState = OrderStates.Done;

		return new MatchResult
		{
			Order = order,
			Trades = trades,
			MatchedOrders = [],
			RemainingVolume = leftBalance,
			ShouldPlaceInBook = shouldPlaceInBook,
			FinalState = finalState,
		};
	}

	private void Reset()
	{
		_securityEmulators.Clear();
		_lastInputTime = default;

		// Reset engine state (clears security states, portfolio manager, stop orders, id generators)
		var engineResults = new List<Message>();
		_engine.ProcessMessage(new ResetMessage(), engineResults);
		// engineResults will contain a ResetMessage from engine, which we ignore
		// since our caller already adds its own
	}

	private void ApplyCommissions(List<Message> messages, int startIndex = 0)
	{
		for (var i = startIndex; i < messages.Count; i++)
		{
			if (messages[i] is ExecutionMessage exec && exec.DataType == DataType.Transactions && exec.TradeId is not null)
			{
				exec.Commission = _commissionManager.Process(exec);
			}
		}
	}

	private static ExecutionMessage CreateOrderResponse(OrderRegisterMessage regMsg, OrderStates state,
		long? orderId = null, decimal? balance = null, decimal? volume = null, Exception error = null)
	{
		return new()
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = regMsg.SecurityId,
			LocalTime = regMsg.LocalTime,
			ServerTime = regMsg.LocalTime,
			OriginalTransactionId = regMsg.TransactionId,
			OrderId = orderId,
			OrderState = state,
			Balance = balance,
			OrderVolume = volume ?? regMsg.Volume,
			OrderPrice = regMsg.Price,
			Side = regMsg.Side,
			PortfolioName = regMsg.PortfolioName,
			Error = error,
			HasOrderInfo = true,
		};
	}

	private static void AddPortfolioUpdate(IPortfolio portfolio, DateTime time, List<Message> results)
	{
		var unrealizedPnL = 0m;
		var totalPnL = portfolio.RealizedPnL - portfolio.Commission + unrealizedPnL;

		results.Add(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = time,
			LocalTime = time,
			PortfolioName = portfolio.Name,
		}
		.Add(PositionChangeTypes.RealizedPnL, portfolio.RealizedPnL)
		.TryAdd(PositionChangeTypes.UnrealizedPnL, unrealizedPnL, true)
		.Add(PositionChangeTypes.VariationMargin, totalPnL)
		.Add(PositionChangeTypes.CurrentValue, portfolio.CurrentMoney)
		.Add(PositionChangeTypes.BlockedValue, portfolio.BlockedMoney)
		.Add(PositionChangeTypes.Commission, portfolio.Commission));
	}

	/// <inheritdoc />
	public IEnumerable<MessageTypeInfo> PossibleSupportedMessages { get; } =
	[
		MessageTypes.SecurityLookup.ToInfo(),
		MessageTypes.DataTypeLookup.ToInfo(),
		MessageTypes.BoardLookup.ToInfo(),
		MessageTypes.MarketData.ToInfo(),
		MessageTypes.PortfolioLookup.ToInfo(),
		MessageTypes.OrderStatus.ToInfo(),
		MessageTypes.OrderRegister.ToInfo(),
		MessageTypes.OrderCancel.ToInfo(),
		MessageTypes.OrderReplace.ToInfo(),
		MessageTypes.OrderGroupCancel.ToInfo(),
		MessageTypes.BoardState.ToInfo(),
		MessageTypes.Security.ToInfo(),
		MessageTypes.Portfolio.ToInfo(),
		MessageTypes.Board.ToInfo(),
		MessageTypes.Reset.ToInfo(),
		MessageTypes.QuoteChange.ToInfo(),
		MessageTypes.Level1Change.ToInfo(),
		MessageTypes.EmulationState.ToInfo(),
		HistoryMessageTypes.CommissionRule.ToInfo(),
	];

	/// <summary>
	/// Send out message.
	/// </summary>
	public ValueTask SendOutMessageAsync(Message message, CancellationToken cancellationToken)
		=> NewOutMessageAsync?.Invoke(message, cancellationToken) ?? default;
}

/// <summary>
/// Security-specific emulator state.
/// Handles emulation-specific logic: tick→orderbook, L1→orderbook, candle storage, price limits.
/// </summary>
internal class SecurityEmulator(MarketEmulator parent, MatchingEngineAdapter engine, SecurityId securityId)
{
	private readonly MarketEmulator _parent = parent;
	private readonly MatchingEngineAdapter _engine = engine;
	private SecurityMessage _securityDefinition;
	private long? _depthSubscription;
	private long? _ticksSubscription;
	private long? _candlesSubscription;
	private bool _candlesNonFinished;
	private readonly SortedDictionary<DateTime, List<CandleMessage>> _storedCandles = [];

	public SecurityId SecurityId { get; } = securityId;

	private SecurityState GetEngineState() => _engine.GetSecurityState(SecurityId);

	private bool _priceStepUpdated;
	private bool _volumeStepUpdated;
	private DateTime _lastPriceLimitDate;

	public decimal PriceStep => _securityDefinition?.PriceStep ?? 0.01m;
	public decimal VolumeStep => _securityDefinition?.VolumeStep ?? 1m;
	public bool HasDepthSubscription => _depthSubscription.HasValue;
	public bool HasTicksSubscription => _ticksSubscription.HasValue;

	/// <summary>
	/// True when order book was populated from candle data.
	/// </summary>
	public bool IsCandleMatchingMode { get; private set; }

	public void ProcessSecurity(SecurityMessage msg)
	{
		_securityDefinition = msg;
	}

	private void UpdateSteps(decimal price, decimal? volume)
	{
		if (!_priceStepUpdated && price > 0)
		{
			_securityDefinition ??= new SecurityMessage { SecurityId = SecurityId };
			_securityDefinition.PriceStep = price.GetDecimalInfo().EffectiveScale.GetPriceStep();
			_priceStepUpdated = true;
		}

		if (!_volumeStepUpdated && volume > 0)
		{
			_securityDefinition ??= new SecurityMessage { SecurityId = SecurityId };
			_securityDefinition.VolumeStep = volume.Value.GetDecimalInfo().EffectiveScale.GetPriceStep();
			_volumeStepUpdated = true;
		}
	}

	private void UpdatePriceLimits(decimal price, DateTime localTime, DateTime serverTime, List<Message> results)
	{
		if (_lastPriceLimitDate == localTime.Date)
			return;

		_lastPriceLimitDate = localTime.Date;

		var priceOffset = _parent.Settings.PriceLimitOffset;
		var priceStep = PriceStep;

		var level1Msg = new Level1ChangeMessage
		{
			SecurityId = SecurityId,
			LocalTime = localTime,
			ServerTime = serverTime,
		}
		.Add(Level1Fields.MinPrice, ShrinkPrice((decimal)(price - priceOffset), priceStep))
		.Add(Level1Fields.MaxPrice, ShrinkPrice((decimal)(price + priceOffset), priceStep));

		results.Add(level1Msg);
	}

	private static decimal ShrinkPrice(decimal price, decimal step)
	{
		if (step == 0)
			return price;
		return (price / step).Round() * step;
	}

	public void ProcessMarketData(MarketDataMessage msg)
	{
		if (msg.IsSubscribe)
		{
			if (msg.DataType2 == DataType.MarketDepth)
				_depthSubscription = msg.TransactionId;
			else if (msg.DataType2 == DataType.Ticks)
				_ticksSubscription = msg.TransactionId;
			else if (msg.DataType2.IsCandles)
			{
				_candlesSubscription = msg.TransactionId;
				_candlesNonFinished = !msg.IsFinishedOnly;
			}
		}
		else
		{
			if (_depthSubscription == msg.OriginalTransactionId)
				_depthSubscription = null;
			else if (_ticksSubscription == msg.OriginalTransactionId)
				_ticksSubscription = null;
			else if (_candlesSubscription == msg.OriginalTransactionId)
			{
				_candlesSubscription = null;
				_candlesNonFinished = false;
			}
		}
	}

	/// <summary>
	/// Called when QuoteChange is received - exit candle matching mode.
	/// </summary>
	public void OnQuoteChange()
	{
		IsCandleMatchingMode = false;
	}

	public void ProcessLevel1(Level1ChangeMessage msg, List<Message> results)
	{
		IsCandleMatchingMode = false;

		var bidPrice = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestBidPrice);
		var askPrice = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestAskPrice);
		var bidVol = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestBidVolume);
		if (bidVol == 0) bidVol = null;
		bidVol ??= _parent.RandomProvider.NextVolume();

		var askVol = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestAskVolume);
		if (askVol == 0) askVol = null;
		askVol ??= _parent.RandomProvider.NextVolume();

		UpdateSteps(bidPrice ?? askPrice ?? 0, bidVol.Value);

		var engineState = GetEngineState();

		if (bidPrice.HasValue)
			engineState.OrderBook.UpdateLevel(Sides.Buy, bidPrice.Value, bidVol.Value);

		if (askPrice.HasValue)
			engineState.OrderBook.UpdateLevel(Sides.Sell, askPrice.Value, askVol.Value);

		if (_depthSubscription.HasValue)
		{
			results.Add(engineState.OrderBook.ToMessage(msg.LocalTime, msg.ServerTime));
		}
	}

	public void ProcessTick(ExecutionMessage tick, List<Message> results)
	{
		IsCandleMatchingMode = false;

		var tradePrice = tick.TradePrice ?? 0;
		var tradeVolume = tick.TradeVolume ?? 0m;
		if (tradeVolume == 0)
			tradeVolume = _parent.RandomProvider.NextVolume();

		UpdateSteps(tradePrice, tradeVolume);

		if (tradePrice > 0)
			UpdatePriceLimits(tradePrice, tick.LocalTime, tick.ServerTime, results);

		var priceStep = PriceStep;
		var spread = priceStep * _parent.Settings.SpreadSize;

		var engineState = GetEngineState();

		if (tradePrice <= 0)
		{
			if (_depthSubscription.HasValue)
				results.Add(engineState.OrderBook.ToMessage(tick.LocalTime, tick.ServerTime));
			return;
		}

		var bestBid = engineState.OrderBook.BestBid;
		var bestAsk = engineState.OrderBook.BestAsk;

		Sides originSide;
		if (tick.OriginSide.HasValue)
			originSide = tick.OriginSide.Value.Invert();
		else if (bestBid.HasValue && !bestAsk.HasValue)
			originSide = Sides.Sell;
		else if (bestAsk.HasValue && !bestBid.HasValue)
			originSide = Sides.Buy;
		else
			originSide = Sides.Sell;

		if (engineState.OrderBook.BidLevels == 0 && engineState.OrderBook.AskLevels == 0)
		{
			engineState.OrderBook.UpdateLevel(originSide, tradePrice, tradeVolume);
			var oppositePrice = tradePrice + spread * (originSide == Sides.Buy ? 1 : -1);
			if (oppositePrice > 0)
				engineState.OrderBook.UpdateLevel(originSide.Invert(), oppositePrice, tradeVolume);
		}
		else
		{
			if (bestBid.HasValue && tradePrice <= bestBid.Value.price)
			{
				engineState.OrderBook.UpdateLevel(Sides.Sell, tradePrice, tradeVolume);
			}
			else if (bestAsk.HasValue && tradePrice >= bestAsk.Value.price)
			{
				engineState.OrderBook.UpdateLevel(Sides.Buy, tradePrice, tradeVolume);
			}
			else
			{
				engineState.OrderBook.UpdateLevel(originSide, tradePrice, tradeVolume);
			}
		}

		if (_depthSubscription.HasValue)
		{
			results.Add(engineState.OrderBook.ToMessage(tick.LocalTime, tick.ServerTime));
		}
	}

	public void ProcessOrderLog(ExecutionMessage ol, List<Message> results)
	{
		IsCandleMatchingMode = false;

		if (ol.TradeId is not null)
			return;

		UpdateSteps(ol.OrderPrice, ol.OrderVolume);

		var engineState = GetEngineState();

		if (ol.IsCancellation)
		{
			engineState.OrderBook.UpdateLevel(ol.Side, ol.OrderPrice, 0);
		}
		else
		{
			var currentVol = engineState.OrderBook.GetVolumeAtPrice(ol.Side, ol.OrderPrice);
			engineState.OrderBook.UpdateLevel(ol.Side, ol.OrderPrice, currentVol + (ol.OrderVolume ?? 0));
		}

		if (_depthSubscription.HasValue)
		{
			results.Add(engineState.OrderBook.ToMessage(ol.LocalTime, ol.ServerTime));
		}
	}

	public bool HasCandleSubscription => _candlesSubscription.HasValue;

	public CandleMessage GetLastStoredCandle()
	{
		if (_storedCandles.Count == 0)
			return null;

		var lastPair = _storedCandles.Last();
		return lastPair.Value.FirstOrDefault();
	}

	public void ProcessCandle(CandleMessage candle, List<Message> results)
	{
		UpdateSteps(candle.ClosePrice, candle.TotalVolume);

		var candles = _storedCandles.SafeAdd(candle.OpenTime, key => []);
		candles.Add(candle);
	}

	public void ProcessStoredCandles(DateTime currentTime, List<Message> results)
	{
		if (_storedCandles.Count == 0)
			return;

		List<DateTime> toRemove = null;

		foreach (var pair in _storedCandles)
		{
			if (pair.Key >= currentTime)
				break;

			toRemove ??= [];
			toRemove.Add(pair.Key);

			if (_candlesNonFinished)
			{
				foreach (var candle in pair.Value)
				{
					var openState = candle.TypedClone();
					openState.State = CandleStates.Active;
					openState.HighPrice = openState.LowPrice = openState.ClosePrice = openState.OpenPrice;
					if (candle.OpenTime != default)
						openState.LocalTime = candle.OpenTime;
					results.Add(openState);

					var highState = openState.TypedClone();
					highState.HighPrice = candle.HighPrice;
					if (candle.HighTime != default)
						highState.LocalTime = candle.HighTime;
					results.Add(highState);

					var lowState = openState.TypedClone();
					lowState.HighPrice = candle.HighPrice;
					if (candle.LowTime != default)
						lowState.LocalTime = candle.LowTime;
					results.Add(lowState);
				}
			}

			results.Add(new TimeMessage { LocalTime = currentTime });

			foreach (var candle in pair.Value)
			{
				var finalCandle = candle.TypedClone();
				finalCandle.LocalTime = currentTime;
				results.Add(finalCandle);

				ApplyCandleToOrderBook(candle, results);
			}
		}

		if (toRemove is not null)
		{
			foreach (var key in toRemove)
				_storedCandles.Remove(key);
		}
	}

	private void ApplyCandleToOrderBook(CandleMessage candle, List<Message> results)
	{
		IsCandleMatchingMode = true;

		var engineState = GetEngineState();
		var vol = candle.TotalVolume > 0 ? candle.TotalVolume / 2 : 10m;

		engineState.OrderBook.UpdateLevel(Sides.Sell, candle.LowPrice, vol);
		engineState.OrderBook.UpdateLevel(Sides.Buy, candle.HighPrice, vol);

		if (_depthSubscription.HasValue)
		{
			results.Add(engineState.OrderBook.ToMessage(candle.OpenTime, candle.OpenTime));
		}
	}
}
