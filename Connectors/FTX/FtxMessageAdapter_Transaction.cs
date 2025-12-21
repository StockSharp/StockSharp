namespace StockSharp.FTX;

partial class FtxMessageAdapter
{
	private readonly TimeSpan _orderHistoryInterval = TimeSpan.FromDays(7);
	private const int _fillsPaginationLimit = 100;

	private long? _portfolioLookupSubMessageTransactionID;
	private bool _isOrderSubscribed;

	private string PortfolioName => nameof(FTX) + "_" + Key.ToId().To<string>();

	private ValueTask SessionOnNewFill(Fill fill, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(new ExecutionMessage
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
		}, default);
	}

	private async ValueTask SendProcessOrderStatusResultAsync(Order order, OrderMessage message, CancellationToken cancellationToken)
	{
		if (!long.TryParse(order.ClientId, out var transId))
			return;

		await SendOutMessageAsync(new ExecutionMessage
		{
			OriginalTransactionId = message.TransactionId,
			TransactionId = transId,
			OrderId = order.Id,
			DataTypeEx = DataType.Transactions,
			PortfolioName = GetPortfolioName(),
			HasOrderInfo = true,
			ServerTime = order.CreatedAt ?? CurrentTimeUtc,
			OrderState = order.Status.ToOrderState(),
			OrderType = order.Type.ToOrderType(),
			AveragePrice = order.AvgFillPrice,
			OrderPrice = order.Price ?? (order.AvgFillPrice ?? 0),
			SecurityId = order.Market.ToStockSharp(),
			Side = order.Side.ToSide(),
			OrderVolume = order.Size,
			Balance = order.Size - order.FilledSize,

		}, cancellationToken);
	}
	private ValueTask SessionOnNewOrder(Order order, CancellationToken cancellationToken)
	{
		if (!long.TryParse(order.ClientId, out var transId))
			return default;

		var message = new ExecutionMessage
		{
			OriginalTransactionId = transId,
			TransactionId = transId,
			DataTypeEx = DataType.Transactions,
			PortfolioName = GetPortfolioName(),
			HasOrderInfo = true,
			ServerTime = order.CreatedAt ?? CurrentTimeUtc,
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

		return SendOutMessageAsync(message, default);
	}

	/// <inheritdoc />
	protected override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
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

		await SendProcessOrderStatusResultAsync(order, regMsg, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderId == null)
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		if (!await _restClient.CancelOrder(cancelMsg.OrderId.Value, SubaccountName, cancellationToken))
		{
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));
		}
	}

	/// <inheritdoc />
	protected override async ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		// Handle CancelOrders mode
		if (cancelMsg.Mode.HasFlag(OrderGroupCancelModes.CancelOrders))
		{
			if (!await _restClient.CancelAllOrders(SubaccountName, cancellationToken))
			{
				throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));
			}
		}

		// Handle ClosePositions mode
		if (cancelMsg.Mode.HasFlag(OrderGroupCancelModes.ClosePositions))
		{
			var errors = new List<Exception>();

			// Get current futures positions
			var futures = await _restClient.GetFuturesPositions(SubaccountName, cancellationToken);

			foreach (var fut in futures)
			{
				if (!fut.Cost.HasValue || fut.Cost == 0)
					continue;

				var secId = fut.Name.ToStockSharp();

				// If SecurityId is specified, close only that position
				if (cancelMsg.SecurityId != default && cancelMsg.SecurityId != secId)
					continue;

				// Determine the side to close the position (opposite of current position)
				var closingSide = fut.Cost > 0 ? Sides.Sell : Sides.Buy;

				// Check Side filter
				if (cancelMsg.Side != null && cancelMsg.Side != closingSide)
					continue;

				try
				{
					var size = Math.Abs(fut.Cost.Value);

					// Create market order to close the position
					await _restClient.RegisterOrder(
						fut.Name,
						closingSide,
						null, // market order
						OrderTypes.Market,
						size,
						TransactionIdGenerator.GetNextId().To<string>(),
						SubaccountName,
						cancellationToken);
				}
				catch (Exception ex)
				{
					this.AddErrorLog($"Failed to close position for {fut.Name}: {ex.Message}");
					errors.Add(ex);
				}
			}

			// Get spot balances
			var balances = await _restClient.GetBalances(SubaccountName, cancellationToken);
			if (balances != null)
			{
				foreach (var balance in balances)
				{
					if (balance.Total <= 0)
						continue;

					var secId = balance.Coin.ToStockSharp();

					// If SecurityId is specified, close only that position
					if (cancelMsg.SecurityId != default && cancelMsg.SecurityId != secId)
						continue;

					// Skip USD and stablecoins as they are base currencies
					if (balance.Coin.EqualsIgnoreCase("USD") ||
					    balance.Coin.EqualsIgnoreCase("USDT") ||
					    balance.Coin.EqualsIgnoreCase("USDC"))
						continue;

					// Check Side filter - spot balances are always long positions
					if (cancelMsg.Side != null && cancelMsg.Side != Sides.Sell)
						continue;

					try
					{
						// Try to sell the coin to USD
						var market = $"{balance.Coin}/USD";

						await _restClient.RegisterOrder(
							market,
							Sides.Sell,
							null, // market order
							OrderTypes.Market,
							balance.Total,
							TransactionIdGenerator.GetNextId().To<string>(),
							SubaccountName,
							cancellationToken);
					}
					catch (Exception ex)
					{
						this.AddErrorLog($"Failed to close position for {balance.Coin}: {ex.Message}");
						errors.Add(ex);
					}
				}
			}

			// FTX supports real-time updates, no need to refresh portfolio manually

			// Send result with errors if any
			if (errors.Count > 0)
			{
				await SendOutMessageAsync(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					OriginalTransactionId = cancelMsg.TransactionId,
					ServerTime = CurrentTimeUtc,
					HasOrderInfo = true,
					Error = errors.Count == 1 ? errors[0] : new AggregateException(errors),
				}, cancellationToken);
			}
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		if (statusMsg != null)
		{
			await SendSubscriptionReplyAsync(statusMsg.TransactionId, cancellationToken);
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
					await SessionOnNewOrder(order, cancellationToken);

				var start = orders.Where(x => x.CreatedAt != null).Min(x => x.CreatedAt.Value);
				var fills = await _restClient.GetFills(start, DateTime.UtcNow, SubaccountName, cancellationToken);

				foreach (var fill in fills)
					await SessionOnNewFill(fill, cancellationToken);
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
					await SendProcessOrderStatusResultAsync(order, statusMsg, cancellationToken);
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

					await SessionOnNewFill(fill, cancellationToken);

					lastTime = fill.Time;
				}

				if (fills.Count != _fillsPaginationLimit)
					break;

				startTime = lastTime;
			}

			await SendSubscriptionResultAsync(statusMsg, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		if (lookupMsg != null)
		{
			await SendSubscriptionReplyAsync(lookupMsg.TransactionId, cancellationToken);

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

		await SendOutMessageAsync(new PortfolioMessage
		{
			PortfolioName = GetPortfolioName(),
			BoardCode = BoardCodes.FTX,
			OriginalTransactionId = (long)_portfolioLookupSubMessageTransactionID,
		}, cancellationToken);

		if (lookupMsg != null)
		{
			await SendSubscriptionResultAsync(lookupMsg, cancellationToken);
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
				await SendOutMessageAsync(msg, cancellationToken);
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
			await SendOutMessageAsync(msg, cancellationToken);
		}
	}

	private string GetPortfolioName()
	{
		return SubaccountName.IsEmpty(PortfolioName);
	}
}
