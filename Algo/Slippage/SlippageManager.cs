namespace StockSharp.Algo.Slippage
{
	using System;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Менеджер проскальзывания.
	/// </summary>
	public class SlippageManager : ISlippageManager
	{
		private readonly SynchronizedDictionary<SecurityId, RefPair<decimal, decimal>> _bestPrices = new SynchronizedDictionary<SecurityId, RefPair<decimal, decimal>>();
		private readonly SynchronizedDictionary<long, Tuple<Sides, decimal>> _plannedPrices = new SynchronizedDictionary<long, Tuple<Sides, decimal>>();

		/// <summary>
		/// Создать <see cref="SlippageManager"/>.
		/// </summary>
		public SlippageManager()
		{
			CalculateNegative = true;
		}

		/// <summary>
		/// Суммарное значение проскальзывания.
		/// </summary>
		public virtual decimal Slippage { get; private set; }

		/// <summary>
		/// Считать отрицательное проскальзывание. По-умолчанию расчет выключен.
		/// </summary>
		public bool CalculateNegative { get; set; }

		/// <summary>
		/// Обнулить <see cref="ISlippageManager.Slippage"/>.
		/// </summary>
		public virtual void Reset()
		{
			Slippage = 0;
			_bestPrices.Clear();
			_plannedPrices.Clear();
		}

		/// <summary>
		/// Рассчитать проскальзывание.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <returns>Проскальзывание. Если проскальзывание рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		public decimal? ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Level1Change:
				{
					var l1Msg = (Level1ChangeMessage)message;
					var pair = _bestPrices.SafeAdd(l1Msg.SecurityId);

					var bidPrice = (decimal?)l1Msg.Changes.TryGetValue(Level1Fields.BestBidPrice);
					if (bidPrice != null)
						pair.First = bidPrice.Value;

					var askPrice = (decimal?)l1Msg.Changes.TryGetValue(Level1Fields.BestAskPrice);
					if (askPrice != null)
						pair.Second = askPrice.Value;

					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quotesMsg = (QuoteChangeMessage)message;
					var pair = _bestPrices.SafeAdd(quotesMsg.SecurityId);

					var bid = quotesMsg.GetBestBid();
					if (bid != null)
						pair.First = bid.Price;

					var ask = quotesMsg.GetBestAsk();
					if (ask != null)
						pair.Second = ask.Price;

					break;
				}

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					var prices = _bestPrices.TryGetValue(regMsg.SecurityId);

					if (prices != null)
					{
						var price = regMsg.Side == Sides.Buy ? prices.Second : prices.First;

						if (price != 0)
							_plannedPrices.Add(regMsg.TransactionId, Tuple.Create(regMsg.Side, price));
					}

					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType == ExecutionTypes.Trade)
					{
						var plannedPrice = _plannedPrices.TryGetValue(execMsg.OriginalTransactionId);

						if (plannedPrice != null)
						{
							var slippage = execMsg.TradePrice - plannedPrice.Item2;

							if (plannedPrice.Item1 == Sides.Sell)
								slippage = -slippage;

							if (slippage < 0 && !CalculateNegative)
								slippage = 0;

							return slippage;
						}
					}

					break;
				}
			}

			return null;
		}
	}
}