namespace StockSharp.Bitexbook
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	using Order = StockSharp.Bitexbook.Native.Model.Order;

	public partial class BitexbookMessageAdapter
	{
		private string PortfolioName => nameof(Bitexbook) + "_" + Key.ToId();

		private readonly SynchronizedDictionary<long, RefTriple<long, decimal, string>> _orderInfo = new();

		private void ProcessOrderRegister(OrderRegisterMessage regMsg)
		{
			var symbol = regMsg.SecurityId.ToNative();

			switch (regMsg.OrderType)
			{
				case null:
				case OrderTypes.Limit:
				case OrderTypes.Market:
					break;
				case OrderTypes.Conditional:
				{
					var condition = (BitexbookOrderCondition)regMsg.Condition;

					if (!condition.IsWithdraw)
						throw new NotSupportedException(LocalizedStrings.OrderUnsupportedType.Put(regMsg.OrderType, regMsg.TransactionId));

					var withdrawId = _httpClient.Withdraw(symbol, regMsg.Volume, condition.WithdrawInfo);

					SendOutMessage(new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						OrderId = withdrawId,
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
			var price = regMsg.OrderType == OrderTypes.Market ? (decimal?)null : regMsg.Price;

			var orderId = _httpClient.RegisterOrder(symbol, regMsg.Side.ToNative(), price, regMsg.Volume);

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

		private void ProcessPortfolioLookup(PortfolioLookupMessage message)
		{
			if (message != null)
			{
				if (!message.IsSubscribe)
					return;

				SendOutMessage(new PortfolioMessage
				{
					PortfolioName = PortfolioName,
					BoardCode = BoardCodes.Bitexbook,
					OriginalTransactionId = message.TransactionId
				});

				SendSubscriptionResult(message);
			}

			//ProcessPosition("BTC", balance.Free?.Btc, balance.Freezed?.Btc);
			//ProcessPosition("BCH", balance.Free?.Bch, balance.Freezed?.Bch);
			//ProcessPosition("ETC", balance.Free?.Etc, balance.Freezed?.Etc);
			//ProcessPosition("ETH", balance.Free?.Eth, balance.Freezed?.Eth);
			//ProcessPosition("LTC", balance.Free?.Ltc, balance.Freezed?.Ltc);
			//ProcessPosition("USD", balance.Free?.Usd, balance.Freezed?.Usd);

			_lastTimeBalanceCheck = CurrentTime;
		}

		private void ProcessOrderStatus(OrderStatusMessage message)
		{
			if (message == null)
			{
				
			}
			else
			{
				if (!message.IsSubscribe)
					return;
			
				SendSubscriptionResult(message);
			}
		}

		private void ProcessOrder(SecurityId secId, Order order, long transId, long origTransId, OrderStates state)
		{
			SendOutMessage(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				ServerTime = (transId != 0 ? order.CreatedTimestamp : order.ClosedTimestamp) ?? CurrentTime.ConvertToUtc(),
				SecurityId = secId,
				TransactionId = transId,
				OriginalTransactionId = origTransId,
				OrderStringId = order.Id,
				OrderVolume = (decimal)order.Volume,
				Balance = order.GetBalance(),
				Side = order.Type.ToSide(),
				OrderPrice = (decimal)(order.Price ?? 0),
				PortfolioName = PortfolioName,
				OrderState = state,
			});
		}

		private void ProcessPosition(string currency, decimal? free, decimal? freezed)
		{
			if (free == null && freezed == null)
				return;

			SendOutMessage(new PositionChangeMessage
			{
				PortfolioName = PortfolioName,
				SecurityId = currency.ToStockSharp(),
				ServerTime = CurrentTime.ConvertToUtc(),
			}
			.TryAdd(PositionChangeTypes.CurrentValue, free, true)
			.TryAdd(PositionChangeTypes.BlockedValue, freezed, true));
		}
	}
}