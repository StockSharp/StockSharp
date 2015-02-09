namespace StockSharp.Algo.PnL
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Очередь расчета прибыли по потому сообщений.
	/// </summary>
	public class PnLQueue
	{
		private Sides _openedPosSide;
		private readonly SynchronizedStack<RefPair<decimal, decimal>> _openedTrades = new SynchronizedStack<RefPair<decimal, decimal>>();
		private decimal _multiplier;

		/// <summary>
		/// Создать <see cref="PnLQueue"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		public PnLQueue(SecurityId securityId)
		{
			SecurityId = securityId;
			PriceStep = 1;
			StepPrice = 1;
		}

		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		public SecurityId SecurityId { get; private set; }

		private decimal _priceStep;

		/// <summary>
		/// Шаг цены.
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
		/// Стоимость шага цены.
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
		/// Последняя цена тиковой сделки.
		/// </summary>
		public decimal TradePrice { get; private set; }

		/// <summary>
		/// Последняя цена спроса.
		/// </summary>
		public decimal BidPrice { get; private set; }

		/// <summary>
		/// Последняя цена предложения.
		/// </summary>
		public decimal AskPrice { get; private set; }

		private decimal? _unrealizedPnL;

		/// <summary>
		/// Нереализованная прибыль.
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

						return TraderHelper.GetPnL(t.First, t.Second, _openedPosSide, price);
					}));

				v = _unrealizedPnL = sum * _multiplier;
				return v.Value;
			}
		}

		/// <summary>
		/// Реализованная прибыль.
		/// </summary>
		public decimal RealizedPnL { get; private set; }

		/// <summary>
		/// Рассчитать прибыльность сделки. Если сделка уже ранее была обработана, то возвращается предыдущая информация.
		/// </summary>
		/// <param name="trade">Сделка.</param>
		/// <returns>Информация о новой сделке.</returns>
		public PnLInfo Process(ExecutionMessage trade)
		{
			if (trade == null)
				throw new ArgumentNullException("trade");

			var closedVolume = 0m;
			var pnl = 0m;
			var volume = trade.Volume;

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

							pnl += TraderHelper.GetPnL(currTrade.First, diff, _openedPosSide, trade.TradePrice);

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
					_openedTrades.Push(new RefPair<decimal, decimal>(trade.TradePrice, volume));
				}

				RealizedPnL += _multiplier * pnl;
			}

			return new PnLInfo(trade, closedVolume, pnl);
		}

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="levelMsg">Сообщение, содержащее рыночные данные.</param>
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
		/// Обработать сообщение, содержащее информацию о тиковой сделке.
		/// </summary>
		/// <param name="execMsg">Сообщение, содержащее информацию о тиковой сделке.</param>
		public void ProcessExecution(ExecutionMessage execMsg)
		{
			if (execMsg.TradePrice != 0)
			{
				TradePrice = execMsg.TradePrice;
				_unrealizedPnL = null;
			}
		}

		/// <summary>
		/// Обработать сообщение, содержащее данные о стакане.
		/// </summary>
		/// <param name="quoteMsg">Сообщение, содержащее данные о стакане.</param>
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
	}
}