namespace StockSharp.Sterling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using SterlingLib;

	using StockSharp.Algo;
	using StockSharp.Localization;
	using StockSharp.Messages;

	partial class SterlingMessageAdapter
	{
		private readonly CachedSynchronizedSet<string> _subscribedSecuritiesToTrade = new CachedSynchronizedSet<string>(); 
		
		private void ProcessMarketData(MarketDataMessage mdMsg)
		{
			var secCode = mdMsg.SecurityId.SecurityCode;
			var boardCode = mdMsg.SecurityId.BoardCode;

			switch (mdMsg.DataType)
			{
				case MarketDataTypes.Level1:
				{
					if (mdMsg.IsSubscribe)
						_client.SubscribeQuote(secCode, boardCode);
					else
						_client.UnsubsribeQuote(secCode, boardCode);

					break;
				}

				case MarketDataTypes.MarketDepth:
				{
					if (mdMsg.IsSubscribe)
						_client.SubscribeLevel2(secCode, boardCode);
					else
						_client.UnsubsribeLevel2(secCode, boardCode);

					break;
				}

				case MarketDataTypes.Trades:
				{
					if (mdMsg.IsSubscribe)
					{
						_subscribedSecuritiesToTrade.Add(secCode);
						_client.SubscribeQuote(secCode, boardCode);
					}
					else
					{
						_subscribedSecuritiesToTrade.Remove(secCode);
						_client.UnsubsribeQuote(secCode, boardCode);
					}

					break;
				}
				
				case MarketDataTypes.News:
				{
					if (mdMsg.IsSubscribe)
						_client.SubscribeNews();
					else
						_client.UnsubscribeNews();

					break;
				}

				default:
					throw new ArgumentOutOfRangeException("mdMsg", mdMsg.DataType, LocalizedStrings.Str1618);
			}

			var reply = (MarketDataMessage)mdMsg.Clone();
			reply.OriginalTransactionId = mdMsg.TransactionId;
			SendOutMessage(reply);
		}

		private void ProcessBoardMessage(BoardMessage boardMsg)
		{
			SendOutMessage(boardMsg);
		}

		private void ProcessSecurityMessage(SecurityMessage securityMsg)
		{
			SendOutMessage(securityMsg);
		}

		private void SessionOnStiQuoteUpdate(ref structSTIQuoteUpdate structQuoteUpdate)
		{
			var message = new Level1ChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = structQuoteUpdate.bstrSymbol,
					BoardCode = structQuoteUpdate.bstrExch,
				},
				ServerTime = structQuoteUpdate.bstrUpdateTime.StrToTime(),
			};

			message.TryAdd(Level1Fields.BestAskPrice, (decimal)structQuoteUpdate.fAskPrice);
			message.TryAdd(Level1Fields.BestBidPrice, (decimal)structQuoteUpdate.fAskPrice);
			message.TryAdd(Level1Fields.BestAskVolume, (decimal)structQuoteUpdate.nAskSize);
			message.TryAdd(Level1Fields.BestBidVolume, (decimal)structQuoteUpdate.nBidSize);
			
			message.TryAdd(Level1Fields.OpenPrice, (decimal)structQuoteUpdate.fOpenPrice);
			message.TryAdd(Level1Fields.HighPrice, (decimal)structQuoteUpdate.fHighPrice);
			message.TryAdd(Level1Fields.LowPrice, (decimal)structQuoteUpdate.fLowPrice);

			message.TryAdd(Level1Fields.LastTradePrice, (decimal)structQuoteUpdate.fLastPrice);
			message.TryAdd(Level1Fields.LastTradeVolume, (decimal)structQuoteUpdate.nLastSize);

			message.TryAdd(Level1Fields.OpenInterest, (decimal)structQuoteUpdate.nOpenInterest);
			message.TryAdd(Level1Fields.Volume, (decimal)structQuoteUpdate.nCumVolume);
			message.TryAdd(Level1Fields.VWAP, (decimal)structQuoteUpdate.fVwap);
			
			SendOutMessage(message);

			if (_subscribedSecuritiesToTrade.Cache.Contains(structQuoteUpdate.bstrSymbol) && structQuoteUpdate.fLastPrice != 0)
			{
				var tickMsg = new ExecutionMessage
				{
					ExecutionType = ExecutionTypes.Tick,
					SecurityId = new SecurityId { SecurityCode = structQuoteUpdate.bstrSymbol, BoardCode = structQuoteUpdate.bstrExch },
					//TradeId = structQuoteSnap.,
					TradePrice = (decimal)structQuoteUpdate.fLastPrice,
					Volume = structQuoteUpdate.nLastSize,
					//OriginSide = action.ToSide(),
					ServerTime = structQuoteUpdate.bstrUpdateTime.StrToTime()
				};

				SendOutMessage(tickMsg);
			}
		}

		private void SessionOnStiQuoteSnap(ref structSTIQuoteSnap structQuoteSnap)
		{
			var l1CngMsg = new Level1ChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = structQuoteSnap.bstrSymbol,
					BoardCode = structQuoteSnap.bstrExch
				},
				ServerTime = structQuoteSnap.bstrUpdateTime.StrToTime(),
			};

			l1CngMsg.TryAdd(Level1Fields.BestAskPrice, (decimal)structQuoteSnap.fAskPrice);
			l1CngMsg.TryAdd(Level1Fields.BestBidPrice, (decimal)structQuoteSnap.fAskPrice);
			l1CngMsg.TryAdd(Level1Fields.BestAskVolume, (decimal)structQuoteSnap.nAskSize);
			l1CngMsg.TryAdd(Level1Fields.BestBidVolume, (decimal)structQuoteSnap.nBidSize);

			l1CngMsg.TryAdd(Level1Fields.OpenPrice, (decimal)structQuoteSnap.fOpenPrice);
			l1CngMsg.TryAdd(Level1Fields.HighPrice, (decimal)structQuoteSnap.fHighPrice);
			l1CngMsg.TryAdd(Level1Fields.LowPrice, (decimal)structQuoteSnap.fLowPrice);

			l1CngMsg.TryAdd(Level1Fields.LastTradePrice, (decimal)structQuoteSnap.fLastPrice);
			l1CngMsg.TryAdd(Level1Fields.LastTradeVolume, (decimal)structQuoteSnap.nLastSize);

			l1CngMsg.TryAdd(Level1Fields.OpenInterest, (decimal)structQuoteSnap.nOpenInterest);
			l1CngMsg.TryAdd(Level1Fields.Volume, (decimal)structQuoteSnap.nCumVolume);
			l1CngMsg.TryAdd(Level1Fields.VWAP, (decimal)structQuoteSnap.fVwap);

			l1CngMsg.TryAdd(Level1Fields.ClosePrice, (decimal)structQuoteSnap.fClosePrice); // цена закрытия прошлого дня.

			SendOutMessage(l1CngMsg);

			if (_subscribedSecuritiesToTrade.Cache.Contains(structQuoteSnap.bstrSymbol) && structQuoteSnap.fLastPrice != 0)
			{
				var tickMsg= new ExecutionMessage
				{
					ExecutionType = ExecutionTypes.Tick,
					SecurityId = new SecurityId{SecurityCode = structQuoteSnap.bstrSymbol,BoardCode = structQuoteSnap.bstrExch},
					//TradeId = structQuoteSnap.,
					TradePrice = (decimal)structQuoteSnap.fLastPrice,
					Volume = structQuoteSnap.nLastSize,
					//OriginSide = action.ToSide(),
					ServerTime = structQuoteSnap.bstrUpdateTime.StrToTime()
				};

				SendOutMessage(tickMsg);
			}
		}

		private void SessionOnStiQuoteRqst(ref structSTIQuoteRqst structQuoteRqst)
		{
		}

		private readonly Dictionary<string, Tuple<List<QuoteChange>, List<QuoteChange>>> _depths = new Dictionary<string, Tuple<List<QuoteChange>, List<QuoteChange>>>();  

		private void SessionOnStil2Update(ref structSTIL2Update structL2Update)
		{
			var asksUpdate = _depths[structL2Update.bstrSymbol].Item1;
			var bidsUpdate = _depths[structL2Update.bstrSymbol].Item2;

			var quote = new QuoteChange(structL2Update.bstrSide.ToSide(), (decimal) structL2Update.fPrice, structL2Update.nQty) {BoardCode = structL2Update.bstrMaker};

			switch (structL2Update.bstrSide.ToSide())
			{
				case Sides.Buy:
				{
					switch (structL2Update.bstrAction)
					{
						case "A": // add
						{
							bidsUpdate.Add(quote);
							break;
						}
						case "C": // change
						{
							bidsUpdate.RemoveWhere(q => q.Price == quote.Price && q.BoardCode == quote.BoardCode);
							bidsUpdate.Add(quote);
							break;
						}
						case "D": // delete
						{
							bidsUpdate.RemoveWhere(q => q.Price == quote.Price && q.BoardCode == quote.BoardCode);
							break;
						}
					}

					break;
				}

				case Sides.Sell:
				{
					switch (structL2Update.bstrAction)
					{
						case "A": // add
						{
							asksUpdate.Add(quote);
							break;
						}
						case "C": // change
						{
							asksUpdate.RemoveWhere(q => q.Price == quote.Price && q.BoardCode == quote.BoardCode);
							asksUpdate.Add(quote);
							break;
						}
						case "D": // delete
						{
							asksUpdate.RemoveWhere(q => q.Price == quote.Price && q.BoardCode == quote.BoardCode);
							break;
						}
					}

					break;
				}
			}

			var message = new QuoteChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = structL2Update.bstrSymbol,
					BoardCode = "All",
				},
				Asks = asksUpdate,
				Bids = bidsUpdate,
				ServerTime = structL2Update.bstrTime.StrToTime(),
			};

			SendOutMessage(message);			
		}

		private void SessionOnStil2Reply(ref Array arrayL2Update)
		{
			var asksUpdate = new List<QuoteChange>();
			var bidsUpdate = new List<QuoteChange>();

			foreach (structSTIL2Update structL2Update in arrayL2Update)
			{
				switch (structL2Update.bstrSide.ToSide())
				{
					case Sides.Buy:
					{
						bidsUpdate.Add(new QuoteChange(structL2Update.bstrSide.ToSide(), (decimal) structL2Update.fPrice, structL2Update.nQty) {BoardCode = structL2Update.bstrMaker});
						break;
					}
					case Sides.Sell:
					{
						asksUpdate.Add(new QuoteChange(structL2Update.bstrSide.ToSide(), (decimal)structL2Update.fPrice, structL2Update.nQty) { BoardCode = structL2Update.bstrMaker});
						break;
					}
				}
			}

			var quote = (structSTIL2Update)arrayL2Update.GetValue(0);

			if (_depths.ContainsKey(quote.bstrSymbol))
			{
				_depths.Remove(quote.bstrSymbol);
			}

			_depths.Add(quote.bstrSymbol, new Tuple<List<QuoteChange>, List<QuoteChange>>(asksUpdate, bidsUpdate));

			var message = new QuoteChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = quote.bstrSymbol,
					BoardCode = "All",
				},
				Asks = asksUpdate,
				Bids = bidsUpdate,
				ServerTime = quote.bstrTime.StrToTime(),
			};

			SendOutMessage(message);			
		}

		private void SessionOnStiGreeksUpdate(ref structSTIGreeksUpdate structGreeksUpdate)
		{
			var message = new Level1ChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = structGreeksUpdate.bstrSymbol,
					//BoardCode = structGreeksUpdate.bstrExch,
				},
				ServerTime = SessionHolder.CurrentTime,
			};

			message.TryAdd(Level1Fields.Delta, (decimal)structGreeksUpdate.fDelta);
			message.TryAdd(Level1Fields.Gamma, (decimal)structGreeksUpdate.fGamma);
			message.TryAdd(Level1Fields.Theta, (decimal)structGreeksUpdate.fTheta);
			message.TryAdd(Level1Fields.Vega, (decimal)structGreeksUpdate.fVega);
			message.TryAdd(Level1Fields.Rho, (decimal)structGreeksUpdate.fRho);
			message.TryAdd(Level1Fields.TheorPrice, (decimal)structGreeksUpdate.fTheoPrice);
			message.TryAdd(Level1Fields.ImpliedVolatility, (decimal)structGreeksUpdate.fImpVol);

			SendOutMessage(message);			
		}

		private void SessionOnStiNewsUpdate(ref structSTINewsUpdate structNewsUpdate)
		{
			SendOutMessage(new NewsMessage
			{
				SecurityId = new SecurityId { SecurityCode = structNewsUpdate.bstrKeys},
				Headline = structNewsUpdate.bstrHeadline,
				Story = structNewsUpdate.bstrSeq,
				Source = structNewsUpdate.bstrService, 
				ServerTime = structNewsUpdate.bstrDisplayTime.StrToDateTime()
			});
		}
	}
}