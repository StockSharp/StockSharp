namespace StockSharp.Coinbase;

public partial class CoinbaseMessageAdapter
{
	private readonly Dictionary<string, RefPair<long, decimal>> _orderInfo = new(StringComparer.InvariantCultureIgnoreCase);

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

				var withdrawId = await _httpClient.Withdraw(regMsg.SecurityId.SecurityCode, regMsg.Volume, condition.WithdrawInfo, cancellationToken);

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
		
		var result = await _httpClient.RegisterOrder(regMsg.SecurityId.ToCurrency(), regMsg.OrderType.ToNative(),
			regMsg.Side, price, condition?.StopPrice, regMsg.Volume, regMsg.TimeInForce, regMsg.TillDate.EnsureToday()/*, regMsg.TransactionId*/, cancellationToken);

		var orderState = result.Status.ToOrderState();

		if (orderState == OrderStates.Failed)
		{
			SendOutMessage(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				ServerTime = result.CreatedAt,
				OriginalTransactionId = regMsg.TransactionId,
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException(),
				HasOrderInfo = true,
			});

			return;
		}
			
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderStringId = result.Id,
			ServerTime = result.CreatedAt,
			OriginalTransactionId = regMsg.TransactionId,
			OrderState = isMarket ? OrderStates.Done : orderState,
			Balance = isMarket ? 0 : null,
			HasOrderInfo = true,
		});

		if (isMarket)
		{
			await ProcessOwnTrades(result.Id, regMsg.TransactionId, cancellationToken);
		}
		else
		{
			_orderInfo.Add(result.Id, RefTuple.Create(regMsg.TransactionId, regMsg.Volume));
		}

		await PortfolioLookupAsync(null, cancellationToken);
	}

	/// <inheritdoc />
	public override async ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		if (cancelMsg.OrderStringId.IsEmpty())
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		await _httpClient.CancelOrder(cancelMsg.OrderStringId, cancellationToken);

		/*var order = */await _httpClient.GetOrderInfo(cancelMsg.OrderStringId, cancellationToken);

		//if (order == null)
		//{
		//	_orderInfo.Remove(cancelMsg.OrderStringId);

		//	SendOutMessage(new ExecutionMessage
		//	{
		//		DataTypeEx = DataType.Transactions,
		//		ServerTime = CurrentTime.ConvertToUtc(),
		//		OriginalTransactionId = cancelMsg.TransactionId,
		//		OrderState = OrderStates.Done,
		//		HasOrderInfo = true,
		//	});
		//}
		//else
		//	ProcessOrder(order, 0, cancelMsg.TransactionId, true);

		await OrderStatusAsync(null, cancellationToken);
		await PortfolioLookupAsync(null, cancellationToken);
	}

	/// <inheritdoc />
	public override async ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		var ids = await _httpClient.CancelAllOrders(cancellationToken);

		foreach (var id in ids)
		{
			if (!_orderInfo.TryGetValue(id, out var info))
				continue;

			var order = await _httpClient.GetOrderInfo(id, cancellationToken);

			if (order == null)
			{
				_orderInfo.Remove(id);

				SendOutMessage(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					ServerTime = CurrentTime.ConvertToUtc(),
					OriginalTransactionId = info.First,
					OrderState = OrderStates.Done,
					HasOrderInfo = true,
				});
			}
			else
				await ProcessOrder(order, 0, cancelMsg.TransactionId, true, cancellationToken);
		}

		await PortfolioLookupAsync(null, cancellationToken);
	}

	/// <inheritdoc />
	public override async ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		var transId = lookupMsg?.TransactionId ?? 0;

		if (lookupMsg != null)
		{
			SendSubscriptionReply(transId);

			if (!lookupMsg.IsSubscribe)
				return;

			SendOutMessage(new PortfolioMessage
			{
				PortfolioName = PortfolioName,
				BoardCode = BoardCodes.Coinbase,
				OriginalTransactionId = transId,
			});
		}

		var accounts = await _httpClient.GetAccounts(cancellationToken);

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
			.TryAdd(PositionChangeTypes.CurrentValue, account.Available.RemoveTrailingZeros(), true)
			.TryAdd(PositionChangeTypes.BlockedValue, account.Hold.RemoveTrailingZeros(), true));
		}

		if (lookupMsg != null)
			SendSubscriptionResult(lookupMsg);

		_lastTimeBalanceCheck = CurrentTime;
	}

	/// <inheritdoc />
	public override async ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		if (statusMsg == null)
		{
			var portfolioRefresh = false;

			var ids = _orderInfo.Keys.ToIgnoreCaseSet();

			var orders = await _httpClient.GetOrders(new[] { "open", "pending" }, cancellationToken);

			foreach (var order in orders)
			{
				var info = _orderInfo.TryGetValue(order.Id);

				var balance = order.GetBalance();

				if (info == null)
				{
					var transId = TransactionIdGenerator.GetNextId();

					_orderInfo.Add(order.Id, RefTuple.Create(transId, balance));

					await ProcessOrder(order, transId, 0, true, cancellationToken);

					portfolioRefresh = true;
				}
				else
				{
					ids.Remove(order.Id);

					await ProcessOrder(order, 0, info.First, balance < info.Second, cancellationToken);

					info.Second = balance;

					//portfolioRefresh = true;
				}
			}

			foreach (var id in ids)
			{
				var info = _orderInfo.GetAndRemove(id);
				var order = await _httpClient.GetOrderInfo(id, cancellationToken);

				if (order == null)
				{
					SendOutMessage(new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						ServerTime = CurrentTime.ConvertToUtc(),
						OriginalTransactionId = info.First,
						OrderState = OrderStates.Done,
						HasOrderInfo = true,
					});
				}
				else
				{
					await ProcessOrder(order, 0, info.First, true, cancellationToken);
				}

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

			var orders = await _httpClient.GetOrders(new[] { "open", "pending" }, cancellationToken);

			foreach (var order in orders)
			{
				var transId = TransactionIdGenerator.GetNextId();

				_orderInfo.Add(order.Id, RefTuple.Create(transId, order.GetBalance()));

				await ProcessOrder(order, transId, statusMsg.TransactionId, true, cancellationToken);
			}
		
			SendSubscriptionResult(statusMsg);
		}
	}

	private async ValueTask ProcessOrder(Order order, long transId, long originTransId, bool processFills, CancellationToken cancellationToken)
	{
		var state = order.Status.ToOrderState();

		SendOutMessage(new ExecutionMessage
		{
			ServerTime = transId == 0 ? CurrentTime.ConvertToUtc() : order.CreatedAt,
			DataTypeEx = DataType.Transactions,
			SecurityId = order.Product.ToStockSharp(),
			TransactionId = transId,
			OriginalTransactionId = originTransId,
			OrderState = state,
			Error = state == OrderStates.Failed ? new InvalidOperationException() : null,
			OrderType = order.Type.ToOrderType(),
			Side = order.Side.ToSide(),
			OrderStringId = order.Id,
			OrderPrice = order.Price?.ToDecimal() ?? 0,
			OrderVolume = order.Size?.ToDecimal(),
			TimeInForce = order.TimeInForce.ToTimeInForce(),
			Balance = order.GetBalance(),
			HasOrderInfo = true,
		});

		if (state == OrderStates.Done || state == OrderStates.Failed)
			_orderInfo.Remove(order.Id);

		if (processFills && order.FilledSize > 0)
		{
			await ProcessOwnTrades(order.Id, transId == 0 ? originTransId : transId, cancellationToken);
		}
	}

	private async ValueTask ProcessOwnTrades(string orderId, long transId, CancellationToken cancellationToken)
	{
		var fills = await _httpClient.GetFills(orderId, cancellationToken);

		foreach (var fill in fills)
		{
			SendOutMessage(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = transId,
				ServerTime = fill.CreatedAt,
				TradeId = fill.TradeId,
				TradePrice = fill.Price,
				TradeVolume = fill.Size,
				Commission = fill.Fee,
				OrderStringId = fill.OrderId,
				OriginSide = fill.Liquidity.IsEmpty() ? null : (fill.Liquidity == "T" ? Sides.Buy : Sides.Sell),
			});
		}
	}
}