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

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The queue of profit calculation by messages stream.
	/// </summary>
	public class PnLQueue
	{
		private Sides _openedPosSide;
		private readonly SynchronizedStack<RefPair<decimal, decimal>> _openedTrades = new();
		private decimal _multiplier;

		/// <summary>
		/// Initializes a new instance of the <see cref="PnLQueue"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		public PnLQueue(SecurityId securityId)
		{
			SecurityId = securityId;
			UpdateMultiplier();
		}

		/// <summary>
		/// Security ID.
		/// </summary>
		public SecurityId SecurityId { get; }

		private decimal _priceStep = 1;

		/// <summary>
		/// Price step.
		/// </summary>
		public decimal PriceStep
		{
			get => _priceStep;
			private set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_priceStep = value;
				UpdateMultiplier();
			}
		}

		private decimal? _stepPrice;

		/// <summary>
		/// Step price.
		/// </summary>
		public decimal? StepPrice
		{
			get => _stepPrice;
			private set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_stepPrice = value;
				UpdateMultiplier();
			}
		}

		private decimal _leverage = 1;

		/// <summary>
		/// Leverage.
		/// </summary>
		public decimal Leverage
		{
			get => _leverage;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_leverage = value;
				UpdateMultiplier();
			}
		}

		private decimal _lotMultiplier = 1;

		/// <summary>
		/// Lot multiplier.
		/// </summary>
		public decimal LotMultiplier
		{
			get => _lotMultiplier;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_lotMultiplier = value;
				UpdateMultiplier();
			}
		}

		/// <summary>
		/// Last price of tick trade.
		/// </summary>
		public decimal? LastPrice { get; private set; }

		/// <summary>
		/// Last price of bid.
		/// </summary>
		public decimal? BidPrice { get; private set; }

		/// <summary>
		/// Last price of offer.
		/// </summary>
		public decimal? AskPrice { get; private set; }

		private bool _recalcUnrealizedPnL;
		private decimal? _unrealizedPnL;

		/// <summary>
		/// Unrealized profit.
		/// </summary>
		public decimal? UnrealizedPnL
		{
			get
			{
				if (!_recalcUnrealizedPnL)
					return _unrealizedPnL;

				var sum = _openedTrades
					.SyncGet(c => c.Sum(t =>
					{
						var price = (_openedPosSide == Sides.Buy ? AskPrice : BidPrice) ?? LastPrice;

						if (price == null)
							return null;

						return GetPnL(t.First, t.Second, _openedPosSide, price.Value);
					}));

				_unrealizedPnL = sum * _multiplier;
				return _unrealizedPnL;
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

			decimal tradePnL;

			lock (_openedTrades.SyncRoot)
			{
				if (_openedTrades.Count > 0)
				{
					var currTrade = _openedTrades.Peek();

					if (_openedPosSide != trade.Side)
					{
						while (volume > 0)
						{
							currTrade ??= _openedTrades.Peek();

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

				tradePnL = _multiplier * pnl;
				RealizedPnL += tradePnL;
			}

			return new(trade.ServerTime, closedVolume, tradePnL);
		}

		/// <summary>
		/// To process the message, containing market data.
		/// </summary>
		/// <param name="levelMsg">The message, containing market data.</param>
		public void ProcessLevel1(Level1ChangeMessage levelMsg)
		{
			var priceStep = levelMsg.TryGetDecimal(Level1Fields.PriceStep);
			if (priceStep != null)
			{
				PriceStep = (decimal)priceStep;
				_recalcUnrealizedPnL = true;
			}

			var stepPrice = levelMsg.TryGetDecimal(Level1Fields.StepPrice);
			if (stepPrice != null)
			{
				StepPrice = (decimal)stepPrice;
				_recalcUnrealizedPnL = true;
			}

			var lotMultiplier = levelMsg.TryGetDecimal(Level1Fields.Multiplier);
			if (lotMultiplier != null)
			{
				LotMultiplier = (decimal)lotMultiplier;
				_recalcUnrealizedPnL = true;
			}

			var tradePrice = levelMsg.TryGetDecimal(Level1Fields.LastTradePrice);
			if (tradePrice != null)
			{
				LastPrice = (decimal)tradePrice;
				_recalcUnrealizedPnL = true;
			}

			var bidPrice = levelMsg.TryGetDecimal(Level1Fields.BestBidPrice);
			if (bidPrice != null)
			{
				BidPrice = (decimal)bidPrice;
				_recalcUnrealizedPnL = true;
			}

			var askPrice = levelMsg.TryGetDecimal(Level1Fields.BestAskPrice);
			if (askPrice != null)
			{
				AskPrice = (decimal)askPrice;
				_recalcUnrealizedPnL = true;
			}
		}

		/// <summary>
		/// To process <see cref="CandleMessage"/> message.
		/// </summary>
		/// <param name="candleMsg"><see cref="CandleMessage"/>.</param>
		public void ProcessCandle(CandleMessage candleMsg)
		{
			LastPrice = candleMsg.ClosePrice;

			_recalcUnrealizedPnL = true;
		}

		/// <summary>
		/// To process the message, containing information on tick trade.
		/// </summary>
		/// <param name="execMsg">The message, containing information on tick trade.</param>
		public void ProcessExecution(ExecutionMessage execMsg)
		{
			if (execMsg.TradePrice == null)
				return;

			LastPrice = execMsg.TradePrice.Value;

			_recalcUnrealizedPnL = true;
		}

		/// <summary>
		/// To process the message, containing data on order book.
		/// </summary>
		/// <param name="quoteMsg">The message, containing data on order book.</param>
		public void ProcessQuotes(QuoteChangeMessage quoteMsg)
		{
			AskPrice = quoteMsg.GetBestAsk()?.Price;
			BidPrice = quoteMsg.GetBestBid()?.Price;

			_recalcUnrealizedPnL = true;
		}

		private void UpdateMultiplier()
		{
			var stepPrice = StepPrice;

			_multiplier = (stepPrice == null ? 1 : stepPrice.Value / PriceStep) * Leverage * LotMultiplier;
		}

		private static decimal GetPnL(decimal price, decimal volume, Sides side, decimal marketPrice)
		{
			return (price - marketPrice) * volume * (side == Sides.Sell ? 1 : -1);
		}
	}
}