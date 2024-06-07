namespace StockSharp.Bitalong;

public partial class BitalongMessageAdapter
{
	private string PortfolioName => nameof(Bitalong) + "_" + Key.ToId();

	private readonly SynchronizedDictionary<long, RefTriple<long, decimal, string>> _orderInfo = new();

	private void ProcessOrderRegister(OrderRegisterMessage regMsg)
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

				_httpClient.Withdraw(symbol, regMsg.Volume, condition.WithdrawInfo);

				SendOutMessage(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
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

		var orderId = _httpClient.RegisterOrder(symbol, regMsg.Side.ToNative(), regMsg.Price, regMsg.Volume);

		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OrderId = orderId,
			ServerTime = CurrentTime.ConvertToUtc(),
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

		ProcessPortfolioLookup(null);
	}

	private void ProcessOrderCancel(OrderCancelMessage cancelMsg)
	{
		if (cancelMsg.OrderId == null)
			throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));

		_httpClient.CancelOrder(cancelMsg.SecurityId.ToNative(), cancelMsg.OrderId.Value);

		//SendOutMessage(new ExecutionMessage
		//{
		//	ServerTime = CurrentTime.ConvertToUtc(),
		//	DataTypeEx = DataType.Transactions,
		//	OriginalTransactionId = cancelMsg.TransactionId,
		//	OrderState = OrderStates.Done,
		//	HasOrderInfo = true,
		//});

		ProcessOrderStatus(null);
		ProcessPortfolioLookup(null);
	}

	private void ProcessOrderGroupCancel(OrderGroupCancelMessage message)
	{
		_httpClient.CancelAllOrders(message.SecurityId.ToNative(), message.Side);
	}

	private void ProcessPortfolioLookup(PortfolioLookupMessage message)
	{
		if (message != null)
		{
			if (!message.IsSubscribe)
				return;

			SendOutMessage(new PortfolioMessage
			{
				PortfolioName = PortfolioName,
				BoardCode = BoardCodes.Bitalong,
				OriginalTransactionId = message.TransactionId
			});

			SendSubscriptionResult(message);
		}

		var tuple = _httpClient.GetBalances();

		PositionChangeMessage CreateMessage(string asset)
		{
			return new PositionChangeMessage
			{
				PortfolioName = PortfolioName,
				SecurityId = asset.ToStockSharp(),
				ServerTime = CurrentTime.ConvertToUtc(),
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

		_lastTimeBalanceCheck = CurrentTime;
	}

	private void ProcessOrderStatus(OrderStatusMessage message)
	{
		if (message == null)
		{
			if (_orderInfo.Count == 0)
				return;

			var portfolioRefresh = false;

			var opened = _httpClient.GetOrders();

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
						ServerTime = CurrentTime.ConvertToUtc(),
						Balance = balance,
					});

					//ProcessTrades(order.Trades, info.Third, info.Second);

					portfolioRefresh = true;
				}
			}

			foreach (var orderId in uuids)
			{
				var info = _orderInfo.GetAndRemove(orderId);

				var order = _httpClient.GetOrderInfo(info.Third, orderId);

				ProcessOrder(order, 0, info.First);
				//ProcessTrades(order.Trades, info.Third, transId);

				portfolioRefresh = true;
			}

			if (portfolioRefresh)
				ProcessPortfolioLookup(null);
		}
		else
		{
			if (!message.IsSubscribe)
				return;

			foreach (var order in _httpClient.GetOrders())
			{
				var transId = TransactionIdGenerator.GetNextId();
				_orderInfo.Add(transId, RefTuple.Create(order.Id, (decimal)order.Amount, order.CurrencyPair));
				ProcessOrder(order, transId, message.TransactionId);
			}
		
			SendSubscriptionResult(message);
		}
	}

	private void ProcessOrder(Order order, long transId, long origTransId)
	{
		SendOutMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			ServerTime = transId != 0 ? order.Timestamp : CurrentTime.ConvertToUtc(),
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