namespace StockSharp.BusinessEntities;

using System.Reflection;

using Ecng.Configuration;
using Ecng.Reflection;

/// <summary>
/// Extension class for <see cref="BusinessEntities"/>.
/// </summary>
public static partial class EntitiesExtensions
{
	/// <summary>
	/// To create from <see cref="int"/> the pips values.
	/// </summary>
	/// <param name="value"><see cref="int"/> value.</param>
	/// <param name="security">The instrument from which information about the price increment is taken.</param>
	/// <returns>Pips.</returns>
	public static Unit Pips(this int value, Security security)
	{
		return Pips((decimal)value, security);
	}

	/// <summary>
	/// To create from <see cref="double"/> the pips values.
	/// </summary>
	/// <param name="value"><see cref="double"/> value.</param>
	/// <param name="security">The instrument from which information about the price increment is taken.</param>
	/// <returns>Pips.</returns>
	public static Unit Pips(this double value, Security security)
	{
		return Pips((decimal)value, security);
	}

	/// <summary>
	/// To create from <see cref="decimal"/> the pips values.
	/// </summary>
	/// <param name="value"><see cref="decimal"/> value.</param>
	/// <param name="security">The instrument from which information about the price increment is taken.</param>
	/// <returns>Pips.</returns>
	public static Unit Pips(this decimal value, Security security)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		var step = security.PriceStep ?? throw new ArgumentException(LocalizedStrings.PriceStepNotSpecified, nameof(security));

		return new Unit(value * step, UnitTypes.Absolute);
	}

	/// <summary>
	/// To create from <see cref="int"/> the points values.
	/// </summary>
	/// <param name="value"><see cref="int"/> value.</param>
	/// <param name="security">The instrument from which information about the price increment cost is taken.</param>
	/// <returns>Points.</returns>
	public static Unit Points(this int value, Security security)
	{
		return Points((decimal)value, security);
	}

	/// <summary>
	/// To create from <see cref="double"/> the points values.
	/// </summary>
	/// <param name="value"><see cref="double"/> value.</param>
	/// <param name="security">The instrument from which information about the price increment cost is taken.</param>
	/// <returns>Points.</returns>
	public static Unit Points(this double value, Security security)
	{
		return Points((decimal)value, security);
	}

	/// <summary>
	/// To create from <see cref="decimal"/> the points values.
	/// </summary>
	/// <param name="value"><see cref="decimal"/> value.</param>
	/// <param name="security">The instrument from which information about the price increment cost is taken.</param>
	/// <returns>Points.</returns>
	public static Unit Points(this decimal value, Security security)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		var step = security.StepPrice ?? throw new ArgumentException(LocalizedStrings.StepPriceDesc, nameof(security));

		return new Unit(value * step, UnitTypes.Absolute);
	}

	/// <summary>
	/// To cut the price, to make it multiple of minimal step, also to limit number of signs after the comma.
	/// </summary>
	/// <param name="security"><see cref="Security"/></param>
	/// <param name="price">The price to be made multiple.</param>
	/// <returns>The multiple price.</returns>
	public static decimal ShrinkPrice(this Security security, decimal price)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (security.PriceStep == null)
			throw new ArgumentException(LocalizedStrings.PriceStepNotSpecified, nameof(security));

		return price.Round(security.PriceStep ?? 1m, security.Decimals ?? 0);
	}

	/// <summary>
	/// Reregister the order.
	/// </summary>
	/// <param name="provider">The transactional provider.</param>
	/// <param name="order">Order.</param>
	/// <param name="clone">Changes.</param>
	public static void ReRegisterOrderEx(this ITransactionProvider provider, Order order, Order clone)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		if (provider.IsOrderReplaceable(order) == true)
		{
			if (provider.IsOrderEditable(order) == true)
				provider.EditOrder(order, clone);
			else
				provider.ReRegisterOrder(order, clone);
		}
		else
		{
			provider.CancelOrder(order);
			provider.RegisterOrder(clone);
		}
	}

	/// <summary>
	/// To create copy of the order for re-registration.
	/// </summary>
	/// <param name="oldOrder">The original order.</param>
	/// <param name="newPrice">Price of the new order.</param>
	/// <param name="newVolume">Volume of the new order.</param>
	/// <returns>New order.</returns>
	public static Order ReRegisterClone(this Order oldOrder, decimal? newPrice = null, decimal? newVolume = null)
	{
		if (oldOrder == null)
			throw new ArgumentNullException(nameof(oldOrder));

		return new Order
		{
			Portfolio = oldOrder.Portfolio,
			Side = oldOrder.Side,
			TimeInForce = oldOrder.TimeInForce,
			Security = oldOrder.Security,
			Type = oldOrder.Type,
			Price = newPrice ?? oldOrder.Price,
			Volume = newVolume ?? oldOrder.Volume,
			ExpiryDate = oldOrder.ExpiryDate,
			VisibleVolume = oldOrder.VisibleVolume,
			BrokerCode = oldOrder.BrokerCode,
			ClientCode = oldOrder.ClientCode,
			Condition = oldOrder.Condition?.TypedClone(),
			IsManual = oldOrder.IsManual,
			IsMarketMaker = oldOrder.IsMarketMaker,
			MarginMode = oldOrder.MarginMode,
			MinVolume = oldOrder.MinVolume,
			PositionEffect = oldOrder.PositionEffect,
			PostOnly = oldOrder.PostOnly,
			StrategyId = oldOrder.StrategyId,
			Leverage = oldOrder.Leverage,
		};
	}

	/// <summary>
	/// Reregister the order.
	/// </summary>
	/// <param name="provider">The transactional provider.</param>
	/// <param name="oldOrder">Changing order.</param>
	/// <param name="price">Price of the new order.</param>
	/// <param name="volume">Volume of the new order.</param>
	/// <returns>New order.</returns>
	public static Order ReRegisterOrder(this ITransactionProvider provider, Order oldOrder, decimal price, decimal volume)
	{
		if (provider == null)
			throw new ArgumentNullException(nameof(provider));

		var newOrder = oldOrder.ReRegisterClone(price, volume);
		provider.ReRegisterOrder(oldOrder, newOrder);
		return newOrder;
	}

	/// <summary>
	/// To get the instrument by the identifier.
	/// </summary>
	/// <param name="provider">The provider of information about instruments.</param>
	/// <param name="id">Security ID.</param>
	/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
	public static Security LookupById(this ISecurityProvider provider, string id)
		=> provider.LookupById(id.ToSecurityId());

	private const BindingFlags _publicStatic = BindingFlags.Public | BindingFlags.Static;

	/// <summary>
	/// To get a list of exchanges.
	/// </summary>
	/// <returns>Exchanges.</returns>
	public static IEnumerable<Exchange> EnumerateExchanges()
		=> typeof(Exchange)
			.GetMembers<PropertyInfo>(_publicStatic, typeof(Exchange))
			.Select(prop => (Exchange)prop.GetValue(null, null));

	/// <summary>
	/// To get a list of boards.
	/// </summary>
	/// <returns>Boards.</returns>
	public static IEnumerable<ExchangeBoard> EnumerateExchangeBoards()
		=> typeof(ExchangeBoard)
			.GetMembers<PropertyInfo>(_publicStatic, typeof(ExchangeBoard))
			.Select(prop => (ExchangeBoard)prop.GetValue(null, null));

	/// <summary>
	/// All registered candle types.
	/// </summary>
	public static IEnumerable<Type> AllCandleTypes => _candleTypes.CachedKeys;

	/// <summary>
	/// All registered candle message types.
	/// </summary>
	public static IEnumerable<Type> AllCandleMessageTypes => _candleTypes.CachedValues;

	/// <summary>
	/// To convert the own trade into message.
	/// </summary>
	/// <param name="trade">Own trade.</param>
	/// <returns>Message.</returns>
	public static ExecutionMessage ToMessage(this MyTrade trade)
	{
		if (trade == null)
			throw new ArgumentNullException(nameof(trade));

		var tick = trade.Trade;
		var order = trade.Order;

		return new ExecutionMessage
		{
			TradeId = tick.Id,
			TradeStringId = tick.StringId,
			TradePrice = tick.Price,
			TradeVolume = tick.Volume,
			OriginalTransactionId = order.TransactionId,
			OrderId = order.Id,
			OrderStringId = order.StringId,
			OrderType = order.Type,
			Side = order.Side,
			OrderPrice = order.Price,
			SecurityId = order.Security.ToSecurityId(),
			PortfolioName = order.Portfolio.Name,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			ServerTime = tick.ServerTime,
			LocalTime = tick.LocalTime,
			OriginSide = tick.OriginSide,
			Currency = tick.Currency,
			Position = trade.Position,
			PnL = trade.PnL,
			Slippage = trade.Slippage,
			Commission = trade.Commission,
			CommissionCurrency = trade.CommissionCurrency,
			SeqNum = trade.Trade.SeqNum,
			OrderBuyId = trade.Trade.OrderBuyId,
			OrderSellId = trade.Trade.OrderSellId,
		};
	}

	/// <summary>
	/// To convert the order into message.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <returns>Message.</returns>
	public static ExecutionMessage ToMessage(this Order order)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		var message = new ExecutionMessage
		{
			OrderId = order.Id,
			OrderStringId = order.StringId,
			TransactionId = order.TransactionId,
			OriginalTransactionId = order.TransactionId,
			SecurityId = order.Security.ToSecurityId(),
			PortfolioName = order.Portfolio.Name,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderPrice = order.Price,
			OrderType = order.Type,
			OrderVolume = order.Volume,
			Balance = order.Balance,
			Side = order.Side,
			OrderState = order.State,
			OrderStatus = order.Status,
			TimeInForce = order.TimeInForce,
			ServerTime = order.ServerTime,
			LocalTime = order.LocalTime,
			ExpiryDate = order.ExpiryDate,
			UserOrderId = order.UserOrderId,
			StrategyId = order.StrategyId,
			Commission = order.Commission,
			CommissionCurrency = order.CommissionCurrency,
			IsSystem = order.IsSystem,
			Comment = order.Comment,
			VisibleVolume = order.VisibleVolume,
			Currency = order.Currency,
			SeqNum = order.SeqNum,
		};

		return message;
	}

	/// <summary>
	/// To convert the error description into message.
	/// </summary>
	/// <param name="fail">Error details.</param>
	/// <param name="originalTransactionId">ID of original transaction, for which this message is the answer.</param>
	/// <returns>Message.</returns>
	public static ExecutionMessage ToMessage(this OrderFail fail, long originalTransactionId)
	{
		if (fail == null)
			throw new ArgumentNullException(nameof(fail));

		var order = fail.Order ?? throw new InvalidOperationException();

		return new ExecutionMessage
		{
			OrderId = order.Id,
			OrderStringId = order.StringId,
			TransactionId = order.TransactionId,
			OriginalTransactionId = originalTransactionId,
			SecurityId = order.Security?.ToSecurityId() ?? default,
			PortfolioName = order.Portfolio?.Name,
			Error = fail.Error,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderState = OrderStates.Failed,
			ServerTime = fail.ServerTime,
			LocalTime = fail.LocalTime,
			SeqNum = fail.SeqNum,
		};
	}

	/// <summary>
	/// To create the message of new order registration.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <param name="securityId">Security ID.</param>
	/// <returns>Message.</returns>
	public static OrderRegisterMessage CreateRegisterMessage(this Order order, SecurityId? securityId = null)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		var msg = new OrderRegisterMessage
		{
			TransactionId = order.TransactionId,
			PortfolioName = order.Portfolio.Name,
			Side = order.Side,
			Price = order.Price,
			Volume = order.Volume,
			VisibleVolume = order.VisibleVolume,
			OrderType = order.Type,
			Comment = order.Comment,
			Condition = order.Condition?.TypedClone(),
			TimeInForce = order.TimeInForce,
			TillDate = order.ExpiryDate,
			//IsSystem = order.IsSystem,
			UserOrderId = order.UserOrderId,
			StrategyId = order.StrategyId,
			BrokerCode = order.BrokerCode,
			ClientCode = order.ClientCode,
			Currency = order.Currency,
			IsMarketMaker = order.IsMarketMaker,
			MarginMode = order.MarginMode,
			Slippage = order.Slippage,
			IsManual = order.IsManual,
			MinOrderVolume = order.MinVolume,
			PositionEffect = order.PositionEffect,
			PostOnly = order.PostOnly,
			Leverage = order.Leverage,
		};

		order.Security.ToMessage(securityId).CopyTo(msg, false);

		return msg;
	}

	/// <summary>
	/// To create the message of cancelling old order.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <param name="securityId">Security ID.</param>
	/// <param name="transactionId">The transaction number.</param>
	/// <returns>Message.</returns>
	public static OrderCancelMessage CreateCancelMessage(this Order order, SecurityId securityId, long transactionId)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		var msg = new OrderCancelMessage
		{
			PortfolioName = order.Portfolio.Name,
			OrderType = order.Type,
			OriginalTransactionId = order.TransactionId,
			TransactionId = transactionId,
			UserOrderId = order.UserOrderId,
			StrategyId = order.StrategyId,
			BrokerCode = order.BrokerCode,
			ClientCode = order.ClientCode,
			OrderId = order.Id,
			OrderStringId = order.StringId,
			Balance = order.Balance,
			Volume = order.Volume,
			Side = order.Side,
			MarginMode = order.MarginMode,
		};

		order.Security.ToMessage(securityId).CopyTo(msg, false);

		return msg;
	}

	/// <summary>
	/// To create the message of replacing old order with new one.
	/// </summary>
	/// <param name="oldOrder">Old order.</param>
	/// <param name="newOrder">New order.</param>
	/// <param name="securityId">Security ID.</param>
	/// <returns>Message.</returns>
	public static OrderReplaceMessage CreateReplaceMessage(this Order oldOrder, Order newOrder, SecurityId securityId)
	{
		if (oldOrder == null)
			throw new ArgumentNullException(nameof(oldOrder));

		if (newOrder == null)
			throw new ArgumentNullException(nameof(newOrder));

		var msg = new OrderReplaceMessage
		{
			TransactionId = newOrder.TransactionId,
			PortfolioName = newOrder.Portfolio.Name,
			Side = newOrder.Side,
			Price = newOrder.Price,
			Volume = newOrder.Volume,
			VisibleVolume = newOrder.VisibleVolume,
			OrderType = newOrder.Type,
			Comment = newOrder.Comment,
			Condition = newOrder.Condition,
			TimeInForce = newOrder.TimeInForce,
			TillDate = newOrder.ExpiryDate,
			//IsSystem = newOrder.IsSystem,

			OldOrderId = oldOrder.Id,
			OldOrderStringId = oldOrder.StringId,
			OriginalTransactionId = oldOrder.TransactionId,

			OldOrderPrice = oldOrder.Price,
			OldOrderVolume = oldOrder.Volume,

			UserOrderId = oldOrder.UserOrderId,
			StrategyId = oldOrder.StrategyId,

			BrokerCode = oldOrder.BrokerCode,
			ClientCode = oldOrder.ClientCode,

			Currency = newOrder.Currency,

			IsManual = newOrder.IsManual,
			IsMarketMaker = newOrder.IsMarketMaker,
			MarginMode = newOrder.MarginMode,

			Slippage = newOrder.Slippage,

			MinOrderVolume = newOrder.MinVolume,
			PositionEffect = newOrder.PositionEffect,
			PostOnly = newOrder.PostOnly,
			Leverage = newOrder.Leverage,
		};

		oldOrder.Security.ToMessage(securityId).CopyTo(msg, false);

		return msg;
	}

	/// <summary>
	/// To convert the instrument into message.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="securityId">Security ID.</param>
	/// <param name="originalTransactionId">ID of original transaction, for which this message is the answer.</param>
	/// <param name="copyExtendedId">Copy <see cref="Security.ExternalId"/> and <see cref="Security.Type"/>.</param>
	/// <returns>Message.</returns>
	public static SecurityMessage ToMessage(this Security security, SecurityId? securityId = null, long originalTransactionId = 0, bool copyExtendedId = false)
	{
		if (security is null)
			throw new ArgumentNullException(nameof(security));

		if (security.IsAllSecurity())
		{
			return new SecurityMessage();

			// not immutable
			//return Messages.Extensions.AllSecurity;
		}

		return security.FillMessage(new SecurityMessage
		{
			SecurityId = securityId ?? security.ToSecurityId(copyExtended: copyExtendedId),
			OriginalTransactionId = originalTransactionId,
		});
	}

	/// <summary>
	/// To convert the instrument into message.
	/// </summary>
	/// <typeparam name="TMessage">Message type.</typeparam>
	/// <param name="security">Security.</param>
	/// <param name="message">Message.</param>
	/// <returns>Message.</returns>
	public static TMessage FillMessage<TMessage>(this Security security, TMessage message)
		where TMessage : SecurityMessage
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		message.Name = security.Name;
		message.ShortName = security.ShortName;
		message.PriceStep = security.PriceStep;
		message.Decimals = security.Decimals;
		message.VolumeStep = security.VolumeStep;
		message.MinVolume = security.MinVolume;
		message.MaxVolume = security.MaxVolume;
		message.Multiplier = security.Multiplier;
		message.Currency = security.Currency;
		message.SecurityType = security.Type;
		message.Class = security.Class;
		message.CfiCode = security.CfiCode;
		message.OptionType = security.OptionType;
		message.Strike = security.Strike;
		message.BinaryOptionType = security.BinaryOptionType;
		message.UnderlyingSecurityId = security.UnderlyingSecurityId.IsEmpty() ? default : security.UnderlyingSecurityId.ToSecurityId();
		message.SettlementDate = security.SettlementDate;
		message.ExpiryDate = security.ExpiryDate;
		message.IssueSize = security.IssueSize;
		message.IssueDate = security.IssueDate;
		message.UnderlyingSecurityType = security.UnderlyingSecurityType;
		message.UnderlyingSecurityMinVolume = security.UnderlyingSecurityMinVolume;
		message.Shortable = security.Shortable;
		message.BasketCode = security.BasketCode;
		message.BasketExpression = security.BasketExpression;
		message.FaceValue = security.FaceValue;
		message.OptionStyle = security.OptionStyle;
		message.SettlementType = security.SettlementType;

		if (!security.PrimaryId.IsEmpty())
			message.PrimaryId = security.PrimaryId.ToSecurityId();

		return message;
	}

	/// <summary>
	/// Lookup all securities predefined criteria.
	/// </summary>
	public static readonly Security LookupAllCriteria = new();

	/// <summary>
	/// Determine the <paramref name="criteria"/> contains lookup all filter.
	/// </summary>
	/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
	/// <returns>Check result.</returns>
	public static bool IsLookupAll(this Security criteria)
	{
		if (criteria == null)
			throw new ArgumentNullException(nameof(criteria));

		if (criteria == LookupAllCriteria)
			return true;

		return criteria.ToLookupMessage().IsLookupAll();
	}

	/// <summary>
	/// Convert <see cref="SecurityLookupMessage"/> message to <see cref="Security"/> criteria.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <returns>Criteria.</returns>
	public static Security ToLookupCriteria(this SecurityLookupMessage message, IExchangeInfoProvider exchangeInfoProvider)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (exchangeInfoProvider == null)
			throw new ArgumentNullException(nameof(exchangeInfoProvider));

		if (message.IsLookupAll())
			return LookupAllCriteria;

		var criteria = new Security();
		criteria.ApplyChanges(message, exchangeInfoProvider);

		criteria.Type ??= message.GetSecurityTypes().FirstOr();

		return criteria;
	}

	/// <summary>
	/// Convert <see cref="Security"/> criteria to <see cref="SecurityLookupMessage"/>.
	/// </summary>
	/// <param name="criteria">Criteria.</param>
	/// <returns>Message.</returns>
	public static SecurityLookupMessage ToLookupMessage(this Security criteria)
	{
		if (criteria == null)
			throw new ArgumentNullException(nameof(criteria));

		var message = new SecurityLookupMessage();

		if (criteria.Id.IsEmpty())
		{
			message.SecurityId = criteria.ExternalId.ToSecurityId(new()
			{
				SecurityCode = criteria.Code,
				BoardCode = criteria.Board?.Code,
			});

			criteria.FillMessage(message);
		}
		else
		{
			message.SecurityId = criteria.Id.ToSecurityId();
		}

		return message;
	}

	/// <summary>
	/// To convert the message into instrument.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <returns>Security.</returns>
	public static Security ToSecurity(this SecurityMessage message, IExchangeInfoProvider exchangeInfoProvider)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (exchangeInfoProvider == null)
			throw new ArgumentNullException(nameof(exchangeInfoProvider));

		var security = new Security
		{
			Id = message.SecurityId == default ? null : message.SecurityId.ToStringId(nullIfEmpty: message is SecurityLookupMessage)
		};

		security.ApplyChanges(message, exchangeInfoProvider);

		return security;
	}

	/// <summary>
	/// To convert the portfolio into message.
	/// </summary>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="originalTransactionId">ID of original transaction, for which this message is the answer.</param>
	/// <returns>Message.</returns>
	public static PortfolioMessage ToMessage(this Portfolio portfolio, long originalTransactionId = 0)
	{
		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		return new PortfolioMessage
		{
			PortfolioName = portfolio.Name,
			BoardCode = portfolio.Board?.Code,
			Currency = portfolio.Currency,
			ClientCode = portfolio.ClientCode,
			OriginalTransactionId = originalTransactionId,
		};
	}

	/// <summary>
	/// To convert the portfolio into message.
	/// </summary>
	/// <param name="portfolio">Portfolio.</param>
	/// <returns>Message.</returns>
	public static PositionChangeMessage ToChangeMessage(this Portfolio portfolio)
	{
		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		return new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = portfolio.Name,
			BoardCode = portfolio.Board?.Code,
			LocalTime = portfolio.LocalTime,
			ServerTime = portfolio.LastChangeTime,
			ClientCode = portfolio.ClientCode,
		}
		.TryAdd(PositionChangeTypes.BeginValue, portfolio.BeginValue, true)
		.TryAdd(PositionChangeTypes.CurrentValue, portfolio.CurrentValue, true);
	}

	/// <summary>
	/// Convert <see cref="Portfolio"/> to <see cref="PortfolioLookupMessage"/> value.
	/// </summary>
	/// <param name="criteria">The criterion which fields will be used as a filter.</param>
	/// <returns>Message portfolio lookup for specified criteria.</returns>
	public static PortfolioLookupMessage ToLookupCriteria(this Portfolio criteria)
	{
		if (criteria == null)
			throw new ArgumentNullException(nameof(criteria));

		return new PortfolioLookupMessage
		{
			IsSubscribe = true,
			BoardCode = criteria.Board?.Code,
			Currency = criteria.Currency,
			PortfolioName = criteria.Name,
			ClientCode = criteria.ClientCode,
		};
	}

	/// <summary>
	/// Convert <see cref="Order"/> to <see cref="OrderStatusMessage"/> value.
	/// </summary>
	/// <param name="criteria">The criterion which fields will be used as a filter.</param>
	/// <param name="volume">Volume.</param>
	/// <param name="side">Order side.</param>
	/// <returns>A message requesting current registered orders and trades.</returns>
	public static OrderStatusMessage ToLookupCriteria(this Order criteria, decimal? volume, Sides? side)
	{
		if (criteria == null)
			throw new ArgumentNullException(nameof(criteria));

		var statusMsg = new OrderStatusMessage
		{
			IsSubscribe = true,
			PortfolioName = criteria.Portfolio?.Name,
			OrderId = criteria.Id,
			OrderStringId = criteria.StringId,
			OrderType = criteria.Type,
			UserOrderId = criteria.UserOrderId,
			StrategyId = criteria.StrategyId,
			BrokerCode = criteria.BrokerCode,
			ClientCode = criteria.ClientCode,
			Volume = volume,
			Side = side,
		};

		criteria.Security?.ToMessage().CopyTo(statusMsg);

		return statusMsg;
	}

	/// <summary>
	/// To convert the position into message.
	/// </summary>
	/// <param name="position">Position.</param>
	/// <param name="originalTransactionId">ID of original transaction, for which this message is the answer.</param>
	/// <returns>Message.</returns>
	public static PositionChangeMessage ToChangeMessage(this Position position, long originalTransactionId = 0)
	{
		if (position is null)
			throw new ArgumentNullException(nameof(position));

		return new PositionChangeMessage
		{
			LocalTime = position.LocalTime,
			ServerTime = position.LastChangeTime,
			PortfolioName = position.Portfolio.Name,
			SecurityId = position.Security.ToSecurityId(),
			ClientCode = position.ClientCode,
			StrategyId = position.StrategyId,
			Side = position.Side,
			OriginalTransactionId = originalTransactionId,
		}
		.TryAdd(PositionChangeTypes.BeginValue, position.BeginValue, true)
		.TryAdd(PositionChangeTypes.CurrentValue, position.CurrentValue, true)
		.TryAdd(PositionChangeTypes.BlockedValue, position.BlockedValue, true);
	}

	/// <summary>
	/// To convert the board into message.
	/// </summary>
	/// <param name="board">Board.</param>
	/// <param name="originalTransactionId">ID of original transaction, for which this message is the answer.</param>
	/// <returns>Message.</returns>
	public static BoardMessage ToMessage(this ExchangeBoard board, long originalTransactionId = 0)
	{
		if (board == null)
			throw new ArgumentNullException(nameof(board));

		return new BoardMessage
		{
			Code = board.Code,
			ExchangeCode = board.Exchange.Name,
			WorkingTime = board.WorkingTime.Clone(),
			//IsSupportMarketOrders = board.IsSupportMarketOrders,
			//IsSupportAtomicReRegister = board.IsSupportAtomicReRegister,
			ExpiryTime = board.ExpiryTime,
			TimeZone = board.TimeZone,
			OriginalTransactionId = originalTransactionId,
		};
	}

	/// <summary>
	/// To convert the message into exchange.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>Exchange.</returns>
	public static Exchange ToExchange(this BoardMessage message)
	{
		return message.ToExchange(new Exchange { Name = message.ExchangeCode });
	}

	/// <summary>
	/// To convert the message into exchange.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="exchange">Exchange.</param>
	/// <returns>Exchange.</returns>
	public static Exchange ToExchange(this BoardMessage message, Exchange exchange)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (exchange == null)
			throw new ArgumentNullException(nameof(exchange));

		return exchange;
	}

	/// <summary>
	/// To convert the message into board.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>Board.</returns>
	public static ExchangeBoard ToBoard(this BoardMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var board = new ExchangeBoard
		{
			Code = message.Code,
			Exchange = new Exchange { Name = message.ExchangeCode }
		};
		board.ApplyChanges(message);
		return board;
	}

	/// <summary>
	/// To convert the message into board.
	/// </summary>
	/// <param name="board">Board.</param>
	/// <param name="message">Message.</param>
	/// <returns>Board.</returns>
	public static ExchangeBoard ApplyChanges(this ExchangeBoard board, BoardMessage message)
	{
		if (board == null)
			throw new ArgumentNullException(nameof(board));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		board.WorkingTime = message.WorkingTime;
		//board.IsSupportAtomicReRegister = message.IsSupportAtomicReRegister;
		//board.IsSupportMarketOrders = message.IsSupportMarketOrders;

		if (message.ExpiryTime != default)
			board.ExpiryTime = message.ExpiryTime;

		if (message.TimeZone != null)
			board.TimeZone = message.TimeZone;

		return board;
	}

	/// <summary>
	/// To convert the message into order.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="security">Security.</param>
	/// <returns>Order.</returns>
	public static Order ToOrder(this ExecutionMessage message, Security security)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		return message.ToOrder(new Order { Security = security });
	}

	/// <summary>
	/// To convert the message into order.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="order">The order.</param>
	/// <returns>Order.</returns>
	public static Order ToOrder(this ExecutionMessage message, Order order)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		order.Id = message.OrderId;
		order.StringId = message.OrderStringId;
		order.TransactionId = message.TransactionId;
		order.Portfolio = new Portfolio { Board = order.Security.Board, Name = message.PortfolioName };
		order.Side = message.Side;
		order.Price = message.OrderPrice;
		order.Volume = message.OrderVolume ?? 0;
		order.Balance = message.Balance ?? 0;
		order.VisibleVolume = message.VisibleVolume;
		order.Type = message.OrderType;
		order.Status = message.OrderStatus;
		order.IsSystem = message.IsSystem;
		order.Time = message.ServerTime;
		order.ServerTime = message.ServerTime;
		order.LocalTime = message.LocalTime;
		order.TimeInForce = message.TimeInForce;
		order.ExpiryDate = message.ExpiryDate;
		order.UserOrderId = message.UserOrderId;
		order.StrategyId = message.StrategyId;
		order.Comment = message.Comment;
		order.Commission = message.Commission;
		order.CommissionCurrency = message.CommissionCurrency;
		order.Currency = message.Currency;
		order.IsMarketMaker = message.IsMarketMaker;
		order.MarginMode = message.MarginMode;
		order.Slippage = message.Slippage;
		order.IsManual = message.IsManual;
		order.AveragePrice = message.AveragePrice;
		order.Yield = message.Yield;
		order.MinVolume = message.MinVolume;
		order.PositionEffect = message.PositionEffect;
		order.PostOnly = message.PostOnly;
		order.SeqNum = message.SeqNum;
		order.Leverage = message.Leverage;

		if (message.OrderState != null)
			order.ApplyNewState(message.OrderState.Value);

		return order;
	}

	/// <summary>
	/// Check the possibility order's state change.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <param name="state">Current order's state.</param>
	/// <param name="logs">Logs.</param>
	public static void ApplyNewState(this Order order, OrderStates state, ILogReceiver logs = null)
	{
		((OrderStates?)order.State).VerifyOrderState(state, order.TransactionId, logs);
		order.State = state;
	}

	/// <summary>
	/// To convert news into message.
	/// </summary>
	/// <param name="news">News.</param>
	/// <returns>Message.</returns>
	public static NewsMessage ToMessage(this News news)
	{
		if (news == null)
			throw new ArgumentNullException(nameof(news));

		return new NewsMessage
		{
			LocalTime = news.LocalTime,
			ServerTime = news.ServerTime,
			Id = news.Id,
			Story = news.Story,
			Source = news.Source,
			Headline = news.Headline,
			SecurityId = news.Security?.ToSecurityId(),
			BoardCode = news.Board == null ? string.Empty : news.Board.Code,
			Url = news.Url,
			Priority = news.Priority,
			Language = news.Language,
			ExpiryDate = news.ExpiryDate,
			SeqNum = news.SeqNum,
		};
	}

	/// <summary>
	/// To convert the instrument into <see cref="SecurityId"/>.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="idGenerator">The instrument identifiers generator <see cref="Security.Id"/>.</param>
	/// <param name="boardIsRequired"><see cref="Security.Board"/> is required.</param>
	/// <param name="copyExtended">Copy <see cref="Security.ExternalId"/> and <see cref="Security.Type"/>.</param>
	/// <returns>Security ID.</returns>
	public static SecurityId ToSecurityId(this Security security, SecurityIdGenerator idGenerator = null, bool boardIsRequired = true, bool copyExtended = false)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (security.IsAllSecurity())
			return default;

		string secCode;
		string boardCode;

		// http://stocksharp.com/forum/yaf_postsm32581_Security-SPFB-RTS-FORTS.aspx#post32581
		// иногда в Security.Code может быть записано неправильное, и необходимо опираться на Security.Id
		if (!security.Id.IsEmpty())
		{
			var id = idGenerator.EnsureGetGenerator().Split(security.Id);

			secCode = id.SecurityCode;

			// http://stocksharp.com/forum/yaf_postst5143findunread_API-4-2-4-0-Nie-vystavliaiutsia-zaiavki-po-niekotorym-instrumientam-FORTS.aspx
			// для Quik необходимо соблюдение регистра в коде инструмента при выставлении заявок
			if (secCode.EqualsIgnoreCase(security.Code))
				secCode = security.Code;

			//if (!boardCode.EqualsIgnoreCase(ExchangeBoard.Test.Code))
			boardCode = id.BoardCode;
		}
		else
		{
			if (security.Code.IsEmpty())
			{
				if (!security.BasketCode.IsEmpty())
				{
					return new SecurityId
					{
						SecurityCode = security.BasketExpression.Replace('@', '_'),
						BoardCode = security.Board?.Code ?? SecurityId.AssociatedBoardCode
					};
				}

				throw new ArgumentException(LocalizedStrings.SecurityNotContainsId);
			}

			if (security.Board == null && boardIsRequired)
				throw new ArgumentException(LocalizedStrings.SecurityNotContainsBoard.Put(security.Code));

			secCode = security.Code;
			boardCode = security.Board?.Code;
		}

		SecurityId secId;

		if (copyExtended)
			secId = security.ExternalId.ToSecurityId(secCode, boardCode);
		else
		{
			secId = new()
			{
				SecurityCode = secCode,
				BoardCode = boardCode,
			};
		}

		// force hash code caching
		secId.GetHashCode();

		return secId;
	}

	/// <summary>
	/// Cast <see cref="SecurityId"/> to the <see cref="SecurityExternalId"/>.
	/// </summary>
	/// <param name="securityId"><see cref="SecurityId"/>.</param>
	/// <returns><see cref="SecurityExternalId"/>.</returns>
	public static SecurityExternalId ToExternalId(this SecurityId securityId)
	{
		return new SecurityExternalId
		{
			Bloomberg = securityId.Bloomberg,
			Cusip = securityId.Cusip,
			IQFeed = securityId.IQFeed,
			Isin = securityId.Isin,
			Ric = securityId.Ric,
			Sedol = securityId.Sedol,
			InteractiveBrokers = securityId.InteractiveBrokers,
			Plaza = securityId.Plaza,
		};
	}

	/// <summary>
	/// To check, if <see cref="SecurityId"/> contains identifiers of external sources.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <returns><see langword="true" />, if there are identifiers of external sources, otherwise, <see langword="false" />.</returns>
	public static bool HasExternalId(this SecurityId securityId)
	{
		return !securityId.Bloomberg.IsEmpty() ||
			   !securityId.Cusip.IsEmpty() ||
			   !securityId.IQFeed.IsEmpty() ||
			   !securityId.Isin.IsEmpty() ||
			   !securityId.Ric.IsEmpty() ||
			   !securityId.Sedol.IsEmpty() ||
			   securityId.InteractiveBrokers != default ||
			   !securityId.Plaza.IsEmpty();
	}

	/// <summary>
	/// Cast <see cref="SecurityExternalId"/> to the <see cref="SecurityId"/>.
	/// </summary>
	/// <param name="externalId"><see cref="SecurityExternalId"/>.</param>
	/// <param name="securityCode">Security code.</param>
	/// <param name="boardCode">Board code.</param>
	/// <returns><see cref="SecurityId"/>.</returns>
	public static SecurityId ToSecurityId(this SecurityExternalId externalId, string securityCode, string boardCode)
	{
		return externalId.ToSecurityId(new SecurityId
		{
			SecurityCode = securityCode,
			BoardCode = boardCode,
		});
	}

	/// <summary>
	/// Cast <see cref="SecurityExternalId"/> to the <see cref="SecurityId"/>.
	/// </summary>
	/// <param name="externalId"><see cref="SecurityExternalId"/>.</param>
	/// <param name="secId"><see cref="SecurityId"/>.</param>
	/// <returns><see cref="SecurityId"/>.</returns>
	public static SecurityId ToSecurityId(this SecurityExternalId externalId, SecurityId secId)
	{
		if (externalId == null)
			throw new ArgumentNullException(nameof(externalId));

		secId.Bloomberg = externalId.Bloomberg;
		secId.Cusip = externalId.Cusip;
		secId.IQFeed = externalId.IQFeed;
		secId.Isin = externalId.Isin;
		secId.Ric = externalId.Ric;
		secId.Sedol = externalId.Sedol;
		secId.InteractiveBrokers = externalId.InteractiveBrokers;
		secId.Plaza = externalId.Plaza;

		return secId;
	}

	/// <summary>
	/// Cast <see cref="NewsMessage"/> to the <see cref="News"/>.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <returns>News.</returns>
	public static News ToNews(this NewsMessage message, IExchangeInfoProvider exchangeInfoProvider)
	{
		return new News
		{
			Id = message.Id,
			Source = message.Source,
			ServerTime = message.ServerTime,
			Story = message.Story,
			Url = message.Url,
			Headline = message.Headline,
			Board = message.BoardCode.IsEmpty() ? null : exchangeInfoProvider?.GetOrCreateBoard(message.BoardCode),
			LocalTime = message.LocalTime,
			Priority = message.Priority,
			Language = message.Language,
			ExpiryDate = message.ExpiryDate,
			Security = message.SecurityId == null ? null : new Security
			{
				Id = message.SecurityId.Value.SecurityCode
			},
			SeqNum = message.SeqNum,
		};
	}

	/// <summary>
	/// Cast <see cref="PortfolioMessage"/> to the <see cref="Portfolio"/>.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <returns>Portfolio.</returns>
	public static Portfolio ToPortfolio(this PortfolioMessage message, Portfolio portfolio, IExchangeInfoProvider exchangeInfoProvider)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		if (!message.BoardCode.IsEmpty())
			portfolio.Board = exchangeInfoProvider.GetOrCreateBoard(message.BoardCode);

		if (message.Currency != null)
			portfolio.Currency = message.Currency;

		if (!message.ClientCode.IsEmpty())
			portfolio.ClientCode = message.ClientCode;

		//if (message.State != null)
		//	portfolio.State = message.State;

		return portfolio;
	}

	/// <summary>
	/// To get a board by its code. If board with the passed name does not exist, then it will be created.
	/// </summary>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="code">Board code.</param>
	/// <param name="createBoard">The handler creating a board, if it is not found. If the value is <see langword="null" />, then the board is created by default initialization.</param>
	/// <returns>Exchange board.</returns>
	public static ExchangeBoard GetOrCreateBoard(this IExchangeInfoProvider exchangeInfoProvider, string code, Func<string, ExchangeBoard> createBoard = null)
	{
		return exchangeInfoProvider.GetOrCreateBoard(code, out _, createBoard);
	}

	/// <summary>
	/// To get a board by its code. If board with the passed name does not exist, then it will be created.
	/// </summary>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="code">Board code.</param>
	/// <param name="isNew">Is newly created.</param>
	/// <param name="createBoard">The handler creating a board, if it is not found. If the value is <see langword="null" />, then the board is created by default initialization.</param>
	/// <returns>Exchange board.</returns>
	public static ExchangeBoard GetOrCreateBoard(this IExchangeInfoProvider exchangeInfoProvider, string code, out bool isNew, Func<string, ExchangeBoard> createBoard = null)
	{
		if (exchangeInfoProvider == null)
			throw new ArgumentNullException(nameof(exchangeInfoProvider));

		if (code.IsEmpty())
			throw new ArgumentNullException(nameof(code));

		isNew = false;

		//if (code.EqualsIgnoreCase("RTS"))
		//	return ExchangeBoard.Forts;

		var board = exchangeInfoProvider.TryGetExchangeBoard(code);

		if (board != null)
			return board;

		isNew = true;

		if (createBoard == null)
		{
			var exchange = exchangeInfoProvider.TryGetExchange(code);

			if (exchange == null)
			{
				exchange = new Exchange { Name = code };
				exchangeInfoProvider.Save(exchange);
			}

			board = new ExchangeBoard
			{
				Code = code,
				Exchange = exchange
			};
		}
		else
		{
			board = createBoard(code);

			if (exchangeInfoProvider.TryGetExchange(board.Exchange.Name) == null)
				exchangeInfoProvider.Save(board.Exchange);
		}

		exchangeInfoProvider.Save(board);

		return board;
	}

	/// <summary>
	/// Format data type into into human-readable string.
	/// </summary>
	/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
	/// <returns>String.</returns>
	public static string ToDataTypeString(this MarketDataMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var str = message.DataType2.MessageType.GetDisplayName();

		if (message.DataType2.IsCandles)
			str += " " + message.GetArg();

		return str;
	}

	/// <summary>
	/// Check if the specified security is <see cref="AllSecurity"/>.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <returns><see langword="true"/>, if the specified security is <see cref="AllSecurity"/>, otherwise, <see langword="false"/>.</returns>
	public static bool IsAllSecurity(this Security security)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		return security == AllSecurity || security.Id.EqualsIgnoreCase(AllSecurity.Id);
	}

	/// <summary>
	/// "All securities" instance.
	/// </summary>
	public static Security AllSecurity { get; } = new Security
	{
		Id = Messages.Extensions.AllSecurityId,
		Code = SecurityId.AssociatedBoardCode,
		//Class = task.GetDisplayName(),
		Name = LocalizedStrings.AllSecurities,
		Board = ExchangeBoard.Associated,
	};

	/// <summary>
	/// "News" security instance.
	/// </summary>
	public static readonly Security NewsSecurity = new() { Id = SecurityId.News.ToStringId() };

	/// <summary>
	/// "Money" security instance.
	/// </summary>
	public static readonly Security MoneySecurity = new() { Id = SecurityId.Money.ToStringId() };

	/// <summary>
	/// Apply changes to the portfolio object.
	/// </summary>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="message">Portfolio change message.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	public static void ApplyChanges(this Portfolio portfolio, PositionChangeMessage message, IExchangeInfoProvider exchangeInfoProvider)
	{
		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (exchangeInfoProvider == null)
			throw new ArgumentNullException(nameof(exchangeInfoProvider));

		if (!message.BoardCode.IsEmpty())
			portfolio.Board = exchangeInfoProvider.GetOrCreateBoard(message.BoardCode);

		if (!message.ClientCode.IsEmpty())
			portfolio.ClientCode = message.ClientCode;

		ApplyChanges(portfolio, message);
	}

	/// <summary>
	/// Apply changes to the position object.
	/// </summary>
	/// <param name="position">Position.</param>
	/// <param name="message">Position change message.</param>
	public static void ApplyChanges(this Position position, PositionChangeMessage message)
	{
		if (position == null)
			throw new ArgumentNullException(nameof(position));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var pf = position as Portfolio ?? position.Portfolio;

		foreach (var change in message.Changes)
		{
			try
			{
				switch (change.Key)
				{
					case PositionChangeTypes.BeginValue:
						position.BeginValue = (decimal)change.Value;
						break;
					case PositionChangeTypes.CurrentValue:
						position.CurrentValue = (decimal)change.Value;
						break;
					case PositionChangeTypes.BlockedValue:
						position.BlockedValue = (decimal)change.Value;
						break;
					case PositionChangeTypes.CurrentPrice:
						position.CurrentPrice = (decimal)change.Value;
						break;
					case PositionChangeTypes.AveragePrice:
						position.AveragePrice = (decimal)change.Value;
						break;
					case PositionChangeTypes.RealizedPnL:
						position.RealizedPnL = (decimal)change.Value;
						break;
					case PositionChangeTypes.UnrealizedPnL:
						position.UnrealizedPnL = (decimal)change.Value;
						break;
					case PositionChangeTypes.Commission:
						position.Commission = (decimal)change.Value;
						break;
					case PositionChangeTypes.VariationMargin:
						position.VariationMargin = (decimal)change.Value;
						break;
					case PositionChangeTypes.Currency:
						position.Currency = (CurrencyTypes)change.Value;
						break;
					case PositionChangeTypes.ExpirationDate:
						position.ExpirationDate = (DateTimeOffset)change.Value;
						break;
					case PositionChangeTypes.SettlementPrice:
						position.SettlementPrice = (decimal)change.Value;
						break;
					case PositionChangeTypes.Leverage:
						position.Leverage = (decimal)change.Value;
						break;
					case PositionChangeTypes.State:
						if (pf != null)
							pf.State = (PortfolioStates)change.Value;
						break;
					case PositionChangeTypes.CommissionMaker:
						position.CommissionMaker = (decimal)change.Value;
						break;
					case PositionChangeTypes.CommissionTaker:
						position.CommissionTaker = (decimal)change.Value;
						break;
					case PositionChangeTypes.BuyOrdersCount:
						position.BuyOrdersCount = (int)change.Value;
						break;
					case PositionChangeTypes.SellOrdersCount:
						position.SellOrdersCount = (int)change.Value;
						break;
					case PositionChangeTypes.BuyOrdersMargin:
						position.BuyOrdersMargin = (decimal)change.Value;
						break;
					case PositionChangeTypes.SellOrdersMargin:
						position.SellOrdersMargin = (decimal)change.Value;
						break;
					case PositionChangeTypes.OrdersMargin:
						position.OrdersMargin = (decimal)change.Value;
						break;
					case PositionChangeTypes.OrdersCount:
						position.OrdersCount = (int)change.Value;
						break;
					case PositionChangeTypes.TradesCount:
						position.TradesCount = (int)change.Value;
						break;
					case PositionChangeTypes.LiquidationPrice:
						position.LiquidationPrice = (decimal)change.Value;
						break;

						// skip unknown fields
						//default:
						//	throw new ArgumentOutOfRangeException(nameof(change), change.Key, LocalizedStrings.InvalidValue);
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(LocalizedStrings.CommandNotProcessedReason.Put(nameof(PositionChangeMessage), change.Key), ex);
			}
		}

		position.LocalTime = message.LocalTime;
		position.LastChangeTime = message.ServerTime;
	}

	/// <summary>
	/// Apply change to the security object.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="changes">Changes.</param>
	/// <param name="serverTime">Change server time.</param>
	/// <param name="localTime">Local timestamp when a message was received/created.</param>
	/// <param name="defaultHandler">Default handler.</param>
	public static void ApplyChanges(this Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTimeOffset localTime, Action<Security, Level1Fields, object> defaultHandler = null)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (changes == null)
			throw new ArgumentNullException(nameof(changes));

		var bidChanged = false;
		var askChanged = false;
		var lastTradeChanged = false;
		var bestBid = security.BestBid ?? new QuoteChange();
		var bestAsk = security.BestAsk ?? new QuoteChange();

		var lastTrade = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
		};

		var lastTick = security.LastTick;

		if (lastTick is not null)
		{
			lastTrade.TradePrice = lastTick.Price;
			lastTrade.TradeVolume = lastTick.Volume;
		}

		foreach (var pair in changes)
		{
			var value = pair.Value;

			try
			{
				switch (pair.Key)
				{
					case Level1Fields.OpenPrice:
						security.OpenPrice = (decimal)value;
						break;
					case Level1Fields.HighPrice:
						security.HighPrice = (decimal)value;
						break;
					case Level1Fields.LowPrice:
						security.LowPrice = (decimal)value;
						break;
					case Level1Fields.ClosePrice:
						security.ClosePrice = (decimal)value;
						break;
					case Level1Fields.StepPrice:
						security.StepPrice = (decimal)value;
						break;
					case Level1Fields.PriceStep:
						security.PriceStep = (decimal)value;
						break;
					case Level1Fields.Decimals:
						security.Decimals = (int)value;
						break;
					case Level1Fields.VolumeStep:
						security.VolumeStep = (decimal)value;
						break;
					case Level1Fields.Multiplier:
						security.Multiplier = (decimal)value;
						break;
					case Level1Fields.BestBidPrice:
						bestBid.Price = (decimal)value;
						bidChanged = true;
						break;
					case Level1Fields.BestBidVolume:
						bestBid.Volume = (decimal)value;
						bidChanged = true;
						break;
					case Level1Fields.BestAskPrice:
						bestAsk.Price = (decimal)value;
						askChanged = true;
						break;
					case Level1Fields.BestAskVolume:
						bestAsk.Volume = (decimal)value;
						askChanged = true;
						break;
					case Level1Fields.ImpliedVolatility:
						security.ImpliedVolatility = (decimal)value;
						break;
					case Level1Fields.HistoricalVolatility:
						security.HistoricalVolatility = (decimal)value;
						break;
					case Level1Fields.TheorPrice:
						security.TheorPrice = (decimal)value;
						break;
					case Level1Fields.Delta:
						security.Delta = (decimal)value;
						break;
					case Level1Fields.Gamma:
						security.Gamma = (decimal)value;
						break;
					case Level1Fields.Vega:
						security.Vega = (decimal)value;
						break;
					case Level1Fields.Theta:
						security.Theta = (decimal)value;
						break;
					case Level1Fields.Rho:
						security.Rho = (decimal)value;
						break;
					case Level1Fields.MarginBuy:
						security.MarginBuy = (decimal)value;
						break;
					case Level1Fields.MarginSell:
						security.MarginSell = (decimal)value;
						break;
					case Level1Fields.OpenInterest:
						security.OpenInterest = (decimal)value;
						break;
					case Level1Fields.MinPrice:
						security.MinPrice = (decimal)value;
						break;
					case Level1Fields.MaxPrice:
						security.MaxPrice = (decimal)value;
						break;
					case Level1Fields.BidsCount:
						security.BidsCount = (int)value;
						break;
					case Level1Fields.BidsVolume:
						security.BidsVolume = (decimal)value;
						break;
					case Level1Fields.AsksCount:
						security.AsksCount = (int)value;
						break;
					case Level1Fields.AsksVolume:
						security.AsksVolume = (decimal)value;
						break;
					case Level1Fields.State:
						security.State = (SecurityStates)value;
						break;
					case Level1Fields.LastTradePrice:
						lastTrade.TradePrice = (decimal)value;
						lastTradeChanged = true;
						break;
					case Level1Fields.LastTradeVolume:
						lastTrade.TradeVolume = (decimal)value;
						lastTradeChanged = true;
						break;
					case Level1Fields.LastTradeId:
						lastTrade.TradeId = (long)value;
						lastTradeChanged = true;
						break;
					case Level1Fields.LastTradeStringId:
						lastTrade.TradeStringId = (string)value;
						lastTradeChanged = true;
						break;
					case Level1Fields.LastTradeTime:
						lastTrade.ServerTime = (DateTimeOffset)value;
						lastTradeChanged = true;
						break;
					case Level1Fields.LastTradeUpDown:
						lastTrade.IsUpTick = (bool)value;
						lastTradeChanged = true;
						break;
					case Level1Fields.LastTradeOrigin:
						lastTrade.OriginSide = (Sides)value;
						lastTradeChanged = true;
						break;
					case Level1Fields.IsSystem:
						lastTrade.IsSystem = (bool)value;
						lastTradeChanged = true;
						break;
					case Level1Fields.TradesCount:
						security.TradesCount = (int)value;
						break;
					case Level1Fields.HighBidPrice:
						security.HighBidPrice = (decimal)value;
						break;
					case Level1Fields.LowAskPrice:
						security.LowAskPrice = (decimal)value;
						break;
					case Level1Fields.Yield:
						security.Yield = (decimal)value;
						break;
					case Level1Fields.VWAP:
						security.VWAP = (decimal)value;
						break;
					case Level1Fields.SettlementPrice:
						security.SettlementPrice = (decimal)value;
						break;
					case Level1Fields.AveragePrice:
						security.AveragePrice = (decimal)value;
						break;
					case Level1Fields.Volume:
						security.Volume = (decimal)value;
						break;
					case Level1Fields.Turnover:
						security.Turnover = (decimal)value;
						break;
					case Level1Fields.BuyBackPrice:
						security.BuyBackPrice = (decimal)value;
						break;
					case Level1Fields.BuyBackDate:
						security.BuyBackDate = (DateTimeOffset)value;
						break;
					case Level1Fields.CommissionTaker:
						security.CommissionTaker = (decimal)value;
						break;
					case Level1Fields.CommissionMaker:
						security.CommissionMaker = (decimal)value;
						break;
					case Level1Fields.MinVolume:
						security.MinVolume = (decimal)value;
						break;
					case Level1Fields.MaxVolume:
						security.MaxVolume = (decimal)value;
						break;
					case Level1Fields.UnderlyingMinVolume:
						security.UnderlyingSecurityMinVolume = (decimal)value;
						break;
					case Level1Fields.IssueSize:
						security.IssueSize = (decimal)value;
						break;
					default:
					{
						defaultHandler?.Invoke(security, pair.Key, pair.Value);
						break;
						//throw new ArgumentOutOfRangeException();
					}
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(LocalizedStrings.CommandNotProcessedReason.Put(nameof(Level1ChangeMessage), pair.Key), ex);
			}
		}

		if (bidChanged)
			security.BestBid = bestBid;

		if (askChanged)
			security.BestAsk = bestAsk;

		if (lastTradeChanged)
		{
			if (lastTrade.ServerTime == default)
				lastTrade.ServerTime = serverTime;

			lastTrade.LocalTime = localTime;

			security.LastTick = lastTrade;
		}

		security.LocalTime = localTime;
		security.LastChangeTime = serverTime;
	}

	/// <summary>
	/// Apply change to the security object.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="message">Changes.</param>
	public static void ApplyChanges(this Security security, Level1ChangeMessage message)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		security.ApplyChanges(message.Changes, message.ServerTime, message.LocalTime);
	}

	/// <summary>
	/// Apply change to the security object.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="message">Meta info.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="isOverride">Override previous security data by new values.</param>
	public static void ApplyChanges(this Security security, SecurityMessage message, IExchangeInfoProvider exchangeInfoProvider, bool isOverride = true)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (exchangeInfoProvider == null)
			throw new ArgumentNullException(nameof(exchangeInfoProvider));

		var secId = message.SecurityId;

		if (!secId.SecurityCode.IsEmpty())
		{
			if (isOverride || security.Code.IsEmpty())
				security.Code = secId.SecurityCode;
		}

		if (!secId.BoardCode.IsEmpty())
		{
			if (isOverride || security.Board == null)
				security.Board = exchangeInfoProvider.GetOrCreateBoard(secId.BoardCode);
		}

		if (message.Currency != null)
		{
			if (isOverride || security.Currency == null)
				security.Currency = message.Currency;
		}

		if (message.ExpiryDate != null)
		{
			if (isOverride || security.ExpiryDate == null)
				security.ExpiryDate = message.ExpiryDate;
		}

		if (message.VolumeStep != null)
		{
			if (isOverride || security.VolumeStep == null)
				security.VolumeStep = message.VolumeStep.Value;
		}

		if (message.MinVolume != null)
		{
			if (isOverride || security.MinVolume == null)
				security.MinVolume = message.MinVolume.Value;
		}

		if (message.MaxVolume != null)
		{
			if (isOverride || security.MaxVolume == null)
				security.MaxVolume = message.MaxVolume.Value;
		}

		if (message.Multiplier != null)
		{
			if (isOverride || security.Multiplier == null)
				security.Multiplier = message.Multiplier.Value;
		}

		if (message.PriceStep != null)
		{
			if (isOverride || security.PriceStep == null)
				security.PriceStep = message.PriceStep.Value;

			if (message.Decimals == null && security.Decimals == null)
				security.Decimals = message.PriceStep.Value.GetCachedDecimals();
		}

		if (message.Decimals != null)
		{
			if (isOverride || security.Decimals == null)
				security.Decimals = message.Decimals.Value;

			if (message.PriceStep == null && security.PriceStep == null)
				security.PriceStep = message.Decimals.Value.GetPriceStep();
		}

		if (!message.Name.IsEmpty())
		{
			if (isOverride || security.Name.IsEmpty())
				security.Name = message.Name;
		}

		if (!message.Class.IsEmpty())
		{
			if (isOverride || security.Class.IsEmpty())
				security.Class = message.Class;
		}

		if (message.OptionType != null)
		{
			if (isOverride || security.OptionType == null)
				security.OptionType = message.OptionType;
		}

		if (message.Strike != null)
		{
			if (isOverride || security.Strike == null)
				security.Strike = message.Strike.Value;
		}

		if (!message.BinaryOptionType.IsEmpty())
		{
			if (isOverride || security.BinaryOptionType == null)
				security.BinaryOptionType = message.BinaryOptionType;
		}

		if (message.SettlementDate != null)
		{
			if (isOverride || security.SettlementDate == null)
				security.SettlementDate = message.SettlementDate;
		}

		if (!message.ShortName.IsEmpty())
		{
			if (isOverride || security.ShortName.IsEmpty())
				security.ShortName = message.ShortName;
		}

		if (message.SecurityType != null)
		{
			if (isOverride || security.Type == null)
				security.Type = message.SecurityType.Value;
		}

		if (message.Shortable != null)
		{
			if (isOverride || security.Shortable == null)
				security.Shortable = message.Shortable.Value;
		}

		if (!message.CfiCode.IsEmpty())
		{
			if (isOverride || security.CfiCode.IsEmpty())
				security.CfiCode = message.CfiCode;

			security.Type ??= security.CfiCode.Iso10962ToSecurityType();

			if (security.Type == SecurityTypes.Option && security.OptionType == null)
			{
				security.OptionType = security.CfiCode.Iso10962ToOptionType();

				//if (security.CfiCode.Length > 2)
				//	security.BinaryOptionType = security.CfiCode.Substring(2);
			}
		}

		if (!message.GetUnderlyingCode().IsEmpty())
		{
			if (isOverride || security.UnderlyingSecurityId.IsEmpty())
				security.UnderlyingSecurityId = message.UnderlyingSecurityId.ToStringId(nullIfEmpty: true);
		}

		if (secId.HasExternalId())
		{
			if (isOverride || security.ExternalId.Equals(new SecurityExternalId()))
				security.ExternalId = secId.ToExternalId();
		}

		if (message.IssueDate != null)
		{
			if (isOverride || security.IssueDate == null)
				security.IssueDate = message.IssueDate.Value;
		}

		if (message.IssueSize != null)
		{
			if (isOverride || security.IssueSize == null)
				security.IssueSize = message.IssueSize.Value;
		}

		if (message.UnderlyingSecurityType != null)
		{
			if (isOverride || security.UnderlyingSecurityType == null)
				security.UnderlyingSecurityType = message.UnderlyingSecurityType.Value;
		}

		if (message.UnderlyingSecurityMinVolume != null)
		{
			if (isOverride || security.UnderlyingSecurityMinVolume == null)
				security.UnderlyingSecurityMinVolume = message.UnderlyingSecurityMinVolume.Value;
		}

		if (!message.BasketCode.IsEmpty())
		{
			if (isOverride || security.BasketCode.IsEmpty())
				security.BasketCode = message.BasketCode;
		}

		if (!message.BasketExpression.IsEmpty())
		{
			if (isOverride || security.BasketExpression.IsEmpty())
				security.BasketExpression = message.BasketExpression;
		}

		if (message.FaceValue != null)
		{
			if (isOverride || security.FaceValue == null)
				security.FaceValue = message.FaceValue;
		}

		if (message.OptionStyle != null)
		{
			if (isOverride || security.OptionStyle == null)
				security.OptionStyle = message.OptionStyle;
		}

		if (message.SettlementType != null)
		{
			if (isOverride || security.SettlementType == null)
				security.SettlementType = message.SettlementType;
		}

		if (message.PrimaryId != default)
		{
			if (isOverride || security.PrimaryId == default)
				security.PrimaryId = message.PrimaryId.ToStringId();
		}
	}

	/// <summary>
	/// To filter orders for the given portfolio.
	/// </summary>
	/// <param name="orders">All orders, in which the required shall be searched for.</param>
	/// <param name="portfolio">The portfolio, for which the orders shall be filtered.</param>
	/// <returns>Filtered orders.</returns>
	public static IEnumerable<Order> Filter(this IEnumerable<Order> orders, Portfolio portfolio)
	{
		if (orders == null)
			throw new ArgumentNullException(nameof(orders));

		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		return orders.Where(p => p.Portfolio == portfolio);
	}

	/// <summary>
	/// To filter orders for the given condition.
	/// </summary>
	/// <param name="orders">All orders, in which the required shall be searched for.</param>
	/// <param name="state">Order state.</param>
	/// <returns>Filtered orders.</returns>
	public static IEnumerable<Order> Filter(this IEnumerable<Order> orders, OrderStates state)
	{
		if (orders == null)
			throw new ArgumentNullException(nameof(orders));

		return orders.Where(p => p.State == state);
	}

	/// <summary>
	/// To filter orders for the given direction.
	/// </summary>
	/// <param name="orders">All orders, in which the required shall be searched for.</param>
	/// <param name="side">Order side.</param>
	/// <returns>Filtered orders.</returns>
	public static IEnumerable<Order> Filter(this IEnumerable<Order> orders, Sides side)
	{
		if (orders == null)
			throw new ArgumentNullException(nameof(orders));

		return orders.Where(p => p.Side == side);
	}

	/// <summary>
	/// To filter positions for the given portfolio.
	/// </summary>
	/// <param name="positions">All positions, in which the required shall be searched for.</param>
	/// <param name="portfolio">The portfolio, for which positions shall be filtered.</param>
	/// <returns>Filtered positions.</returns>
	public static IEnumerable<Position> Filter(this IEnumerable<Position> positions, Portfolio portfolio)
	{
		if (positions == null)
			throw new ArgumentNullException(nameof(positions));

		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		return positions.Where(p => p.Portfolio == portfolio);
	}

	/// <summary>
	/// To filter own trades for the given portfolio.
	/// </summary>
	/// <param name="myTrades">All own trades, in which the required shall be looked for.</param>
	/// <param name="portfolio">The portfolio, for which the trades shall be filtered.</param>
	/// <returns>Filtered trades.</returns>
	public static IEnumerable<MyTrade> Filter(this IEnumerable<MyTrade> myTrades, Portfolio portfolio)
	{
		if (myTrades == null)
			throw new ArgumentNullException(nameof(myTrades));

		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		return myTrades.Where(t => t.Order.Portfolio == portfolio);
	}

	/// <summary>
	/// To filter own trades for the given order.
	/// </summary>
	/// <param name="myTrades">All own trades, in which the required shall be looked for.</param>
	/// <param name="order">The order, for which trades shall be filtered.</param>
	/// <returns>Filtered orders.</returns>
	public static IEnumerable<MyTrade> Filter(this IEnumerable<MyTrade> myTrades, Order order)
	{
		if (myTrades == null)
			throw new ArgumentNullException(nameof(myTrades));

		if (order == null)
			throw new ArgumentNullException(nameof(order));

		return myTrades.Where(t => t.Order == order);
	}

	/// <summary>
	/// To filter instruments by the trading board.
	/// </summary>
	/// <param name="securities">Securities.</param>
	/// <param name="board">Trading board.</param>
	/// <returns>Instruments filtered.</returns>
	public static IEnumerable<Security> Filter(this IEnumerable<Security> securities, ExchangeBoard board)
	{
		if (securities == null)
			throw new ArgumentNullException(nameof(securities));

		if (board == null)
			throw new ArgumentNullException(nameof(board));

		return securities.Where(s => s.Board == board);
	}

	/// <summary>
	/// To filter instruments by the given criteria.
	/// </summary>
	/// <param name="securities">Securities.</param>
	/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
	/// <returns>Instruments filtered.</returns>
	public static IEnumerable<Security> Filter(this IEnumerable<Security> securities, Security criteria)
	{
		return securities.Filter(criteria.ToLookupMessage());
	}

	/// <summary>
	/// To filter instruments by the given criteria.
	/// </summary>
	/// <param name="securities">Securities.</param>
	/// <param name="criteria">Message security lookup for specified criteria.</param>
	/// <returns>Instruments filtered.</returns>
	public static IEnumerable<Security> Filter(this IEnumerable<Security> securities, SecurityLookupMessage criteria)
	{
		if (securities == null)
			throw new ArgumentNullException(nameof(securities));

		if (criteria.IsLookupAll())
			return [.. securities.TryLimitByCount(criteria)];

		var dict = securities.ToDictionary(s => s.ToMessage(), s => s);
		return [.. dict.Keys.Filter(criteria).TryLimitByCount(criteria).Select(m => dict[m])];
	}

	/// <summary>
	/// To get date of day T +/- of N trading days.
	/// </summary>
	/// <param name="board">Board info.</param>
	/// <param name="date">The start T date, to which are added or subtracted N trading days.</param>
	/// <param name="n">The N size. The number of trading days for the addition or subtraction.</param>
	/// <param name="checkHolidays">Whether to check the passed date for a weekday (Saturday and Sunday are days off, returned value for them is <see langword="false" />).</param>
	/// <returns>The end T +/- N date.</returns>
	public static DateTimeOffset AddOrSubtractTradingDays(this ExchangeBoard board, DateTimeOffset date, int n, bool checkHolidays = true)
		=> board.ToMessage().AddOrSubtractTradingDays(date, n, checkHolidays);

	/// <summary>
	/// To get the expiration date for <see cref="ExchangeBoard.Forts"/>.
	/// </summary>
	/// <param name="from">The start of the expiration range.</param>
	/// <param name="to">The end of the expiration range.</param>
	/// <returns>Expiration dates.</returns>
	public static IEnumerable<DateTimeOffset> GetExpiryDates(this DateTime from, DateTime to)
	{
		if (from > to)
			throw new InvalidOperationException(LocalizedStrings.StartCannotBeMoreEnd.Put(from, to));

		var board = ExchangeBoard.Forts.ToMessage();

		for (var year = from.Year; year <= to.Year; year++)
		{
			var monthFrom = year == from.Year ? from.Month : 1;
			var monthTo = year == to.Year ? to.Month : 12;

			for (var month = monthFrom; month <= monthTo; month++)
			{
				switch (month)
				{
					case 3:
					case 6:
					case 9:
					case 12:
					{
						var dt = new DateTime(year, month, 15).ApplyTimeZone(board.TimeZone);

						while (!board.IsTradeDate(dt))
						{
							dt = dt.AddDays(1);
						}
						yield return dt;
						break;
					}

					default:
						continue;
				}
			}
		}
	}

	/// <summary>
	/// Filter boards by code criteria.
	/// </summary>
	/// <param name="provider">The exchange boards provider.</param>
	/// <param name="criteria">Criteria.</param>
	/// <returns>Found boards.</returns>
	public static IEnumerable<ExchangeBoard> LookupBoards(this IExchangeInfoProvider provider, BoardLookupMessage criteria)
	{
		if (provider == null)
			throw new ArgumentNullException(nameof(provider));

		return provider.Boards.Filter(criteria);
	}

	/// <summary>
	/// Filter boards by code criteria.
	/// </summary>
	/// <param name="provider">The exchange boards provider.</param>
	/// <param name="criteria">Criteria.</param>
	/// <returns>Found boards.</returns>
	public static IEnumerable<BoardMessage> LookupBoards2(this IExchangeInfoProvider provider, BoardLookupMessage criteria)
	{
		if (provider == null)
			throw new ArgumentNullException(nameof(provider));

		return provider.Boards.Select(b => b.ToMessage(criteria.TransactionId)).Filter(criteria);
	}

	/// <summary>
	/// Filter boards by code criteria.
	/// </summary>
	/// <param name="boards">All boards.</param>
	/// <param name="criteria">Criteria.</param>
	/// <returns>Found boards.</returns>
	public static IEnumerable<ExchangeBoard> Filter(this IEnumerable<ExchangeBoard> boards, BoardLookupMessage criteria)
		=> boards.Where(b => b.ToMessage().IsMatch(criteria));

	/// <summary>
	/// Filter portfolios by the specified criteria.
	/// </summary>
	/// <param name="portfolios">All portfolios.</param>
	/// <param name="criteria">Criteria.</param>
	/// <returns>Found portfolios.</returns>
	public static IEnumerable<Portfolio> Filter(this IEnumerable<Portfolio> portfolios, PortfolioLookupMessage criteria)
		=> portfolios.Where(p => p.ToMessage().IsMatch(criteria, false));

	/// <summary>
	/// Filter positions the specified criteria.
	/// </summary>
	/// <param name="positions">All positions.</param>
	/// <param name="criteria">Criteria.</param>
	/// <returns>Found positions.</returns>
	public static IEnumerable<Position> Filter(this IEnumerable<Position> positions, PortfolioLookupMessage criteria)
		=> positions.Where(p => p.ToChangeMessage().IsMatch(criteria, false));

	/// <summary>
	/// <see cref="ISecurityProvider"/>
	/// </summary>
	public static ISecurityProvider TrySecurityProvider => ConfigManager.TryGetService<ISecurityProvider>();

	/// <summary>
	/// Try to get the security for the specified subscription.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="provider"><see cref="ISecurityProvider"/></param>
	/// <returns><see cref="Security"/></returns>
	public static Security TryGetSecurity(this Subscription subscription, ISecurityProvider provider = null)
	{
		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		if (subscription.SecurityId is not SecurityId secId)
			return null;

		return (provider ?? TrySecurityProvider)?.LookupById(secId);
	}

	/// <summary>
	/// To get the weighted mean price of matching by own trades.
	/// </summary>
	/// <param name="trades">Trades, for which the weighted mean price of matching shall be got.</param>
	/// <returns>The weighted mean price. If no trades, 0 is returned.</returns>
	public static decimal GetAveragePrice(this IEnumerable<MyTrade> trades)
	{
		if (trades == null)
			throw new ArgumentNullException(nameof(trades));

		var numerator = 0m;
		var denominator = 0m;
		var currentAvgPrice = 0m;

		foreach (var myTrade in trades)
		{
			var order = myTrade.Order;
			var trade = myTrade.Trade;

			var direction = (order.Side == Sides.Buy) ? 1m : -1m;

			//Если открываемся или переворачиваемся
			if (direction != denominator.Sign() && trade.Volume > denominator.Abs())
			{
				var newVolume = trade.Volume - denominator.Abs();
				numerator = direction * trade.Price * newVolume;
				denominator = direction * newVolume;
			}
			else
			{
				//Если добавляемся в сторону уже открытой позиции
				if (direction == denominator.Sign())
					numerator += direction * trade.Price * trade.Volume;
				else
					numerator += direction * currentAvgPrice * trade.Volume;

				denominator += direction * trade.Volume;
			}

			currentAvgPrice = (denominator != 0) ? numerator / denominator : 0m;
		}

		return currentAvgPrice;
	}

	/// <summary>
	/// Find subscriptions for the specified security and data type.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="security">Security.</param>
	/// <param name="dataType">Data type info.</param>
	/// <returns>Subscriptions.</returns>
	public static IEnumerable<Subscription> FindSubscriptions(this ISubscriptionProvider provider, Security security, DataType dataType)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		return provider.FindSubscriptions(security.ToSecurityId(), dataType);
	}

	/// <summary>
	/// Find subscriptions for the specified security and data type.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="securityId"><see cref="SecurityId"/></param>
	/// <param name="dataType">Data type info.</param>
	/// <returns>Subscriptions.</returns>
	public static IEnumerable<Subscription> FindSubscriptions(this ISubscriptionProvider provider, SecurityId securityId, DataType dataType)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		return provider.Subscriptions.Where(s => s.DataType == dataType && s.SecurityId == securityId);
	}

	/// <summary>
	/// Request news story subscription.
	/// </summary>
	/// <param name="provider">Subscription provider.</param>
	/// <param name="news">News item to subscribe to.</param>
	public static void RequestNewsStory(this ISubscriptionProvider provider, News news)
	{
		if (news is null)
			throw new ArgumentNullException(nameof(news));

		provider.Subscribe(new(new MarketDataMessage
		{
			DataType2 = DataType.News,
			IsSubscribe = true,
			NewsId = news.Id.To<string>(),
		}));
	}
}