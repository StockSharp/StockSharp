namespace StockSharp.FTX
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.Messages;

	using FTX.Native.Model;
	using FTX.Native;

	partial class FtxMessageAdapter
	{
		private const int _TickPaginationLimit = 5000;
		private const int _CandlesPaginationLimit = 1501;

		private void SessionOnNewTrade(string pair, List<Trade> trades)
		{
			foreach (var trade in trades)
			{
				SendOutMessage(new ExecutionMessage
				{
					DataTypeEx = DataType.Ticks,
					SecurityId = pair.ToStockSharp(),
					TradeId = trade.Id,
					TradePrice = trade.Price,
					TradeVolume = trade.Size,
					ServerTime = trade.Time,
					OriginSide = trade.Side.ToSide(),
				});
			}
		}

		private void SessionOnNewOrderBook(string pair, OrderBook book, QuoteChangeStates quoteChangeStates)
		{

			SendOutMessage(new QuoteChangeMessage
			{
				State = quoteChangeStates,
				SecurityId = pair.ToStockSharp(),
				Bids = book.Bids.Select(e => new QuoteChange(e.Price, e.Size)).ToArray(),
				Asks = book.Asks.Select(e => new QuoteChange(e.Price, e.Size)).ToArray(),

				ServerTime = book.ConvertTime(),
			});

		}
		private void SessionOnNewLevel1(string pair, Level1 level1)
		{
			var l1 = new Level1ChangeMessage()
			{
				SecurityId = pair.ToStockSharp(),
				ServerTime = level1.ConvertTime()
			};
			if (level1.Bid > 0) l1.Changes[Level1Fields.BestBidPrice] = level1.Bid;
			if (level1.Ask > 0) l1.Changes[Level1Fields.BestAskPrice] = level1.Ask;
			if (level1.BidSize > 0) l1.Changes[Level1Fields.BestBidVolume] = level1.BidSize;
			if (level1.AskSize > 0) l1.Changes[Level1Fields.BestAskVolume] = level1.AskSize;
			SendOutMessage(l1);
		}

		private void ProcessMarketData(MarketDataMessage mdMsg)
		{
			var currency = mdMsg.SecurityId.ToCurrency();

			if (mdMsg.DataType == MarketDataTypes.Level1)
			{
				if (mdMsg.IsSubscribe)
					_wsClient.SubscribeLevel1(currency);
				else
					_wsClient.UnsubscribeLevel1(currency);
			}
			else if (mdMsg.DataType == MarketDataTypes.MarketDepth)
			{
				if (mdMsg.IsSubscribe)
					_wsClient.SubscribeOrderBook(currency);
				else
					_wsClient.UnsubscribeOrderBook(currency);
			}
			else if (mdMsg.DataType2 == DataType.Ticks)
			{
				if (mdMsg.IsSubscribe)
					if (mdMsg.To != null)
					{
						if (mdMsg.From == null)
							throw new ArgumentException(nameof(mdMsg.From));

						if (mdMsg.From > mdMsg.To)
							throw new Exception(
							                    $"Property \"From\" of type {mdMsg.GetType().Name} is greater than property \"To\".");

						SendSubscriptionReply(mdMsg.TransactionId);

						GetPaginatedTradesFromMarket(mdMsg, currency, mdMsg.From.Value.DateTime.ToUniversalTime(), mdMsg.To.Value.DateTime.ToUniversalTime());

						SendSubscriptionResult(mdMsg);
					}
					else
					{


						if (mdMsg.From != null)
						{
							GetPaginatedTradesFromMarket(mdMsg, currency, mdMsg.From.Value.DateTime.ToUniversalTime(), DateTime.UtcNow);
						}


						_wsClient.SubscribeTradesChannel(currency, WsTradeChannelSubscriber.Trade);
					}
				else
				{
					_wsClient.UnsubscribeTradesChannel(currency, WsTradeChannelSubscriber.Trade);
				}
			}
			else if (mdMsg.DataType == MarketDataTypes.CandleTimeFrame)
			{
				if (mdMsg.IsSubscribe)
				{
					if (mdMsg.To != null)
					{
						if (mdMsg.From == null)
							throw new ArgumentException(nameof(mdMsg.From));


						if (mdMsg.From > mdMsg.To)
							throw new Exception(
												$"Property \"From\" of type {mdMsg.GetType().Name} is greater than property \"To\".");

						SendSubscriptionReply(mdMsg.TransactionId);

						GetPaginatedCandlesFromMarket(mdMsg, currency, mdMsg.From.Value.DateTime.ToUniversalTime(), mdMsg.To.Value.DateTime.ToUniversalTime());

						SendSubscriptionResult(mdMsg);
					}
					else
					{
						if (mdMsg.From != null)
						{
							GetPaginatedCandlesFromMarket(mdMsg, currency, mdMsg.From.Value.DateTime.ToUniversalTime(), DateTime.UtcNow);
						}

						_wsClient.SubscribeTradesChannel(currency, WsTradeChannelSubscriber.Candles);
					}
				}
				else
				{
					_wsClient.UnsubscribeTradesChannel(currency, WsTradeChannelSubscriber.Candles);
				}

			}
			else
			{
				SendSubscriptionNotSupported(mdMsg.TransactionId);
				return;
			}

			SendSubscriptionReply(mdMsg.TransactionId);
		}

		#region Candles pagination
		private void GetPaginatedCandlesFromMarket(MarketDataMessage mdMsg, string currency, DateTime dateTimeFrom, DateTime dateTimeTo)
		{
			GetMarketCandlesByChunks(mdMsg, currency, dateTimeFrom, dateTimeTo);
		}

		private void GetMarketCandlesByChunks(MarketDataMessage mdMsg, string currency, DateTime dateTimeFrom, DateTime dateTimeTo)
		{
			List<Candle> candles = new();
			var endTime = dateTimeTo;

			var resolution = (TimeSpan)mdMsg.DataType2.Arg;
			while (true)
			{
				var candlesChunk = _restClient.GetMarketCandles(currency, resolution, dateTimeFrom, endTime);

				candles.InsertRange(0, candlesChunk);
				if (candlesChunk.Count != _CandlesPaginationLimit)
				{
					break;
				}
				endTime -= candlesChunk.Last().OpenTime - candlesChunk.First().OpenTime;

			}
			CreateCandlesSendoutMessage(mdMsg, candles, resolution);
		}

		private void CreateCandlesSendoutMessage(MarketDataMessage mdMsg, List<Candle> candles, TimeSpan resolution)
		{
			foreach (var candle in candles.OrderBy(t => t.OpenTime))
			{
				SendOutMessage(new TimeFrameCandleMessage
				{
					OriginalTransactionId = mdMsg.TransactionId,
					SecurityId = mdMsg.SecurityId,
					ClosePrice = candle.ClosePrice,
					HighPrice = candle.HightPrice,
					LowPrice = candle.LowPrice,
					OpenPrice = candle.OpenPrice,
					TotalVolume = candle.WindowVolume,
					OpenTime = candle.OpenTime,
					State = CandleStates.Finished,
					TypedArg = resolution
				});
			}
		}

		#endregion

		#region Ticks pagination
		private void GetPaginatedTradesFromMarket(MarketDataMessage mdMsg, string currency, DateTime dateTimeFrom, DateTime dateTimeTo)
		{
			GetMarketTradesByChunks(mdMsg, currency, dateTimeFrom, dateTimeTo);
		}

		private void GetMarketTradesByChunks(MarketDataMessage mdMsg, string currency, DateTime dateTimeFrom, DateTime dateTimeTo)
		{
			List<Trade> trades = new();
			var startTime = dateTimeFrom;
			var endTime = dateTimeTo;

			while (true)
			{
				var tradesChunk = _restClient.GetMarketTrades(currency, startTime, endTime);
				tradesChunk.Reverse();
				trades.InsertRange(0, tradesChunk);
				if (tradesChunk.Count != _TickPaginationLimit)
				{
					break;
				}
				endTime -= tradesChunk.Last().Time - tradesChunk.First().Time;
			}

			CreateTradesSendoutMessage(mdMsg, trades);
		}

		private void CreateTradesSendoutMessage(MarketDataMessage mdMsg, List<Trade> trades)
		{
			foreach (var trade in trades.OrderBy(t => t.Time))
			{
				SendOutMessage(new ExecutionMessage
				{
					DataTypeEx = DataType.Ticks,
					SecurityId = mdMsg.SecurityId,
					TradeId = trade.Id,
					TradePrice = trade.Price,
					TradeVolume = trade.Size,
					ServerTime = trade.Time,
					OriginSide = trade.Side.ToSide(),
					OriginalTransactionId = mdMsg.TransactionId
				});
			}
		}
		#endregion

		private static int GetDecimalPlaces(decimal n)
		{
			n = Math.Abs(n);
			n -= (int)n;
			var decimalPlaces = 0;
			while (n > 0)
			{
				decimalPlaces++;
				n *= 10;
				n -= (int)n;
			}
			return decimalPlaces;
		}
		private void ProcessSecurityLookup(SecurityLookupMessage lookupMsg)
		{
			var secTypes = lookupMsg.GetSecurityTypes();
			var markets = _restClient.GetMarkets();
			foreach (var info in markets)
			{
				var secMsg = new SecurityMessage
				{
					SecurityId = info.Name.ToStockSharp(),
					SecurityType = info.Type == "future" ? SecurityTypes.Future : SecurityTypes.CryptoCurrency,
					MinVolume = info.MinProvideSize,
					Decimals = GetDecimalPlaces(info.PriceIncrement),
					Name = info.Name,
					VolumeStep = info.SizeIncrement,
					OriginalTransactionId = lookupMsg.TransactionId,
					PriceStep = info.PriceIncrement
				};

				if (!secMsg.IsMatch(lookupMsg, secTypes))
					continue;

				SendOutMessage(secMsg);
			}
			SendSubscriptionResult(lookupMsg);
		}
	}
}