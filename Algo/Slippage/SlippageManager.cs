#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Slippage.Algo
File: SlippageManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Slippage
{
	using System;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The slippage manager.
	/// </summary>
	public class SlippageManager : ISlippageManager
	{
		private readonly SynchronizedDictionary<SecurityId, RefPair<decimal, decimal>> _bestPrices = new SynchronizedDictionary<SecurityId, RefPair<decimal, decimal>>();
		private readonly SynchronizedDictionary<long, Tuple<Sides, decimal>> _plannedPrices = new SynchronizedDictionary<long, Tuple<Sides, decimal>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="SlippageManager"/>.
		/// </summary>
		public SlippageManager()
		{
			CalculateNegative = true;
		}

		/// <inheritdoc />
		public virtual decimal Slippage { get; private set; }

		/// <summary>
		/// To calculate negative slippage. By default, the calculation is enabled.
		/// </summary>
		public bool CalculateNegative { get; set; }

		/// <inheritdoc />
		public virtual void Reset()
		{
			Slippage = 0;
			_bestPrices.Clear();
			_plannedPrices.Clear();
		}

		/// <inheritdoc />
		public decimal? ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					Reset();
					break;
				}

				case MessageTypes.Level1Change:
				{
					var l1Msg = (Level1ChangeMessage)message;
					var pair = _bestPrices.SafeAdd(l1Msg.SecurityId);

					var bidPrice = l1Msg.TryGetDecimal(Level1Fields.BestBidPrice);
					if (bidPrice != null)
						pair.First = bidPrice.Value;

					var askPrice = l1Msg.TryGetDecimal(Level1Fields.BestAskPrice);
					if (askPrice != null)
						pair.Second = askPrice.Value;

					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quotesMsg = (QuoteChangeMessage)message;

					if (quotesMsg.State != null)
						break;

					var pair = _bestPrices.SafeAdd(quotesMsg.SecurityId);

					var bid = quotesMsg.GetBestBid();
					if (bid != null)
						pair.First = bid.Value.Price;

					var ask = quotesMsg.GetBestAsk();
					if (ask != null)
						pair.Second = ask.Value.Price;

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
					
					if (execMsg.HasTradeInfo())
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

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public void Load(SettingsStorage storage)
		{
			CalculateNegative = storage.GetValue<bool>(nameof(CalculateNegative));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(CalculateNegative), CalculateNegative);
		}
	}
}