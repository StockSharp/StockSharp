namespace StockSharp.FTX
{
	partial class FtxMessageAdapter
	{
		private readonly TimeSpan _orderHistoryInterval = TimeSpan.FromDays(7);
		private const int _FillsPaginationLimit = 100;

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

		private void ProcessOrderRegister(OrderRegisterMessage regMsg)
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

			var order = _restClient.RegisterOrder(regMsg.SecurityId.ToCurrency(), regMsg.Side, price, regMsg.OrderType.Value, regMsg.Volume, regMsg.TransactionId.To<string>(), SubaccountName);
			if (order == null)
			{
				throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(regMsg.TransactionId));
			}

			SendProcessOrderStatusResult(order, regMsg);
		}

		private void ProcessOrderCancel(OrderCancelMessage cancelMsg)
		{
			if (cancelMsg.OrderId == null)
				throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));
			if (!_restClient.CancelOrder(cancelMsg.OrderId.Value, SubaccountName))
			{
				throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));
			}
		}

		private void ProcessOrderGroupCancel(OrderGroupCancelMessage cancelMsg)
		{
			if (!_restClient.CancelAllOrders(SubaccountName))
			{
				throw new InvalidOperationException(LocalizedStrings.OrderNoExchangeId.Put(cancelMsg.OriginalTransactionId));
			}
		}

		private bool _isOrderSubscribed;
		private void ProcessOrderStatus(OrderStatusMessage message)
		{

			if (message != null)
			{
				_isOrderSubscribed = message.IsSubscribe;
			}

			if (!_isOrderSubscribed)
			{
				return;
			}

			var orders = _restClient.GetOpenOrders(SubaccountName);

			List<Fill> fills = new();

			if (orders != null && orders.Count > 0)
			{
				var start = orders.Where(x => x.CreatedAt != null).Min(x => x.CreatedAt.Value);
				var fillsResult = _restClient.GetFills(start, DateTime.UtcNow, SubaccountName);
				if (fillsResult != null)
				{
					fills.AddRange(fillsResult);
				}
			}

			if (message == null)
			{
				if (orders != null) foreach (var order in orders) SessionOnNewOrder(order);
				foreach (var fill in fills) SessionOnNewFill(fill);
			}
			else
			{
				if (!message.IsSubscribe)
				{
					return;
				}

				GetPaginationOrderHistory(message);
				GetPaginationFillsFromMarket(DateTime.UtcNow - _orderHistoryInterval, DateTime.UtcNow);
				SendSubscriptionResult(message);
			}
		}

		#region Pagination Fills
		private void GetPaginationFillsFromMarket(DateTime dateTimeFrom, DateTime dateTimeTo)
		{
			var startTime = dateTimeFrom;
			var endTime = (dateTimeTo - dateTimeFrom) < _orderHistoryInterval ? (dateTimeFrom + (dateTimeTo - dateTimeFrom)) : dateTimeFrom + _orderHistoryInterval;

			while (true)
			{
				GetMarketFillsByChunks(startTime, endTime);
				if (endTime == dateTimeTo)
				{
					GetMarketFillsByChunks(endTime, DateTime.UtcNow);
					break;
				}
				startTime = new DateTime(endTime.Ticks);
				endTime = (dateTimeTo - startTime) < _orderHistoryInterval ? (startTime + (dateTimeTo - startTime)) : startTime + _orderHistoryInterval;

			}
		}

		private void GetMarketFillsByChunks(DateTime dateTimeFrom, DateTime dateTimeTo)
		{
			List<Fill> fills = new();
			var startTime = dateTimeFrom;
			var endTime = dateTimeTo;

			while (true)
			{
				var fillsChunk = _restClient.GetFills(startTime, endTime, SubaccountName);
				fillsChunk.Reverse();
				fills.InsertRange(0, fillsChunk);

				if (fillsChunk.Count > 0)
				{
					endTime -= fillsChunk.Last().Time - startTime;
				}
				else
				{
					endTime -= endTime - startTime;
				}

				if (fillsChunk.Count != _FillsPaginationLimit)
				{
					break;
				}
			}

			foreach (var fill in fills)
			{
				SessionOnNewFill(fill);
			}
		}
		#endregion

		#region Pagination Orders
		private void GetPaginationOrderHistory(OrderStatusMessage message)
		{
			List<Order> resultOrders = new();
			var fromTime = DateTime.UtcNow - _orderHistoryInterval;

			do
			{
				var prevFromTime = fromTime;

				var ordersTuple = _restClient.GetMarketOrderHistoryAndHasMoreOrders(SubaccountName, fromTime);

				if(!ordersTuple.histOrders.Any())
					break;

				foreach (var o in ordersTuple.histOrders)
				{
					if(o.CreatedAt > fromTime)
						fromTime = o.CreatedAt.Value;

					resultOrders.Add(o);
				}

				if(!ordersTuple.hasMoreData || fromTime <= prevFromTime)
					break;
			}
			while(true);

			resultOrders = resultOrders
						.Where(o => o.ClientId != null)
						.OrderBy(o => o.CreatedAt)
						.ToList();

			foreach (var order in resultOrders)
				SendProcessOrderStatusResult(order, message);
		}
		#endregion

		private long? _portfolioLookupSubMessageTransactionID;
		private void ProcessPortfolioLookup(PortfolioLookupMessage message)
		{
			if (message != null)
			{
				_portfolioLookupSubMessageTransactionID = message.IsSubscribe ? message.TransactionId : null;

				if (!message.IsSubscribe)
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
			if (message != null)
			{
				SendSubscriptionResult(message);
			}

			var balances = _restClient.GetBalances(SubaccountName);
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
			var futures = _restClient.GetFuturesPositions(SubaccountName);

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
}
