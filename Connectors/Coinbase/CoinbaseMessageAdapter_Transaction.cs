namespace StockSharp.Coinbase;

public partial class CoinbaseMessageAdapter
{
	private string PortfolioName => nameof(Coinbase) + "_" + Key.ToId();

	/// <inheritdoc />
	public override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
	{
		var condition = (CoinbaseOrderCondition)regMsg.Condition;

		switch (regMsg.OrderType)
		{
			case null:
			case OrderTypes.Limit:
			case OrderTypes.Market:
				break;
			case OrderTypes.Conditional:
			{
				if (!condition.IsWithdraw)
					break;

				var withdrawId = await _restClient.Withdraw(regMsg.SecurityId.SecurityCode, regMsg.Volume, condition.WithdrawInfo, cancellationToken);

				SendOutMessage(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					OrderStringId = withdrawId,
					ServerTime = CurrentTime.ConvertToUtc(),
					OriginalTransactionId = regMsg.TransactionId,
					OrderState = OrderStates.Done,
					HasOrderInfo = true,
				});

				await PortfolioLookupAsync(null, cancellationToken);
				return;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));
		}

		var isMarket = regMsg.OrderType == OrderTypes.Market;
		var price = isMarket ? (decimal?)null : regMsg.Price;
		
		var result = await _restClient.RegisterOrder(
			regMsg.TransactionId.To<string>(), regMsg.SecurityId.ToSymbol(),
			regMsg.OrderType.ToNative(), regMsg.Side.ToNative(), price,
			condition?.StopPrice, regMsg.Volume, regMsg.TimeInForce,
			regMsg.TillDate.EnsureToday(), regMsg.Leverage, cancellationToken);

		var orderState = result.Status.ToOrderState();

		if (orderState == OrderStates.Failed)
		{
			SendOutMessage(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				ServerTime = result.CreationTime,
				OriginalTransactionId = regMsg.TransactionId,
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException(),
				HasOrderInfo = true,
			});
		}
	}

	/// <inheritdoc />
	public override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderStringId.IsEmpty())
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		await _restClient.CancelOrder(cancelMsg.OrderStringId, cancellationToken);
	}

	/// <inheritdoc />
	public override async ValueTask ReplaceOrderAsync(OrderReplaceMessage replaceMsg, CancellationToken cancellationToken)
	{
		await _restClient.EditOrder(replaceMsg.OldOrderId.To<string>(), replaceMsg.Price, replaceMsg.Volume, cancellationToken);
	}

	/// <inheritdoc />
	public override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var transId = lookupMsg.TransactionId;

		SendSubscriptionReply(transId);

		if (!lookupMsg.IsSubscribe)
			return;

		SendOutMessage(new PortfolioMessage
		{
			PortfolioName = PortfolioName,
			BoardCode = BoardCodes.Coinbase,
			OriginalTransactionId = transId,
		});

		var accounts = await _restClient.GetAccounts(cancellationToken);

		foreach (var account in accounts)
		{
			//var currency = account.Currency;

			//if (!currency.StartsWithIgnoreCase("USD") &&
			//    !currency.StartsWithIgnoreCase("EUR"))
			//{
			//	currency += "-USD";
			//}

			SendOutMessage(new PositionChangeMessage
			{
				//PortfolioName = account.Id.To<string>(),
				PortfolioName = PortfolioName,
				SecurityId = new SecurityId
				{
					SecurityCode = account.Currency,
					BoardCode = BoardCodes.Coinbase,
				},
				ServerTime = CurrentTime.ConvertToUtc(),
			}
			.TryAdd(PositionChangeTypes.CurrentValue, (decimal)account.Available, true)
			.TryAdd(PositionChangeTypes.BlockedValue, (decimal)account.Hold, true));
		}

		SendSubscriptionResult(lookupMsg);
	}

	/// <inheritdoc />
	public override async ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		SendSubscriptionReply(statusMsg.TransactionId);

		if (!statusMsg.IsSubscribe)
			return;

		var orders = await _restClient.GetOrders(cancellationToken);

		foreach (var order in orders)
			ProcessOrder(order, statusMsg.TransactionId);

		if (!statusMsg.IsHistoryOnly())
			await _socketClient.SubscribeOrders(statusMsg.TransactionId, cancellationToken);

		SendSubscriptionResult(statusMsg);
	}

	private void ProcessOrder(Order order, long originTransId)
	{
		if (!long.TryParse(order.ClientOrderId, out var transId))
			return;

		var state = order.Status.ToOrderState();

		SendOutMessage(new ExecutionMessage
		{
			ServerTime = originTransId == 0 ? CurrentTime.ConvertToUtc() : order.CreationTime,
			DataTypeEx = DataType.Transactions,
			SecurityId = order.Product.ToStockSharp(),
			TransactionId = originTransId== 0 ? 0 : transId,
			OriginalTransactionId = originTransId,
			OrderState = state,
			Error = state == OrderStates.Failed ? new InvalidOperationException() : null,
			OrderType = order.Type.ToOrderType(),
			Side = order.Side.ToSide(),
			OrderStringId = order.Id,
			OrderPrice = order.Price?.ToDecimal() ?? 0,
			OrderVolume = order.Size?.ToDecimal(),
			TimeInForce = order.TimeInForce.ToTimeInForce(),
			Balance = (decimal?)order.LeavesQuantity,
			HasOrderInfo = true,
		});
	}

	private void SessionOnOrderReceived(Order order)
		=> ProcessOrder(order, 0);
}