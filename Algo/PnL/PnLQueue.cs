#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.PnL.Algo
File: PnLQueue.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.PnL
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The queue of profit calculation by messages stream.
	/// </summary>
	public class PnLQueue
	{
		private Sides _openedPosSide;
		private readonly SynchronizedStack<RefPair<decimal, decimal>> _openedTrades = new SynchronizedStack<RefPair<decimal, decimal>>();
		private decimal _multiplier;

		/// <summary>
		/// Initializes a new instance of the <see cref="PnLQueue"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		public PnLQueue(SecurityId securityId)
		{
			SecurityId = securityId;
			PriceStep = 1;
			StepPrice = 1;
		}

		/// <summary>
		/// Security ID.
		/// </summary>
		public SecurityId SecurityId { get; private set; }

		private decimal _priceStep;

		/// <summary>
		/// Price step.
		/// </summary>
		public decimal PriceStep
		{
			get { return _priceStep; }
			private set
			{
				_priceStep = value;
				UpdateMultiplier();
			}
		}

		private decimal _stepPrice;

		/// <summary>
		/// Step price.
		/// </summary>
		public decimal StepPrice
		{
			get { return _stepPrice; }
			private set
			{
				_stepPrice = value;
				UpdateMultiplier();
			}
		}

		/// <summary>
		/// Last price of tick trade.
		/// </summary>
		public decimal TradePrice { get; private set; }

		/// <summary>
		/// Last price of demand.
		/// </summary>
		public decimal BidPrice { get; private set; }

		/// <summary>
		/// Last price of offer.
		/// </summary>
		public decimal AskPrice { get; private set; }

		private decimal? _unrealizedPnL;

		/// <summary>
		/// Unrealized profit.
		/// </summary>
		public decimal UnrealizedPnL
		{
			get
			{
				var v = _unrealizedPnL;

				if (v != null)
					return v.Value;

				var sum = _openedTrades
					.SyncGet(c => c.Sum(t =>
					{
						var price = _openedPosSide == Sides.Buy ? AskPrice : BidPrice;

						if (price == 0)
							price = TradePrice;

						return GetPnL(t.First, t.Second, _openedPosSide, price);
					}));

				v = _unrealizedPnL = sum * _multiplier;
				return v.Value;
			}
		}

		/// <summary>
		/// Realized profit.
		/// </summary>
		public decimal RealizedPnL { get; private set; }

		/// <summary>
		/// To calculate trade profitability. If the trade was already processed earlier, previous information returns.
		/// </summary>
		/// <param name="trade">Trade.</param>
		/// <returns>Information on new trade.</returns>
		public PnLInfo Process(ExecutionMessage trade)
		{
			if (trade == null)
				throw new ArgumentNullException(nameof(trade));

			var closedVolume = 0m;
			var pnl = 0m;
			var volume = trade.SafeGetVolume();
			var price = trade.GetTradePrice();

			_unrealizedPnL = null;

			lock (_openedTrades.SyncRoot)
			{
				if (_openedTrades.Count > 0)
				{
					var currTrade = _openedTrades.Peek();

					if (_openedPosSide != trade.Side)
					{
						while (volume > 0)
						{
							if (currTrade == null)
								currTrade = _openedTrades.Peek();

							var diff = currTrade.Second.Min(volume);
							closedVolume += diff;

							pnl += GetPnL(currTrade.First, diff, _openedPosSide, price);

							volume -= diff;
							currTrade.Second -= diff;

							if (currTrade.Second != 0)
								continue;

							currTrade = null;
							_openedTrades.Pop();

							if (_openedTrades.Count == 0)
								break;
						}
					}
				}

				if (volume > 0)
				{
					_openedPosSide = trade.Side;
					_openedTrades.Push(RefTuple.Create(price, volume));
				}

				RealizedPnL += _multiplier * pnl;
			}

			return new PnLInfo(trade, closedVolume, pnl);
		}

		/// <summary>
		/// To process the message, containing market data.
		/// </summary>
		/// <param name="levelMsg">The message, containing market data.</param>
		public void ProcessLevel1(Level1ChangeMessage levelMsg)
		{
			var priceStep = levelMsg.Changes.TryGetValue(Level1Fields.PriceStep);
			if (priceStep != null)
			{
				PriceStep = (decimal)priceStep;
				_unrealizedPnL = null;
			}

			var stepPrice = levelMsg.Changes.TryGetValue(Level1Fields.StepPrice);
			if (stepPrice != null)
			{
				StepPrice = (decimal)stepPrice;
				_unrealizedPnL = null;
			}

			var tradePrice = levelMsg.Changes.TryGetValue(Level1Fields.LastTradePrice);
			if (tradePrice != null)
			{
				TradePrice = (decimal)tradePrice;
				_unrealizedPnL = null;
			}

			var bidPrice = levelMsg.Changes.TryGetValue(Level1Fields.BestBidPrice);
			if (bidPrice != null)
			{
				BidPrice = (decimal)bidPrice;
				_unrealizedPnL = null;
			}

			var askPrice = levelMsg.Changes.TryGetValue(Level1Fields.BestAskPrice);
			if (askPrice != null)
			{
				AskPrice = (decimal)askPrice;
				_unrealizedPnL = null;
			}
		}

		/// <summary>
		/// To process the message, containing information on tick trade.
		/// </summary>
		/// <param name="execMsg">The message, containing information on tick trade.</param>
		public void ProcessExecution(ExecutionMessage execMsg)
		{
			if (execMsg.TradePrice != null)
			{
				TradePrice = execMsg.TradePrice.Value;
				_unrealizedPnL = null;
			}
		}

		/// <summary>
		/// To process the message, containing data on order book.
		/// </summary>
		/// <param name="quoteMsg">The message, containing data on order book.</param>
		public void ProcessQuotes(QuoteChangeMessage quoteMsg)
		{
			var ask = quoteMsg.GetBestAsk();
			AskPrice = ask != null ? ask.Price : 0;

			var bid = quoteMsg.GetBestBid();
			BidPrice = bid != null ? bid.Price : 0;

			_unrealizedPnL = null;
		}

		private void UpdateMultiplier()
		{
			_multiplier = StepPrice == 0 || PriceStep == 0 
				? 1 
				: StepPrice / PriceStep;
		}

		private static decimal GetPnL(decimal price, decimal volume, Sides side, decimal marketPrice)
		{
			return (price - marketPrice) * volume * (side == Sides.Sell ? 1 : -1);
		}
	}
}