namespace StockSharp.Coinbase;

public partial class CoinbaseMessageAdapter
{
	private readonly Dictionary<string, RefPair<long, decimal>> _orderInfo = new(StringComparer.InvariantCultureIgnoreCase);

	private string PortfolioName => nameof(Coinbase) + "_" + Key.ToId();

	private void ProcessOrderRegister(OrderRegisterMessage regMsg)
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

				var withdrawId = _httpClient.Withdraw(regMsg.SecurityId.SecurityCode, regMsg.Volume, condition.WithdrawInfo);

				SendOutMessage(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					OrderStringId = withdrawId,
					ServerTime = CurrentTime.ConvertToUtc(),
					OriginalTransactionId = regMsg.TransactionId,
					OrderState = OrderStates.Done,
					HasOrderInfo = true,
				});

				ProcessPortfolioLookup(null);
				return;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));
		}

		var isMarket = regMsg.OrderType == OrderTypes.Market;
		var price = isMarket ? (decimal?)null : regMsg.Price;
		
		var result = _httpClient.RegisterOrder(regMsg.SecurityId.ToCurrency(), regMsg.OrderType.ToNative(),
			regMsg.Side, price, condition?.StopPrice, regMsg.Volume, regMsg.TimeInForce, regMsg.TillDate.EnsureToday()/*, regMsg.TransactionId*/);

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
			ProcessOwnTrades(result.Id, regMsg.TransactionId);
		}
		else
		{
			_orderInfo.Add(result.Id, RefTuple.Create(regMsg.TransactionId, regMsg.Volume));
		}

		ProcessPortfolioLookup(null);
	}

	private void ProcessOrderCancel(OrderCancelMessage cancelMsg)
	{
		if (cancelMsg.OrderStringId.IsEmpty())
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		_httpClient.CancelOrder(cancelMsg.OrderStringId);

		/*var order = */_httpClient.GetOrderInfo(cancelMsg.OrderStringId);

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

		ProcessOrderStatus(null);
		ProcessPortfolioLookup(null);
	}

	private void ProcessOrderGroupCancel(OrderGroupCancelMessage cancelMsg)
	{
		var ids = _httpClient.CancelAllOrders();

		foreach (var id in ids)
		{
			if (!_orderInfo.TryGetValue(id, out var info))
				continue;

			var order = _httpClient.GetOrderInfo(id);

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
				ProcessOrder(order, 0, cancelMsg.TransactionId, true);
		}

		ProcessPortfolioLookup(null);
	}

	private void ProcessPortfolioLookup(PortfolioLookupMessage message)
	{
		var transId = message?.TransactionId ?? 0;

		if (message != null)
		{
			if (!message.IsSubscribe)
				return;

			SendOutMessage(new PortfolioMessage
			{
				PortfolioName = PortfolioName,
				BoardCode = BoardCodes.Coinbase,
				OriginalTransactionId = transId,
			});
		}

		var accounts = _httpClient.GetAccounts();

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

		if (message != null)
			SendSubscriptionResult(message);

		_lastTimeBalanceCheck = CurrentTime;
	}

	private void ProcessOrderStatus(OrderStatusMessage message)
	{
		if (message == null)
		{
			var portfolioRefresh = false;

			var ids = _orderInfo.Keys.ToIgnoreCaseSet();

			var orders = _httpClient.GetOrders("open", "pending");

			foreach (var order in orders)
			{
				var info = _orderInfo.TryGetValue(order.Id);

				var balance = order.GetBalance();

				if (info == null)
				{
					var transId = TransactionIdGenerator.GetNextId();

					_orderInfo.Add(order.Id, RefTuple.Create(transId, balance));

					ProcessOrder(order, transId, 0, true);

					portfolioRefresh = true;
				}
				else
				{
					ids.Remove(order.Id);

					ProcessOrder(order, 0, info.First, balance < info.Second);

					info.Second = balance;

					//portfolioRefresh = true;
				}
			}

			foreach (var id in ids)
			{
				var info = _orderInfo.GetAndRemove(id);
				var order = _httpClient.GetOrderInfo(id);

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
					ProcessOrder(order, 0, info.First, true);
				}

				portfolioRefresh = true;
			}

			if (portfolioRefresh)
				ProcessPortfolioLookup(null);
		}
		else
		{
			if (!message.IsSubscribe)
				return;

			var orders = _httpClient.GetOrders("open", "pending");

			foreach (var order in orders)
			{
				var transId = TransactionIdGenerator.GetNextId();

				_orderInfo.Add(order.Id, RefTuple.Create(transId, order.GetBalance()));

				ProcessOrder(order, transId, message.TransactionId, true);
			}
		
			SendSubscriptionResult(message);
		}
	}

	private void ProcessOrder(Native.Model.Order order, long transId, long originTransId, bool processFills)
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
			ProcessOwnTrades(order.Id, transId == 0 ? originTransId : transId);
		}
	}

	private void ProcessOwnTrades(string orderId, long transId)
	{
		var fills = _httpClient.GetFills(orderId);

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