namespace StockSharp.BitStamp
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.BitStamp.Native;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The message adapter for BitStamp.
	/// </summary>
	partial class BitStampMessageAdapter
	{
		private void ProcessOrderRegister(OrderRegisterMessage regMsg)
		{
			var order = _httpClient.RegisterOrder(regMsg.Side, regMsg.Price, regMsg.Volume);

			_orderInfo.Add(order.Id, RefTuple.Create(regMsg.TransactionId, regMsg.Volume));

			_hasActiveOrders = true;

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Order,
				OrderId = order.Id,
				ServerTime = order.Time.ApplyTimeZone(TimeZoneInfo.Utc),
				OriginalTransactionId = regMsg.TransactionId,
				OrderState = OrderStates.Active,
			});
		}

		private void ProcessOrderCancel(OrderCancelMessage cancelMsg)
		{
			if (cancelMsg.OrderId == null)
				throw new InvalidOperationException(LocalizedStrings.Str2252Params.Put(cancelMsg.OrderTransactionId));

			var isOk = _httpClient.CancelOrder(cancelMsg.OrderId.Value);

			SendOutMessage(new ExecutionMessage
			{
				ServerTime = CurrentTime.Convert(TimeZoneInfo.Utc),
				ExecutionType = ExecutionTypes.Order,
				OriginalTransactionId = cancelMsg.TransactionId,
				OrderState = isOk ? OrderStates.Done : OrderStates.Failed,
				Error = isOk ? null : new InvalidOperationException(LocalizedStrings.Str3300),
			});
		}

		private void ProcessBalance(Balance balance)
		{
			SendOutMessage(this.CreatePortfolioChangeMessage(GetPortfolioName())
				.Add(PositionChangeTypes.CurrentValue, (decimal)balance.UsdAvailable)
				.Add(PositionChangeTypes.BlockedValue, (decimal)balance.UsdReserved)
				.Add(PositionChangeTypes.Commission, (decimal)balance.Fee));

			SendOutMessage(
				this.CreatePositionChangeMessage(GetPortfolioName(), _btcUsd)
				.Add(PositionChangeTypes.CurrentValue, (decimal)balance.BtcAvailable)
				.Add(PositionChangeTypes.BlockedValue, (decimal)balance.BtcReserved));
		}

		private string GetPortfolioName()
		{
			return ClientId.To<string>();
		}

		private void ProcessOrder(Order order)
		{
			var info = _orderInfo[order.Id];

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Order,
				OrderId = order.Id,
				OriginalTransactionId = info.First,
				Price = (decimal)order.Price,
				Balance = info.Second,
				Volume = (decimal)order.Amount,
				Side = order.Type.ToStockSharp(),
				SecurityId = _btcUsd,
				ServerTime = order.Time.ApplyTimeZone(TimeZoneInfo.Utc),
				PortfolioName = GetPortfolioName(),
				OrderState = OrderStates.Active
			});
		}

		private void ProcessExecution(UserTransaction trade)
		{
			if (_lastMyTradeId >= trade.Id)
				return;

			_hasMyTrades = true;

			_lastMyTradeId = trade.Id;

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Trade,
				OrderId = trade.OrderId,
				TradeId = trade.Id,
				TradePrice = (decimal)trade.UsdAmount,
				Volume = (decimal)trade.BtcAmount,
				SecurityId = _btcUsd,
				ServerTime = trade.Time.ApplyTimeZone(TimeZoneInfo.Utc),
				PortfolioName = GetPortfolioName(),
			});

			var info = _orderInfo.TryGetValue(trade.OrderId);

			if (info == null || info.Second <= 0)
				return;

			info.Second -= (decimal)trade.BtcAmount;

			if (info.Second < 0)
				throw new InvalidOperationException(LocalizedStrings.Str3301Params.Put(trade.OrderId, info.Second));

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Order,
				OrderId = trade.OrderId,
				Balance = info.Second,
				OrderState = info.Second > 0 ? OrderStates.Active : OrderStates.Done
			});
		}

		private void ProcessOrderStatus()
		{
			if (_requestOrderFirst)
			{
				_requestOrderFirst = false;

				var orders = _httpClient.RequestOpenOrders().ToArray();

				foreach (var o in orders)
				{
					var order = o;
					_orderInfo.SafeAdd(order.Id, key => RefTuple.Create(TransactionIdGenerator.GetNextId(), (decimal)order.Amount));
				}

				var trades = _httpClient.RequestUserTransactions().ToArray();

				foreach (var trade in trades.OrderBy(t => t.Id))
				{
					var info = _orderInfo.TryGetValue(trade.OrderId);

					if (info == null)
						continue;

					info.Second -= (decimal)trade.BtcAmount;
				}

				_hasActiveOrders = false;

				foreach (var order in orders)
				{
					_hasActiveOrders = true;
					ProcessOrder(order);
				}

				_hasMyTrades = false;

				foreach (var trade in trades)
				{
					ProcessExecution(trade);
				}

				return;
			}

			if (_hasMyTrades)
			{
				var transactions = _httpClient.RequestUserTransactions();

				_hasMyTrades = false;

				foreach (var trade in transactions.Where(t => t.Type == 2).OrderBy(t => t.Id))
				{
					ProcessExecution(trade);
				}
			}

			if (_hasActiveOrders)
			{
				var orders = _httpClient.RequestOpenOrders();

				_hasActiveOrders = false;

				foreach (var order in orders)
				{
					_hasActiveOrders = true;
					ProcessOrder(order);
				}
			}
		}

		private void ProcessPortfolioLookup(PortfolioLookupMessage message)
		{
			//SendOutMessage(new PortfolioMessage
			//{
			//	PortfolioName = GetPortfolioName()
			//});

			ProcessBalance(_httpClient.RequestBalance());

			if (message != null)
				SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = message.TransactionId });
		}
	}
}
