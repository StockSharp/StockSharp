namespace StockSharp.Bittrex;

public partial class BittrexMessageAdapter
{
	private readonly Dictionary<string, RefPair<decimal, long>> _orderInfo = new(StringComparer.InvariantCultureIgnoreCase);

	private string PortfolioName => nameof(Bittrex) + "_" + Key.ToId();

	/// <inheritdoc />
	protected override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
	{
		switch (regMsg.OrderType)
		{
			case null:
			case OrderTypes.Limit:
				break;
			case OrderTypes.Conditional:
			{
				var condition = (BittrexOrderCondition)regMsg.Condition;

				if (!condition.IsWithdraw)
					throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));

				var withdrawId = await _httpClient.WithdrawAsync(regMsg.SecurityId.SecurityCode, regMsg.Volume, condition.WithdrawInfo, cancellationToken);

				await SendOutMessageAsync(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					OrderStringId = withdrawId,
					ServerTime = CurrentTime,
					OriginalTransactionId = regMsg.TransactionId,
					OrderState = OrderStates.Done,
					HasOrderInfo = true,
				}, cancellationToken);

				await PortfolioLookupAsync(null, cancellationToken);
				return;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));
		}

		var uuid = await _httpClient.RegisterOrderAsync(regMsg.SecurityId.ToSymbol(), regMsg.Side, regMsg.Price, regMsg.Volume, cancellationToken);

		_orderInfo.Add(uuid, RefTuple.Create(regMsg.Volume, regMsg.TransactionId));
	}

	/// <inheritdoc />
	protected override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderStringId == null)
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		await _httpClient.CancelOrderAsync(cancelMsg.OrderStringId, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage message, CancellationToken cancellationToken)
	{
		if (message != null && !message.IsSubscribe)
			return;

		//var transId = message?.TransactionId ?? 0;
		var balances = await _httpClient.GetBalancesAsync(cancellationToken);

		foreach (var balance in balances)
		{
			//var currency = balance.Currency;

			//if (!currency.StartsWithIgnoreCase("usd") &&
			//    !currency.StartsWithIgnoreCase("eur"))
			//{
			//	currency += "-USD";
			//}

			await SendOutMessageAsync(new PositionChangeMessage
			{
				PortfolioName = PortfolioName,
				SecurityId = new SecurityId
				{
					SecurityCode = balance.Currency,
					BoardCode = BoardCodes.Bittrex,
				},
				ServerTime = CurrentTime,
			}
			.TryAdd(PositionChangeTypes.CurrentValue, balance.Available)
			.TryAdd(PositionChangeTypes.BlockedValue, balance.Pending), cancellationToken);
		}

		if (message != null)
		{
			await SendOutMessageAsync(new PortfolioMessage
			{
				PortfolioName = PortfolioName,
				BoardCode = BoardCodes.Bittrex,
				OriginalTransactionId = message.TransactionId,
			}, cancellationToken);

			await SendSubscriptionResultAsync(message, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OrderStatusAsync(OrderStatusMessage message, CancellationToken cancellationToken)
	{
		if (!message.IsSubscribe)
			return;

		var orders = await _httpClient.GetOpenOrdersAsync(null, cancellationToken);

		foreach (var order in orders)
		{
			var transId = TransactionIdGenerator.GetNextId();
			var info = _orderInfo.TryGetValue(order.OrderUuid);

			if (info == null)
			{
				_orderInfo.Add(order.OrderUuid, RefTuple.Create(order.QuantityRemaining, transId));
				await ProcessOrderAsync(order, transId, message.TransactionId, cancellationToken);
			}
			else
				await ProcessOrderAsync(order, 0, info.Second, cancellationToken);
		}

		await SendSubscriptionResultAsync(message, cancellationToken);
	}

	private ValueTask ProcessOrderAsync(Order order, long transId, long origTransId, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			ServerTime = (transId == 0 ? order.Closed ?? order.Opened : order.Opened).UtcKind(),
			SecurityId = order.Exchange.ToStockSharp(),
			TransactionId = transId,
			OriginalTransactionId = origTransId,
			OrderStringId = order.OrderUuid,
			OrderVolume = order.Quantity,
			Balance = order.QuantityRemaining,
			OrderType = OrderTypes.Limit,
			Side = (order.OrderType ?? order.Type).ToSide(),
			OrderPrice = order.Limit ?? 0,
			PortfolioName = PortfolioName,
			Commission = order.CommissionPaid,
			OrderState = order.Closed == null ? OrderStates.Active : OrderStates.Done,
		}, cancellationToken);
	}

	private ValueTask SessionOnOrderChanged(int state, WsOrder order, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			ServerTime = order.Closed ?? order.Updated ?? order.Opened,
			SecurityId = order.Exchange.ToStockSharp(),
			OriginalTransactionId = _orderInfo.TryGetValue(order.OrderUuid)?.Second ?? 0,
			OrderStringId = order.OrderUuid,
			OrderVolume = order.Quantity,
			Balance = order.QuantityRemaining,
			OrderType = OrderTypes.Limit,
			Side = order.OrderType.ToSide(),
			OrderPrice = order.Limit ?? 0,
			PortfolioName = PortfolioName,
			Commission = order.CommissionPaid,
			OrderState = state.ToOrderState(),
		}, cancellationToken);
	}

	private ValueTask SessionOnBalanceChanged(WsBalance balance, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new PositionChangeMessage
		{
			PortfolioName = PortfolioName,
			SecurityId = new SecurityId
			{
				SecurityCode = balance.Currency,
				BoardCode = BoardCodes.Bittrex,
			},
			ServerTime = balance.Updated ?? CurrentTime,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, balance.Available)
		.TryAdd(PositionChangeTypes.BlockedValue, balance.Pending), cancellationToken);
	}
}
