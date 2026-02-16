namespace StockSharp.ZB;

partial class ZBMessageAdapter
{
	private string PortfolioName => nameof(ZB) + "_" + Key.ToId();

	/// <inheritdoc />
	protected override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
	{
		var symbol = regMsg.SecurityId.ToSymbol();

		switch (regMsg.OrderType)
		{
			case null:
			case OrderTypes.Limit:
			case OrderTypes.Market:
				break;
			case OrderTypes.Conditional:
			{
				var condition = (ZBOrderCondition)regMsg.Condition;

				if (!condition.IsWithdraw)
					throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));

				var withdrawId = await _httpClient.WithdrawAsync(symbol, regMsg.Volume, condition.WithdrawInfo, Passphrase, cancellationToken);

				await SendOutMessageAsync(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					OrderStringId = withdrawId,
					ServerTime = CurrentTime,
					OriginalTransactionId = regMsg.TransactionId,
					OrderState = OrderStates.Done,
					HasOrderInfo = true,
				}, cancellationToken);

				//ProcessPortfolioLookup(null);
				return;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));
		}

		await _pusherClient.RegisterOrderAsync(regMsg.TransactionId, symbol, regMsg.Side.ToNativeAsInt(), regMsg.Price, regMsg.Volume, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderId == null)
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		await _pusherClient.CancelOrderAsync(cancelMsg.TransactionId, cancelMsg.SecurityId.ToSymbol(), cancelMsg.OrderId.Value, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage message, CancellationToken cancellationToken)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		await SendSubscriptionReplyAsync(message.TransactionId, cancellationToken);

		if (!message.IsSubscribe)
			return;

		await SendOutMessageAsync(new PortfolioMessage
		{
			PortfolioName = PortfolioName,
			BoardCode = BoardCodes.ZB,
			OriginalTransactionId = message.TransactionId
		}, cancellationToken);

		await SendSubscriptionResultAsync(message, cancellationToken);

		//_pusherClient.SubscribeAccount(message.TransactionId);
	}

	/// <inheritdoc />
	protected override async ValueTask OrderStatusAsync(OrderStatusMessage message, CancellationToken cancellationToken)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		await SendSubscriptionReplyAsync(message.TransactionId, cancellationToken);

		if (!message.IsSubscribe)
			return;

		await SendSubscriptionResultAsync(message, cancellationToken);
		//_pusherClient.SubscribeOrders(message.TransactionId, null);
	}

	private ValueTask SessionOnOrderChanged(long origTransId, Order order, CancellationToken cancellationToken)
	{
		return ProcessOrder(0, origTransId, order, cancellationToken);
	}

	private ValueTask ProcessOrder(long transId, long origTransId, Order order, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			ServerTime = transId != 0 ? CurrentTime : CurrentTime,
			SecurityId = order.Currency.ToStockSharp(),
			TransactionId = transId,
			OriginalTransactionId = origTransId,
			OrderId = order.Id,
			OrderVolume = order.TotalAmount?.ToDecimal(),
			Balance = order.GetBalance(),
			Side = order.Type.ToSide(),
			OrderPrice = order.Price?.ToDecimal() ?? 0,
			PortfolioName = PortfolioName,
			OrderState = order.Status.ToOrderState(),
		}, cancellationToken);
	}

	private async ValueTask SessionOnBalancesChanged(long origTransId, IEnumerable<Balance> balances, CancellationToken cancellationToken)
	{
		foreach (var balance in balances)
		{
			await SendOutMessageAsync(new PositionChangeMessage
			{
				PortfolioName = PortfolioName,
				SecurityId = balance.EnName.ToStockSharp(),
				ServerTime = CurrentTime,
				OriginalTransactionId = origTransId,
			}
			.TryAdd(PositionChangeTypes.CurrentValue, balance.Available?.ToDecimal(), true)
			.TryAdd(PositionChangeTypes.BlockedValue, balance.Freez?.ToDecimal(), true), cancellationToken);
		}
	}

	private async ValueTask SessionOnNewOrders(long origTransId, IEnumerable<Order> orders, CancellationToken cancellationToken)
	{
		foreach (var order in orders)
		{
			await ProcessOrder(TransactionIdGenerator.GetNextId(), origTransId, order, cancellationToken);
		}
	}
}