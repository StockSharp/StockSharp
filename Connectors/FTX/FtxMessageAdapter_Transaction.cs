namespace StockSharp.FTX;

partial class FtxMessageAdapter
{
	private readonly TimeSpan _orderHistoryInterval = TimeSpan.FromDays(7);
	private const int _fillsPaginationLimit = 100;

	private long? _portfolioLookupSubMessageTransactionID;
	private bool _isOrderSubscribed;

	private string PortfolioName => nameof(FTX) + "_" + Key.ToId().To<string>();

	private void SessionOnNewFill(Fill fill)
	{
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			PortfolioName = GetPortfolioName(),
			TradeId = fill.TradeId,
			TradePrice = fill.Price,
			TradeVolume = fill.Size,
			SecurityId = fill.Market.ToStockSharp(),
			OrderId = fill.OrderId,
			Side = fill.Side.ToSide(),
			ServerTime = fill.Time
		});
	}

	private void SendProcessOrderStatusResult(Order order, OrderMessage message)
	{
		if (!long.TryParse(order.ClientId, out var transId))
			return;

		SendOutMessage(new ExecutionMessage
		{
			OriginalTransactionId = message.TransactionId,
			TransactionId = transId,
			OrderId = order.Id,
			DataTypeEx = DataType.Transactions,
			PortfolioName = GetPortfolioName(),
			HasOrderInfo = true,
			ServerTime = order.CreatedAt ?? CurrentTime.ConvertToUtc(),
			OrderState = order.Status.ToOrderState(),
			OrderType = order.Type.ToOrderType(),
			AveragePrice = order.AvgFillPrice,
			OrderPrice = order.Price ?? (order.AvgFillPrice ?? 0),
			SecurityId = order.Market.ToStockSharp(),
			Side = order.Side.ToSide(),
			OrderVolume = order.Size,
			Balance = order.Size - order.FilledSize,

		});
	}
	private void SessionOnNewOrder(Order order)
	{
		if (!long.TryParse(order.ClientId, out var transId))
			return;

		var message = new ExecutionMessage
		{
			OriginalTransactionId = transId,
			TransactionId = transId,
			DataTypeEx = DataType.Transactions,
			PortfolioName = GetPortfolioName(),
			HasOrderInfo = true,
			ServerTime = order.CreatedAt ?? CurrentTime.ConvertToUtc(),
			OrderId = order.Id,
			OrderState = order.Status.ToOrderState(),
			OrderType = order.Type.ToOrderType(),
			AveragePrice = order.AvgFillPrice,
			OrderPrice = order.Price ?? (order.AvgFillPrice ?? 0),
			SecurityId = order.Market.ToStockSharp(),
			Side = order.Side.ToSide(),
			OrderVolume = order.Size,
			Balance = order.Size - order.FilledSize,
		};

		SendOutMessage(message);
	}

	/// <inheritdoc />
	public override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
	{
		switch (regMsg.OrderType)
		{
			case OrderTypes.Limit:
			case OrderTypes.Market:
				break;
			default:
				throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));
		}

		var price = regMsg.OrderType == OrderTypes.Market ? (decimal?)null : regMsg.Price;

		var order = await _restClient.RegisterOrder(regMsg.SecurityId.ToCurrency(), regMsg.Side, price, regMsg.OrderType.Value, regMsg.Volume, regMsg.TransactionId.To<string>(), SubaccountName, cancellationToken)
			?? throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(regMsg.TransactionId));

		SendProcessOrderStatusResult(order, regMsg);
	}

	/// <inheritdoc />
	public override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderId == null)
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		if (!await _restClient.CancelOrder(cancelMsg.OrderId.Value, SubaccountName, cancellationToken))
		{
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));
		}
	}

	/// <inheritdoc />
	public override async ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (!await _restClient.CancelAllOrders(SubaccountName, cancellationToken))
		{
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));
		}
	}

	/// <inheritdoc />
	public override async ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		if (statusMsg != null)
		{
			SendSubscriptionReply(statusMsg.TransactionId);
			_isOrderSubscribed = statusMsg.IsSubscribe;
		}

		if (!_isOrderSubscribed)
		{
			return;
		}

		var orders = await _restClient.GetOpenOrders(SubaccountName, cancellationToken);

		if (statusMsg == null)
		{
			if (orders != null && orders.Count > 0)
			{
				foreach (var order in orders)
					SessionOnNewOrder(order);

				var start = orders.Where(x => x.CreatedAt != null).Min(x => x.CreatedAt.Value);
				var fills = await _restClient.GetFills(start, DateTime.UtcNow, SubaccountName, cancellationToken);

				foreach (var fill in fills)
					SessionOnNewFill(fill);
			}
		}
		else
		{
			if (!statusMsg.IsSubscribe)
			{
				return;
			}

			var fromTime = DateTime.UtcNow - _orderHistoryInterval;

			do
			{
				var prevFromTime = fromTime;

				var (histOrders, hasMoreData) = await _restClient.GetMarketOrderHistoryAndHasMoreOrders(SubaccountName, fromTime, cancellationToken);

				if (!histOrders.Any())
					break;

				foreach (var order in histOrders.Where(o => o.ClientId != null).OrderBy(o => o.CreatedAt))
				{
					if (order.CreatedAt > fromTime)
						fromTime = order.CreatedAt.Value;

					if (order.ClientId != null)
					SendProcessOrderStatusResult(order, statusMsg);
				}

				if (!hasMoreData || fromTime <= prevFromTime)
					break;
			}
			while (true);

			var now = DateTime.UtcNow;

			var startTime = now - _orderHistoryInterval;
			var endTime = (now - startTime) < _orderHistoryInterval ? (startTime + (now - startTime)) : startTime + _orderHistoryInterval;

			while (startTime < endTime)
			{
				var fills = await _restClient.GetFills(startTime, endTime, SubaccountName, cancellationToken);

				var lastTime = startTime;

				foreach (var fill in fills.OrderBy(f => f.Time))
				{
					if (fill.Time < startTime)
						continue;

					if (fill.Time > endTime)
						break;

					SessionOnNewFill(fill);

					lastTime = fill.Time;
				}

				if (fills.Count != _fillsPaginationLimit)
					break;

				startTime = lastTime;
			}

			SendSubscriptionResult(statusMsg);
		}
	}

	/// <inheritdoc />
	public override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		if (lookupMsg != null)
		{
			SendSubscriptionReply(lookupMsg.TransactionId);

			_portfolioLookupSubMessageTransactionID = lookupMsg.IsSubscribe ? lookupMsg.TransactionId : null;

			if (!lookupMsg.IsSubscribe)
			{
				return;
			}
		}

		if (_portfolioLookupSubMessageTransactionID == null)
		{
			return;
		}

		SendOutMessage(new PortfolioMessage
		{
			PortfolioName = GetPortfolioName(),
			BoardCode = BoardCodes.FTX,
			OriginalTransactionId = (long)_portfolioLookupSubMessageTransactionID,
		});

		if (lookupMsg != null)
		{
			SendSubscriptionResult(lookupMsg);
		}

		var balances = await _restClient.GetBalances(SubaccountName, cancellationToken);
		if (balances != null)
		{
			foreach (var balance in balances)
			{
				var msg = this.CreatePositionChangeMessage(GetPortfolioName(), balance.Coin.ToStockSharp());
				msg.TryAdd(PositionChangeTypes.CurrentValue, balance.Total, true);
				msg.TryAdd(PositionChangeTypes.BlockedValue, balance.Total - balance.Free, true);
				if (_portfolioLookupSubMessageTransactionID != null) msg.OriginalTransactionId = (long)_portfolioLookupSubMessageTransactionID;
				SendOutMessage(msg);
			}
		}

		var futures = await _restClient.GetFuturesPositions(SubaccountName, cancellationToken);

		foreach (var fut in futures)
		{
			var msg = this.CreatePositionChangeMessage(GetPortfolioName(), fut.Name.ToStockSharp());
			msg.TryAdd(PositionChangeTypes.CurrentValue, fut.Cost, true);
			msg.TryAdd(PositionChangeTypes.UnrealizedPnL, fut.UnrealizedPnl, true);
			msg.TryAdd(PositionChangeTypes.AveragePrice, fut.EntryPrice, true);
			if (_portfolioLookupSubMessageTransactionID != null) msg.OriginalTransactionId = (long)_portfolioLookupSubMessageTransactionID;
			SendOutMessage(msg);
		}
	}

	private string GetPortfolioName()
	{
		return SubaccountName.IsEmpty(PortfolioName);
	}
}
