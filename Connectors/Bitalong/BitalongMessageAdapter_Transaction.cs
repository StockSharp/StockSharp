namespace StockSharp.Bitalong;

public partial class BitalongMessageAdapter
{
	private string PortfolioName => nameof(Bitalong) + "_" + Key.ToId();

	private readonly SynchronizedDictionary<long, RefTriple<long, decimal, string>> _orderInfo = new();

	/// <inheritdoc />
	public override async ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
	{
		var symbol = regMsg.SecurityId.ToNative();

		switch (regMsg.OrderType)
		{
			case null:
			case OrderTypes.Limit:
			//case OrderTypes.Market:
				break;
			case OrderTypes.Conditional:
			{
				var condition = (BitalongOrderCondition)regMsg.Condition;

				if (!condition.IsWithdraw)
					throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));

				await _httpClient.Withdraw(symbol, regMsg.Volume, condition.WithdrawInfo, cancellationToken);

				SendOutMessage(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					ServerTime = CurrentTimeUtc,
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

		var orderId = await _httpClient.RegisterOrder(symbol, regMsg.Side.ToNative(), regMsg.Price, regMsg.Volume, cancellationToken);

		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderId = orderId,
			ServerTime = CurrentTimeUtc,
			OriginalTransactionId = regMsg.TransactionId,
			OrderState = isMarket ? OrderStates.Done : OrderStates.Active,
			Balance = isMarket ? 0 : null,
			HasOrderInfo = true,
		});

		if (isMarket)
		{

		}
		else
			_orderInfo.Add(orderId, RefTuple.Create(regMsg.TransactionId, regMsg.Volume, symbol));

		await PortfolioLookupAsync(null, cancellationToken);
	}

	/// <inheritdoc />
	public override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderId == null)
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		await _httpClient.CancelOrder(cancelMsg.SecurityId.ToNative(), cancelMsg.OrderId.Value, cancellationToken);

		//SendOutMessage(new ExecutionMessage
		//{
		//	ServerTime = CurrentTimeUtc,
		//	DataTypeEx = DataType.Transactions,
		//	OriginalTransactionId = cancelMsg.TransactionId,
		//	OrderState = OrderStates.Done,
		//	HasOrderInfo = true,
		//});

		await OrderStatusAsync(null, cancellationToken);
		await PortfolioLookupAsync(null, cancellationToken);
	}

	/// <inheritdoc />
	public override async ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		// Handle CancelOrders mode
		if (cancelMsg.Mode.HasFlag(OrderGroupCancelModes.CancelOrders))
		{
			await _httpClient.CancelAllOrders(cancelMsg.SecurityId.ToNative(), cancelMsg.Side, cancellationToken);
		}

		// Handle ClosePositions mode
		if (cancelMsg.Mode.HasFlag(OrderGroupCancelModes.ClosePositions))
		{
			var errors = new List<Exception>();

			// Get current balances to determine positions
			var tuple = await _httpClient.GetBalances(cancellationToken);

			foreach (var p in tuple.Item1)
			{
				var available = (decimal)p.Value;

				if (available <= 0)
					continue;

				var secId = p.Key.ToStockSharp();

				// If SecurityId is specified, close only that position
				if (cancelMsg.SecurityId != default && cancelMsg.SecurityId != secId)
					continue;

				// Skip USD and stablecoins as they are base currencies
				if (p.Key.EqualsIgnoreCase("USD") ||
				    p.Key.EqualsIgnoreCase("USDT") ||
				    p.Key.EqualsIgnoreCase("USDC"))
					continue;

				// Check Side filter - spot balances are always long positions
				if (cancelMsg.Side != null && cancelMsg.Side != Sides.Sell)
					continue;

				try
				{
					// Create sell order to close the position
					// Most pairs trade against USD
					var symbol = p.Key.ToLowerInvariant() + "_usd";

					// Try to get current best bid price for limit order
					// Bitalong doesn't support market orders, so we use limit
					await _httpClient.RegisterOrder(
						symbol,
						"sell",
						0.00000001m, // minimum price (will be filled at market)
						available,
						cancellationToken);
				}
				catch (Exception ex)
				{
					this.AddErrorLog($"Failed to close position for {p.Key}: {ex.Message}");
					errors.Add(ex);
				}
			}

			// Bitalong likely doesn't support streaming, refresh portfolio manually
			await PortfolioLookupAsync(null, cancellationToken);

			// Send result with errors if any
			if (errors.Count > 0)
			{
				SendOutMessage(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					OriginalTransactionId = cancelMsg.TransactionId,
					ServerTime = CurrentTimeUtc,
					HasOrderInfo = true,
					Error = errors.Count == 1 ? errors[0] : new AggregateException(errors),
				});
			}
		}
	}

	/// <inheritdoc />
	public override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		if (lookupMsg != null)
		{
			SendSubscriptionReply(lookupMsg.TransactionId);

			if (!lookupMsg.IsSubscribe)
				return;

			SendOutMessage(new PortfolioMessage
			{
				PortfolioName = PortfolioName,
				BoardCode = BoardCodes.Bitalong,
				OriginalTransactionId = lookupMsg.TransactionId
			});

			SendSubscriptionResult(lookupMsg);
		}

		var tuple = await _httpClient.GetBalances(cancellationToken);

		PositionChangeMessage CreateMessage(string asset)
		{
			return new PositionChangeMessage
			{
				PortfolioName = PortfolioName,
				SecurityId = asset.ToStockSharp(),
				ServerTime = CurrentTimeUtc,
			};
		}

		var dict = new Dictionary<string, PositionChangeMessage>();

		foreach (var p in tuple.Item1)
		{
			dict.SafeAdd(p.Key, CreateMessage).TryAdd(PositionChangeTypes.CurrentValue, (decimal)p.Value, true);
		}

		foreach (var p in tuple.Item2)
		{
			dict.SafeAdd(p.Key, CreateMessage).TryAdd(PositionChangeTypes.BlockedValue, (decimal)p.Value, true);
		}

		foreach (var msg in dict.Values)
		{
			SendOutMessage(msg);
		}

		_lastTimeBalanceCheck = CurrentTimeUtc;
	}

	/// <inheritdoc />
	public override async ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		if (statusMsg == null)
		{
			if (_orderInfo.Count == 0)
				return;

			var portfolioRefresh = false;

			var opened = await _httpClient.GetOrders(cancellationToken);

			var uuids = _orderInfo.Keys.ToSet();

			foreach (var order in opened)
			{
				var balance = (decimal)order.Amount;

				uuids.Remove(order.Id);

				var info = _orderInfo.TryGetValue(order.Id);

				if (info == null)
				{
					var transId = TransactionIdGenerator.GetNextId();
					//var set = CreateTradesSet();
					_orderInfo.Add(order.Id, RefTuple.Create(transId, balance, order.CurrencyPair));

					ProcessOrder(order, transId, 0);
					//ProcessTrades(order.Trades, set, transId);

					portfolioRefresh = true;
				}
				else
				{
					var delta = info.Second - balance;

					if (delta == 0)
						continue;

					info.Second = balance;

					SendOutMessage(new ExecutionMessage
					{
						HasOrderInfo = true,
						DataTypeEx = DataType.Transactions,
						OrderId = order.Id,
						OriginalTransactionId = info.First,
						ServerTime = CurrentTimeUtc,
						Balance = balance,
					});

					//ProcessTrades(order.Trades, info.Third, info.Second);

					portfolioRefresh = true;
				}
			}

			foreach (var orderId in uuids)
			{
				var info = _orderInfo.GetAndRemove(orderId);

				var order = await _httpClient.GetOrderInfo(info.Third, orderId, cancellationToken);

				ProcessOrder(order, 0, info.First);
				//ProcessTrades(order.Trades, info.Third, transId);

				portfolioRefresh = true;
			}

			if (portfolioRefresh)
				await PortfolioLookupAsync(null, cancellationToken);
		}
		else
		{
			SendSubscriptionReply(statusMsg.TransactionId);

			if (!statusMsg.IsSubscribe)
				return;

			foreach (var order in await _httpClient.GetOrders(cancellationToken))
			{
				var transId = TransactionIdGenerator.GetNextId();
				_orderInfo.Add(transId, RefTuple.Create(order.Id, (decimal)order.Amount, order.CurrencyPair));
				ProcessOrder(order, transId, statusMsg.TransactionId);
			}
		
			SendSubscriptionResult(statusMsg);
		}
	}

	private void ProcessOrder(Order order, long transId, long origTransId)
	{
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			ServerTime = transId != 0 ? order.Timestamp : CurrentTimeUtc,
			SecurityId = order.CurrencyPair.ToStockSharp(),
			TransactionId = transId,
			OriginalTransactionId = origTransId,
			OrderId = order.Id,
			OrderVolume = (decimal)order.InitialAmount,
			Balance = (decimal)order.Amount,
			Side = order.Type.ToSide(),
			OrderPrice = (decimal)order.InitialRate,
			PortfolioName = PortfolioName,
			OrderState = order.Status.ToOrderState(),
		});
	}
}