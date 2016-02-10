#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.BitStamp
File: BitStampMessageAdapter_MarketData.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BitStamp.Native;
	using StockSharp.Messages;

	/// <summary>
	/// The message adapter for BitStamp.
	/// </summary>
	partial class BitStampMessageAdapter
	{
		private readonly CachedSynchronizedSet<SecurityId> _subscribedDepths = new CachedSynchronizedSet<SecurityId>();
		private readonly CachedSynchronizedSet<SecurityId> _subscribedTicks = new CachedSynchronizedSet<SecurityId>();
		private DateTimeOffset _prevLevel1Time;
		private static readonly TimeSpan _level1Interval = TimeSpan.FromSeconds(10);

		private void SessionOnNewTrade(Trade trade)
		{
			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Tick,
				SecurityId = _btcUsd,
				TradeId = trade.Id,
				TradePrice = (decimal)trade.Price,
				TradeVolume = (decimal)trade.Amount,
				ServerTime = CurrentTime.Convert(TimeZoneInfo.Utc),
			});
		}

		private void SessionOnNewOrderBook(OrderBook book)
		{
			SendOutMessage(new QuoteChangeMessage
			{
				SecurityId = _btcUsd,
				Bids = book.Bids.Select(b => b.ToStockSharp(Sides.Buy)),
				Asks = book.Asks.Select(b => b.ToStockSharp(Sides.Sell)),
				ServerTime = CurrentTime.Convert(TimeZoneInfo.Utc),
			});
		}

		private void ProcessMarketData(MarketDataMessage mdMsg)
		{
			switch (mdMsg.DataType)
			{
				case MarketDataTypes.Level1:
				{
					//if (mdMsg.IsSubscribe)
					//	_subscribedLevel1.Add(secCode);
					//else
					//	_subscribedLevel1.Remove(secCode);

					break;
				}
				case MarketDataTypes.MarketDepth:
				{
					if (mdMsg.IsSubscribe)
					{
						_subscribedDepths.Add(mdMsg.SecurityId);

						if (_subscribedDepths.Count == 1)
							_pusherClient.SubscribeDepths();
					}
					else
					{
						_subscribedDepths.Remove(mdMsg.SecurityId);

						if (_subscribedDepths.Count == 0)
							_pusherClient.UnSubscribeDepths();
					}

					break;
				}
				case MarketDataTypes.Trades:
				{
					if (mdMsg.IsSubscribe)
					{
						if (mdMsg.From != null && mdMsg.From.Value.IsToday())
						{
							_httpClient.RequestTransactions().Select(t => new ExecutionMessage
							{
								ExecutionType = ExecutionTypes.Tick,
								SecurityId = _btcUsd,
								TradeId = t.Id,
								TradePrice = (decimal)t.Price,
								TradeVolume = (decimal)t.Amount,
								ServerTime = t.Time.ApplyTimeZone(TimeZoneInfo.Utc)
							}).ForEach(SendOutMessage);
						}

						_subscribedTicks.Add(mdMsg.SecurityId);

						if (_subscribedTicks.Count == 1)
							_pusherClient.SubscribeTrades();
					}
					else
					{
						_subscribedTicks.Remove(mdMsg.SecurityId);

						if (_subscribedTicks.Count == 0)
							_pusherClient.UnSubscribeTrades();
					}

					break;
				}
				default:
				{
					SendOutMarketDataNotSupported(mdMsg.TransactionId);
					return;
				}
			}

			var reply = (MarketDataMessage)mdMsg.Clone();
			reply.OriginalTransactionId = mdMsg.OriginalTransactionId;
			SendOutMessage(reply);
		}

		private void ProcessSecurityLookup(SecurityLookupMessage message)
		{
			SendOutMessage(new SecurityMessage
			{
				OriginalTransactionId = message.TransactionId,
				SecurityId = _btcUsd,
				SecurityType = SecurityTypes.CryptoCurrency,
				VolumeStep = 0.00000001m,
				PriceStep = 0.01m,
			});

			SendOutMessage(new SecurityMessage
			{
				OriginalTransactionId = message.TransactionId,
				SecurityId = _eurUsd,
				SecurityType = SecurityTypes.Currency,
				PriceStep = 0.0001m,
			});

			SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = message.TransactionId });
		}

		private void ProcessLevel1()
		{
			var currTime = CurrentTime;

			if ((currTime - _prevLevel1Time) < _level1Interval)
				return;

			_prevLevel1Time = currTime;

			var btcUsd = _httpClient.RequestBtcUsd();

			if (btcUsd != null)
			{
				SendOutMessage(new Level1ChangeMessage
				{
					SecurityId = _btcUsd,
					ServerTime = btcUsd.Time.ApplyTimeZone(TimeZoneInfo.Utc)
				}
				.TryAdd(Level1Fields.HighBidPrice, (decimal)btcUsd.High)
				.TryAdd(Level1Fields.LowAskPrice, (decimal)btcUsd.Low)
				.TryAdd(Level1Fields.VWAP, (decimal)btcUsd.VWAP)
				.TryAdd(Level1Fields.LastTradePrice, (decimal)btcUsd.Last)
				.TryAdd(Level1Fields.Volume, (decimal)btcUsd.Volume)
				.TryAdd(Level1Fields.BestBidPrice, (decimal)btcUsd.Bid)
				.TryAdd(Level1Fields.BestAskPrice, (decimal)btcUsd.Ask));
			}

			var eurUsd = _httpClient.RequestEurUsd();

			if (eurUsd != null)
			{
				SendOutMessage(new Level1ChangeMessage
				{
					SecurityId = _eurUsd,
					ServerTime = CurrentTime.Convert(TimeZoneInfo.Utc),
				}
				.TryAdd(Level1Fields.BestBidPrice, (decimal)eurUsd.Buy)
				.TryAdd(Level1Fields.BestAskPrice, (decimal)eurUsd.Sell));
			}
		}
	}
}