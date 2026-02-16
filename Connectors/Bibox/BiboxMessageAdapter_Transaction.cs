namespace StockSharp.Bibox;

public partial class BiboxMessageAdapter
{
	private const int _accountSpot = 0;
	private const int _accountCredit = 1;

	private string PortfolioName => nameof(Bibox) + "_" + Key.ToId();

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
				var condition = (BiboxOrderCondition)regMsg.Condition;

				if (!condition.IsWithdraw)
					throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));

				var withdrawId = await _httpClient.Withdraw(regMsg.TransactionId, symbol, regMsg.Volume, condition.WithdrawInfo, regMsg.ClientCode.To<int>(), AdminPassword.UnSecure(), regMsg.Comment, cancellationToken);

				await SendOutMessageAsync(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					OrderId = withdrawId,
					ServerTime = CurrentTime,
					OriginalTransactionId = regMsg.TransactionId,
					OrderState = OrderStates.Done,
					HasOrderInfo = true,
				}, cancellationToken);

				return;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));
		}

		var isMarket = regMsg.OrderType == OrderTypes.Market;

		var orderId = await _httpClient.RegisterOrder(regMsg.TransactionId, symbol, regMsg.MarginMode is not null ? _accountCredit : _accountSpot, regMsg.OrderType.ToNative(), regMsg.Side.ToNative(), regMsg.Price, regMsg.Volume, cancellationToken);

		await SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderId = orderId,
			ServerTime = CurrentTime,
			OriginalTransactionId = regMsg.TransactionId,
			OrderState = isMarket ? OrderStates.Done : OrderStates.Active,
			Balance = isMarket ? 0 : null,
			HasOrderInfo = true,
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderId == null)
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		return _httpClient.CancelOrder(cancelMsg.TransactionId, cancelMsg.OrderId.Value, cancellationToken).AsValueTask();
	}

	/// <inheritdoc />
	protected override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		if (lookupMsg == null)
			throw new ArgumentNullException(nameof(lookupMsg));

		await SendSubscriptionReplyAsync(lookupMsg.TransactionId, cancellationToken);

		if (!lookupMsg.IsSubscribe)
		{
			await _pusherClient.UnSubscribeAccount(lookupMsg.OriginalTransactionId, cancellationToken);
			return;
		}

		await SendOutMessageAsync(new PortfolioMessage
		{
			PortfolioName = PortfolioName,
			BoardCode = BoardCodes.Bibox,
			OriginalTransactionId = lookupMsg.TransactionId
		}, cancellationToken);

		foreach (var balance in await _httpClient.GetBalances(cancellationToken))
		{
			await SendOutMessageAsync(new PositionChangeMessage
			{
				PortfolioName = PortfolioName,
				SecurityId = balance.CoinSymbol.ToStockSharp(),
				ServerTime = CurrentTime,
			}
			.TryAdd(PositionChangeTypes.CurrentValue, balance.Value?.ToDecimal(), true)
			.TryAdd(PositionChangeTypes.BlockedValue, balance.Freeze?.ToDecimal(), true), cancellationToken);
		}

		if (!lookupMsg.IsHistoryOnly())
			await _pusherClient.SubscribeAccount(lookupMsg.TransactionId, cancellationToken);

		await SendSubscriptionResultAsync(lookupMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		if (statusMsg == null)
			throw new ArgumentNullException(nameof(statusMsg));

		await SendSubscriptionReplyAsync(statusMsg.TransactionId, cancellationToken);

		if (!statusMsg.IsSubscribe)
			return;

		foreach (var accountType in new[] { _accountSpot, _accountCredit })
		{
			foreach (var order in await _httpClient.GetOrders(accountType, cancellationToken))
			{
				await ProcessOrder(order, TransactionIdGenerator.GetNextId(), statusMsg.TransactionId, cancellationToken);
			}
		}

		await SendSubscriptionResultAsync(statusMsg, cancellationToken);
	}

	private ValueTask SessionOnOrderChanged(Order order, CancellationToken cancellationToken)
	{
		return ProcessOrder(order, 0, 0, cancellationToken);
	}

	private ValueTask ProcessOrder(Order order, long transId, long originTransId, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			ServerTime = order.CreatedAt,
			SecurityId = $"{order.CoinSymbol}/{order.CurrencySymbol}".ToStockSharp(),
			TransactionId = transId,
			OriginalTransactionId = originTransId,
			OrderId = order.Id,
			OrderPrice = order.Price?.ToDecimal() ?? 0,
			OrderVolume = (decimal)order.Amount,
			Balance = (decimal?)order.Unexecuted,
			Side = order.OrderSide.ToSide(),
			PortfolioName = PortfolioName,
			OrderType = order.OrderType.ToOrderType(),
			OrderState = order.Status.ToOrderState(),
		}, cancellationToken);
	}

	private async ValueTask SessionOnBalancesChanged(IDictionary<string, Balance> balances, CancellationToken cancellationToken)
	{
		foreach (var pair in balances)
		{
			var balance = pair.Value;

			await SendOutMessageAsync(new PositionChangeMessage
			{
				PortfolioName = PortfolioName,
				SecurityId = pair.Key.ToStockSharp(),
				ServerTime = CurrentTime,
			}
			.TryAdd(PositionChangeTypes.CurrentValue, balance.Value?.ToDecimal(), true)
			.TryAdd(PositionChangeTypes.BlockedValue, balance.Freeze?.ToDecimal(), true), cancellationToken);
		}
	}
}
