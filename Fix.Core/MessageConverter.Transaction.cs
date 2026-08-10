namespace StockSharp.Fix;

/// <summary>
/// Transaction message converters: NewOrderSingle, OrderCancel, OrderReplace, etc.
/// </summary>
partial class MessageConverter
{
	/// <inheritdoc />
	public virtual OrderRegisterMessage ToMessage(FixNewOrderSingle fix)
	{
		// A NewOrderSingle without a valid Side (tag 54) or OrdType (tag 40)
		// must be rejected, not silently coerced into Buy/Limit (which would
		// route an order on the wrong side or as the wrong type).
		if (GetSide(fix.Side) is not { } side)
			throw new InvalidOperationException("NewOrderSingle: Side (tag 54) is missing or invalid.");

		if (GetOrderType(fix.OrdType) is not { } orderType)
			throw new InvalidOperationException("NewOrderSingle: OrdType (tag 40) is missing or invalid.");

		var msg = new OrderRegisterMessage
		{
			TransactionId = fix.ClOrdId.ToLong(),
			PortfolioName = fix.Account,
			Price = fix.Price ?? 0,
			Volume = fix.OrderQty ?? 0,
			VisibleVolume = fix.MaxFloor,
			Side = side,
			OrderType = orderType,
			TimeInForce = GetTimeInForce(fix.TimeInForce),
			TillDate = GetTillDate(fix.TimeInForce, fix.ExpiryDate),
			Comment = fix.Text,
			Condition = fix.Condition,
			Slippage = fix.Slippage,
			IsManual = fix.IsManual,
			MinOrderVolume = fix.MinQty,
			PositionEffect = GetPositionEffect(fix.PositionEffect),
			PostOnly = fix.PostOnly,
			StrategyId = fix.StrategyId,
			Leverage = fix.Leverage,
			MarginMode = GetMarginMode(fix.CashMargin),
			SecurityId = new SecurityId
			{
				SecurityCode = fix.Symbol,
				BoardCode = fix.SecurityExchange,
			},
			SecurityType = GetSecurityType(fix.SecurityType),
			CfiCode = fix.CfiCode,
		};

		if (!fix.SecondaryOrderId.IsEmpty())
			msg.UserOrderId = fix.SecondaryOrderId;

		// Handle party information for ClientCode and BrokerCode
		if (fix.Parties != null)
		{
			foreach (var party in fix.Parties)
			{
				switch (party.PartyRole)
				{
					case PartyRole.ClientId:
						msg.ClientCode = party.PartyId;
						break;
					case PartyRole.EnteringFirm:
						msg.BrokerCode = party.PartyId;
						break;
				}
			}
		}

		// Handle market maker flag
		if (FixExtensions.IsMarketMaker(fix.OrderCapacity, fix.OrderRestrictions))
			msg.IsMarketMaker = true;

		return msg;
	}

	/// <inheritdoc />
	public virtual FixNewOrderSingle ToFixNewOrderSingle(OrderRegisterMessage message)
	{
		return new FixNewOrderSingle(
			ClOrdId: FixId.FromLong(message.TransactionId),
			Account: message.PortfolioName,
			Price: message.Price,
			OrderQty: message.Volume,
			MaxFloor: message.VisibleVolume,
			SecurityType: ToFixSecurityType(message.SecurityType),
			CfiCode: message.CfiCode,
			Symbol: message.SecurityId.SecurityCode,
			SecurityExchange: message.SecurityId.BoardCode,
			Side: ToFixSide(message.Side),
			OrdType: ToFixOrderType(message.OrderType, message.Condition),
			TimeInForce: ToFixTimeInForce(message.TimeInForce, message.TillDate),
			ExpiryDate: message.TillDate,
			Condition: message.Condition,
			Text: message.Comment,
			Parties: BuildParties(message.ClientCode, message.BrokerCode),
			OrderCapacity: null,
			OrderRestrictions: null,
			CashMargin: ToFixCashMargin(message.MarginMode),
			Slippage: message.Slippage,
			IsManual: message.IsManual,
			MinQty: message.MinOrderVolume,
			PositionEffect: ToFixPositionEffect(message.PositionEffect),
			PostOnly: message.PostOnly,
			SecondaryOrderId: message.UserOrderId,
			StrategyId: message.StrategyId,
			Leverage: message.Leverage);
	}

	/// <inheritdoc />
	public virtual OrderCancelMessage ToMessage(FixOrderCancelRequest fix)
	{
		var msg = new OrderCancelMessage
		{
			TransactionId = fix.ClOrdId.ToLong(),
			OriginalTransactionId = fix.OrigClOrdId.ToLongN() ?? 0,
			PortfolioName = fix.Account,
			Balance = fix.Balance,
			Volume = fix.OrderQty,
			MarginMode = GetMarginMode(fix.CashMargin),
			Comment = fix.Text,
			CfiCode = fix.CfiCode,
			Side = GetSide(fix.Side),
			SecurityId = new SecurityId
			{
				SecurityCode = fix.Symbol,
				BoardCode = fix.SecurityExchange,
			},
			SecurityType = GetSecurityType(fix.SecurityType),
		};

		// Handle order ID (numeric vs string)
		if (!long.TryParse(fix.OrderId, out var orderNumId))
			msg.OrderStringId = fix.OrderId;
		else
			msg.OrderId = orderNumId;

		// Apply order type using extension
		if (fix.OrdType != null)
			msg.SetOrderType(fix.OrdType.Value);

		// Handle party information for ClientCode and BrokerCode
		if (fix.Parties != null)
		{
			foreach (var party in fix.Parties)
			{
				switch (party.PartyRole)
				{
					case PartyRole.ClientId:
						msg.ClientCode = party.PartyId;
						break;
					case PartyRole.EnteringFirm:
						msg.BrokerCode = party.PartyId;
						break;
				}
			}
		}

		return msg;
	}

	/// <inheritdoc />
	public virtual FixOrderCancelRequest ToFixOrderCancelRequest(OrderCancelMessage message)
	{
		return new FixOrderCancelRequest(
			ClOrdId: FixId.FromLong(message.TransactionId),
			OrigClOrdId: message.OriginalTransactionId != 0 ? FixId.FromLong(message.OriginalTransactionId) : default,
			OrderId: message.OrderId?.To<string>() ?? (message.OrderStringId.IsEmpty() ? null : message.OrderStringId),
			Account: message.PortfolioName,
			Balance: message.Balance,
			OrderQty: message.Volume,
			SecurityType: ToFixSecurityType(message.SecurityType),
			// Decode reads both back into CfiCode/Comment, so encoding null dropped instrument
			// identification (CFI) and the operator cancel reason on the wire.
			CfiCode: message.CfiCode,
			Symbol: message.SecurityId.SecurityCode,
			SecurityExchange: message.SecurityId.BoardCode,
			Text: message.Comment,
			OrdType: ToFixOrderType(message.OrderType, null),
			CashMargin: ToFixCashMargin(message.MarginMode),
			Side: ToFixSide(message.Side),
			Parties: BuildParties(message.ClientCode, message.BrokerCode));
	}

	/// <inheritdoc />
	public virtual OrderReplaceMessage ToMessage(FixOrderCancelReplaceRequest fix)
	{
		var msg = new OrderReplaceMessage
		{
			TransactionId = fix.ClOrdId.ToLong(),
			OriginalTransactionId = fix.OrigClOrdId.ToLongN() ?? 0,
			PortfolioName = fix.Account,
			Price = fix.Price ?? 0,
			Volume = fix.OrderQty ?? 0,
			OldOrderPrice = fix.OldPrice,
			OldOrderVolume = fix.OldVolume,
			Slippage = fix.Slippage,
			IsManual = fix.IsManual,
			MinOrderVolume = fix.MinQty,
			PositionEffect = GetPositionEffect(fix.PositionEffect),
			PostOnly = fix.PostOnly,
			StrategyId = fix.StrategyId,
			Leverage = fix.Leverage,
			MarginMode = GetMarginMode(fix.CashMargin),
			CfiCode = fix.CfiCode,
			TimeInForce = GetTimeInForce(fix.TimeInForce),
			TillDate = GetTillDate(fix.TimeInForce, fix.ExpiryDate),
			VisibleVolume = fix.MaxFloor,
			Comment = fix.Text,
			Condition = fix.Condition,
			SecurityId = new SecurityId
			{
				SecurityCode = fix.Symbol,
				BoardCode = fix.SecurityExchange,
			},
			SecurityType = GetSecurityType(fix.SecurityType),
		};

		// Side is non-nullable on the message; only overwrite the default when the wire carried it.
		if (GetSide(fix.Side) is { } replaceSide)
			msg.Side = replaceSide;

		// Handle old order ID (numeric vs string)
		if (!long.TryParse(fix.OrderId, out var orderNumId))
			msg.OldOrderStringId = fix.OrderId;
		else
			msg.OldOrderId = orderNumId;

		if (!fix.SecondaryOrderId.IsEmpty())
			msg.UserOrderId = fix.SecondaryOrderId;

		// Handle party information for ClientCode and BrokerCode
		if (fix.Parties != null)
		{
			foreach (var party in fix.Parties)
			{
				switch (party.PartyRole)
				{
					case PartyRole.ClientId:
						msg.ClientCode = party.PartyId;
						break;
					case PartyRole.EnteringFirm:
						msg.BrokerCode = party.PartyId;
						break;
				}
			}
		}

		return msg;
	}

	/// <inheritdoc />
	public virtual FixOrderCancelReplaceRequest ToFixOrderCancelReplaceRequest(OrderReplaceMessage message)
	{
		return new FixOrderCancelReplaceRequest(
			ClOrdId: FixId.FromLong(message.TransactionId),
			OrigClOrdId: message.OriginalTransactionId != 0 ? FixId.FromLong(message.OriginalTransactionId) : default,
			// Fall back to the string exchange id when there is no numeric one, mirroring cancel.
			// Dropping it left a replace on an alphanumeric-id venue with no reference to the order.
			OrderId: message.OldOrderId?.To<string>() ?? (message.OldOrderStringId.IsEmpty() ? null : message.OldOrderStringId),
			Account: message.PortfolioName,
			Price: message.Price,
			OrderQty: message.Volume,
			SecurityType: ToFixSecurityType(message.SecurityType),
			CfiCode: null,
			Symbol: message.SecurityId.SecurityCode,
			SecurityExchange: message.SecurityId.BoardCode,
			CashMargin: ToFixCashMargin(message.MarginMode),
			Slippage: message.Slippage,
			IsManual: message.IsManual,
			MinQty: message.MinOrderVolume,
			PositionEffect: ToFixPositionEffect(message.PositionEffect),
			PostOnly: message.PostOnly,
			SecondaryOrderId: message.UserOrderId,
			StrategyId: message.StrategyId,
			OldPrice: message.OldOrderPrice,
			OldVolume: message.OldOrderVolume,
			Leverage: message.Leverage,
			Parties: BuildParties(message.ClientCode, message.BrokerCode),
			Side: ToFixSide(message.Side),
			TimeInForce: ToFixTimeInForce(message.TimeInForce, message.TillDate),
			ExpiryDate: message.TillDate,
			MaxFloor: message.VisibleVolume,
			Text: message.Comment,
			Condition: message.Condition);
	}

	/// <inheritdoc />
	public virtual OrderGroupCancelMessage ToMessage(FixOrderMassCancelRequest fix)
	{
		var msg = new OrderGroupCancelMessage
		{
			TransactionId = fix.ClOrdId.ToLong(),
			PortfolioName = fix.Account,
			Side = GetSide(fix.Side),
			Mode = fix.Mode,
			CfiCode = fix.CfiCode,
			SecurityId = new SecurityId
			{
				SecurityCode = fix.Symbol,
				BoardCode = fix.SecurityExchange,
			},
			SecurityType = GetSecurityType(fix.SecurityType),
			SecurityTypes = fix.SecurityTypes?
				.Select(t => t.FromFixType())
				.Where(t => t is not null)
				.Select(t => t.Value)
				.ToArray(),
		};

		// Handle party information for ClientCode and BrokerCode
		if (fix.Parties != null)
		{
			foreach (var party in fix.Parties)
			{
				switch (party.PartyRole)
				{
					case PartyRole.ClientId:
						msg.ClientCode = party.PartyId;
						break;
					case PartyRole.EnteringFirm:
						msg.BrokerCode = party.PartyId;
						break;
				}
			}
		}

		return msg;
	}

	/// <inheritdoc />
	public virtual FixOrderMassCancelRequest ToFixOrderMassCancelRequest(OrderGroupCancelMessage message)
	{
		return new FixOrderMassCancelRequest(
			ClOrdId: FixId.FromLong(message.TransactionId),
			Account: message.PortfolioName,
			SecurityType: ToFixSecurityType(message.SecurityType),
			// Preserve the per-type filter — dropping it silently widened the cancel
			// scope from "these security types" to "all orders" on the wire.
			SecurityTypes: message.SecurityTypes?.Select(t => ToFixSecurityType(t)).Where(s => !s.IsEmpty()).ToArray(),
			CfiCode: null,
			Symbol: message.SecurityId.SecurityCode,
			SecurityExchange: message.SecurityId.BoardCode,
			Side: ToFixSide(message.Side),
			MassCancelRequestType: GetMassCancelRequestType(message),
			Mode: message.Mode,
			Parties: BuildParties(message.ClientCode, message.BrokerCode));
	}

	// Derives the FIX MassCancelRequestType (tag 530) from the cancel scope so the request
	// is well-formed (the field is required) and reflects the security-type / security
	// filter instead of defaulting to "all orders": '9' = for a security type,
	// '1' = for a security, '7' = all orders.
	private static char GetMassCancelRequestType(OrderGroupCancelMessage message)
	{
		if (message.SecurityTypes?.Length > 0 || message.SecurityType != null)
			return '9';

		if (!message.SecurityId.SecurityCode.IsEmpty())
			return '1';

		return '7';
	}

	/// <inheritdoc />
	public virtual OrderStatusMessage ToMessage(FixOrderStatusRequest fix)
	{
		var msg = new OrderStatusMessage
		{
			TransactionId = fix.OrdStatusReqId.ToLong(),
			OrderId = fix.OrderId,
			IsSubscribe = GetIsSubscribe(fix.SubscriptionRequestType),
		};

		if (!fix.OrderStringId.IsEmpty())
			msg.OrderStringId = fix.OrderStringId;

		// ClOrdId can be numeric (OriginalTransactionId) or string (OrderStringId)
		if (!fix.ClOrdId.IsEmpty())
		{
			if (long.TryParse(fix.ClOrdId, out var origTransId))
				msg.OriginalTransactionId = origTransId;
			else
				msg.OrderStringId = fix.ClOrdId;
		}

		return msg;
	}

	/// <inheritdoc />
	public virtual FixOrderStatusRequest ToFixOrderStatusRequest(OrderStatusMessage message)
	{
		// ClOrdId from OriginalTransactionId (numeric) or OrderStringId (string)
		string clOrdId = null;
		if (message.OriginalTransactionId != 0)
			clOrdId = message.OriginalTransactionId.To<string>();
		else if (!message.OrderStringId.IsEmpty())
			clOrdId = message.OrderStringId;

		return new FixOrderStatusRequest(
			OrdStatusReqId: FixId.FromLong(message.TransactionId),
			SubscriptionRequestType: ToFixSubscriptionType(message.IsSubscribe),
			OrderId: message.OrderId,
			OrderStringId: message.OrderStringId.IsEmpty() ? null : message.OrderStringId,
			ClOrdId: clOrdId);
	}

	/// <inheritdoc />
	public virtual OrderStatusMessage ToMessage(FixOrderMassStatusRequest fix)
	{
		var msg = new OrderStatusMessage
		{
			TransactionId = fix.MassStatusReqId.ToLong(),
			IsSubscribe = GetIsSubscribe(fix.SubscriptionRequestType),
			PortfolioName = fix.Account,
			SecurityId = new SecurityId
			{
				SecurityCode = fix.Symbol,
				BoardCode = fix.SecurityExchange,
			},
			IsIncremental = fix.IsIncremental ?? false,
			UserId = fix.UserId,
			Skip = fix.Skip,
		};

		if (!fix.OrigClOrdId.IsEmpty())
		{
			// On unsubscribe OrigClOrdId carries the id of the subscribe request being
			// torn down. The numeric form is the contract; a non-numeric id falls back
			// to OrderStringId.
			if (!msg.IsSubscribe && long.TryParse(fix.OrigClOrdId, out var origTxId))
				msg.OriginalTransactionId = origTxId;
			else
				msg.OrderStringId = fix.OrigClOrdId;
		}

		if (fix.SecurityIds != null && fix.SecurityIds.Length > 0)
			msg.SecurityIds = fix.SecurityIds;

		return msg;
	}

	/// <inheritdoc />
	public virtual FixOrderMassStatusRequest ToFixOrderMassStatusRequest(OrderStatusMessage message)
	{
		// On unsubscribe MassStatusReqId is a fresh id and OrigClOrdId references the
		// subscribe request being torn down, mirroring how OrderCancelRequest uses
		// OrigClOrdId to reference the registering transaction.
		FixId origReqId = !message.IsSubscribe && message.OriginalTransactionId != 0
			? FixId.FromLong(message.OriginalTransactionId)
			: (FixId)message.OrderStringId;

		return new FixOrderMassStatusRequest(
			MassStatusReqId: FixId.FromLong(message.TransactionId),
			OrigClOrdId: origReqId,
			SubscriptionRequestType: ToFixSubscriptionType(message.IsSubscribe),
			OrdStatus: null,
			Symbol: message.SecurityId.SecurityCode,
			SecurityExchange: message.SecurityId.BoardCode,
			Account: message.PortfolioName,
			IsIncremental: message.IsIncremental ? true : null,
			UserId: message.UserId,
			SecurityIds: message.SecurityIds,
			Skip: message.Skip);
	}
}
