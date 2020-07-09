namespace StockSharp.BitStamp
{
	using System;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.BitStamp.Native.Model;
	using StockSharp.Messages;

	using Order = Native.Model.Order;
	using Trade = Native.Model.Trade;

	partial class BitStampMessageAdapter
	{
		private const string _eurusd = "eurusd";

		private void SessionOnNewTrade(string pair, Trade trade)
		{
			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Tick,
				SecurityId = pair.ToStockSharp(),
				TradeId = trade.Id,
				TradePrice = (decimal)trade.Price,
				TradeVolume = (decimal)trade.Amount,
				ServerTime = trade.Time,
				OriginSide = trade.Type.ToSide(),
			});
		}

		private void SessionOnNewOrderBook(string pair, OrderBook book)
		{
			SendOutMessage(new QuoteChangeMessage
			{
				SecurityId = pair.ToStockSharp(),
				Bids = book.Bids.Select(e => new QuoteChange(e.Price, e.Size)).ToArray(),
				Asks = book.Asks.Select(e => new QuoteChange(e.Price, e.Size)).ToArray(),
				ServerTime = book.Time,
			});
		}

		private void SessionOnNewOrderLog(string pair, OrderStates state, Order order)
		{
			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.OrderLog,
				SecurityId = pair.ToStockSharp(),
				ServerTime = order.Time,
				OrderVolume = (decimal)order.Amount,
				OrderPrice = (decimal)order.Price,
				OrderId = order.Id,
				Side = order.Type.ToSide(),
				OrderState = state,
			});
		}

		private void ProcessMarketData(MarketDataMessage mdMsg)
		{
			if (!mdMsg.SecurityId.IsAssociated())
			{
				SendSubscriptionNotSupported(mdMsg.TransactionId);
				return;
			}

			var currency = mdMsg.SecurityId.ToCurrency();

			if (mdMsg.DataType2 == DataType.OrderLog)
			{
				if (mdMsg.IsSubscribe)
					_pusherClient.SubscribeOrderLog(currency);
				else
					_pusherClient.UnSubscribeOrderLog(currency);
			}
			else if (mdMsg.DataType2 == DataType.MarketDepth)
			{
				if (mdMsg.IsSubscribe)
					_pusherClient.SubscribeOrderBook(currency);
				else
					_pusherClient.UnSubscribeOrderBook(currency);
			}
			else if (mdMsg.DataType2 == DataType.Ticks)
			{
				if (mdMsg.IsSubscribe)
				{
					if (mdMsg.To != null)
					{
						SendSubscriptionReply(mdMsg.TransactionId);

						var diff = DateTimeOffset.Now - (mdMsg.From ?? DateTime.Today);

						string interval;

						if (diff.TotalMinutes < 1)
							interval = "minute";
						else if (diff.TotalDays < 1)
							interval = "hour";
						else
							interval = "day";

						var trades = _httpClient.RequestTransactions(currency, interval);

						foreach (var trade in trades.OrderBy(t => t.Time))
						{
							SendOutMessage(new ExecutionMessage
							{
								ExecutionType = ExecutionTypes.Tick,
								SecurityId = mdMsg.SecurityId,
								TradeId = trade.Id,
								TradePrice = (decimal)trade.Price,
								TradeVolume = trade.Amount.ToDecimal(),
								ServerTime = trade.Time,
								OriginSide = trade.Type.ToSide(),
								OriginalTransactionId = mdMsg.TransactionId
							});
						}

						SendSubscriptionResult(mdMsg);
						return;
					}
					else
						_pusherClient.SubscribeTrades(currency);
				}
				else
				{
					_pusherClient.UnSubscribeTrades(currency);
				}
			}
			else
			{
				SendSubscriptionNotSupported(mdMsg.TransactionId);
				return;
			}

			SendSubscriptionReply(mdMsg.TransactionId);
		}

		private void ProcessSecurityLookup(SecurityLookupMessage lookupMsg)
		{
			var secTypes = lookupMsg.GetSecurityTypes();

			foreach (var info in _httpClient.GetPairsInfo())
			{
				var secMsg = new SecurityMessage
				{
					SecurityId = info.Name.ToStockSharp(),
					SecurityType = info.UrlSymbol == _eurusd ? SecurityTypes.Currency : SecurityTypes.CryptoCurrency,
					MinVolume = info.MinimumOrder.Substring(0, info.MinimumOrder.IndexOf(' ')).To<decimal>(),
					Decimals = info.BaseDecimals,
					Name = info.Description,
					VolumeStep = info.UrlSymbol == _eurusd ? 0.00001m : 0.00000001m,
					OriginalTransactionId = lookupMsg.TransactionId,
				};

				if (!secMsg.IsMatch(lookupMsg, secTypes))
					continue;

				SendOutMessage(secMsg);
			}

			SendSubscriptionResult(lookupMsg);
		}
	}
}