namespace StockSharp.MatchingEngine;

using System.Threading.Tasks;

using StockSharp.Localization;

/// <summary>
/// Message-based matching engine adapter.
/// Contains pure matching logic without emulation (no random, no candle matching, no commissions).
/// Implements <see cref="IMessageTransport"/> for message-based integration.
/// </summary>
public class MatchingEngineAdapter : IMessageTransport
{
	private readonly Dictionary<SecurityId, SecurityState> _securityStates = [];
	private readonly EmulatedPortfolioManager _portfolioManager = new();
	private readonly IStopOrderManager _stopOrderManager = new StopOrderManager();

	private IncrementalIdGenerator _orderIdGenerator = new();
	private IncrementalIdGenerator _tradeIdGenerator = new();

	private DateTime _currentTime;
	private DateTime _lastInputTime;

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	public MatchingEngineAdapter()
	{
		_portfolioManager.MarginController = new MarginController();

		// Open positions are revalued against the engine's own order books.
		_portfolioManager.MarkPrices = new BookMarkPrices(this);
	}

	/// <summary>
	/// Matching engine settings.
	/// </summary>
	public MatchingEngineSettings Settings { get; } = new();

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
	/// Portfolio manager for handling portfolio state.
	/// </summary>
	public virtual EmulatedPortfolioManager PortfolioManager => _portfolioManager;

	/// <summary>
	/// Forgets the book stated so far for <paramref name="securityId"/>, so an incremental feed has
	/// to state a whole one again before its increments are folded in. Used when a venue has gone
	/// quiet on depth: what it sends after the break describes a market its old base no longer holds.
	/// </summary>
	/// <param name="securityId">Security whose stated book is forgotten.</param>
	public void ForgetBook(SecurityId securityId)
	{
		if (_securityStates.TryGetValue(securityId, out var state))
			state.ForgetBook();
	}

	/// <summary>
	/// Stop order manager (read-only access).
	/// </summary>
	public IStopOrderManager StopOrderManager => _stopOrderManager;

	/// <summary>
	/// Transaction id generator for internal use (group cancel, close positions).
	/// </summary>
	public IdGenerator TransactionIdGenerator { get; set; } = new IncrementalIdGenerator();

	/// <inheritdoc />
	public event Func<Message, CancellationToken, ValueTask> NewOutMessageAsync;

	/// <summary>
	/// Get or create security state for the given security.
	/// </summary>
	public SecurityState GetSecurityState(SecurityId securityId)
	{
		securityId.GetHashCode(); // force caching

		if (!_securityStates.TryGetValue(securityId, out var state))
		{
			state = new SecurityState(securityId);
			_securityStates[securityId] = state;
		}

		return state;
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
				throw new InvalidOperationException($"Message {message.Type} time {message.LocalTime:O} is less than current engine time {_lastInputTime:O}");

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

		foreach (var msg in results)
		{
			_currentTime = msg.LocalTime;
			await SendOutMessageAsync(msg, cancellationToken);
		}
	}

	/// <summary>
	/// Process a message and collect results. Can be called directly (without async dispatch).
	/// </summary>
	public void ProcessMessage(Message message, List<Message> results)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				Reset(results);
				break;

			case MessageTypes.Time:
				ProcessTime(message.LocalTime, results);
				break;

			case MessageTypes.QuoteChange:
				var quoteMsg = (QuoteChangeMessage)message;
				GetSecurityState(quoteMsg.SecurityId).ProcessQuoteChange(quoteMsg, results);
				break;

			case MessageTypes.Level1Change:
			{
				var l1Msg = (Level1ChangeMessage)message;
				ProcessLevel1(l1Msg, results);
				if (l1Msg.TryGetDecimal(Level1Fields.LastTradePrice) is { } l1LastPrice)
					CheckStopOrders(l1Msg.SecurityId, l1LastPrice, l1Msg.LocalTime, results);
				break;
			}

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
				GetSecurityState(secMsg.SecurityId).ProcessSecurity(secMsg);
				break;

			case MessageTypes.PositionChange:
				ProcessPositionChange((PositionChangeMessage)message, results);
				break;

			case MessageTypes.MarketData:
				var mdMsg = (MarketDataMessage)message;
				if (!mdMsg.SecurityId.IsAllSecurity())
					GetSecurityState(mdMsg.SecurityId).ProcessMarketData(mdMsg);
				break;
		}

		// Process time-based events (expired orders)
		ProcessTime(message.LocalTime, results);
	}

	private void ProcessLevel1(Level1ChangeMessage msg, List<Message> results)
	{
		var state = GetSecurityState(msg.SecurityId);

		// A quote says where the market is and nothing about what rests behind it, so the only thing
		// the engine takes from one is whether the instrument may be traded at all.
		if (msg.Changes.TryGetValue(Level1Fields.State) is SecurityStates tradingState)
			state.ProcessTradingState(tradingState);
	}

	// Tells an order the engine is being told about from one it is being asked to place. Only the
	// side that already holds it can say all three: that it is active, the identity it is known by,
	// and how much of it is left.
	private static bool IsAlreadyActive(ExecutionMessage execMsg)
		=> execMsg.OrderState == OrderStates.Active
			&& AdoptedTransactionId(execMsg) != 0
			&& execMsg.Balance is > 0;

	// Which field carries the registration id depends on what the report is. The row that
	// registered the order carries it as its own transaction; a later state report carries it as
	// the transaction it answers. Both describe the same order, and reading only one of them would
	// leave the other quietly treated as a fresh request.
	private static long AdoptedTransactionId(ExecutionMessage execMsg)
		=> execMsg.OriginalTransactionId != 0 ? execMsg.OriginalTransactionId : execMsg.TransactionId;

	/// <summary>
	/// Takes an order that is already standing somewhere else into this book, exactly as it is.
	/// </summary>
	/// <remarks>
	/// Nothing is reported back: the order was not registered here, so there is no request to
	/// answer, and an owner that receives a registration report for an order it has held for hours
	/// would reasonably treat it as a new one. Margin is reserved for what is left of it, because
	/// the account is carrying that exposure whether or not this process was running when it was
	/// placed. An identity already in the book is left alone rather than duplicated - being told
	/// twice about the same order is ordinary, and the second telling must change nothing.
	/// </remarks>
	private void AdoptActiveOrder(ExecutionMessage execMsg, Sides side, List<Message> results)
	{
		var state = GetSecurityState(execMsg.SecurityId);

		var transactionId = AdoptedTransactionId(execMsg);

		if (state.OrderManager.TryGetOrder(transactionId, out _))
			return;

		var order = new EmulatorOrder
		{
			TransactionId = transactionId,
			OrderId = execMsg.OrderId,
			Side = side,
			Price = execMsg.OrderPrice,
			Volume = execMsg.OrderVolume ?? execMsg.Balance.Value,
			Balance = execMsg.Balance.Value,
			PortfolioName = execMsg.PortfolioName,
			TimeInForce = execMsg.TimeInForce,
			OrderType = execMsg.OrderType ?? OrderTypes.Limit,
			ExpiryDate = execMsg.ExpiryDate,
		};

		state.OrderBook.AddQuote(order);
		state.OrderManager.RegisterOrder(order, execMsg.LocalTime);

		if (!execMsg.PortfolioName.IsEmpty())
		{
			var portfolio = GetPortfolio(execMsg.PortfolioName);
			portfolio.ProcessOrderRegistration(execMsg.SecurityId, order.Side, order.Balance, order.Price);
			AddPortfolioUpdate(portfolio, execMsg.LocalTime, results);
		}

		if (state.HasDepthSubscription)
			results.Add(state.OrderBook.ToMessage(execMsg.LocalTime, execMsg.ServerTime));
	}

	private void ProcessExecution(ExecutionMessage execMsg, List<Message> results)
	{
		if (execMsg.DataType == DataType.Transactions && execMsg.HasOrderInfo())
		{
			// An order that is already live elsewhere arrives as state, not as a request. Putting
			// it through registration would charge the account a second time for something it
			// already owns, hand it a new identity, and cross it against the book on the way in -
			// the engine would then be holding an order that does not exist anywhere else.
			// Everything below treats the message as an order, and an order naming no side can
			// neither join a book that has two halves nor be placed into one. Nothing downstream
			// can pick one, and picking one here would place an order nobody asked for.
			if (execMsg.Side is not Sides side)
			{
				var fail = execMsg.TransactionId.CreateOrderReply(execMsg.LocalTime);

				fail.SecurityId = execMsg.SecurityId;
				fail.PortfolioName = execMsg.PortfolioName;
				fail.OrderState = OrderStates.Failed;
				fail.Error = new InvalidOperationException($"Order {execMsg.TransactionId}: {nameof(execMsg.Side)} is not specified.");

				results.Add(fail);
				return;
			}

			if (IsAlreadyActive(execMsg))
			{
				AdoptActiveOrder(execMsg, side, results);
				return;
			}

			var regMsg = new OrderRegisterMessage
			{
				TransactionId = execMsg.TransactionId,
				SecurityId = execMsg.SecurityId,
				PortfolioName = execMsg.PortfolioName,
				Side = side,
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
		else if (execMsg.DataType == DataType.Ticks)
		{
			// A print is news about a trade that happened, not about the book: it says nothing about
			// what is resting behind the touch, so the book it is matched against is left alone.
			if (execMsg.TradePrice is { } tickPrice)
				CheckStopOrders(execMsg.SecurityId, tickPrice, execMsg.LocalTime, results);
		}
	}

	/// <summary>
	/// Process order registration.
	/// </summary>
	public void ProcessOrderRegister(OrderRegisterMessage regMsg, List<Message> results)
	{
		// Intercept conditional (stop) orders
		if (regMsg.OrderType == OrderTypes.Conditional && regMsg.Condition is IStopLossOrderCondition stopCond)
		{
			RegisterStopOrder(regMsg, stopCond, results);
			return;
		}

		var state = GetSecurityState(regMsg.SecurityId);
		var portfolio = GetPortfolio(regMsg.PortfolioName);
		var serverTime = regMsg.LocalTime;

		// Validate registration
		var chargedPrice = GetChargedPrice(regMsg, state);
		var error = ValidateRegistration(regMsg, chargedPrice);
		if (error != null)
		{
			results.Add(CreateOrderResponse(regMsg, OrderStates.Failed, error: error));
			return;
		}

		// Create order response first (will be mutated later)
		var replyMsg = new ExecutionMessage
		{
			HasOrderInfo = true,
			DataTypeEx = DataType.Transactions,
			ServerTime = serverTime,
			LocalTime = regMsg.LocalTime,
			OriginalTransactionId = regMsg.TransactionId,
			Side = regMsg.Side,
			// Report acceptance as Active; a reject, expiry or full fill overwrites this below, but an
			// order that fills immediately still passes through Active so every order reports acceptance.
			OrderState = OrderStates.Active,
		};
		results.Add(replyMsg);

		// Use market price for margin calculation
		var marginPrice = regMsg.Side == Sides.Buy
			? state.OrderBook.BestBid?.price ?? 0
			: state.OrderBook.BestAsk?.price ?? 0;

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

		// Check PostOnly BEFORE blocking funds
		var matcher = new OrderMatcher();
		if (order.PostOnly && matcher.WouldCross(order, state.OrderBook))
		{
			replyMsg.Balance = regMsg.Volume;
			replyMsg.OrderVolume = regMsg.Volume;
			replyMsg.OrderState = OrderStates.Done;
			return;
		}

		// Increase depth volume if needed
		if (Settings.IncreaseDepthVolume)
		{
			IncreaseDepthVolumeIfNeeded(regMsg, state);
		}

		portfolio.ProcessOrderRegistration(regMsg.SecurityId, regMsg.Side, regMsg.Volume, marginPrice);

		// Portfolio change message after order registration
		AddPortfolioUpdate(portfolio, regMsg.LocalTime, results);

		// Match order against order book
		var matchSettings = new MatchingSettings
		{
			PriceStep = state.PriceStep,
			VolumeStep = state.VolumeStep,
		};

		var matchResult = matcher.Match(order, state.OrderBook, matchSettings);

		// Process result
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

		// For IOC/FOK with trades, send Done message BEFORE trades
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
				Side = regMsg.Side,
				PortfolioName = regMsg.PortfolioName,
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				ServerTime = serverTime,
			});
		}

		// Generate trades
		foreach (var trade in matchResult.Trades)
		{
			var tradeId = TradeIdGenerator.GetNextId();

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
				PortfolioName = regMsg.PortfolioName,
				MarketPrice = marketPrice,
			};

			var (realizedPnL, positionChange, position) = portfolio.ProcessTrade(
				regMsg.SecurityId, regMsg.Side, trade.Price, trade.Volume, tradeMsg.Commission);

			// For non-IOC/FOK orders: send order state update in trade loop
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
					Side = regMsg.Side,
					PortfolioName = regMsg.PortfolioName,
					HasOrderInfo = true,
				});
			}

			results.Add(tradeMsg);

			// Position change. Named after the traded instrument, not money: the value carried
			// here is a lot quantity, and the account's cash follows on its own row just below.
			results.Add(new PositionChangeMessage
			{
				SecurityId = regMsg.SecurityId,
				ServerTime = serverTime,
				LocalTime = regMsg.LocalTime,
				PortfolioName = regMsg.PortfolioName,
			}
			.Add(PositionChangeTypes.CurrentValue, position.CurrentValue)
			.TryAdd(PositionChangeTypes.AveragePrice, position.AveragePrice));

			AddPortfolioUpdate(portfolio, regMsg.LocalTime, results);

			// Generate trades for the resting orders this trade actually took from
			foreach (var fill in trade.CounterFills)
			{
				var counterOrder = fill.Order;

				if (!counterOrder.IsUserOrder)
					continue;

				var counterPortfolio = GetPortfolio(counterOrder.PortfolioName);

				var counterMarketPrice = counterOrder.Side == Sides.Buy
					? state.OrderBook.BestAsk?.price
					: state.OrderBook.BestBid?.price;

				// The resting order's own state goes out ahead of its trade, the same order the taker's
				// rows use, so a maker eaten to nothing reports a final state.
				results.Add(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					LocalTime = regMsg.LocalTime,
					ServerTime = serverTime,
					SecurityId = regMsg.SecurityId,
					OrderId = counterOrder.OrderId,
					OriginalTransactionId = counterOrder.TransactionId,
					Balance = fill.Remaining,
					OrderVolume = counterOrder.Volume,
					OrderState = fill.Remaining <= 0 ? OrderStates.Done : OrderStates.Active,
					Side = counterOrder.Side,
					PortfolioName = counterOrder.PortfolioName,
					HasOrderInfo = true,
				});

				var counterTradeMsg = new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					SecurityId = regMsg.SecurityId,
					LocalTime = regMsg.LocalTime,
					ServerTime = serverTime,
					OriginalTransactionId = counterOrder.TransactionId,
					OrderId = counterOrder.OrderId,
					TradeId = tradeId,
					TradePrice = trade.Price,
					TradeVolume = fill.Volume,
					Side = counterOrder.Side,
					PortfolioName = counterOrder.PortfolioName,
					MarketPrice = counterMarketPrice,
				};

				var (_, _, counterPosition) = counterPortfolio.ProcessTrade(
					regMsg.SecurityId, counterOrder.Side, trade.Price, fill.Volume, counterTradeMsg.Commission);

				results.Add(counterTradeMsg);

				results.Add(new PositionChangeMessage
				{
					SecurityId = regMsg.SecurityId,
					ServerTime = serverTime,
					LocalTime = regMsg.LocalTime,
					PortfolioName = counterOrder.PortfolioName,
				}
				.Add(PositionChangeTypes.CurrentValue, counterPosition.CurrentValue)
				.TryAdd(PositionChangeTypes.AveragePrice, counterPosition.AveragePrice));

				AddPortfolioUpdate(counterPortfolio, regMsg.LocalTime, results);

				// A maker consumed in full leaves the book; it must leave the active orders with it.
				if (fill.Remaining <= 0)
					state.OrderManager.TryRemoveOrder(counterOrder.TransactionId, out _);
			}
		}

		// Handle IOC/FOK cancelled portion
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
					Side = regMsg.Side,
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

		// Add to order book if needed
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

		// Send depth update
		if (state.HasDepthSubscription)
		{
			results.Add(state.OrderBook.ToMessage(regMsg.LocalTime, serverTime));
		}
	}

	/// <summary>
	/// Process order cancellation.
	/// </summary>
	public void ProcessOrderCancel(OrderCancelMessage cancelMsg, List<Message> results)
	{
		var state = GetSecurityState(cancelMsg.SecurityId);
		var serverTime = cancelMsg.LocalTime;

		// Try cancelling stop order first
		if (_stopOrderManager.Cancel(cancelMsg.OriginalTransactionId, out var cancelledStopInfo))
		{
			results.Add(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				LocalTime = cancelMsg.LocalTime,
				ServerTime = serverTime,
				OriginalTransactionId = cancelMsg.OriginalTransactionId,
				OrderState = OrderStates.Done,
				Side = cancelledStopInfo.Side,
				HasOrderInfo = true,
			});
			return;
		}

		if (!state.OrderManager.TryRemoveOrder(cancelMsg.OriginalTransactionId, out var order))
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

		state.OrderBook.RemoveQuote(order.TransactionId, order.Side, order.Price);

		var portfolio = GetPortfolio(order.PortfolioName);
		portfolio.ProcessOrderCancellation(cancelMsg.SecurityId, order.Side, order.Balance, order.MarginPrice);

		results.Add(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			LocalTime = cancelMsg.LocalTime,
			ServerTime = serverTime,
			OriginalTransactionId = cancelMsg.OriginalTransactionId,
			OrderState = OrderStates.Done,
			Side = order.Side,
			Balance = order.Balance,
			OrderVolume = order.Volume,
			HasOrderInfo = true,
		});

		if (state.HasDepthSubscription)
		{
			results.Add(state.OrderBook.ToMessage(cancelMsg.LocalTime, serverTime));
		}

		AddPortfolioUpdate(portfolio, cancelMsg.LocalTime, results);
	}

	private void ProcessOrderReplace(OrderReplaceMessage replaceMsg, List<Message> results)
	{
		// Try stop order replace first
		if (_stopOrderManager.Cancel(replaceMsg.OriginalTransactionId, out var oldStopInfo))
		{
			results.Add(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				LocalTime = replaceMsg.LocalTime,
				ServerTime = replaceMsg.LocalTime,
				OriginalTransactionId = replaceMsg.OriginalTransactionId,
				OrderState = OrderStates.Done,
				Side = oldStopInfo.Side,
				Balance = oldStopInfo.Volume,
				OrderVolume = oldStopInfo.Volume,
			});

			if (replaceMsg.Condition is IStopLossOrderCondition stopCond)
			{
				var stopRegMsg = new OrderRegisterMessage
				{
					SecurityId = replaceMsg.SecurityId,
					LocalTime = replaceMsg.LocalTime,
					TransactionId = replaceMsg.TransactionId,
					Side = replaceMsg.Side,
					Volume = replaceMsg.Volume > 0 ? replaceMsg.Volume : oldStopInfo.Volume,
					OrderType = OrderTypes.Conditional,
					Condition = replaceMsg.Condition,
					PortfolioName = replaceMsg.PortfolioName ?? oldStopInfo.PortfolioName,
				};

				RegisterStopOrder(stopRegMsg, stopCond, results);
			}
			else
			{
				var regMsg = new OrderRegisterMessage
				{
					SecurityId = replaceMsg.SecurityId,
					LocalTime = replaceMsg.LocalTime,
					TransactionId = replaceMsg.TransactionId,
					Side = replaceMsg.Side,
					Price = replaceMsg.Price,
					Volume = replaceMsg.Volume > 0 ? replaceMsg.Volume : oldStopInfo.Volume,
					OrderType = replaceMsg.OrderType ?? OrderTypes.Limit,
					PortfolioName = replaceMsg.PortfolioName ?? oldStopInfo.PortfolioName,
					TimeInForce = replaceMsg.TimeInForce,
					PostOnly = replaceMsg.PostOnly,
					TillDate = replaceMsg.TillDate,
				};

				ProcessOrderRegister(regMsg, results);
			}
			return;
		}

		var state = GetSecurityState(replaceMsg.SecurityId);
		var serverTime = replaceMsg.LocalTime;

		if (!state.OrderManager.TryRemoveOrder(replaceMsg.OriginalTransactionId, out var oldOrder))
		{
			var error = new InvalidOperationException($"Order {replaceMsg.OriginalTransactionId} not found for replace");

			results.Add(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				SecurityId = replaceMsg.SecurityId,
				LocalTime = replaceMsg.LocalTime,
				ServerTime = serverTime,
				OriginalTransactionId = replaceMsg.OriginalTransactionId,
				OrderState = OrderStates.Done,
				Balance = 0,
				OrderVolume = 0,
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

		state.OrderBook.RemoveQuote(oldOrder.TransactionId, oldOrder.Side, oldOrder.Price);

		var portfolio = GetPortfolio(oldOrder.PortfolioName);
		portfolio.ProcessOrderCancellation(replaceMsg.SecurityId, oldOrder.Side, oldOrder.Balance, oldOrder.MarginPrice);

		results.Add(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			LocalTime = replaceMsg.LocalTime,
			ServerTime = serverTime,
			OriginalTransactionId = replaceMsg.OriginalTransactionId,
			OrderState = OrderStates.Done,
			Side = oldOrder.Side,
			Balance = oldOrder.Balance,
			OrderVolume = oldOrder.Volume,
			HasOrderInfo = true,
		});

		AddPortfolioUpdate(portfolio, replaceMsg.LocalTime, results);

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

		if (mode.HasFlag(OrderGroupCancelModes.ClosePositions) && groupMsg.PortfolioName.IsEmpty())
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
			foreach (var state in _securityStates.Values)
			{
				if (groupMsg.SecurityId != default && groupMsg.SecurityId != state.SecurityId)
					continue;

				var ordersToCancel = state.OrderManager
					.GetActiveOrders(groupMsg.PortfolioName, groupMsg.SecurityId == default ? null : groupMsg.SecurityId, groupMsg.Side)
					.ToList();

				foreach (var order in ordersToCancel)
				{
					var cancelMsg = new OrderCancelMessage
					{
						SecurityId = state.SecurityId,
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
			foreach (var portfolio in _portfolioManager.GetAllPortfolios())
			{
				if (!groupMsg.PortfolioName.IsEmpty() &&
					!groupMsg.PortfolioName.Equals(portfolio.Name, StringComparison.OrdinalIgnoreCase))
					continue;

				foreach (var (securityId, volume, _) in portfolio.GetPositions())
				{
					if (groupMsg.SecurityId != default && groupMsg.SecurityId != securityId)
						continue;

					if (volume == 0)
						continue;

					if (groupMsg.Side.HasValue)
					{
						var positionSide = volume > 0 ? Sides.Buy : Sides.Sell;
						if (positionSide != groupMsg.Side.Value)
							continue;
					}

					var closeSide = volume > 0 ? Sides.Sell : Sides.Buy;
					var closeVolume = volume.Abs();

					var state = GetSecurityState(securityId);
					var bestPrice = closeSide == Sides.Buy
						? state.OrderBook.BestAsk?.price
						: state.OrderBook.BestBid?.price;

					if (bestPrice.HasValue)
					{
						var closeMsg = new OrderRegisterMessage
						{
							SecurityId = securityId,
							LocalTime = groupMsg.LocalTime,
							// One request closes every position the account holds, and each close is an
							// order of its own: sharing the request's id would make two orders standing at
							// once indistinguishable, and fills keyed by it would book against a single one.
							TransactionId = TransactionIdGenerator.GetNextId(),
							Side = closeSide,
							Price = bestPrice.Value,
							Volume = closeVolume,
							OrderType = OrderTypes.Limit,
							PortfolioName = portfolio.Name,
						};

						var firstRow = results.Count;

						ProcessOrderRegister(closeMsg, results);
						NameAccountOn(results, firstRow, closeMsg);
					}
				}
			}
		}
	}

	/// <summary>
	/// States the account and the instrument on each transactional row from <paramref name="from"/>
	/// onwards that is missing them.
	/// </summary>
	/// <remarks>
	/// An order the engine materialised on its own behalf crossed no caller on its way in, so no
	/// map anywhere holds the account to put back on its rows afterwards - unattributed, they are
	/// booked against nobody.
	/// </remarks>
	private static void NameAccountOn(List<Message> results, int from, OrderRegisterMessage regMsg)
	{
		for (var i = from; i < results.Count; i++)
		{
			if (results[i] is not ExecutionMessage row)
				continue;

			if (row.PortfolioName.IsEmpty())
				row.PortfolioName = regMsg.PortfolioName;

			if (row.SecurityId == default)
				row.SecurityId = regMsg.SecurityId;
		}
	}

	private void ProcessOrderStatus(OrderStatusMessage statusMsg, List<Message> results)
	{
		results.Add(statusMsg.CreateResponse());

		if (!statusMsg.IsSubscribe)
			return;

		foreach (var state in _securityStates.Values)
		{
			var orders = statusMsg.PortfolioName.IsEmpty()
				? state.OrderManager.GetActiveOrders()
				: state.OrderManager.GetActiveOrders(statusMsg.PortfolioName);

			foreach (var order in orders)
			{
				if (statusMsg.OrderId.HasValue && order.TransactionId != statusMsg.OrderId.Value)
					continue;

				results.Add(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					SecurityId = state.SecurityId,
					LocalTime = order.LocalTime,
					ServerTime = order.ServerTime,
					OriginalTransactionId = statusMsg.TransactionId,
					TransactionId = order.TransactionId,
					OrderId = order.OrderId,
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

		results.Add(statusMsg.CreateResult());
	}

	private void ProcessPortfolioLookup(PortfolioLookupMessage lookupMsg, List<Message> results)
	{
		results.Add(lookupMsg.CreateResponse());

		if (!lookupMsg.IsSubscribe)
			return;

		foreach (var portfolio in _portfolioManager.GetAllPortfolios())
		{
			if (!lookupMsg.PortfolioName.IsEmpty() &&
				!lookupMsg.PortfolioName.Equals(portfolio.Name, StringComparison.OrdinalIgnoreCase))
				continue;

			results.Add(new PortfolioMessage
			{
				PortfolioName = portfolio.Name,
				OriginalTransactionId = lookupMsg.TransactionId,
			});

			foreach (var (securityId, volume, avgPrice) in portfolio.GetPositions())
			{
				if (volume == 0)
					continue;

				results.Add(new PositionChangeMessage
				{
					SecurityId = securityId,
					ServerTime = lookupMsg.LocalTime,
					LocalTime = lookupMsg.LocalTime,
					PortfolioName = portfolio.Name,
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
			{
				// Keep the average price the opening state was given. Without it the position
				// starts at 0 and the first close realizes the whole notional as profit.
				var avgPrice = (decimal?)posMsg.Changes.TryGetValue(PositionChangeTypes.AveragePrice);
				portfolio.SetPosition(posMsg.SecurityId, beginValue.Value, avgPrice ?? 0m);
			}

			var leverage = (decimal?)posMsg.Changes.TryGetValue(PositionChangeTypes.Leverage);
			if (leverage.HasValue)
			{
				var pos = portfolio.GetPosition(posMsg.SecurityId);
				if (pos is not null)
					pos.Leverage = leverage.Value;
			}
		}

		results.Add(posMsg.Clone());
	}

	private void ProcessTime(DateTime time, List<Message> results)
	{
		foreach (var state in _securityStates.Values)
		{
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
					Side = order.Side,
					Balance = order.Balance,
					OrderVolume = order.Volume,
					PortfolioName = order.PortfolioName,
					HasOrderInfo = true,
				});
			}
		}
	}

	private void RegisterStopOrder(OrderRegisterMessage regMsg, IStopLossOrderCondition stopCond, List<Message> results)
	{
		var info = new StopOrderInfo
		{
			TransactionId = regMsg.TransactionId,
			SecurityId = regMsg.SecurityId,
			Side = regMsg.Side,
			Volume = regMsg.Volume,
			PortfolioName = regMsg.PortfolioName,
			StopPrice = stopCond.ActivationPrice ?? 0,
			LimitPrice = stopCond.ClosePositionPrice,
			IsTrailing = stopCond.IsTrailing,
			TrailingOffset = stopCond is StopOrderCondition soc ? soc.TrailingOffset : null,
		};

		_stopOrderManager.Register(info);

		results.Add(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			SecurityId = regMsg.SecurityId,
			LocalTime = regMsg.LocalTime,
			ServerTime = regMsg.LocalTime,
			OriginalTransactionId = regMsg.TransactionId,
			OrderId = OrderIdGenerator.GetNextId(),
			OrderState = OrderStates.Active,
			OrderType = OrderTypes.Conditional,
			PortfolioName = regMsg.PortfolioName,
			Side = regMsg.Side,
			OrderVolume = regMsg.Volume,
			Balance = regMsg.Volume,
		});
	}

	/// <summary>
	/// Check stop orders against a price and process triggered orders.
	/// </summary>
	public void CheckStopOrders(SecurityId securityId, decimal price, DateTime time, List<Message> results)
	{
		var triggers = _stopOrderManager.CheckPrice(securityId, price, time);

		foreach (var trigger in triggers)
		{
			// The derived order carries the stop's own transaction id, so the rows it raises are the
			// stop's own lifecycle from here on.
			ProcessOrderRegister(trigger.ResultingOrder, results);
		}
	}

	private void Reset(List<Message> results)
	{
		_securityStates.Clear();
		_portfolioManager.Clear();
		_stopOrderManager.Clear();
		_lastInputTime = default;

		OrderIdGenerator.Current = Settings.InitialOrderId;
		TradeIdGenerator.Current = Settings.InitialTradeId;

		results.Add(new ResetMessage());
	}

	/// <summary>
	/// Check whether the registration is allowed: the instrument is trading, and the account can
	/// afford it.
	/// </summary>
	/// <param name="regMsg">Registration to check.</param>
	/// <param name="chargedPrice">Price the order will actually be charged at, or <see langword="null"/> when it cannot be priced.</param>
	/// <returns>The reason the registration must be refused, or <see langword="null"/> when it is allowed.</returns>
	public InvalidOperationException ValidateRegistration(OrderRegisterMessage regMsg, decimal? chargedPrice)
	{
		if (regMsg is null)
			throw new ArgumentNullException(nameof(regMsg));

		// A halted instrument takes no orders at all, whatever the account can pay for.
		if (Settings.CheckTradingState && GetSecurityState(regMsg.SecurityId).TradingState == SecurityStates.Stoped)
			return new InvalidOperationException(LocalizedStrings.SecurityStopped.Put(regMsg.SecurityId));

		if (!Settings.CheckMoney)
			return null;

		// An order with no price to charge cannot be paid for, so it cannot be let through.
		if (chargedPrice is not decimal price)
			return new InvalidOperationException($"Order {regMsg.TransactionId}: no price to charge, the {regMsg.Side.Invert()} side is empty.");

		return _portfolioManager.ValidateFunds(regMsg.PortfolioName, regMsg.SecurityId, price, regMsg.Volume);
	}

	/// <summary>
	/// Price the order will be charged at: its own price when it names one, the opposite best for a market order.
	/// </summary>
	private static decimal? GetChargedPrice(OrderRegisterMessage regMsg, SecurityState state)
	{
		if (regMsg.OrderType != OrderTypes.Market)
			return regMsg.Price;

		return regMsg.Side == Sides.Buy
			? state.OrderBook.BestAsk?.price
			: state.OrderBook.BestBid?.price;
	}

	/// <summary>
	/// The engine's own books, read as the price an open position could be closed at.
	/// </summary>
	private sealed class BookMarkPrices(MatchingEngineAdapter engine) : IMarkPrices
	{
		public decimal? TryGetClosePrice(SecurityId securityId, Sides closeSide)
		{
			// Asking must not create a state for an instrument the engine has never seen.
			if (!engine._securityStates.TryGetValue(securityId, out var state))
				return null;

			return closeSide == Sides.Sell
				? state.OrderBook.BestBid?.price
				: state.OrderBook.BestAsk?.price;
		}
	}

	private EmulatedPortfolio GetPortfolio(string name)
	{
		return _portfolioManager.GetPortfolio(name);
	}

	private static void IncreaseDepthVolumeIfNeeded(OrderRegisterMessage regMsg, SecurityState state)
	{
		var oppositeSide = regMsg.Side.Invert();
		var oppositeBest = regMsg.Side == Sides.Buy
			? state.OrderBook.BestAsk
			: state.OrderBook.BestBid;

		if (!oppositeBest.HasValue)
			return;

		var bestOppPrice = oppositeBest.Value.price;

		// A market order names no price and crosses by definition, on either side.
		var canMatch = regMsg.OrderType == OrderTypes.Market || (regMsg.Side == Sides.Buy
			? regMsg.Price >= bestOppPrice
			: regMsg.Price <= bestOppPrice);

		var quotesVolume = regMsg.Side == Sides.Buy
			? state.OrderBook.TotalAskVolume
			: state.OrderBook.TotalBidVolume;

		if (!canMatch || quotesVolume > regMsg.Volume)
			return;

		var worstLevel = regMsg.Side == Sides.Buy
			? state.OrderBook.GetWorstAsk()
			: state.OrderBook.GetWorstBid();

		if (!worstLevel.HasValue)
			return;

		var leftVolume = (regMsg.Volume - quotesVolume) + 1;
		var lastVolume = worstLevel.Value.volume;
		var lastPrice = worstLevel.Value.price;
		var priceStep = state.PriceStep;

		while (leftVolume > 0 && lastPrice != 0)
		{
			lastVolume *= 2;
			lastPrice += priceStep * (oppositeSide == Sides.Buy ? -1 : 1);
			leftVolume -= lastVolume;
			state.OrderBook.UpdateLevel(oppositeSide, lastPrice, lastVolume);
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

	/// <summary>
	/// The money row stating what an account holds: its realized and unrealized result, what its
	/// working orders hold, and what it can trade with now.
	/// </summary>
	/// <param name="portfolio">The account the row is about.</param>
	/// <param name="time">Time to stamp the row with.</param>
	/// <returns>The row.</returns>
	public static PositionChangeMessage CreatePortfolioUpdate(EmulatedPortfolio portfolio, DateTime time)
	{
		if (portfolio is null)
			throw new ArgumentNullException(nameof(portfolio));

		var unrealizedPnL = portfolio.UnrealizedPnL;
		var totalPnL = portfolio.RealizedPnL - portfolio.Commission + unrealizedPnL;

		return new PositionChangeMessage
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
		.Add(PositionChangeTypes.Commission, portfolio.Commission);
	}

	private static void AddPortfolioUpdate(EmulatedPortfolio portfolio, DateTime time, List<Message> results)
		=> results.Add(CreatePortfolioUpdate(portfolio, time));

	/// <summary>
	/// Send out message.
	/// </summary>
	public ValueTask SendOutMessageAsync(Message message, CancellationToken cancellationToken)
		=> NewOutMessageAsync.InvokeAsync(message, cancellationToken);
}
