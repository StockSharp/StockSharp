namespace StockSharp.Bitalong
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Bitalong.Native.Model;

	public partial class BitalongMessageAdapter
	{
		private readonly HashSet<string> _orderBookSubscriptions = new(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, long?> _tradesSubscriptions = new(StringComparer.InvariantCultureIgnoreCase);
		private readonly HashSet<string> _level1Subscriptions = new(StringComparer.InvariantCultureIgnoreCase);

		private void ProcessSecurityLookup(SecurityLookupMessage lookupMsg)
		{
			var secTypes = lookupMsg.GetSecurityTypes();

			foreach (var pair in _httpClient.GetSymbols())
			{
				var symbol = pair.Value;

				var secMsg = new SecurityMessage
				{
					SecurityId = pair.Key.ToStockSharp(),
					Decimals = symbol.DecimalPlaces,
					MinVolume = (decimal)symbol.MinAmount,
					OriginalTransactionId = lookupMsg.TransactionId,
				}.FillDefaultCryptoFields();

				if (!secMsg.IsMatch(lookupMsg, secTypes))
					continue;

				SendOutMessage(secMsg);

				SendOutMessage(new Level1ChangeMessage
				{
					SecurityId = pair.Key.ToStockSharp(),
					ServerTime = CurrentTime.ConvertToUtc()
				}
				.TryAdd(Level1Fields.CommissionMaker, (decimal)symbol.FeeSell)
				.TryAdd(Level1Fields.CommissionTaker, (decimal)symbol.FeeBuy));
			}

			SendSubscriptionResult(lookupMsg);
		}

		private void ProcessMarketData(MarketDataMessage mdMsg)
		{
			var secId = mdMsg.SecurityId;
			var transId = mdMsg.TransactionId;

			var symbol = secId.ToNative();

			switch (mdMsg.DataType)
			{
				case MarketDataTypes.Level1:
				{
					if (mdMsg.IsSubscribe)
					{
						ProcessLevel1Subscriptions(new[] { symbol });
						_level1Subscriptions.Add(symbol);
					}
					else
						_level1Subscriptions.Remove(symbol);

					break;
				}
				case MarketDataTypes.Trades:
				{
					if (mdMsg.IsSubscribe)
					{
						if (mdMsg.To != null)
						{
							SendSubscriptionReply(mdMsg.TransactionId);

							ProcessTicksSubscription(mdMsg.TransactionId, symbol);
							SendSubscriptionResult(mdMsg);
							return;
						}
						else
							ProcessTicksSubscription(0, symbol);
					}
					else
					{
						_tradesSubscriptions.Remove(symbol);
					}

					break;
				}
				case MarketDataTypes.MarketDepth:
				{
					if (mdMsg.IsSubscribe)
					{
						ProcessOrderBookSubscription(symbol);
						_orderBookSubscriptions.Add(symbol);
					}
					else
						_orderBookSubscriptions.Remove(symbol);

					break;
				}
				default:
				{
					SendSubscriptionNotSupported(transId);
					return;
				}
			}

			SendSubscriptionReply(transId);
		}

		private void ProcessSubscriptions()
		{
			foreach (var symbol in _orderBookSubscriptions)
			{
				ProcessOrderBookSubscription(symbol);
			}

			foreach (var symbol in _tradesSubscriptions.Keys.ToArray())
			{
				ProcessTicksSubscription(0, symbol);
			}

			if (_level1Subscriptions.Count > 0)
			{
				ProcessLevel1Subscriptions(_level1Subscriptions.ToArray());
			}
		}

		private void ProcessOrderBookSubscription(string symbol)
		{
			var book = _httpClient.GetOrderBook(symbol);

			SendOutMessage(new QuoteChangeMessage
			{
				SecurityId = symbol.ToStockSharp(),
				Bids = book.Bids?.Select(e => new QuoteChange((decimal)e.Price, (decimal)e.Size)).ToArray() ?? Array.Empty<QuoteChange>(),
				Asks = book.Asks?.Select(e => new QuoteChange((decimal)e.Price, (decimal)e.Size)).ToArray() ?? Array.Empty<QuoteChange>(),
				ServerTime = CurrentTime.ConvertToUtc(),
			});
		}

		private void ProcessTicksSubscription(long transId, string symbol)
		{
			var secId = symbol.ToStockSharp();

			var lastId = _tradesSubscriptions.TryGetValue(symbol);

			foreach (var trade in _httpClient.GetTradeHistory(symbol).OrderBy(t => t.Timestamp))
			{
				if (lastId != null && trade.Id <= lastId)
					continue;

				lastId = trade.Id;
				ProcessTick(transId, secId, trade);

				_tradesSubscriptions[symbol] = lastId;
			}
		}

		private void ProcessLevel1Subscriptions(string[] symbols)
		{
			void ProcessTicker(string symbol, Ticker ticker)
			{
				SendOutMessage(new Level1ChangeMessage
				{
					SecurityId = symbol.ToStockSharp(),
					ServerTime = CurrentTime.ConvertToUtc(),
				}
				.TryAdd(Level1Fields.HighPrice, (decimal?)ticker.High24)
				.TryAdd(Level1Fields.LowPrice, (decimal?)ticker.Low24)
				.TryAdd(Level1Fields.LastTradePrice, (decimal?)ticker.Last)
				.TryAdd(Level1Fields.HighBidPrice, (decimal?)ticker.HighestBid)
				.TryAdd(Level1Fields.LowAskPrice, (decimal?)ticker.LowestAsk)
				.TryAdd(Level1Fields.Volume, (decimal?)ticker.QuoteVolume)
				.TryAdd(Level1Fields.Change, (decimal?)ticker.PercentChange));
			}
			
			if (symbols.Length > 2)
			{
				foreach (var pair in _httpClient.GetTickers())
				{
					ProcessTicker(pair.Key, pair.Value);
				}
			}
			else
			{
				foreach (var symbol in symbols)
				{
					ProcessTicker(symbol, _httpClient.GetTicker(symbol));
				}
			}
		}

		private void ProcessTick(long transactionId, SecurityId securityId, Native.Model.Trade trade)
		{
			SendOutMessage(new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				SecurityId = securityId,
				OriginSide = trade.Type.ToSide(),
				TradeId = trade.Id,
				TradePrice = (decimal)trade.Price,
				TradeVolume = (decimal)trade.Amount,
				ServerTime = trade.Timestamp,
				OriginalTransactionId = transactionId,
			});
		}
	}
}