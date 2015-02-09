namespace StockSharp.CQG
{
	using System;
	using System.Linq;

	using Ecng.Common;

	using global::CQG;

	using StockSharp.Messages;

	partial class CQGMessageAdapter
	{
		private void SendLevel1Message(CQGInstrument cqgInstrument)
		{
			
		}

		private void SessionOnInstrumentSubscribed(string symbol, CQGInstrument cqgInstrument)
		{
			SessionHolder.Instruments[symbol] = cqgInstrument;

			OptionTypes? optionType = null;

			if (cqgInstrument.InstrumentType == eInstrumentType.itOptionCall)
				optionType = OptionTypes.Call;
			else if (cqgInstrument.InstrumentType == eInstrumentType.itOptionPut)
				optionType = OptionTypes.Put;

			SendOutMessage(new SecurityMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = cqgInstrument.Commodity,
					BoardCode = cqgInstrument.ExchangeAbbreviation,
					Native = cqgInstrument.InstrumentID,
				},
				Currency = cqgInstrument.Currency.To<CurrencyTypes>(),
				Name = cqgInstrument.FullName,
				ExpiryDate = cqgInstrument.ExpirationDate.ApplyTimeZone(TimeHelper.Est),
				SecurityType = cqgInstrument.InstrumentType.ToStockSharp(),
				Strike = cqgInstrument.Strike,
				UnderlyingSecurityCode = cqgInstrument.UnderlyingInstrumentName,
				OptionType = optionType,
			});

			SendLevel1Message(cqgInstrument);
		}

		private void SessionOnIncorrectSymbol(string symbol)
		{

		}

		private void SessionOnInstrumentDomChanged(CQGInstrument cqgInstrument, CQGDOMQuotes prevAsks, CQGDOMQuotes prevBids)
		{
			SendOutMessage(new QuoteChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = cqgInstrument.Commodity,
					BoardCode = cqgInstrument.ExchangeAbbreviation,
				},

				ServerTime = cqgInstrument.ServerTimestamp.ApplyTimeZone(TimeHelper.Est),

				Bids = cqgInstrument.DOMBids
							.Cast<CQGQuote>()
							.Where(q => q.IsValid)
							.Select(q => new QuoteChange(Sides.Buy, (decimal)q.Price, q.HasVolume ? q.Volume : 0)),
				
				Asks = cqgInstrument.DOMAsks
							.Cast<CQGQuote>()
							.Where(q => q.IsValid)
							.Select(q => new QuoteChange(Sides.Sell, (decimal)q.Price, q.HasVolume ? q.Volume : 0)),
			});
		}

		private void SessionOnInstrumentChanged(CQGInstrument cqgInstrument, CQGQuotes cqgQuotes, CQGInstrumentProperties cqgInstrumentProperties)
		{
			SendLevel1Message(cqgInstrument);
		}

		private void SessionOnTicksAdded(CQGTicks cqgTicks, int addedTicksCount)
		{
			for (var i = 0; i < addedTicksCount; i++)
			{
				var tick = cqgTicks[cqgTicks.Count - i - 1];

				SendOutMessage(new ExecutionMessage
				{
					// TODO
					TradePrice = (decimal)tick.Price,
					Volume = tick.Volume,
					ExecutionType = ExecutionTypes.Tick,
				});
			}
		}

		private void SessionOnPointAndFigureBarsAdded(CQGPointAndFigureBars cqgPointAndFigureBars)
		{
			
		}

		private void SessionOnPointAndFigureBarsUpdated(CQGPointAndFigureBars cqgPointAndFigureBars, int index)
		{
			
		}

		private void SessionOnConstantVolumeBarsUpdated(CQGConstantVolumeBars cqgConstantVolumeBars, int index)
		{
			
		}

		private void SessionOnConstantVolumeBarsAdded(CQGConstantVolumeBars cqgConstantVolumeBars)
		{
			
		}

		private void SessionOnFlowBarsUpdated(CQGTFlowBars cqgTflowBars, int index)
		{
			
		}

		private void SessionOnFlowBarsAdded(CQGTFlowBars cqgTflowBars)
		{
			
		}

		private void SessionOnTimedBarsUpdated(CQGTimedBars cqgTimedBars, int index)
		{
			
		}

		private void SessionOnTimedBarsAdded(CQGTimedBars cqgTimedBars)
		{
			
		}
	}
}