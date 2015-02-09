namespace StockSharp.SmartCom
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Derivatives;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.SmartCom.Native;
	using StockSharp.Localization;

	partial class SmartComMessageAdapter
	{
		private readonly SynchronizedDictionary<SecurityId, Tuple<List<QuoteChange>, List<QuoteChange>>> _tempDepths = new SynchronizedDictionary<SecurityId, Tuple<List<QuoteChange>, List<QuoteChange>>>();
		private readonly SynchronizedDictionary<string, SynchronizedDictionary<TimeSpan, Tuple<long, List<CandleMessage>>>> _candleTransactions = new SynchronizedDictionary<string, SynchronizedDictionary<TimeSpan, Tuple<long, List<CandleMessage>>>>(StringComparer.InvariantCultureIgnoreCase);

		private long _lookupSecuritiesId;
		private long _lookupPortfoliosId;

		private readonly SynchronizedDictionary<SecurityId, RefPair<Tuple<decimal, decimal>, Tuple<decimal, decimal>>> _bestQuotes = new SynchronizedDictionary<SecurityId, RefPair<Tuple<decimal, decimal>, Tuple<decimal, decimal>>>();

		/// <summary>
		/// Поддерживается ли торговой системой поиск инструментов.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup
		{
			get { return true; }
		}

		private void ProcessMarketDataMessage(MarketDataMessage mdMsg)
		{
			var smartId = (string)mdMsg.SecurityId.Native;

			if (smartId.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.Str1853Params.Put(mdMsg.SecurityId));

			switch (mdMsg.DataType)
			{
				case MarketDataTypes.Level1:
				{
					if (mdMsg.IsSubscribe)
						Session.SubscribeSecurity(smartId);
					else
						Session.UnSubscribeSecurity(smartId);

					break;
				}
				case MarketDataTypes.MarketDepth:
				{
					if (mdMsg.IsSubscribe)
						Session.SubscribeMarketDepth(smartId);
					else
						Session.UnSubscribeMarketDepth(smartId);

					break;
				}
				case MarketDataTypes.Trades:
				{
					if (mdMsg.From.IsDefault())
					{
						if (mdMsg.IsSubscribe)
							Session.SubscribeTrades(smartId);
						else
							Session.UnSubscribeTrades(smartId);
					}
					else
					{
						const int maxTradeCount = 1000000;
						SessionHolder.AddDebugLog("RequestHistoryTrades SecId = {0} From {1} Count = {2}", smartId, mdMsg.From, maxTradeCount);
						Session.RequestHistoryTrades(smartId, mdMsg.From.ToLocalTime(TimeHelper.Moscow), maxTradeCount);
					}

					break;
				}
				case MarketDataTypes.CandleTimeFrame:
				{
					var count = mdMsg.Count;
					var direction = (SmartComHistoryDirections)mdMsg.ExtensionInfo["Direction"];

					if (direction == SmartComHistoryDirections.Forward)
						count = -count;

					var tf = (TimeSpan)mdMsg.Arg;

					_candleTransactions.SafeAdd(smartId)[tf] = Tuple.Create(mdMsg.TransactionId, new List<CandleMessage>());

					SessionHolder.AddDebugLog("RequestHistoryBars SecId {0} TF {1} From {2} Count {3}", smartId, tf, mdMsg.From, count);
					Session.RequestHistoryBars(smartId, tf, mdMsg.From.ToLocalTime(TimeHelper.Moscow), (int)count);

					break;
				}
				default:
					throw new ArgumentOutOfRangeException("mdMsg", mdMsg.DataType, LocalizedStrings.Str1618);
			}

			var reply = (MarketDataMessage)mdMsg.Clone();
			reply.OriginalTransactionId = mdMsg.TransactionId;
			SendOutMessage(reply);
		}

		private void ProcessSecurityLookupMessage(SecurityLookupMessage message)
		{
			if (_lookupSecuritiesId == 0)
			{
				_lookupSecuritiesId = message.TransactionId;
				Session.LookupSecurities();
			}
			else
				SendOutError(LocalizedStrings.Str1854);
		}

		private static ExecutionMessage CreateTrade(string smartId, DateTime time, decimal price, decimal volume, long tradeId, SmartOrderAction action)
		{
			return new ExecutionMessage
			{
				SecurityId = new SecurityId { Native = smartId },
				TradeId = tradeId,
				TradePrice = price,
				Volume = volume,
				OriginSide = action.ToSide(),
				ServerTime = time.ApplyTimeZone(TimeHelper.Moscow),
				ExecutionType = ExecutionTypes.Tick,
			};
		}

		private void OnNewHistoryTrade(int row, int rowCount, string smartId, DateTime time, decimal price, decimal volume, long tradeId, SmartOrderAction action)
		{
			SessionHolder.AddDebugLog("OnNewHistoryTrade row = {0} rowCount = {1} securityId = {2} time = {3} price = {4} volume = {5} id = {6} action = {7}",
				row, rowCount, smartId, time, price, volume, tradeId, action);

			var msg = CreateTrade(smartId, time, price, volume, tradeId, action);
			//msg.IsFinished = row == (rowCount - 1);
			SendOutMessage(msg);
		}

		private void OnNewBar(int row, int rowCount, string smartId, SmartComTimeFrames timeFrame, DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume, decimal openInt)
		{
			SessionHolder.AddDebugLog("OnNewHistoryTrade row = {0} rowCount = {1} securityId = {2} timeFrame = {3} time = {4} open = {5} high = {6} low = {7} close = {8} volume = {9} openInt = {10}",
				row, rowCount, smartId, timeFrame, time, open, high, low, close, volume, openInt);

			var infos = _candleTransactions.TryGetValue(smartId);
			var timeFrameKey = (TimeSpan)timeFrame;

			Tuple<long, List<CandleMessage>> transactionInfo;
			if (infos == null || !infos.TryGetValue(timeFrameKey, out transactionInfo))
			{
				SessionHolder.AddErrorLog(LocalizedStrings.Str1855Params, smartId, timeFrame);
				return;
			}

			transactionInfo.Item2.Add(new TimeFrameCandleMessage
			{
				SecurityId = new SecurityId { Native = smartId },
				OpenPrice = open,
				HighPrice = high,
				LowPrice = low,
				ClosePrice = close,
				TotalVolume = volume,
				OpenTime = time.ApplyTimeZone(TimeHelper.Moscow) - (TimeSpan)timeFrame,
				CloseTime = time.ApplyTimeZone(TimeHelper.Moscow),
				OpenInterest = openInt,
				OriginalTransactionId = transactionInfo.Item1,
				IsFinished = row == (rowCount - 1),
			});

			if ((row + 1) < rowCount)
				return;

			transactionInfo.Item2.OrderBy(c => c.OpenTime).ForEach(SendOutMessage);

			infos.Remove(timeFrameKey);

			if (infos.IsEmpty())
				_candleTransactions.Remove(smartId);
		}

		private void OnNewTrade(string smartId, DateTime time, decimal price, decimal volume, long tradeId, SmartOrderAction action)
		{
			SendOutMessage(CreateTrade(smartId, time, price, volume, tradeId, action));
		}

		private void OnNewSecurity(int row, int rowCount, string smartId, string name, string secCode, string secClass, int decimals, int lotSize,
			decimal stepPrice, decimal priceStep, string isin, string board, DateTime? expiryDate, decimal daysBeforeExpiry, decimal strike)
		{
			//AMU: заглушка. 11.01.2013 обнаружил, что через SmartCom стали приходить инструменты (класс EQBR и FISS) с пустым secCode - "longName" в понятии АйтиИнвеста
			if (secCode.IsEmpty())
				secCode = smartId;

			var securityId = new SecurityId
			{
				SecurityCode = secCode,
				BoardCode = board,
				Native = smartId,
				Isin = isin
			};

			var secMsg = new SecurityMessage
			{
				PriceStep = priceStep,
				Multiplier = lotSize,
				Name = name,
				ShortName = name,
				ExpiryDate = expiryDate == null ? (DateTimeOffset?)null : expiryDate.Value.ApplyTimeZone(TimeHelper.Moscow),
				ExtensionInfo = new Dictionary<object, object>
				{
					{ "Class", secClass }
				},
				OriginalTransactionId = _lookupSecuritiesId
			};

			if (secClass.CompareIgnoreCase("IDX"))
			{
				secMsg.SecurityType = SecurityTypes.Index;

				switch (secMsg.SecurityId.BoardCode)
				{
					case "RUSIDX":
						securityId.BoardCode = secCode.ContainsIgnoreCase("MICEX") || secCode.ContainsIgnoreCase("MCX") ? ExchangeBoard.Micex.Code : ExchangeBoard.Forts.Code;
						break;
					//default:
					//	security.Board = ExchangeBoard.Test;
					//	break;
				}
			}
			else
			{
				var info = SessionHolder.GetSecurityClassInfo(secClass);

				secMsg.SecurityType = info.Item1;
				securityId.BoardCode = info.Item2;

				// http://stocksharp.com/forum/yaf_postsm16847_Vopros-po-vystavlieniiu-zaiavok.aspx#post16847
				if (ExchangeBoard.GetOrCreateBoard(info.Item2).IsMicex
					&&
					/* проверяем, что не началась ли трансляция правильных дробных шагов */
					secMsg.PriceStep == (int)secMsg.PriceStep)
				{
					// http://stocksharp.com/forum/yaf_postsm21245_Sokhranieniie-stakanov-po-GAZP-EQNE.aspx#post21245
					secMsg.PriceStep = 1m / 10m.Pow(secMsg.PriceStep);
				}
			}

			secMsg.SecurityId = securityId;

			if (secMsg.SecurityType == SecurityTypes.Option)
			{
				var optionInfo = secMsg.Name.GetOptionInfo();

				if (optionInfo != null)
				{
					// http://stocksharp.com/forum/yaf_postst1355_Exception-Change-Set-11052.aspx
					if (!secCode.IsEmpty())
					{
						var futureInfo = optionInfo.UnderlyingSecurityId.GetFutureInfo(secCode);
						if (futureInfo != null)
							secMsg.UnderlyingSecurityCode = futureInfo.SecurityId.SecurityCode;
					}

					secMsg.ExpiryDate = optionInfo.ExpiryDate;
					secMsg.OptionType = optionInfo.OptionType;
					secMsg.Strike = optionInfo.Strike;
				}
			}

			SendOutMessage(secMsg);

			if (stepPrice > 0)
			{
				SendOutMessage(
					new Level1ChangeMessage
					{
						SecurityId = securityId,
						ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
					}
					.Add(Level1Fields.StepPrice, stepPrice));
			}

			if ((row + 1) < rowCount)
				return;

			SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = _lookupSecuritiesId });
			_lookupSecuritiesId = 0;
		}

		private void OnSecurityChanged(string smartId, Tuple<decimal, decimal, DateTime> lastTrade, decimal open, decimal high, decimal low, decimal close, decimal volume, QuoteChange bid, QuoteChange ask,
			decimal openInt, Tuple<decimal, decimal> goBuySell, Tuple<decimal, decimal> goBase, Tuple<decimal, decimal> limits, int tradingStatus, Tuple<decimal, decimal> volatTheorPrice)
		{
			var secId = new SecurityId { Native = smartId };

			var message = new Level1ChangeMessage
			{
				SecurityId = secId,
				ExtensionInfo = new Dictionary<object, object>
				{
					{ SmartComExtensionInfoHelper.SecurityOptionsMargin, goBase.Item1 },
					{ SmartComExtensionInfoHelper.SecurityOptionsSyntheticMargin, goBase.Item2 }
				},
				ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
			};

			message.TryAdd(Level1Fields.LastTradePrice, lastTrade.Item1);
			message.TryAdd(Level1Fields.LastTradeVolume, lastTrade.Item2);
			message.Add(Level1Fields.LastTradeTime, lastTrade.Item3.ApplyTimeZone(TimeHelper.Moscow));

			var prevQuotes = _bestQuotes.TryGetValue(secId);

			if (bid.Price != 0)
			{
				message.Add(Level1Fields.BestBidPrice, bid.Price);

				if (prevQuotes != null && prevQuotes.First != null && prevQuotes.First.Item1 == bid.Price)
					message.Add(Level1Fields.BestBidVolume, prevQuotes.First.Item2);
			}

			if (ask.Price != 0)
			{
				message.Add(Level1Fields.BestAskPrice, ask.Price);

				if (prevQuotes != null && prevQuotes.Second != null && prevQuotes.Second.Item1 == ask.Price)
					message.Add(Level1Fields.BestAskVolume, prevQuotes.Second.Item2);
			}

			message.TryAdd(Level1Fields.BidsVolume, bid.Volume);
			message.TryAdd(Level1Fields.AsksVolume, ask.Volume);

			message.TryAdd(Level1Fields.OpenPrice, open);
			message.TryAdd(Level1Fields.LowPrice, low);
			message.TryAdd(Level1Fields.HighPrice, high);
			message.TryAdd(Level1Fields.ClosePrice, close);

			message.TryAdd(Level1Fields.MinPrice, limits.Item1);
			message.TryAdd(Level1Fields.MaxPrice, limits.Item2);

			message.TryAdd(Level1Fields.MarginBuy, goBuySell.Item1);
			message.TryAdd(Level1Fields.MarginSell, goBuySell.Item2);
			message.TryAdd(Level1Fields.OpenInterest, openInt);

			message.TryAdd(Level1Fields.ImpliedVolatility, volatTheorPrice.Item1);
			message.TryAdd(Level1Fields.TheorPrice, volatTheorPrice.Item2);

			message.TryAdd(Level1Fields.Volume, volume);

			message.Add(Level1Fields.State, tradingStatus == 0 ? SecurityStates.Trading : SecurityStates.Stoped);

			SendOutMessage(message);
		}

		private void OnQuoteChanged(string smartId, int row, int rowCount, decimal bidPrice, decimal bidVolume, decimal askPrice, decimal askVolume)
		{
			//Debug.Write("Row = " + row + " Bid = " + bidPrice + " BidVolume = " + bidVolume + " Ask = " + askPrice + " AskVolume = " + askVolume);
			var secId = new SecurityId { Native = smartId };

			var tempDepth = _tempDepths.SafeAdd(secId, key => Tuple.Create(new List<QuoteChange>(), new List<QuoteChange>()));

			var bestQuotes = _bestQuotes.SafeAdd(secId);

			try
			{
				if (bidPrice != 0)
				{
					tempDepth.Item1.Add(new QuoteChange(Sides.Buy, bidPrice, bidVolume));

					if (row == 0)
						bestQuotes.First = Tuple.Create(bidPrice, bidVolume);
				}

				if (askPrice != 0)
				{
					tempDepth.Item2.Add(new QuoteChange(Sides.Sell, askPrice, askVolume));

					if (row == 0)
						bestQuotes.Second = Tuple.Create(askPrice, askVolume);
				}
			}
			finally
			{
				if ((row + 1) == rowCount)
				{
					SendOutMessage(new QuoteChangeMessage
					{
						Bids = tempDepth.Item1.ToArray(),
						Asks = tempDepth.Item2.ToArray(),
						SecurityId = secId,
						ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
					});

					tempDepth.Item1.Clear();
					tempDepth.Item2.Clear();
				}
			}
		}
	}
}