namespace StockSharp.PrizmBit;

partial class PrizmBitMessageAdapter
{
	private string PortfolioName => nameof(PrizmBit) + "_" + Key.ToId();

	private readonly SynchronizedDictionary<long, string> _pfNames = [];

	private string GetPortfolioName(long accountId) => _pfNames[accountId];
	private static string GetAccountType(string portfolioName) => portfolioName.Split('_').Last();

	/// <inheritdoc />
	protected override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
	{
		var symbol = regMsg.SecurityId.ToNative();
		var condition = (PrizmBitOrderCondition)regMsg.Condition;

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

				var withdrawId = await _httpClient.WithdrawAsync(symbol, regMsg.Volume, condition.WithdrawInfo, cancellationToken);

				await SendOutMessageAsync(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					OrderId = withdrawId,
					ServerTime = CurrentTime,
					OriginalTransactionId = regMsg.TransactionId,
					OrderState = OrderStates.Done,
					HasOrderInfo = true,
				}, cancellationToken);

				//await ProcessPortfolioLookupAsync(null, cancellationToken);
				return;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));
		}

		var isMarket = regMsg.OrderType == OrderTypes.Market;

		var order = await _httpClient.RegisterOrderAsync(symbol, regMsg.TransactionId.To<string>(),
			regMsg.OrderType.ToOrderType(regMsg.TimeInForce, condition),
			regMsg.Side.ToNative(), GetAccountType(regMsg.PortfolioName), regMsg.Price, regMsg.Volume,
			null, condition?.StopLossActivationPrice, condition?.StopLossClosePositionPrice, cancellationToken);

		await SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderId = order.Id,
			ServerTime = CurrentTime,
			OriginalTransactionId = regMsg.TransactionId,
			OrderState = isMarket ? OrderStates.Done : OrderStates.Active,
			Balance = isMarket ? 0 : null,
			HasOrderInfo = true,
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderId == null)
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		await _httpClient.CancelOrderAsync(cancelMsg.OrderId.Value, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask ReplaceOrderAsync(OrderReplaceMessage replaceMsg, CancellationToken cancellationToken)
	{
		if (replaceMsg.OldOrderId == null)
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(replaceMsg.OriginalTransactionId));

		await _httpClient.ModifyOrderAsync(replaceMsg.OldOrderId.Value, replaceMsg.Price, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		await _httpClient.CancelOrdersAsync(cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		if (!lookupMsg.IsSubscribe)
			return;

		var time = CurrentTime;
		var accounts = await _httpClient.GetAccountsAsync(cancellationToken);

		_pfNames.Clear();

		foreach (var account in accounts)
		{
			var pfName = $"{PortfolioName}_{account.Id}_{account.Type}";

			_pfNames.Add(account.Id, pfName);

			await SendOutMessageAsync(new PortfolioMessage
			{
				PortfolioName = pfName,
				BoardCode = BoardCodes.PrizmBit,
				OriginalTransactionId = lookupMsg.TransactionId
			}, cancellationToken);

			await SendOutMessageAsync(new PositionChangeMessage
			{
				PortfolioName = pfName,
				SecurityId = SecurityId.Money,
				ServerTime = time,
			}
			.Add(PositionChangeTypes.State, account.Suspended ? PortfolioStates.Blocked : PortfolioStates.Active)
			.TryAdd(PositionChangeTypes.CommissionMaker, (decimal?)account.MakerFee, true)
			.TryAdd(PositionChangeTypes.CommissionTaker, (decimal?)account.TakerFee, true), cancellationToken);

			foreach (var balance in account.Balances)
			{
				await ProcessBalanceAsync(time, balance, pfName, cancellationToken);
			}
		}

		if (!lookupMsg.IsHistoryOnly())
			await _pusherClient.SubscribeAccount(cancellationToken);

		await SendSubscriptionResultAsync(lookupMsg, cancellationToken);
	}

	private async ValueTask ProcessBalanceAsync(DateTime time, Balance balance, string pfName, CancellationToken cancellationToken)
	{
		await SendOutMessageAsync(new PositionChangeMessage
		{
			PortfolioName = pfName,
			SecurityId = (await GetCurrencyCodeAsync(balance.CurrencyId, cancellationToken)).ToStockSharp(),
			ServerTime = time,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, (decimal?)balance.Available, true)
		.TryAdd(PositionChangeTypes.BlockedValue, (decimal?)balance.Frozen, true), cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		if (!statusMsg.IsSubscribe)
			return;

		var orders = await _httpClient.GetOrdersAsync(cancellationToken);

		foreach (var order in orders)
			await ProcessOrderAsync(order, statusMsg.TransactionId, cancellationToken);

		var trades = await _httpClient.GetOwnTradesAsync(cancellationToken);

		foreach (var trade in trades)
		{
			await SendOutMessageAsync(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				ServerTime = trade.Time,
				OrderId = trade.OrderId,
				TradeId = trade.TradeId,
				TradePrice = (decimal)trade.Price,
				TradeVolume = (decimal)trade.Amount,
				Commission = (decimal)trade.Fee,
				CommissionCurrency = trade.FeeCurrency,
			}, cancellationToken);
		}

		await SendSubscriptionResultAsync(statusMsg, cancellationToken);
	}

	private async ValueTask ProcessOrderAsync(Order order, long origTransId, CancellationToken cancellationToken)
	{
		if (!long.TryParse(order.CliOrdId, out var transId))
			transId = TransactionIdGenerator.GetNextId();

		await SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			ServerTime = transId != 0 ? order.DateCreated : CurrentTime,
			SecurityId = await GetSecurityIdAsync(order.MarketId, cancellationToken),
			TransactionId = transId,
			OriginalTransactionId = origTransId,
			OrderId = order.Id,
			OrderVolume = (decimal)order.OrderQty,
			Balance = (decimal)order.LeavesQty,
			Side = order.Side.ToSide(),
			OrderType = order.OrderType.ToOrderType(out var tif, out var isTrailing),
			TimeInForce = tif,
			OrderPrice = (decimal)order.Price,
			PortfolioName = PortfolioName,
			OrderState = order.OrderStatus.ToOrderState(),
			AveragePrice = (decimal?)order.AveragePrice,
			Condition = new PrizmBitOrderCondition
			{
				StopLossClosePositionPrice = (decimal?)order.LimitPrice,
				StopLossActivationPrice = (decimal?)order.StopPrice,
				IsStopLossTrailing = isTrailing
			},
		}, cancellationToken);
	}

	private ValueTask SessionOnBalanceChanged(DateTime timestamp, Balance balance, CancellationToken cancellationToken)
	{
		return ProcessBalanceAsync(timestamp, balance, GetPortfolioName(balance.AccountId), cancellationToken);
	}

	private ValueTask SessionOnUserOrderCanceled(DateTime timestamp, UserCanceledOrder order, CancellationToken cancellationToken)
	{
		if (!long.TryParse(order.CliOrdId, out var transId))
			return default;

		return SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderId = order.Id,
			OriginalTransactionId = transId,
			Balance = (decimal)order.Amount,
			ServerTime = timestamp,
		}, cancellationToken);
	}

	private ValueTask SessionOnNewUserTrade(DateTime timestamp, SocketUserTrade trade, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			ServerTime = trade.Time,
			OrderId = trade.OrderId,
			TradeId = trade.TradeId,
			TradePrice = (decimal)trade.Price,
			TradeVolume = (decimal)trade.Amount,
		}, cancellationToken);
	}
}