namespace StockSharp.Btce
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class BtceMessageAdapter
	{
		private readonly CachedSynchronizedSet<SecurityId> _subscribedLevel1 = new CachedSynchronizedSet<SecurityId>();
		private readonly CachedSynchronizedDictionary<SecurityId, int> _subscribedDepths = new CachedSynchronizedDictionary<SecurityId, int>();
		private readonly CachedSynchronizedSet<SecurityId> _subscribedTicks = new CachedSynchronizedSet<SecurityId>();
		private int _tickCount = 2000;
		private long _lastTickId;

		private void ProcessSecurityLookup(SecurityLookupMessage lookupMsg)
		{
			var reply = Session.GetInstruments();

			foreach (var info in reply.Items.Values)
			{
				var secId = new SecurityId
				{
					SecurityCode = info.Name.ToStockSharpCode(),
					BoardCode = _boardCode,
				};

				// NOTE сейчас BTCE транслирует для данного тикера
				// кол-во знаков после запятой 3 и мин цена 0.0001
				if (secId.SecurityCode.CompareIgnoreCase("ltc/eur"))
					info.MinPrice = 0.001;

				// NOTE сейчас BTCE транслирует для данного тикера
				// кол-во знаков после запятой 2, но цены содержат 5 знаков
				if (secId.SecurityCode.CompareIgnoreCase("btc/cnh"))
					info.DecimalDigits = 5;
				
				SendOutMessage(new SecurityMessage
				{
					SecurityId = secId,
					PriceStep = info.DecimalDigits.GetPriceStep(),
					VolumeStep = (decimal)info.MinVolume,
					SecurityType = SecurityTypes.CryptoCurrency,
					OriginalTransactionId = lookupMsg.TransactionId,
				});

				SendOutMessage(new Level1ChangeMessage
				{
					SecurityId = secId,
					ServerTime = reply.Timestamp.ApplyTimeZone(TimeHelper.Moscow)
				}
				.TryAdd(Level1Fields.MinPrice, (decimal)info.MinPrice)
				.TryAdd(Level1Fields.MaxPrice, (decimal)info.MaxPrice)
				.Add(Level1Fields.State, info.IsHidden ? SecurityStates.Stoped : SecurityStates.Trading));
			}

			SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = lookupMsg.TransactionId });
		}

		private void ProcessMarketData(MarketDataMessage mdMsg)
		{
			switch (mdMsg.DataType)
			{
				case MarketDataTypes.Level1:
				{
					if (mdMsg.IsSubscribe)
						_subscribedLevel1.Add(mdMsg.SecurityId);
					else
						_subscribedLevel1.Remove(mdMsg.SecurityId);

					break;
				}
				case MarketDataTypes.MarketDepth:
				{
					if (mdMsg.IsSubscribe)
						_subscribedDepths.Add(mdMsg.SecurityId, mdMsg.MaxDepth);
					else
						_subscribedDepths.Remove(mdMsg.SecurityId);

					break;
				}
				case MarketDataTypes.Trades:
				{
					if (mdMsg.IsSubscribe)
						_subscribedTicks.Add(mdMsg.SecurityId);
					else
						_subscribedTicks.Remove(mdMsg.SecurityId);

					break;
				}
				default:
					throw new ArgumentOutOfRangeException("mdMsg", mdMsg.DataType, LocalizedStrings.Str1618);
			}

			var reply = (MarketDataMessage)mdMsg.Clone();
			reply.OriginalTransactionId = mdMsg.OriginalTransactionId;
			SendOutMessage(reply);
		}

		private void ProcessSubscriptions()
		{
			if (_subscribedLevel1.Count > 0)
			{
				var tickerReply = Session.GetTickers(_subscribedLevel1.Cache.Select(id => id.SecurityCode.ToBtceCode()));

				foreach (var ticker in tickerReply.Items.Values)
				{
					var l1Msg = new Level1ChangeMessage
					{
						SecurityId = new SecurityId
						{
							SecurityCode = ticker.Instrument.ToStockSharpCode(),
							BoardCode = _boardCode,
						},
						ServerTime = ticker.Timestamp.ApplyTimeZone(TimeHelper.Moscow)
					}
					.TryAdd(Level1Fields.Volume, (decimal)ticker.Volume)
					.TryAdd(Level1Fields.HighPrice, (decimal)ticker.HighPrice)
					.TryAdd(Level1Fields.LowPrice, (decimal)ticker.LowPrice)
					.TryAdd(Level1Fields.LastTradePrice, (decimal)ticker.LastPrice)

					// BTCE транслирует потенциальные цену для покупки и продажи
					.TryAdd(Level1Fields.BestBidPrice, (decimal)ticker.Ask)
					.TryAdd(Level1Fields.BestAskPrice, (decimal)ticker.Bid)
					.TryAdd(Level1Fields.AveragePrice, (decimal)ticker.AveragePrice);

					if (l1Msg.Changes.Count > 0)
						SendOutMessage(l1Msg);
				}
			}

			if (_subscribedDepths.Count > 0)
			{
				foreach (var group in _subscribedDepths.CachedPairs.GroupBy(p => p.Value))
				{
					var depthReply = Session.GetDepths(group.Key, group.Select(p => p.Key).Select(id => id.SecurityCode.ToBtceCode()));

					foreach (var pair in depthReply.Items)
					{
						SendOutMessage(new QuoteChangeMessage
						{
							SecurityId = new SecurityId
							{
								SecurityCode = pair.Key.ToStockSharpCode(),
								BoardCode = _boardCode,
							},
							Bids = pair.Value.Bids.Select(vp => vp.ToStockSharp(Sides.Buy)).ToArray(),
							Asks = pair.Value.Asks.Select(vp => vp.ToStockSharp(Sides.Sell)).ToArray(),
							ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
						});
					}
				}
			}

			if (_subscribedTicks.Count > 0)
			{
				var tradeReply = Session.GetTrades(_tickCount, _subscribedTicks.Cache.Select(id => id.SecurityCode.ToBtceCode()));

				// меняем на глубину 50
				_tickCount = 50;

				foreach (var pair in tradeReply.Items)
				{
					foreach (var trade in pair.Value.OrderBy(t => t.Id))
					{
						if (_lastTickId >= trade.Id)
							continue;

						_lastTickId = trade.Id;

						SendOutMessage(new ExecutionMessage
						{
							SecurityId = new SecurityId
							{
								SecurityCode = pair.Key.ToStockSharpCode(),
								BoardCode = _boardCode,
							},
							ExecutionType = ExecutionTypes.Tick,
							TradePrice = (decimal)trade.Price,
							Volume = (decimal)trade.Volume,
							TradeId = trade.Id,
							ServerTime = trade.Timestamp.ApplyTimeZone(TimeHelper.Moscow),
							OriginSide = trade.Side.ToStockSharp()
						});
					}
				}
			}
		}
	}
}