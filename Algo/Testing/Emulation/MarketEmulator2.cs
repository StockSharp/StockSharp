namespace StockSharp.Algo.Testing.Emulation;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.Testing;

/// <summary>
/// Market emulator v2 with modular architecture.
/// </summary>
public class MarketEmulator2 : BaseLogReceiver, IMarketEmulator
{
	private readonly Dictionary<SecurityId, SecurityEmulator> _securityEmulators = [];
	private readonly Dictionary<string, PortfolioEmulator2> _portfolios = [];
	private readonly ICommissionManager _commissionManager = new CommissionManager();
	private DateTime _currentTime;

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	public MarketEmulator2(
		ISecurityProvider securityProvider,
		IPortfolioProvider portfolioProvider,
		IExchangeInfoProvider exchangeInfoProvider,
		IdGenerator transactionIdGenerator)
	{
		SecurityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
		PortfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
		ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
		TransactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));

		((IMessageAdapter)this).SupportedInMessages = [.. ((IMessageAdapter)this).PossibleSupportedMessages.Select(i => i.Type)];
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

	/// <summary>
	/// Order ID generator.
	/// </summary>
	public IncrementalIdGenerator OrderIdGenerator { get; set; } = new();

	/// <summary>
	/// Trade ID generator.
	/// </summary>
	public IncrementalIdGenerator TradeIdGenerator { get; set; } = new();

	/// <summary>
	/// Processed message count.
	/// </summary>
	public long ProcessedMessageCount { get; private set; }

	/// <inheritdoc />
	public override DateTime CurrentTimeUtc => _currentTime;

	/// <inheritdoc />
	public event Action<Message> NewOutMessage;

	/// <summary>
	/// Extended verification mode.
	/// </summary>
	public bool VerifyMode { get; set; }

	private SecurityEmulator GetEmulator(SecurityId securityId)
	{
		securityId.GetHashCode(); // force caching

		if (!_securityEmulators.TryGetValue(securityId, out var emulator))
		{
			emulator = new SecurityEmulator(this, securityId);
			_securityEmulators[securityId] = emulator;

			var sec = SecurityProvider.LookupById(securityId);
			if (sec != null)
				emulator.ProcessSecurity(sec.ToMessage());
		}

		return emulator;
	}

	private PortfolioEmulator2 GetPortfolio(string name)
	{
		if (!_portfolios.TryGetValue(name, out var portfolio))
		{
			portfolio = new PortfolioEmulator2(name);
			_portfolios[name] = portfolio;
		}
		return portfolio;
	}

	/// <inheritdoc />
	ValueTask IMessageAdapter.SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

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

		foreach (var msg in results)
		{
			_currentTime = msg.LocalTime;
			NewOutMessage?.Invoke(msg);
		}

		return default;
	}

	private void ProcessMessage(Message message, List<Message> results)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				Reset();
				results.Add(new ResetMessage());
				break;

			case MessageTypes.Time:
				ProcessTime(message.LocalTime, results);
				break;

			case MessageTypes.QuoteChange:
				var quoteMsg = (QuoteChangeMessage)message;
				GetEmulator(quoteMsg.SecurityId).ProcessQuoteChange(quoteMsg, results);
				break;

			case MessageTypes.Level1Change:
				var l1Msg = (Level1ChangeMessage)message;
				GetEmulator(l1Msg.SecurityId).ProcessLevel1(l1Msg, results);
				break;

			case MessageTypes.Execution:
				ProcessExecution((ExecutionMessage)message, results);
				break;

			case MessageTypes.OrderRegister:
				ProcessOrderRegister((OrderRegisterMessage)message, results);
				break;

			case MessageTypes.OrderCancel:
				ProcessOrderCancel((OrderCancelMessage)message, results);
				break;

			case MessageTypes.OrderReplace:
				ProcessOrderReplace((OrderReplaceMessage)message, results);
				break;

			case MessageTypes.OrderGroupCancel:
				ProcessOrderGroupCancel((OrderGroupCancelMessage)message, results);
				break;

			case MessageTypes.OrderStatus:
				ProcessOrderStatus((OrderStatusMessage)message, results);
				break;

			case MessageTypes.PortfolioLookup:
				ProcessPortfolioLookup((PortfolioLookupMessage)message, results);
				break;

			case MessageTypes.Security:
				var secMsg = (SecurityMessage)message;
				GetEmulator(secMsg.SecurityId).ProcessSecurity(secMsg);
				break;

			case MessageTypes.PositionChange:
				ProcessPositionChange((PositionChangeMessage)message, results);
				break;

			case MessageTypes.MarketData:
				var mdMsg = (MarketDataMessage)message;
				if (!mdMsg.SecurityId.IsAllSecurity())
					GetEmulator(mdMsg.SecurityId).ProcessMarketData(mdMsg);
				break;

			default:
				if (message is CandleMessage candleMsg)
					GetEmulator(candleMsg.SecurityId).ProcessCandle(candleMsg, results);
				break;
		}
	}

	private void ProcessExecution(ExecutionMessage execMsg, List<Message> results)
	{
		var emulator = GetEmulator(execMsg.SecurityId);

		if (execMsg.DataType == DataType.Ticks)
		{
			emulator.ProcessTick(execMsg, results);
		}
		else if (execMsg.DataType == DataType.OrderLog)
		{
			emulator.ProcessOrderLog(execMsg, results);
		}
		else if (execMsg.DataType == DataType.Transactions)
		{
			if (execMsg.HasOrderInfo())
			{
				// Internal order processing
				emulator.ProcessOrderExecution(execMsg, results);
			}
		}
	}

	private void ProcessOrderRegister(OrderRegisterMessage regMsg, List<Message> results)
	{
		var emulator = GetEmulator(regMsg.SecurityId);
		var portfolio = GetPortfolio(regMsg.PortfolioName);
		var serverTime = regMsg.LocalTime;

		// Validate registration
		var error = ValidateRegistration(regMsg);
		if (error != null)
		{
			results.Add(CreateOrderResponse(regMsg, OrderStates.Failed, error: error));
			return;
		}

		// Create emulator order
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
		};

		// Track order registration for blocked funds
		portfolio.ProcessOrderRegistration(regMsg.SecurityId, regMsg.Side, regMsg.Volume, regMsg.Price);

		// Match order
		var matcher = new OrderMatcher();
		var matchSettings = new MatchingSettings
		{
			PriceStep = emulator.PriceStep,
			VolumeStep = emulator.VolumeStep,
		};

		var matchResult = matcher.Match(order, emulator.OrderBook, matchSettings);

		// Process result
		if (matchResult.IsRejected)
		{
			// Unblock funds on rejection
			portfolio.ProcessOrderCancellation(regMsg.SecurityId, regMsg.Side, regMsg.Volume, regMsg.Price);
			results.Add(CreateOrderResponse(regMsg, OrderStates.Done, balance: regMsg.Volume,
				error: new InvalidOperationException(matchResult.RejectionReason)));
			return;
		}

		// Generate trades
		var orderId = OrderIdGenerator.GetNextId();
		foreach (var trade in matchResult.Trades)
		{
			var tradeId = TradeIdGenerator.GetNextId();

			// Calculate commission
			var commission = _commissionManager.Process(new ExecutionMessage
			{
				TradePrice = trade.Price,
				TradeVolume = trade.Volume,
				Side = regMsg.Side,
			});

			// Update portfolio and get position info
			var (realizedPnL, positionChange, position) = portfolio.ProcessTrade(
				regMsg.SecurityId, regMsg.Side, trade.Price, trade.Volume, commission);

			// Trade for our order
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
				Commission = commission,
			};
			results.Add(tradeMsg);

			// Send position change for the security
			results.Add(new PositionChangeMessage
			{
				SecurityId = regMsg.SecurityId,
				ServerTime = serverTime,
				LocalTime = regMsg.LocalTime,
				PortfolioName = regMsg.PortfolioName,
			}
			.Add(PositionChangeTypes.CurrentValue, position.CurrentValue)
			.TryAdd(PositionChangeTypes.AveragePrice, position.AveragePrice));

			// Generate trades for matched counter orders
			foreach (var counterOrder in trade.CounterOrders)
			{
				if (counterOrder.IsUserOrder)
				{
					var counterPortfolio = GetPortfolio(counterOrder.PortfolioName);
					var counterVolume = Math.Min(trade.Volume, counterOrder.Balance);

					var counterCommission = _commissionManager.Process(new ExecutionMessage
					{
						TradePrice = trade.Price,
						TradeVolume = counterVolume,
						Side = counterOrder.Side,
					});

					var (_, _, counterPosition) = counterPortfolio.ProcessTrade(
						regMsg.SecurityId, counterOrder.Side, trade.Price, counterVolume, counterCommission);

					results.Add(new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						SecurityId = regMsg.SecurityId,
						LocalTime = regMsg.LocalTime,
						ServerTime = serverTime,
						OriginalTransactionId = counterOrder.TransactionId,
						TradeId = tradeId,
						TradePrice = trade.Price,
						TradeVolume = counterVolume,
						Side = counterOrder.Side,
						Commission = counterCommission,
					});

					// Send position change for counter order
					results.Add(new PositionChangeMessage
					{
						SecurityId = regMsg.SecurityId,
						ServerTime = serverTime,
						LocalTime = regMsg.LocalTime,
						PortfolioName = counterOrder.PortfolioName,
					}
					.Add(PositionChangeTypes.CurrentValue, counterPosition.CurrentValue)
					.TryAdd(PositionChangeTypes.AveragePrice, counterPosition.AveragePrice));

					// Send portfolio (money) update for counter order
					AddPortfolioUpdate(counterPortfolio, regMsg.LocalTime, results);
				}
			}
		}

		// Send order response
		var finalBalance = matchResult.RemainingVolume;
		var finalState = matchResult.FinalState;

		results.Add(CreateOrderResponse(regMsg, finalState,
			orderId: orderId,
			balance: finalBalance,
			volume: regMsg.Volume));

		// Add to order book if needed
		if (matchResult.ShouldPlaceInBook && finalBalance > 0)
		{
			order.Balance = finalBalance;
			emulator.OrderBook.AddQuote(order);
			emulator.OrderManager.RegisterOrder(order, regMsg.LocalTime);
		}
		else if (finalBalance == 0)
		{
			// Order fully executed - already handled in ProcessTrade
		}

		// Send depth update
		if (emulator.HasDepthSubscription)
		{
			results.Add(emulator.OrderBook.ToMessage(regMsg.LocalTime, serverTime));
		}

		// Send portfolio (money) update
		AddPortfolioUpdate(portfolio, regMsg.LocalTime, results);
	}

	private void ProcessOrderCancel(OrderCancelMessage cancelMsg, List<Message> results)
	{
		var emulator = GetEmulator(cancelMsg.SecurityId);
		var serverTime = cancelMsg.LocalTime;

		if (!emulator.OrderManager.TryRemoveOrder(cancelMsg.OriginalTransactionId, out var order))
		{
			results.Add(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				SecurityId = cancelMsg.SecurityId,
				LocalTime = cancelMsg.LocalTime,
				ServerTime = serverTime,
				OriginalTransactionId = cancelMsg.TransactionId,
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException($"Order {cancelMsg.OriginalTransactionId} not found"),
				HasOrderInfo = true,
			});
			return;
		}

		// Remove from order book
		emulator.OrderBook.RemoveQuote(order.TransactionId, order.Side, order.Price);

		// Unblock funds
		var portfolio = GetPortfolio(order.PortfolioName);
		portfolio.ProcessOrderCancellation(cancelMsg.SecurityId, order.Side, order.Balance, order.Price);

		// Send cancellation confirmation
		results.Add(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = cancelMsg.SecurityId,
			LocalTime = cancelMsg.LocalTime,
			ServerTime = serverTime,
			OriginalTransactionId = cancelMsg.OriginalTransactionId,
			OrderState = OrderStates.Done,
			Balance = order.Balance,
			OrderVolume = order.Volume,
			IsCancellation = true,
			HasOrderInfo = true,
		});

		if (emulator.HasDepthSubscription)
		{
			results.Add(emulator.OrderBook.ToMessage(cancelMsg.LocalTime, serverTime));
		}

		// Send portfolio update after cancellation
		AddPortfolioUpdate(portfolio, cancelMsg.LocalTime, results);
	}

	private void ProcessOrderReplace(OrderReplaceMessage replaceMsg, List<Message> results)
	{
		var emulator = GetEmulator(replaceMsg.SecurityId);
		var serverTime = replaceMsg.LocalTime;

		// Cancel old order
		if (!emulator.OrderManager.TryRemoveOrder(replaceMsg.OriginalTransactionId, out var oldOrder))
		{
			var error = new InvalidOperationException($"Order {replaceMsg.OriginalTransactionId} not found for replace");

			results.Add(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				SecurityId = replaceMsg.SecurityId,
				LocalTime = replaceMsg.LocalTime,
				ServerTime = serverTime,
				OriginalTransactionId = replaceMsg.TransactionId,
				OrderState = OrderStates.Failed,
				IsCancellation = true,
				Error = error,
				HasOrderInfo = true,
			});

			results.Add(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				SecurityId = replaceMsg.SecurityId,
				LocalTime = replaceMsg.LocalTime,
				ServerTime = serverTime,
				OriginalTransactionId = replaceMsg.TransactionId,
				OrderState = OrderStates.Failed,
				Error = error,
				HasOrderInfo = true,
			});
			return;
		}

		emulator.OrderBook.RemoveQuote(oldOrder.TransactionId, oldOrder.Side, oldOrder.Price);

		// Send old order cancellation
		results.Add(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = replaceMsg.SecurityId,
			LocalTime = replaceMsg.LocalTime,
			ServerTime = serverTime,
			OriginalTransactionId = replaceMsg.OriginalTransactionId,
			OrderState = OrderStates.Done,
			Balance = oldOrder.Balance,
			OrderVolume = oldOrder.Volume,
			IsCancellation = true,
			HasOrderInfo = true,
		});

		// Register new order
		var newRegMsg = new OrderRegisterMessage
		{
			SecurityId = replaceMsg.SecurityId,
			LocalTime = replaceMsg.LocalTime,
			TransactionId = replaceMsg.TransactionId,
			Side = replaceMsg.Side,
			Price = replaceMsg.Price,
			Volume = replaceMsg.Volume > 0 ? replaceMsg.Volume : oldOrder.Volume,
			OrderType = replaceMsg.OrderType ?? oldOrder.OrderType ?? OrderTypes.Limit,
			PortfolioName = replaceMsg.PortfolioName ?? oldOrder.PortfolioName,
			TimeInForce = replaceMsg.TimeInForce ?? oldOrder.TimeInForce,
			PostOnly = replaceMsg.PostOnly ?? oldOrder.PostOnly,
			TillDate = replaceMsg.TillDate ?? oldOrder.ExpiryDate?.DateTime,
		};

		ProcessOrderRegister(newRegMsg, results);
	}

	private void ProcessOrderGroupCancel(OrderGroupCancelMessage groupMsg, List<Message> results)
	{
		var mode = groupMsg.Mode;

		if (mode.HasFlag(OrderGroupCancelModes.CancelOrders))
		{
			foreach (var emulator in _securityEmulators.Values)
			{
				if (groupMsg.SecurityId != default && groupMsg.SecurityId != emulator.SecurityId)
					continue;

				var ordersToCancel = emulator.OrderManager
					.GetActiveOrders(groupMsg.PortfolioName, groupMsg.SecurityId == default ? null : groupMsg.SecurityId, groupMsg.Side)
					.ToList();

				foreach (var order in ordersToCancel)
				{
					var cancelMsg = new OrderCancelMessage
					{
						SecurityId = emulator.SecurityId,
						LocalTime = groupMsg.LocalTime,
						TransactionId = TransactionIdGenerator.GetNextId(),
						OriginalTransactionId = order.TransactionId,
						PortfolioName = order.PortfolioName,
					};

					ProcessOrderCancel(cancelMsg, results);
				}
			}
		}

		if (mode.HasFlag(OrderGroupCancelModes.ClosePositions))
		{
			foreach (var kvp in _portfolios)
			{
				if (!string.IsNullOrEmpty(groupMsg.PortfolioName) &&
					!groupMsg.PortfolioName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
					continue;

				var portfolio = kvp.Value;

				foreach (var (securityId, volume, _) in portfolio.GetPositions())
				{
					if (groupMsg.SecurityId != default && groupMsg.SecurityId != securityId)
						continue;

					if (volume == 0)
						continue;

					// Filter by side if specified
					if (groupMsg.Side.HasValue)
					{
						var positionSide = volume > 0 ? Sides.Buy : Sides.Sell;
						if (positionSide != groupMsg.Side.Value)
							continue;
					}

					var closeSide = volume > 0 ? Sides.Sell : Sides.Buy;
					var closeVolume = Math.Abs(volume);

					var emulator = GetEmulator(securityId);
					var bestPrice = closeSide == Sides.Buy
						? emulator.OrderBook.BestAsk?.price
						: emulator.OrderBook.BestBid?.price;

					if (bestPrice.HasValue)
					{
						var closeMsg = new OrderRegisterMessage
						{
							SecurityId = securityId,
							LocalTime = groupMsg.LocalTime,
							TransactionId = TransactionIdGenerator.GetNextId(),
							Side = closeSide,
							Price = bestPrice.Value,
							Volume = closeVolume,
							OrderType = OrderTypes.Limit,
							PortfolioName = kvp.Key,
						};

						ProcessOrderRegister(closeMsg, results);
					}
				}
			}
		}
	}

	private void ProcessOrderStatus(OrderStatusMessage statusMsg, List<Message> results)
	{
		foreach (var emulator in _securityEmulators.Values)
		{
			var orders = string.IsNullOrEmpty(statusMsg.PortfolioName)
				? emulator.OrderManager.GetActiveOrders()
				: emulator.OrderManager.GetActiveOrders(statusMsg.PortfolioName);

			foreach (var order in orders)
			{
				if (statusMsg.OrderId.HasValue && order.TransactionId != statusMsg.OrderId.Value)
					continue;

				results.Add(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					SecurityId = emulator.SecurityId,
					LocalTime = statusMsg.LocalTime,
					ServerTime = order.ServerTime.DateTime,
					OriginalTransactionId = statusMsg.TransactionId,
					TransactionId = order.TransactionId,
					OrderState = OrderStates.Active,
					Balance = order.Balance,
					OrderVolume = order.Volume,
					OrderPrice = order.Price,
					Side = order.Side,
					PortfolioName = order.PortfolioName,
					HasOrderInfo = true,
				});
			}
		}

		results.Add(statusMsg.CreateResult());
	}

	private void ProcessPortfolioLookup(PortfolioLookupMessage lookupMsg, List<Message> results)
	{
		results.Add(lookupMsg.CreateResponse());

		if (!lookupMsg.IsSubscribe)
			return;

		foreach (var kvp in _portfolios)
		{
			if (!string.IsNullOrEmpty(lookupMsg.PortfolioName) &&
				!lookupMsg.PortfolioName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
				continue;

			results.Add(new PortfolioMessage
			{
				PortfolioName = kvp.Key,
				OriginalTransactionId = lookupMsg.TransactionId,
			});

			foreach (var (securityId, volume, avgPrice) in kvp.Value.GetPositions())
			{
				if (volume == 0)
					continue;

				results.Add(new PositionChangeMessage
				{
					SecurityId = securityId,
					ServerTime = lookupMsg.LocalTime,
					LocalTime = lookupMsg.LocalTime,
					PortfolioName = kvp.Key,
				}
				.Add(PositionChangeTypes.CurrentValue, volume)
				.TryAdd(PositionChangeTypes.AveragePrice, avgPrice));
			}
		}

		results.Add(lookupMsg.CreateResult());
	}

	private void ProcessPositionChange(PositionChangeMessage posMsg, List<Message> results)
	{
		var portfolio = GetPortfolio(posMsg.PortfolioName);

		if (posMsg.IsMoney())
		{
			var beginValue = (decimal?)posMsg.Changes.TryGetValue(PositionChangeTypes.BeginValue);
			if (beginValue.HasValue)
				portfolio.SetMoney(beginValue.Value);
		}
		else
		{
			var beginValue = (decimal?)posMsg.Changes.TryGetValue(PositionChangeTypes.BeginValue);
			if (beginValue.HasValue)
				portfolio.SetPosition(posMsg.SecurityId, beginValue.Value);
		}

		results.Add(posMsg.Clone());
	}

	private void ProcessTime(DateTimeOffset time, List<Message> results)
	{
		foreach (var emulator in _securityEmulators.Values)
		{
			// Process expired orders
			var expired = emulator.OrderManager.ProcessTime(time);
			foreach (var order in expired)
			{
				emulator.OrderBook.RemoveQuote(order.TransactionId, order.Side, order.Price);

				results.Add(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					SecurityId = emulator.SecurityId,
					LocalTime = time.DateTime,
					ServerTime = time.DateTime,
					OriginalTransactionId = order.TransactionId,
					OrderState = OrderStates.Done,
					Balance = order.Balance,
					OrderVolume = order.Volume,
					HasOrderInfo = true,
				});
			}
		}
	}

	private void Reset()
	{
		_securityEmulators.Clear();
		_portfolios.Clear();
		ProcessedMessageCount = 0;
	}

	private InvalidOperationException ValidateRegistration(OrderRegisterMessage regMsg)
	{
		// Check portfolio funds if needed
		if (Settings.CheckMoney && _portfolios.TryGetValue(regMsg.PortfolioName, out var portfolio))
		{
			var needMoney = regMsg.Price * regMsg.Volume;
			if (portfolio.AvailableMoney < needMoney)
			{
				return new InvalidOperationException($"Insufficient funds: need {needMoney}, available {portfolio.AvailableMoney}");
			}
		}

		return null;
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

	private void AddPortfolioUpdate(PortfolioEmulator2 portfolio, DateTimeOffset time, List<Message> results)
	{
		var totalPnL = portfolio.RealizedPnL - portfolio.Commission;

		results.Add(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = time.DateTime,
			LocalTime = time.DateTime,
			PortfolioName = portfolio.Name,
		}
		.Add(PositionChangeTypes.RealizedPnL, portfolio.RealizedPnL)
		.Add(PositionChangeTypes.VariationMargin, totalPnL)
		.Add(PositionChangeTypes.CurrentValue, portfolio.AvailableMoney)
		.Add(PositionChangeTypes.BlockedValue, portfolio.BlockedMoney)
		.Add(PositionChangeTypes.Commission, portfolio.Commission));
	}

	#region IMessageAdapter implementation

	IdGenerator IMessageAdapter.TransactionIdGenerator => new IncrementalIdGenerator();

	IEnumerable<MessageTypeInfo> IMessageAdapter.PossibleSupportedMessages { get; } =
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
	];

	IEnumerable<MessageTypes> IMessageAdapter.SupportedInMessages { get; set; }
	IEnumerable<MessageTypes> IMessageAdapter.NotSupportedResultMessages { get; } = [];

	IEnumerable<DataType> IMessageAdapter.GetSupportedMarketDataTypes(SecurityId securityId, DateTime? from, DateTime? to) =>
	[
		DataType.OrderLog,
		DataType.Ticks,
		DataType.CandleTimeFrame,
		DataType.MarketDepth,
	];

	IEnumerable<Level1Fields> IMessageAdapter.CandlesBuildFrom => [];
	bool IMessageAdapter.CheckTimeFrameByRequest => true;
	ReConnectionSettings IMessageAdapter.ReConnectionSettings { get; } = new();
	TimeSpan IMessageAdapter.HeartbeatInterval { get => TimeSpan.Zero; set { } }
	string IMessageAdapter.StorageName => null;
	bool IMessageAdapter.IsNativeIdentifiersPersistable => false;
	bool IMessageAdapter.IsNativeIdentifiers => false;
	bool IMessageAdapter.IsFullCandlesOnly => false;
	bool IMessageAdapter.IsSupportSubscriptions => true;
	bool IMessageAdapter.IsSupportCandlesUpdates(MarketDataMessage subscription) => true;
	bool IMessageAdapter.IsSupportCandlesPriceLevels(MarketDataMessage subscription) => false;
	MessageAdapterCategories IMessageAdapter.Categories => default;
	IEnumerable<(string, Type)> IMessageAdapter.SecurityExtendedFields { get; } = [];
	IEnumerable<int> IMessageAdapter.SupportedOrderBookDepths => throw new NotSupportedException();
	bool IMessageAdapter.IsSupportOrderBookIncrements => false;
	bool IMessageAdapter.IsSupportExecutionsPnL => true;
	bool IMessageAdapter.IsSecurityNewsOnly => false;
	Type IMessageAdapter.OrderConditionType => null;
	bool IMessageAdapter.HeartbeatBeforeConnect => false;
	Uri IMessageAdapter.Icon => null;
	bool IMessageAdapter.IsAutoReplyOnTransactonalUnsubscription => true;
	bool IMessageAdapter.EnqueueSubscriptions { get; set; }
	bool IMessageAdapter.IsSupportTransactionLog => false;
	bool IMessageAdapter.UseInChannel => false;
	bool IMessageAdapter.UseOutChannel => false;
	TimeSpan IMessageAdapter.IterationInterval => default;
	string IMessageAdapter.FeatureName => string.Empty;
	string[] IMessageAdapter.AssociatedBoards => [];
	bool? IMessageAdapter.IsPositionsEmulationRequired => true;
	bool IMessageAdapter.IsReplaceCommandEditCurrent => false;
	TimeSpan? IMessageAdapter.LookupTimeout => null;
	bool IMessageAdapter.ExtraSetup => false;

	IOrderLogMarketDepthBuilder IMessageAdapter.CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		=> new OrderLogMarketDepthBuilder(securityId);

	bool IMessageAdapter.IsAllDownloadingSupported(DataType dataType) => false;
	bool IMessageAdapter.IsSecurityRequired(DataType dataType) => dataType.IsSecurityRequired;

	TimeSpan IMessageAdapter.DisconnectTimeout => default;
	int IMessageAdapter.MaxParallelMessages { get => default; set => throw new NotSupportedException(); }
	TimeSpan IMessageAdapter.FaultDelay { get => default; set => throw new NotSupportedException(); }

	IMessageAdapter ICloneable<IMessageAdapter>.Clone()
		=> new MarketEmulator2(SecurityProvider, PortfolioProvider, ExchangeInfoProvider, TransactionIdGenerator) { VerifyMode = VerifyMode };

	object ICloneable.Clone() => ((ICloneable<IMessageAdapter>)this).Clone();

	void IMessageAdapter.SendOutMessage(Message message)
		=> NewOutMessage?.Invoke(message);

	#endregion
}

/// <summary>
/// Security-specific emulator state.
/// </summary>
internal class SecurityEmulator(MarketEmulator2 parent, SecurityId securityId)
{
	private readonly MarketEmulator2 _parent = parent;
	private SecurityMessage _securityDefinition;
	private long? _depthSubscription;

	public SecurityId SecurityId { get; } = securityId;
	public OrderBook OrderBook { get; } = new(securityId);
	public OrderLifecycleManager OrderManager { get; } = new();

	public decimal PriceStep => _securityDefinition?.PriceStep ?? 0.01m;
	public decimal VolumeStep => _securityDefinition?.VolumeStep ?? 1m;
	public bool HasDepthSubscription => _depthSubscription.HasValue;

	public void ProcessSecurity(SecurityMessage msg)
	{
		_securityDefinition = msg;
	}

	public void ProcessMarketData(MarketDataMessage msg)
	{
		if (msg.IsSubscribe)
		{
			if (msg.DataType2 == DataType.MarketDepth)
				_depthSubscription = msg.TransactionId;
		}
		else
		{
			if (_depthSubscription == msg.OriginalTransactionId)
				_depthSubscription = null;
		}
	}

	public void ProcessQuoteChange(QuoteChangeMessage msg, List<Message> results)
	{
		if (msg.State is null)
		{
			OrderBook.SetSnapshot(msg.Bids, msg.Asks);
		}

		if (_depthSubscription.HasValue)
		{
			results.Add(OrderBook.ToMessage(msg.LocalTime, msg.ServerTime));
		}
	}

	public void ProcessLevel1(Level1ChangeMessage msg, List<Message> results)
	{
		var bidPrice = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestBidPrice);
		var askPrice = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestAskPrice);
		var bidVol = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestBidVolume) ?? 10;
		var askVol = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestAskVolume) ?? 10;

		if (bidPrice.HasValue)
			OrderBook.UpdateLevel(Sides.Buy, bidPrice.Value, bidVol);

		if (askPrice.HasValue)
			OrderBook.UpdateLevel(Sides.Sell, askPrice.Value, askVol);

		if (_depthSubscription.HasValue)
		{
			results.Add(OrderBook.ToMessage(msg.LocalTime, msg.ServerTime));
		}
	}

	public void ProcessTick(ExecutionMessage tick, List<Message> results)
	{
		// Generate order book from tick if empty
		var priceStep = PriceStep;
		var spread = priceStep * _parent.Settings.SpreadSize;

		if (OrderBook.BidLevels == 0 && OrderBook.AskLevels == 0)
		{
			var price = tick.TradePrice ?? 100m;
			var vol = tick.TradeVolume ?? 10m;

			OrderBook.UpdateLevel(Sides.Buy, price - spread, vol);
			OrderBook.UpdateLevel(Sides.Sell, price + spread, vol);
		}

		// Match orders against tick price
		var tradePrice = tick.TradePrice ?? 0;
		if (tradePrice > 0)
		{
			// Update best prices based on tick
			var bestBid = OrderBook.BestBid;
			var bestAsk = OrderBook.BestAsk;

			if (bestBid.HasValue && tradePrice <= bestBid.Value.price)
			{
				// Tick at or below best bid - might execute sell orders
			}

			if (bestAsk.HasValue && tradePrice >= bestAsk.Value.price)
			{
				// Tick at or above best ask - might execute buy orders
			}
		}

		if (_depthSubscription.HasValue)
		{
			results.Add(OrderBook.ToMessage(tick.LocalTime, tick.ServerTime));
		}
	}

	public void ProcessOrderLog(ExecutionMessage ol, List<Message> results)
	{
		if (ol.TradeId is not null)
			return; // Trade, not order

		if (ol.IsCancellation)
		{
			// Remove quote
			OrderBook.UpdateLevel(ol.Side, ol.OrderPrice, 0);
		}
		else
		{
			// Add quote
			var currentVol = OrderBook.GetVolumeAtPrice(ol.Side, ol.OrderPrice);
			OrderBook.UpdateLevel(ol.Side, ol.OrderPrice, currentVol + (ol.OrderVolume ?? 0));
		}

		if (_depthSubscription.HasValue)
		{
			results.Add(OrderBook.ToMessage(ol.LocalTime, ol.ServerTime));
		}
	}

	public void ProcessCandle(CandleMessage candle, List<Message> results)
	{
		// Generate pseudo order book from candle
		var spread = PriceStep * _parent.Settings.SpreadSize;

		OrderBook.UpdateLevel(Sides.Buy, candle.ClosePrice - spread, candle.TotalVolume / 2);
		OrderBook.UpdateLevel(Sides.Sell, candle.ClosePrice + spread, candle.TotalVolume / 2);

		if (_depthSubscription.HasValue)
		{
			results.Add(OrderBook.ToMessage(candle.OpenTime, candle.OpenTime));
		}
	}

	public void ProcessOrderExecution(ExecutionMessage exec, List<Message> results)
	{
		// Internal processing - not typically used directly
	}
}

/// <summary>
/// Simple portfolio emulator.
/// </summary>
internal class PortfolioEmulator2
{
	private readonly Dictionary<SecurityId, PositionInfo> _positions = [];
	private decimal _beginMoney;
	private decimal _currentMoney;
	private decimal _realizedPnL;
	private decimal _totalBlockedMoney;
	private decimal _commission;

	public string Name { get; }
	public decimal BeginMoney => _beginMoney;
	public decimal AvailableMoney => _currentMoney;
	public decimal RealizedPnL => _realizedPnL;
	public decimal BlockedMoney => _totalBlockedMoney;
	public decimal Commission => _commission;

	public PortfolioEmulator2(string name)
	{
		Name = name;
	}

	public void SetMoney(decimal money)
	{
		_beginMoney = money;
		_currentMoney = money;
	}

	public void SetPosition(SecurityId secId, decimal volume, decimal avgPrice = 0)
	{
		var pos = GetOrCreatePosition(secId);
		pos.BeginValue = volume;
		pos.Diff = 0;
		pos.AveragePrice = avgPrice;
	}

	private PositionInfo GetOrCreatePosition(SecurityId secId)
	{
		if (!_positions.TryGetValue(secId, out var pos))
		{
			pos = new PositionInfo(secId);
			_positions[secId] = pos;
		}
		return pos;
	}

	public PositionInfo GetPosition(SecurityId secId)
	{
		return _positions.TryGetValue(secId, out var pos) ? pos : null;
	}

	public (decimal realizedPnL, decimal positionChange, PositionInfo position) ProcessTrade(
		SecurityId secId, Sides side, decimal price, decimal volume, decimal? commission = null)
	{
		var pos = GetOrCreatePosition(secId);

		// Update commission
		if (commission.HasValue)
			_commission += commission.Value;

		// Calculate position change
		var positionDelta = side == Sides.Buy ? volume : -volume;
		var prevPos = pos.CurrentValue;
		var prevAvgPrice = pos.AveragePrice;

		pos.Diff += positionDelta;

		var currPos = pos.CurrentValue;
		var tradeRealizedPnL = 0m;

		// Calculate AveragePrice and RealizedPnL
		if (currPos == 0)
		{
			// Position closed completely
			if (prevPos != 0)
			{
				// Realized PnL = (exit price - entry price) * volume * direction
				tradeRealizedPnL = (price - prevAvgPrice) * Math.Abs(prevPos) * Math.Sign(prevPos);
				_realizedPnL += tradeRealizedPnL;
			}
			pos.AveragePrice = 0;
		}
		else if (prevPos == 0)
		{
			// New position opened
			pos.AveragePrice = price;
		}
		else if (Math.Sign(prevPos) == Math.Sign(currPos))
		{
			// Position increased or partially closed
			if (Math.Abs(currPos) > Math.Abs(prevPos))
			{
				// Position increased - recalculate average price
				pos.AveragePrice = (prevAvgPrice * Math.Abs(prevPos) + price * volume) / Math.Abs(currPos);
			}
			else
			{
				// Position partially closed - realize PnL for closed portion
				var closedVolume = Math.Abs(prevPos) - Math.Abs(currPos);
				tradeRealizedPnL = (price - prevAvgPrice) * closedVolume * Math.Sign(prevPos);
				_realizedPnL += tradeRealizedPnL;
				// Average price remains the same for remaining position
			}
		}
		else
		{
			// Position flipped (was long, now short or vice versa)
			// First close old position completely
			tradeRealizedPnL = (price - prevAvgPrice) * Math.Abs(prevPos) * Math.Sign(prevPos);
			_realizedPnL += tradeRealizedPnL;
			// Then open new position at current price
			pos.AveragePrice = price;
		}

		// Update blocked volume/value for active orders (order was executed)
		var value = volume * price;
		if (side == Sides.Buy)
		{
			pos.TotalBidsVolume -= volume;
			pos.TotalBidsValue -= value;
		}
		else
		{
			pos.TotalAsksVolume -= volume;
			pos.TotalAsksValue -= value;
		}

		UpdateBlockedMoney();

		// Update current money (simple: just track cost)
		var cost = price * volume * (side == Sides.Buy ? -1 : 1);
		_currentMoney += cost;

		return (tradeRealizedPnL, positionDelta, pos);
	}

	public void ProcessOrderRegistration(SecurityId secId, Sides side, decimal volume, decimal price)
	{
		var pos = GetOrCreatePosition(secId);
		var value = volume * price;

		if (side == Sides.Buy)
		{
			pos.TotalBidsVolume += volume;
			pos.TotalBidsValue += value;
		}
		else
		{
			pos.TotalAsksVolume += volume;
			pos.TotalAsksValue += value;
		}

		UpdateBlockedMoney();
	}

	public void ProcessOrderCancellation(SecurityId secId, Sides side, decimal volume, decimal price = 0)
	{
		var pos = GetOrCreatePosition(secId);
		var value = volume * price;

		if (side == Sides.Buy)
		{
			pos.TotalBidsVolume -= volume;
			pos.TotalBidsValue -= value;
		}
		else
		{
			pos.TotalAsksVolume -= volume;
			pos.TotalAsksValue -= value;
		}

		UpdateBlockedMoney();
	}

	public void ProcessTradeExecution(SecurityId secId, Sides side, decimal volume, decimal price)
	{
		var pos = GetOrCreatePosition(secId);
		var value = volume * price;

		// Reduce blocked amount when order is executed
		if (side == Sides.Buy)
		{
			pos.TotalBidsVolume -= volume;
			pos.TotalBidsValue -= value;
		}
		else
		{
			pos.TotalAsksVolume -= volume;
			pos.TotalAsksValue -= value;
		}

		UpdateBlockedMoney();
	}

	private void UpdateBlockedMoney()
	{
		_totalBlockedMoney = 0;
		foreach (var pos in _positions.Values)
		{
			// Blocked = value of all active orders
			_totalBlockedMoney += pos.TotalBidsValue + pos.TotalAsksValue;
		}
	}

	public IEnumerable<(SecurityId securityId, decimal volume, decimal avgPrice)> GetPositions()
	{
		return _positions.Select(kvp => (kvp.Key, kvp.Value.CurrentValue, kvp.Value.AveragePrice));
	}

	public IEnumerable<PositionInfo> GetAllPositions() => _positions.Values;

	internal class PositionInfo(SecurityId securityId)
	{
		public SecurityId SecurityId { get; } = securityId;
		public decimal BeginValue;
		public decimal Diff;
		public decimal CurrentValue => BeginValue + Diff;
		public decimal AveragePrice;
		public decimal TotalBidsVolume;
		public decimal TotalAsksVolume;
		public decimal TotalBidsValue; // volume * price
		public decimal TotalAsksValue; // volume * price
	}
}
