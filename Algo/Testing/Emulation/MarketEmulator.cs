namespace StockSharp.Algo.Testing.Emulation;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.Testing;

/// <summary>
/// Market emulator v2 with modular architecture.
/// </summary>
public class MarketEmulator : BaseLogReceiver, IMarketEmulator
{
	private readonly Dictionary<SecurityId, SecurityEmulator> _securityEmulators = [];
	private readonly Dictionary<string, PortfolioEmulator> _portfolios = [];
	private readonly ICommissionManager _commissionManager = new CommissionManager();
	private DateTime _currentTime;

	private IRandomProvider _randomProvider = new DefaultRandomProvider();
	private IncrementalIdGenerator _orderIdGenerator = new();
	private IncrementalIdGenerator _tradeIdGenerator = new();

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
		get => _orderIdGenerator;
		set => _orderIdGenerator = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Trade ID generator.
	/// </summary>
	public IncrementalIdGenerator TradeIdGenerator
	{
		get => _tradeIdGenerator;
		set => _tradeIdGenerator = value ?? throw new ArgumentNullException(nameof(value));
	}

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

	private PortfolioEmulator GetPortfolio(string name)
	{
		if (!_portfolios.TryGetValue(name, out var portfolio))
		{
			portfolio = new PortfolioEmulator(name);
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

		// Process expired orders on every message (like V1)
		ProcessTime(message.LocalTime, results);
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
				// Convert ExecutionMessage to OrderRegisterMessage and process
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
		var portfolio = GetPortfolio(regMsg.PortfolioName);
		var serverTime = regMsg.LocalTime;

		// Validate registration
		var error = ValidateRegistration(regMsg);
		if (error != null)
		{
			results.Add(CreateOrderResponse(regMsg, OrderStates.Failed, error: error));
			return;
		}

		// [0] Create order response first (like V1 - will be mutated later)
		var replyMsg = new ExecutionMessage
		{
			HasOrderInfo = true,
			DataTypeEx = DataType.Transactions,
			ServerTime = serverTime,
			LocalTime = regMsg.LocalTime,
			OriginalTransactionId = regMsg.TransactionId,
		};
		results.Add(replyMsg);

		// Track order registration for blocked funds
		// Use market price (bid for buy, ask for sell) like V1 GetMarginPrice
		// V1 returns 0 if no quotes available on that side (not order price)
		// In candle mode, V1 doesn't update order book so margin is always 0
		var marginPrice = emulator.IsCandleMatchingMode
			? 0
			: regMsg.Side == Sides.Buy
				? emulator.OrderBook.BestBid?.price ?? 0
				: emulator.OrderBook.BestAsk?.price ?? 0;

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
			MarginPrice = marginPrice,
		};

		// Assign order ID
		var orderId = OrderIdGenerator.GetNextId();
		order.OrderId = orderId;

		// Check PostOnly BEFORE blocking funds (like V1's early return in AcceptExecution)
		var matcher = new OrderMatcher();
		if (order.PostOnly && matcher.WouldCross(order, emulator.OrderBook))
		{
			// Like V1 - just set Done state, no Error, no portfolio update
			replyMsg.Balance = regMsg.Volume;
			replyMsg.OrderVolume = regMsg.Volume;
			replyMsg.OrderState = OrderStates.Done;
			return;
		}

		portfolio.ProcessOrderRegistration(regMsg.SecurityId, regMsg.Side, regMsg.Volume, marginPrice);

		// [1] Portfolio change message after order registration (like V1 ProcessOrder -> AddPortfolioChangeMessage)
		AddPortfolioUpdate(portfolio, regMsg.LocalTime, results);

		// Match order - use candle matching if candle subscription is active and candle is available
		MatchResult matchResult;
		var candle = emulator.HasCandleSubscription ? emulator.GetLastStoredCandle() : null;

		if (candle is not null)
		{
			// Match against candle (like V1's MatchOrderByCandle)
			matchResult = MatchOrderByCandle(order, candle);
		}
		else
		{
			// Match against order book
			var matchSettings = new MatchingSettings
			{
				PriceStep = emulator.PriceStep,
				VolumeStep = emulator.VolumeStep,
				UseOrderPriceForLimitTrades = emulator.IsCandleMatchingMode,
			};

			matchResult = matcher.Match(order, emulator.OrderBook, matchSettings);
		}

		// Process result (for non-PostOnly rejections like FOK)
		if (matchResult.IsRejected)
		{
			// Unblock funds on rejection
			portfolio.ProcessOrderCancellation(regMsg.SecurityId, regMsg.Side, regMsg.Volume, marginPrice);

			replyMsg.OrderState = OrderStates.Done;
			replyMsg.Balance = regMsg.Volume;
			replyMsg.OrderVolume = regMsg.Volume;
			replyMsg.Error = new InvalidOperationException(matchResult.RejectionReason);
			return;
		}

		// Handle IOC/FOK order state messages (like V1's MatchOrderPostProcess - sends Done before trades)
		var isIOC = regMsg.TimeInForce == TimeInForce.CancelBalance;
		var isFOK = regMsg.TimeInForce == TimeInForce.MatchOrCancel;
		var hasTrades = matchResult.Trades.Count > 0;

		// For IOC/FOK with trades, send Done message BEFORE trades (like V1)
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

		// Generate trades (like V1 ProcessTrade - no order state message in trade loop for IOC/FOK)
		foreach (var trade in matchResult.Trades)
		{
			var tradeId = TradeIdGenerator.GetNextId();

			// Calculate commission for trade
			var commission = _commissionManager.Process(new ExecutionMessage
			{
				TradePrice = trade.Price,
				TradeVolume = trade.Volume,
				Side = regMsg.Side,
			});

			// Update portfolio and get position info
			var (realizedPnL, positionChange, position) = portfolio.ProcessTrade(
				regMsg.SecurityId, regMsg.Side, trade.Price, trade.Volume, commission);

			// For non-IOC/FOK orders: send order state update in trade loop (like V1 PutInQueue)
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

			// Trade message (like V1 ProcessTrade)
			results.Add(new ExecutionMessage
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
			});

			// Position change (like V1 ProcessTrade -> result.Add PositionChangeMessage)
			results.Add(new PositionChangeMessage
			{
				SecurityId = SecurityId.Money,
				ServerTime = serverTime,
				LocalTime = regMsg.LocalTime,
				PortfolioName = regMsg.PortfolioName,
			}
			.Add(PositionChangeTypes.CurrentValue, position.CurrentValue)
			.TryAdd(PositionChangeTypes.AveragePrice, position.AveragePrice));

			// Portfolio money update (like V1 ProcessTrade -> AddPortfolioChangeMessage)
			AddPortfolioUpdate(portfolio, regMsg.LocalTime, results);

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

					results.Add(new PositionChangeMessage
					{
						SecurityId = SecurityId.Money,
						ServerTime = serverTime,
						LocalTime = regMsg.LocalTime,
						PortfolioName = counterOrder.PortfolioName,
					}
					.Add(PositionChangeTypes.CurrentValue, counterPosition.CurrentValue)
					.TryAdd(PositionChangeTypes.AveragePrice, counterPosition.AveragePrice));

					AddPortfolioUpdate(counterPortfolio, regMsg.LocalTime, results);
				}
			}
		}

		// Handle IOC/FOK cancelled portion (like V1's IsCanceled block in AcceptExecution)
		var isCancelled = matchResult.FinalState == OrderStates.Done && matchResult.RemainingVolume > 0;

		if ((isIOC || isFOK) && isCancelled)
		{
			// For no trades case, need to send Done message here (Done was only sent before trades if hasTrades)
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

			// Unblock funds for cancelled portion
			portfolio.ProcessOrderCancellation(regMsg.SecurityId, regMsg.Side, matchResult.RemainingVolume, marginPrice);

			// Add portfolio update for cancelled balance (like V1 IsCanceled block)
			AddPortfolioUpdate(portfolio, regMsg.LocalTime, results);
		}
		// Update the initial reply message with final state if no trades and not IOC/FOK
		else if (matchResult.Trades.Count == 0)
		{
			replyMsg.OrderId = orderId;
			replyMsg.OrderState = matchResult.FinalState;
			// Only set Balance/OrderVolume for non-active orders (like V1)
			if (matchResult.FinalState != OrderStates.Active)
			{
				replyMsg.Balance = matchResult.RemainingVolume;
				replyMsg.OrderVolume = regMsg.Volume;
			}
		}

		// Add to order book if needed (check expiry date like V1's AddActiveOrder)
		if (matchResult.ShouldPlaceInBook && matchResult.RemainingVolume > 0)
		{
			// Check expiry date (like V1's AddActiveOrder - returns false if expired)
			if (regMsg.TillDate.HasValue && regMsg.TillDate.Value <= regMsg.LocalTime)
			{
				// Order expired - don't add to book, just set Done
				// V1 doesn't unblock portfolio here - it just returns
				replyMsg.OrderState = OrderStates.Done;
				replyMsg.Balance = matchResult.RemainingVolume;
				replyMsg.OrderVolume = regMsg.Volume;
			}
			else
			{
				order.Balance = matchResult.RemainingVolume;
				emulator.OrderBook.AddQuote(order);
				emulator.OrderManager.RegisterOrder(order, regMsg.LocalTime);
			}
		}

		// Send depth update
		if (emulator.HasDepthSubscription)
		{
			results.Add(emulator.OrderBook.ToMessage(regMsg.LocalTime, serverTime));
		}
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

		// Unblock funds (use MarginPrice that was used for blocking)
		var portfolio = GetPortfolio(order.PortfolioName);
		portfolio.ProcessOrderCancellation(cancelMsg.SecurityId, order.Side, order.Balance, order.MarginPrice);

		// Send cancellation confirmation (like V1 - no SecurityId, no IsCancellation flag)
		results.Add(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			LocalTime = cancelMsg.LocalTime,
			ServerTime = serverTime,
			OriginalTransactionId = cancelMsg.OriginalTransactionId,
			OrderState = OrderStates.Done,
			Balance = order.Balance,
			OrderVolume = order.Volume,
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

		// Unblock funds for old order (like V1's ProcessOrder for cancellation)
		var portfolio = GetPortfolio(oldOrder.PortfolioName);
		portfolio.ProcessOrderCancellation(replaceMsg.SecurityId, oldOrder.Side, oldOrder.Balance, oldOrder.MarginPrice);

		// Send old order cancellation (like V1's CreateReply - no SecurityId, no IsCancellation)
		results.Add(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			LocalTime = replaceMsg.LocalTime,
			ServerTime = serverTime,
			OriginalTransactionId = replaceMsg.OriginalTransactionId,
			OrderState = OrderStates.Done,
			Balance = oldOrder.Balance,
			OrderVolume = oldOrder.Volume,
			HasOrderInfo = true,
		});

		// Portfolio update for cancellation (like V1's ProcessOrder -> AddPortfolioChangeMessage)
		AddPortfolioUpdate(portfolio, replaceMsg.LocalTime, results);

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
			TillDate = replaceMsg.TillDate ?? oldOrder.ExpiryDate,
		};

		ProcessOrderRegister(newRegMsg, results);
	}

	private void ProcessOrderGroupCancel(OrderGroupCancelMessage groupMsg, List<Message> results)
	{
		var mode = groupMsg.Mode;

		// V1 requires PortfolioName for ClosePositions mode
		if (mode.HasFlag(OrderGroupCancelModes.ClosePositions) && string.IsNullOrEmpty(groupMsg.PortfolioName))
		{
			results.Add(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = groupMsg.TransactionId,
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException($"{nameof(OrderGroupCancelMessage)}: PortfolioName is required for ClosePositions mode"),
				LocalTime = groupMsg.LocalTime,
				ServerTime = groupMsg.LocalTime,
				HasOrderInfo = true,
			});
			return;
		}

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
		// Like V1: add SubscriptionResponseMessage first
		results.Add(statusMsg.CreateResponse());

		if (!statusMsg.IsSubscribe)
			return;

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
					ServerTime = order.ServerTime,
					OriginalTransactionId = statusMsg.TransactionId,
					TransactionId = order.TransactionId,
					OrderState = OrderStates.Active,
					Balance = order.Balance,
					OrderVolume = order.Volume,
					OrderPrice = order.Price,
					Side = order.Side,
					PortfolioName = order.PortfolioName,
					OrderType = order.OrderType ?? OrderTypes.Limit,
					HasOrderInfo = true,
				});
			}
		}

		// Like V1: add SubscriptionOnlineMessage at the end
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

	private void ProcessTime(DateTime time, List<Message> results)
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

				// V1 doesn't unblock portfolio or output PositionChangeMessage for expired orders
				// It just outputs ExecutionMessage. This appears to be a bug in V1 (portfolio remains blocked),
				// but we replicate it for compatibility.
			}

			// Process stored candles (like V1's ProcessCandles)
			emulator.ProcessStoredCandles(time, results);
		}
	}

	/// <summary>
	/// Match order against candle (like V1's MatchOrderByCandle).
	/// </summary>
	private static MatchResult MatchOrderByCandle(EmulatorOrder order, CandleMessage candle)
	{
		var balance = order.Balance;

		// Calculate left balance after matching against candle volume
		// If candle has no volume info, assume candle's volume is much more than order's balance
		var leftBalance = candle.TotalVolume == 0
			? 0
			: Math.Max(0, balance - candle.TotalVolume);

		// FOK check - if can't fill entirely, don't execute
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
			// For market orders, use middle price (like V1 default CandlePrice.Middle)
			execPrice = (candle.HighPrice + candle.LowPrice) / 2;

			// Price step is wrong, so adjust by candle boundaries
			if (execPrice > candle.HighPrice || execPrice < candle.LowPrice)
				execPrice = candle.ClosePrice;
		}
		else
		{
			// For limit orders, check if price is within candle range
			if (order.Price > candle.HighPrice || order.Price < candle.LowPrice)
			{
				// Order price is outside candle range - no match, go to book as Active
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

			// Use order price for execution (like V1)
			execPrice = order.Price;
		}

		// Create trade
		var tradeVolume = balance - leftBalance;
		var trades = new List<MatchTrade>
		{
			new(execPrice, tradeVolume, order.Side, [])
		};

		// Determine final state
		var isFullyMatched = leftBalance <= 0;
		var finalState = isFullyMatched ? OrderStates.Done : OrderStates.Active;
		var shouldPlaceInBook = !isFullyMatched && order.TimeInForce != TimeInForce.CancelBalance;

		// IOC (CancelBalance) orders are always Done
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

	private static void AddPortfolioUpdate(PortfolioEmulator portfolio, DateTime time, List<Message> results)
	{
		var totalPnL = portfolio.RealizedPnL - portfolio.Commission;

		results.Add(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = time,
			LocalTime = time,
			PortfolioName = portfolio.Name,
		}
		.Add(PositionChangeTypes.RealizedPnL, portfolio.RealizedPnL)
		.TryAdd(PositionChangeTypes.UnrealizedPnL, 0m, true)
		.Add(PositionChangeTypes.VariationMargin, totalPnL)
		.Add(PositionChangeTypes.CurrentValue, portfolio.CurrentMoney)
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
		=> new MarketEmulator(SecurityProvider, PortfolioProvider, ExchangeInfoProvider, TransactionIdGenerator) { VerifyMode = VerifyMode };

	object ICloneable.Clone() => ((ICloneable<IMessageAdapter>)this).Clone();

	void IMessageAdapter.SendOutMessage(Message message)
		=> NewOutMessage?.Invoke(message);

	#endregion
}

/// <summary>
/// Security-specific emulator state.
/// </summary>
internal class SecurityEmulator(MarketEmulator parent, SecurityId securityId)
{
	private readonly MarketEmulator _parent = parent;
	private SecurityMessage _securityDefinition;
	private long? _depthSubscription;
	private long? _candlesSubscription;
	private bool _candlesNonFinished;
	private readonly SortedDictionary<DateTime, List<CandleMessage>> _storedCandles = [];

	public SecurityId SecurityId { get; } = securityId;
	public OrderBook OrderBook { get; } = new(securityId);
	public OrderLifecycleManager OrderManager { get; } = new();

	private bool _priceStepUpdated;
	private bool _volumeStepUpdated;
	private DateTime _lastPriceLimitDate;

	public decimal PriceStep => _securityDefinition?.PriceStep ?? 0.01m;
	public decimal VolumeStep => _securityDefinition?.VolumeStep ?? 1m;
	public bool HasDepthSubscription => _depthSubscription.HasValue;

	/// <summary>
	/// True when order book was populated from candle data.
	/// In this mode, limit orders should use order price for trades (like V1's MatchOrderByCandle).
	/// </summary>
	public bool IsCandleMatchingMode { get; private set; }

	public void ProcessSecurity(SecurityMessage msg)
	{
		_securityDefinition = msg;
	}

	private void UpdateSteps(decimal price, decimal? volume)
	{
		// Auto-detect price/volume step from market data (like V1)
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
		// V1's UpdatePriceLimits - called once per day to output Level1ChangeMessage with price limits
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
		return Math.Round(price / step) * step;
	}

	public void ProcessMarketData(MarketDataMessage msg)
	{
		if (msg.IsSubscribe)
		{
			if (msg.DataType2 == DataType.MarketDepth)
				_depthSubscription = msg.TransactionId;
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
			else if (_candlesSubscription == msg.OriginalTransactionId)
			{
				_candlesSubscription = null;
				_candlesNonFinished = false;
			}
		}
	}

	public void ProcessQuoteChange(QuoteChangeMessage msg, List<Message> results)
	{
		// Exit candle matching mode when receiving real quotes
		IsCandleMatchingMode = false;

		// Update steps from quote data (like V1)
		if (!_priceStepUpdated || !_volumeStepUpdated)
		{
			var quote = msg.GetBestBid() ?? msg.GetBestAsk();
			if (quote != null)
				UpdateSteps(quote.Value.Price, quote.Value.Volume);
		}

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
		// Exit candle matching mode when receiving real Level1
		IsCandleMatchingMode = false;

		var bidPrice = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestBidPrice);
		var askPrice = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestAskPrice);
		var bidVol = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestBidVolume) ?? 10;
		var askVol = (decimal?)msg.Changes.TryGetValue(Level1Fields.BestAskVolume) ?? 10;

		// Update steps from Level1 data (like V1)
		UpdateSteps(bidPrice ?? askPrice ?? 0, bidVol);

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
		// Exit candle matching mode when receiving real ticks
		IsCandleMatchingMode = false;

		var tradePrice = tick.TradePrice ?? 0;
		var tradeVolume = tick.TradeVolume ?? 10m;

		// Update steps from tick data (like V1's UpdateSteps)
		UpdateSteps(tradePrice, tradeVolume);

		// Update price limits (like V1's UpdatePriceLimits - once per day)
		if (tradePrice > 0)
			UpdatePriceLimits(tradePrice, tick.LocalTime, tick.ServerTime, results);

		var priceStep = PriceStep;
		var spread = priceStep * _parent.Settings.SpreadSize;

		if (tradePrice <= 0)
		{
			if (_depthSubscription.HasValue)
				results.Add(OrderBook.ToMessage(tick.LocalTime, tick.ServerTime));
			return;
		}

		var bestBid = OrderBook.BestBid;
		var bestAsk = OrderBook.BestAsk;

		// Determine tick origin side (like V1's GetOrderSide)
		Sides originSide;
		if (tick.OriginSide.HasValue)
			originSide = tick.OriginSide.Value.Invert();
		else if (bestBid.HasValue && !bestAsk.HasValue)
			originSide = Sides.Sell;
		else if (bestAsk.HasValue && !bestBid.HasValue)
			originSide = Sides.Buy;
		else
			originSide = Sides.Sell; // default: tick was from a sell order

		if (OrderBook.BidLevels == 0 && OrderBook.AskLevels == 0)
		{
			// Empty book: create quote at tick price and opposite at tick ± spread
			OrderBook.UpdateLevel(originSide, tradePrice, tradeVolume);
			var oppositePrice = tradePrice + spread * (originSide == Sides.Buy ? 1 : -1);
			if (oppositePrice > 0)
				OrderBook.UpdateLevel(originSide.Invert(), oppositePrice, tradeVolume);
		}
		else
		{
			// Non-empty book: update levels based on tick
			if (bestBid.HasValue && tradePrice <= bestBid.Value.price)
			{
				// Tick at or below best bid - update/create sell level
				OrderBook.UpdateLevel(Sides.Sell, tradePrice, tradeVolume);
			}
			else if (bestAsk.HasValue && tradePrice >= bestAsk.Value.price)
			{
				// Tick at or above best ask - update/create buy level
				OrderBook.UpdateLevel(Sides.Buy, tradePrice, tradeVolume);
			}
			else
			{
				// Tick in spread or outside - create level at tick price
				OrderBook.UpdateLevel(originSide, tradePrice, tradeVolume);
			}
		}

		if (_depthSubscription.HasValue)
		{
			results.Add(OrderBook.ToMessage(tick.LocalTime, tick.ServerTime));
		}
	}

	public void ProcessOrderLog(ExecutionMessage ol, List<Message> results)
	{
		// Exit candle matching mode when receiving real order log
		IsCandleMatchingMode = false;

		if (ol.TradeId is not null)
			return; // Trade, not order

		// Update steps from order log data (like V1)
		UpdateSteps(ol.OrderPrice, ol.OrderVolume);

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

	/// <summary>
	/// True if candle subscription is active (for candle-based matching).
	/// </summary>
	public bool HasCandleSubscription => _candlesSubscription.HasValue;

	/// <summary>
	/// Get the last stored candle for order matching (like V1's _candleInfo.LastOrDefault()).
	/// </summary>
	public CandleMessage GetLastStoredCandle()
	{
		if (_storedCandles.Count == 0)
			return null;

		var lastPair = _storedCandles.Last();
		return lastPair.Value.FirstOrDefault();
	}

	public void ProcessCandle(CandleMessage candle, List<Message> results)
	{
		// Update steps from candle data (like V1)
		UpdateSteps(candle.ClosePrice, candle.TotalVolume);

		// Store candle for later processing (like V1's _candleInfo)
		// V1 processes candles during ProcessTime, not immediately
		var candles = _storedCandles.SafeAdd(candle.OpenTime, key => []);
		candles.Add(candle);
	}

	/// <summary>
	/// Process stored candles for output and order matching (like V1's ProcessCandles).
	/// Called during ProcessTime.
	/// </summary>
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

			// Output intermediate candle states if _candlesNonFinished
			if (_candlesNonFinished)
			{
				foreach (var candle in pair.Value)
				{
					// Open state (Active)
					var openState = candle.TypedClone();
					openState.State = CandleStates.Active;
					openState.HighPrice = openState.LowPrice = openState.ClosePrice = openState.OpenPrice;
					if (candle.OpenTime != default)
						openState.LocalTime = candle.OpenTime;
					results.Add(openState);

					// High state
					var highState = openState.TypedClone();
					highState.HighPrice = candle.HighPrice;
					if (candle.HighTime != default)
						highState.LocalTime = candle.HighTime;
					results.Add(highState);

					// Low state
					var lowState = openState.TypedClone();
					lowState.HighPrice = candle.HighPrice;
					if (candle.LowTime != default)
						lowState.LocalTime = candle.LowTime;
					results.Add(lowState);
				}
			}

			// Output TimeMessage before final candle (like V1)
			results.Add(new TimeMessage { LocalTime = currentTime });

			// Output final candle and match orders
			foreach (var candle in pair.Value)
			{
				var finalCandle = candle.TypedClone();
				finalCandle.LocalTime = currentTime;
				results.Add(finalCandle);

				// Update order book from candle for matching
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
		// Enter candle matching mode (like V1's MatchOrderByCandle - uses order price for trades)
		IsCandleMatchingMode = true;

		// Generate pseudo order book from candle (like V1's MatchOrderByCandle)
		// Create quotes at Low/High to allow matching within candle range
		var vol = candle.TotalVolume > 0 ? candle.TotalVolume / 2 : 10m;

		// Ask at LowPrice - allows Buy orders at Low or above to match
		OrderBook.UpdateLevel(Sides.Sell, candle.LowPrice, vol);
		// Bid at HighPrice - allows Sell orders at High or below to match
		OrderBook.UpdateLevel(Sides.Buy, candle.HighPrice, vol);

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
internal class PortfolioEmulator(string name)
{
	private readonly Dictionary<SecurityId, PositionInfo> _positions = [];
	private decimal _beginMoney;
	private decimal _realizedPnL;
	private decimal _totalBlockedMoney;
	private decimal _commission;

	public string Name { get; } = name;
	public decimal BeginMoney => _beginMoney;
	/// <summary>
	/// Current money = begin money + total PnL (like V1).
	/// </summary>
	public decimal CurrentMoney => _beginMoney + TotalPnL;
	public decimal AvailableMoney => CurrentMoney - _totalBlockedMoney;
	public decimal RealizedPnL => _realizedPnL;
	public decimal TotalPnL => _realizedPnL - _commission;
	public decimal BlockedMoney => _totalBlockedMoney;
	public decimal Commission => _commission;

	public void SetMoney(decimal money)
	{
		_beginMoney = money;
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
		// Use the average blocked price, not the trade price, to properly unblock
		if (side == Sides.Buy)
		{
			var avgBlockedPrice = pos.TotalBidsVolume > 0 ? pos.TotalBidsValue / pos.TotalBidsVolume : price;
			var blockedValue = volume * avgBlockedPrice;
			pos.TotalBidsVolume -= volume;
			pos.TotalBidsValue -= blockedValue;
		}
		else
		{
			var avgBlockedPrice = pos.TotalAsksVolume > 0 ? pos.TotalAsksValue / pos.TotalAsksVolume : price;
			var blockedValue = volume * avgBlockedPrice;
			pos.TotalAsksVolume -= volume;
			pos.TotalAsksValue -= blockedValue;
		}

		UpdateBlockedMoney();

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
			// V1's TotalPrice logic:
			// - If no position: blocked = buys + sells
			// - If long position: blocked = max(position + buys, sells)
			// - If short position: blocked = max(position + sells, buys)
			var positionValue = Math.Abs(pos.CurrentValue) * pos.AveragePrice;
			var buyOrderValue = pos.TotalBidsValue;
			var sellOrderValue = pos.TotalAsksValue;

			decimal blocked;
			if (positionValue == 0)
			{
				blocked = buyOrderValue + sellOrderValue;
			}
			else if (pos.CurrentValue > 0)
			{
				// Long position: max(position + buys, sells)
				blocked = Math.Max(positionValue + buyOrderValue, sellOrderValue);
			}
			else
			{
				// Short position: max(position + sells, buys)
				blocked = Math.Max(positionValue + sellOrderValue, buyOrderValue);
			}

			_totalBlockedMoney += blocked;
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
