#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rithmic.Rithmic
File: RithmicMessageAdapter_MarketData.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Rithmic
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using com.omnesys.rapi;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Messages;

	partial class RithmicMessageAdapter
	{
		private readonly SynchronizedDictionary<SecurityId, CachedSynchronizedDictionary<DateTimeOffset, RefPair<BidInfo, AskInfo>>> _quotes = new SynchronizedDictionary<SecurityId, CachedSynchronizedDictionary<DateTimeOffset, RefPair<BidInfo, AskInfo>>>();
		private readonly CachedSynchronizedSet<string> _boards = new CachedSynchronizedSet<string>();

		private void ProcessSecurityLookupMessage(SecurityLookupMessage secMsg)
		{
			if (secMsg.SecurityId.IsDefault())
			{
				_client.Session.listExchanges(secMsg.TransactionId);
				_client.Session.listTradeRoutes(secMsg.TransactionId);	
			}

			var board = secMsg.SecurityId.BoardCode;

			if (secMsg.SecurityType == null || secMsg.SecurityType == SecurityTypes.Option)
			{
				var expiration = secMsg.ExpiryDate?.ToString("yyyyMM");

				_client.Session.getOptionList(board, secMsg.UnderlyingSecurityCode, expiration, secMsg.TransactionId);
				_client.Session.getInstrumentByUnderlying(secMsg.UnderlyingSecurityCode, board, expiration, secMsg.TransactionId);
				_client.Session.listBinaryContracts(board, secMsg.UnderlyingSecurityCode, secMsg.TransactionId);
			}

			if (secMsg.SecurityType != SecurityTypes.Option && !secMsg.SecurityId.SecurityCode.IsEmpty())
			{
				if (board.IsEmpty())
				{
					foreach (var b in _boards.Cache)
						_client.Session.getRefData(b, secMsg.SecurityId.SecurityCode, secMsg.TransactionId);
				}
				else
					_client.Session.getRefData(board, secMsg.SecurityId.SecurityCode, secMsg.TransactionId);
			}
		}

		private void ProcessMarketDataMessage(MarketDataMessage mdMsg)
		{
			var secCode = mdMsg.SecurityId.SecurityCode;
			var boardCode = mdMsg.SecurityId.BoardCode;

			switch (mdMsg.DataType)
			{
				case MarketDataTypes.Level1:
				{
					if (mdMsg.IsSubscribe)
						_client.Session.subscribe(boardCode, secCode, SubscriptionFlags.All & ~(SubscriptionFlags.Prints | SubscriptionFlags.PrintsCond | SubscriptionFlags.Quotes), mdMsg.TransactionId);
					else
						_client.Session.unsubscribe(boardCode, secCode);

					break;
				}
				case MarketDataTypes.MarketDepth:
				{
					if (mdMsg.IsSubscribe)
					{
						_client.Session.rebuildBook(boardCode, secCode, mdMsg.TransactionId);
						_client.Session.subscribe(boardCode, secCode, SubscriptionFlags.Quotes, mdMsg.TransactionId);
					}
					else
						_client.Session.unsubscribe(boardCode, secCode);

					break;
				}
				case MarketDataTypes.Trades:
				{
					if (mdMsg.From == null || mdMsg.To == null)
					{
						if (mdMsg.IsSubscribe)
							_client.Session.subscribe(boardCode, secCode, SubscriptionFlags.Prints | SubscriptionFlags.PrintsCond, mdMsg.TransactionId);
						else
							_client.Session.unsubscribe(boardCode, secCode);
					}
					else
						_client.Session.replayTrades(boardCode, secCode, mdMsg.From.Value.ToSsboe(), mdMsg.To.Value.ToSsboe(), mdMsg.TransactionId);

					break;
				}
				//case MarketDataTypes.OrderLog:
				//	break;
				//case MarketDataTypes.News:
				//	break;
				case MarketDataTypes.CandleTimeFrame:
				{
					if (mdMsg.From == null || mdMsg.To == null)
					{
						if (mdMsg.IsSubscribe)
							_client.Session.subscribeTimeBar(boardCode, secCode, mdMsg.TransactionId);
						else
							_client.Session.unsubscribeTimeBar(boardCode, secCode);
					}
					else
						_client.Session.replayTimeBars(boardCode, secCode, mdMsg.From.Value.ToSsboe(), mdMsg.To.Value.ToSsboe(), mdMsg.TransactionId);

					break;
				}
				default:
				{
					SendOutMarketDataNotSupported(mdMsg.TransactionId);
					return;
				}
			}

			var reply = (MarketDataMessage)mdMsg.Clone();
			reply.OriginalTransactionId = mdMsg.TransactionId;
			SendOutMessage(reply);
		}

		private void SessionHolderOnLevel1(string symbol, string exchange, Level1Fields field, decimal value, DateTimeOffset time)
		{
			try
			{
				SendOutMessage(
					new Level1ChangeMessage
					{
						SecurityId = new SecurityId
						{
							SecurityCode = symbol,
							BoardCode = exchange,
						},
						ServerTime = time
					}
					.TryAdd(field, value));
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}
		}

		private void ProcessRefData(RefDataInfo info, long? originalTransactionId)
		{
			SendOutMessage(new SecurityMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = info.Symbol,
					BoardCode = info.Exchange,
				},
				ExpiryDate = RithmicUtils.ToDateTime(info.Expiration, info.ExpirationTime),
				Currency = info.Currency.To<CurrencyTypes?>(),
				Strike = info.StrikePrice.ToDecimal(),
				OptionType = RithmicUtils.ToOptionType(info.PutCallIndicator),
				BinaryOptionType = info.BinaryContractType,
				Name = info.Description,
				SecurityType = RithmicUtils.ToSecurityType(info.InstrumentType),
				UnderlyingSecurityCode = info.Underlying,
				LocalTime = RithmicUtils.ToTime(info.Ssboe),
				Class = info.ProductCode,
				PriceStep = info.SinglePointValue.ToDecimal(),
				OriginalTransactionId = originalTransactionId ?? 0,
			});
		}

		private void ProcessRefDataList(IEnumerable<RefDataInfo> list, long? originalTransactionId)
		{
			list.ForEach(i => ProcessRefData(i, originalTransactionId));

			if (originalTransactionId == null)
				return;

			SendOutMessage(new SecurityLookupResultMessage
			{
				OriginalTransactionId = originalTransactionId.Value
			});
		}

		private void ProcessExchanges(IEnumerable<string> exchanges)
		{
			foreach (var exchange in exchanges)
			{
				_boards.Add(exchange);

				SendOutMessage(new BoardMessage
				{
					Code = exchange,
					ExchangeCode = exchange,
				});
			}
		}

		private void SessionHolderOnSecurityRefData(RefDataInfo info)
		{
			//ProcessErrorCode(info.RpCode);
			if (!ProcessErrorCode(info.RpCode))
				return;

			ProcessRefData(info, (long?)info.Context);
		}

		private void SessionHolderOnSecurityOptions(OptionListInfo info)
		{
			ProcessExchanges(info.Exchanges);

			if (!ProcessErrorCode(info.RpCode))
				return;

			ProcessRefDataList(info.Instruments, (long?)info.Context);
		}

		private void SessionHolderOnSecurityInstrumentByUnderlying(InstrumentByUnderlyingInfo info)
		{
			ProcessExchanges(info.Exchanges);

			if (!ProcessErrorCode(info.RpCode))
				return;

			ProcessRefDataList(info.Instruments, (long?)info.Context);
		}

		private void SessionHolderOnSecurityBinaryContracts(BinaryContractListInfo info)
		{
			ProcessExchanges(info.Exchanges);

			if (!ProcessErrorCode(info.RpCode))
				return;

			ProcessRefDataList(info.Instruments, (long?)info.Context);
		}

		private void SessionHolderOnTimeBarReplay(TimeBarReplayInfo info)
		{
			if (!ProcessErrorCode(info.RpCode))
				return;

			SendOutMessage(new TimeFrameCandleMessage
			{
				OriginalTransactionId = (long?)info.Context ?? 0,
				IsFinished = true,
			});
		}

		private void SessionHolderOnTimeBar(TimeBarInfo info)
		{
			SendOutMessage(new TimeFrameCandleMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = info.Symbol,
					BoardCode = info.Exchange
				},
				OriginalTransactionId = (long?)info.Context ?? 0,
				OpenPrice = info.OpenPrice.ToDecimal() ?? 0,
				OpenVolume = info.OpenSize,
				HighPrice = info.HighPrice.ToDecimal() ?? 0,
				HighVolume = info.HighVolume,
				LowPrice = info.LowPrice.ToDecimal() ?? 0,
				LowVolume = info.LowVolume,
				ClosePrice = info.ClosePrice.ToDecimal() ?? 0,
				CloseVolume = info.CloseSize,
				CloseTime = RithmicUtils.ToTime(info.Ssboe),
				TotalTicks = info.NumTrades,
				UpTicks = info.HighNumTrades,
				DownTicks = info.LowNumTrades
			});
		}

		private void SessionHolderOnTradeReplay(TradeReplayInfo info)
		{
			ProcessErrorCode(info.RpCode);
			//if (!ProcessErrorCode(info.RpCode))
			//	return;
		}

		private void SessionHolderOnTradeVolume(TradeVolumeInfo info)
		{
			if (!info.TotalVolumeFlag)
				return;

			SendOutMessage(new Level1ChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = info.Symbol,
					BoardCode = info.Exchange
				},
				ServerTime = RithmicUtils.ToTime(info.Ssboe, info.Usecs),
			}.TryAdd(Level1Fields.LastTradeVolume, (decimal)info.TotalVolume));
		}

		private void ProcessTick(TradeInfo info)
		{
			var secId = new SecurityId
			{
				SecurityCode = info.Symbol,
				BoardCode = info.Exchange
			};

			SendOutMessage(new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Tick,
				SecurityId = secId,
				ServerTime = RithmicUtils.ToTime(info.SourceSsboe, info.SourceUsecs),
				LocalTime = RithmicUtils.ToTime(info.Ssboe, info.Usecs),
				TradePrice = info.Price.ToDecimal(),
				TradeVolume = info.Size,
				OriginSide = RithmicUtils.ToOriginSide(info.AggressorSide)
			});

			SendOutMessage(new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = RithmicUtils.ToTime(info.Ssboe, info.Usecs),
			}.TryAdd(Level1Fields.Change, info.NetChange.ToDecimal()));
		}

		private void SessionHolderOnTradePrint(TradeInfo info)
		{
			ProcessTick(info);
		}

		private void SessionHolderOnTradeCondition(TradeInfo info)
		{
			ProcessTick(info);
		}

		private void SessionHolderOnSettlementPrice(SettlementPriceInfo info)
		{
			var price = info.Price.ToDecimal();

			if (price == null)
				return;

			Level1Fields field;

			if (info.PriceType == Constants.SETTLEMENT_PRICE_TYPE_FINAL)
				field = Level1Fields.SettlementPrice;
			else if (info.PriceType == Constants.SETTLEMENT_PRICE_TYPE_THEORETICAL)
				field = Level1Fields.TheorPrice;
			else
				field = Level1Fields.LastTradePrice;

			SendOutMessage(new Level1ChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = info.Symbol,
					BoardCode = info.Exchange
				},
				ServerTime = RithmicUtils.ToTime(info.Ssboe, info.Usecs),
			}.TryAdd(field, price.Value));
		}

		private void SessionHolderOnOrderBook(OrderBookInfo info)
		{
			if (!ProcessErrorCode(info.RpCode))
				return;

			SendOutMessage(new QuoteChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = info.Symbol,
					BoardCode = info.Exchange ?? AssociatedBoardCode
				},
				Bids = info.Bids.Select(b => new QuoteChange(Sides.Buy, b.Price.ToDecimal() ?? 0, b.Size) { BoardCode = b.Exchange }).ToArray(),
				Asks = info.Asks.Select(b => new QuoteChange(Sides.Sell, b.Price.ToDecimal() ?? 0, b.Size) { BoardCode = b.Exchange }).ToArray(),
				ServerTime = RithmicUtils.ToTime(info.Ssboe, info.Usecs),
			});
		}

		private void SessionHolderOnEndQuote(EndQuoteInfo info)
		{
			var secId = new SecurityId
			{
				SecurityCode = info.Symbol,
				BoardCode = info.Exchange
			};

			FlushQuotes(secId);
		}

		private void FlushQuotes(SecurityId secId)
		{
			var quotes = _quotes.TryGetValue(secId);

			if (quotes == null)
				return;

			_quotes.Remove(secId);

			foreach (var pair in quotes.CachedPairs)
			{
				var message = new Level1ChangeMessage
				{
					SecurityId = secId,
					ServerTime = pair.Key
				};

				var bid = pair.Value.First;

				if (bid != null)
				{
					message
						.TryAdd(Level1Fields.BestBidPrice, bid.Price.ToDecimal())
						.TryAdd(Level1Fields.BestBidVolume, (decimal)bid.Size);
				}

				var ask = pair.Value.Second;

				if (ask != null)
				{
					message
						.TryAdd(Level1Fields.BestAskPrice, ask.Price.ToDecimal())
						.TryAdd(Level1Fields.BestAskVolume, (decimal)ask.Size);
				}

				SendOutMessage(message);
			}
		}

		private void ProcessBestQuote(string symbol, string exchange, double price, int size, int numOfOrders, UpdateType updateType, int ssboe, int usecs, Level1Fields priceField, Level1Fields volumeField)
		{
			var secId = new SecurityId
			{
				SecurityCode = symbol,
				BoardCode = exchange
			};

			var time = RithmicUtils.ToTime(ssboe, usecs);

			SendOutMessage(new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = time,
			}
			.TryAdd(priceField, price.ToDecimal())
			.TryAdd(volumeField, (decimal)size));

			switch (updateType)
			{
				// [gene.sato] For best bid/ask the update type does not apply.
				// The update type is for market depth/level 2 updates.
				case UpdateType.Undefined:
				//	break;
				case UpdateType.Solo:
				{
					//SendOutMessage(new Level1ChangeMessage
					//{
					//	SecurityId = secId,
					//	ServerTime = time,
					//}
					//.TryAdd(priceField, price.ToDecimal())
					//.TryAdd(volumeField, (decimal)size));
					break;
				}
				case UpdateType.Begin:
				case UpdateType.Middle:
				case UpdateType.Aggregated:
				{
					var pair = _quotes
						.SafeAdd(secId)
						.SafeAdd(time, key => new RefPair<BidInfo, AskInfo>());

					pair.Second = new AskInfo
					{
						Price = price,
						NumOrders = numOfOrders,
						Size = size,
					};
					break;
				}
				case UpdateType.End:
					FlushQuotes(secId);
					break;
				case UpdateType.Clear:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void SessionHolderOnBestAskQuote(AskInfo info)
		{
			ProcessBestQuote(info.Symbol, info.Exchange, info.Price, info.Size, info.NumOrders, info.UpdateType, info.Ssboe, info.Usecs, Level1Fields.BestAskPrice, Level1Fields.BestAskVolume);
		}

		private void SessionHolderOnAskQuote(AskInfo info)
		{

		}

		private void SessionHolderOnBestBidQuote(BidInfo info)
		{
			ProcessBestQuote(info.Symbol, info.Exchange, info.Price, info.Size, info.NumOrders, info.UpdateType, info.Ssboe, info.Usecs, Level1Fields.BestBidPrice, Level1Fields.BestBidVolume);
		}

		private void SessionHolderOnBidQuote(BidInfo info)
		{

		}

		private void SessionHolderOnExchanges(ExchangeListInfo info)
		{
			ProcessErrorCode(info.RpCode);
			//if (!ProcessErrorCode(info.RpCode))
			//	return;

			ProcessExchanges(info.Exchanges);
		}
	}
}