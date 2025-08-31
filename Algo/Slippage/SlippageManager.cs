namespace StockSharp.Algo.Slippage;

/// <summary>
/// The slippage manager.
/// </summary>
public class SlippageManager : ISlippageManager
{
	private readonly SynchronizedDictionary<SecurityId, RefPair<decimal, decimal>> _bestPrices = [];
	private readonly SynchronizedDictionary<long, (Sides side, decimal price)> _plannedPrices = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="SlippageManager"/>.
	/// </summary>
	public SlippageManager()
	{
		CalculateNegative = true;
	}

	/// <inheritdoc />
	public decimal Slippage { get; private set; }

	/// <summary>
	/// To calculate negative slippage. By default, the calculation is enabled.
	/// </summary>
	public bool CalculateNegative { get; set; }

	/// <inheritdoc />
	public void Reset()
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
						_plannedPrices[regMsg.TransactionId] = (regMsg.Side, price);
				}

				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				// remove on order complete messages (no trade info) if possible
				if (!execMsg.HasTradeInfo() && execMsg.HasOrderInfo())
				{
					if (_plannedPrices.ContainsKey(execMsg.OriginalTransactionId))
					{
						if (execMsg.OrderState?.IsFinal() == true)
							_plannedPrices.Remove(execMsg.OriginalTransactionId);
					}

					break;
				}
				
				if (execMsg.HasTradeInfo())
				{
					if (_plannedPrices.TryGetValue(execMsg.OriginalTransactionId, out var t))
					{
						// If there is no trade price, cannot compute slippage; keep planned price for future executions.
						if (execMsg.TradePrice == null)
							return null;

						var diff = t.side == Sides.Buy
							? execMsg.TradePrice.Value - t.price
							: t.price - execMsg.TradePrice.Value;

						var volume = execMsg.TradeVolume ?? 1m;
						var weighted = diff * volume;

						if (!CalculateNegative && weighted < 0)
							weighted = 0;

						Slippage += weighted;

						// cleanup only when order is completed (if such info present)
						if (execMsg.HasOrderInfo() && (execMsg.OrderState == OrderStates.Done || execMsg.Balance == 0))
							_plannedPrices.Remove(execMsg.OriginalTransactionId);

						return weighted;
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