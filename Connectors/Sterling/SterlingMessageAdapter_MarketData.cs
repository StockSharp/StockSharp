namespace StockSharp.Sterling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using SterlingLib;

	using StockSharp.Algo;
	using StockSharp.Messages;

	/// <summary>
	/// The message adapter for Sterling.
	/// </summary>
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
				{
					SendOutMarketDataNotSupported(mdMsg.TransactionId);
					return;
				}
			}

			var reply = (MarketDataMessage)mdMsg.Clone();
			reply.OriginalTransactionId = mdMsg.TransactionId;
			SendOutMessage(reply);
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

			if (structQuoteUpdate.bAskPrice != 0)
				message.TryAdd(Level1Fields.BestAskPrice, (decimal)structQuoteUpdate.fAskPrice);

			if (structQuoteUpdate.bBidPrice != 0)
				message.TryAdd(Level1Fields.BestBidPrice, (decimal)structQuoteUpdate.fBidPrice);

			message.TryAdd(Level1Fields.BestAskVolume, (decimal)structQuoteUpdate.nAskSize);
			message.TryAdd(Level1Fields.BestBidVolume, (decimal)structQuoteUpdate.nBidSize);

			if (structQuoteUpdate.bOpenPrice != 0)
				message.TryAdd(Level1Fields.OpenPrice, (decimal)structQuoteUpdate.fOpenPrice);
			
			if (structQuoteUpdate.bHighPrice != 0)
				message.TryAdd(Level1Fields.HighPrice, (decimal)structQuoteUpdate.fHighPrice);
			
			if (structQuoteUpdate.bLowPrice != 0)
				message.TryAdd(Level1Fields.LowPrice, (decimal)structQuoteUpdate.fLowPrice);

			if (structQuoteUpdate.bLastPrice != 0)
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

			if (structQuoteSnap.bAskPrice != 0)
				l1CngMsg.TryAdd(Level1Fields.BestAskPrice, (decimal)structQuoteSnap.fAskPrice);

			if (structQuoteSnap.bBidPrice != 0)
				l1CngMsg.TryAdd(Level1Fields.BestBidPrice, (decimal)structQuoteSnap.fBidPrice);

			l1CngMsg.TryAdd(Level1Fields.BestAskVolume, (decimal)structQuoteSnap.nAskSize);
			l1CngMsg.TryAdd(Level1Fields.BestBidVolume, (decimal)structQuoteSnap.nBidSize);

			if (structQuoteSnap.bOpenPrice != 0)
				l1CngMsg.TryAdd(Level1Fields.OpenPrice, (decimal)structQuoteSnap.fOpenPrice);

			if (structQuoteSnap.bHighPrice != 0)
				l1CngMsg.TryAdd(Level1Fields.HighPrice, (decimal)structQuoteSnap.fHighPrice);

			if (structQuoteSnap.bLowPrice != 0)
				l1CngMsg.TryAdd(Level1Fields.LowPrice, (decimal)structQuoteSnap.fLowPrice);

			if (structQuoteSnap.bLastPrice != 0)
				l1CngMsg.TryAdd(Level1Fields.LastTradePrice, (decimal)structQuoteSnap.fLastPrice);

			l1CngMsg.TryAdd(Level1Fields.LastTradeVolume, (decimal)structQuoteSnap.nLastSize);

			l1CngMsg.TryAdd(Level1Fields.OpenInterest, (decimal)structQuoteSnap.nOpenInterest);
			l1CngMsg.TryAdd(Level1Fields.Volume, (decimal)structQuoteSnap.nCumVolume);
			l1CngMsg.TryAdd(Level1Fields.VWAP, (decimal)structQuoteSnap.fVwap);

			l1CngMsg.TryAdd(Level1Fields.ClosePrice, (decimal)structQuoteSnap.fClosePrice); // öåíà çàêðûòèÿ ïðîøëîãî äíÿ.

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
			var depth = _depths[structL2Update.bstrSymbol];

			var quote = new QuoteChange(structL2Update.bstrSide.ToSide(), (decimal)structL2Update.fPrice, structL2Update.nQty);

			var quotes = quote.Side == Sides.Sell ? depth.Item1 : depth.Item2;

			switch (structL2Update.bstrAction)
			{
				case "A": // add
				{
					quotes.Add(quote);
					break;
				}
				case "C": // change
				{
					quotes.RemoveWhere(q => q.Price == quote.Price && q.BoardCode == quote.BoardCode);
					quotes.Add(quote);
					break;
				}
				case "D": // delete
				{
					quotes.RemoveWhere(q => q.Price == quote.Price && q.BoardCode == quote.BoardCode);
					break;
				}
			}

			var board = structL2Update.bstrMaker;

			if (board.IsEmpty())
				board = AssociatedBoardCode;

			var message = new QuoteChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = structL2Update.bstrSymbol,
					BoardCode = board,
				},
				Asks = depth.Item1.ToArray(),
				Bids = depth.Item2.ToArray(),
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
						bidsUpdate.Add(new QuoteChange(structL2Update.bstrSide.ToSide(), (decimal)structL2Update.fPrice, structL2Update.nQty) { BoardCode = structL2Update.bstrMaker });
						break;
					}
					case Sides.Sell:
					{
						asksUpdate.Add(new QuoteChange(structL2Update.bstrSide.ToSide(), (decimal)structL2Update.fPrice, structL2Update.nQty) { BoardCode = structL2Update.bstrMaker });
						break;
					}
				}
			}

			var quote = (structSTIL2Update)arrayL2Update.GetValue(0);

			_depths[quote.bstrSymbol] = Tuple.Create(asksUpdate, bidsUpdate);

			var message = new QuoteChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = quote.bstrSymbol,
					BoardCode = AssociatedBoardCode,
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
					BoardCode = AssociatedBoardCode,
				},
				ServerTime = CurrentTime,
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
				SecurityId = new SecurityId { SecurityCode = structNewsUpdate.bstrKeys, BoardCode = AssociatedBoardCode },
				Headline = structNewsUpdate.bstrHeadline,
				Story = structNewsUpdate.bstrSeq,
				Source = structNewsUpdate.bstrService, 
				ServerTime = structNewsUpdate.bstrDisplayTime.StrToDateTime()
			});
		}
	}
}