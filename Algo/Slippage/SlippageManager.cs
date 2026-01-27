namespace StockSharp.Algo.Slippage;

/// <summary>
/// The slippage manager.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SlippageManager"/>.
/// </remarks>
/// <param name="state">State storage.</param>
public class SlippageManager(ISlippageManagerState state) : ISlippageManager
{
	private readonly ISlippageManagerState _state = state ?? throw new ArgumentNullException(nameof(state));

	/// <inheritdoc />
	public decimal Slippage => _state.Slippage;

	/// <summary>
	/// To calculate negative slippage. By default, the calculation is enabled.
	/// </summary>
	public bool CalculateNegative { get; set; } = true;

	/// <inheritdoc />
	public void Reset()
	{
		_state.Clear();
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

				var bidPrice = l1Msg.TryGetDecimal(Level1Fields.BestBidPrice);
				var askPrice = l1Msg.TryGetDecimal(Level1Fields.BestAskPrice);

				if (bidPrice != null || askPrice != null)
					_state.UpdateBestPrices(l1Msg.SecurityId, bidPrice, askPrice, l1Msg.ServerTime);

				break;
			}

			case MessageTypes.QuoteChange:
			{
				var quotesMsg = (QuoteChangeMessage)message;

				if (quotesMsg.State != null)
					break;

				var bid = quotesMsg.GetBestBid();
				var ask = quotesMsg.GetBestAsk();

				if (bid != null || ask != null)
					_state.UpdateBestPrices(quotesMsg.SecurityId, bid?.Price, ask?.Price, quotesMsg.ServerTime);

				break;
			}

			case MessageTypes.OrderRegister:
			{
				var regMsg = (OrderRegisterMessage)message;

				if (_state.TryGetBestPrice(regMsg.SecurityId, regMsg.Side, out var price))
					_state.AddPlannedPrice(regMsg.TransactionId, regMsg.Side, price);

				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				// remove on order complete messages (no trade info) if possible
				if (!execMsg.HasTradeInfo() && execMsg.HasOrderInfo())
				{
					if (execMsg.OrderState?.IsFinal() == true)
						_state.RemovePlannedPrice(execMsg.OriginalTransactionId);

					break;
				}

				if (execMsg.HasTradeInfo())
				{
					if (_state.TryGetPlannedPrice(execMsg.OriginalTransactionId, out var side, out var plannedPrice))
					{
						// fill MarketPrice if not already set by emulator or external source
						execMsg.MarketPrice ??= plannedPrice;

						// If there is no trade price, cannot compute slippage; keep planned price for future executions.
						if (execMsg.TradePrice == null)
							return null;

						// If there is no trade volume, cannot compute weighted slippage; keep planned price for future executions.
						if (execMsg.TradeVolume is not decimal trVol)
							return null;

						var diff = side == Sides.Buy
							? execMsg.TradePrice.Value - plannedPrice
							: plannedPrice - execMsg.TradePrice.Value;

						var weighted = diff * trVol;

						if (!CalculateNegative && weighted < 0)
							weighted = 0;

						_state.AddSlippage(weighted);

						// cleanup only when order is completed (if such info present)
						if (execMsg.HasOrderInfo() && (execMsg.OrderState == OrderStates.Done || execMsg.Balance == 0))
							_state.RemovePlannedPrice(execMsg.OriginalTransactionId);

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

	/// <summary>
	/// Creates a clone of this manager with new state.
	/// </summary>
	/// <returns>Cloned manager.</returns>
	public ISlippageManager Clone()
	{
		var clone = new SlippageManager(_state.GetType().CreateInstance<ISlippageManagerState>());
		clone.Load(this.Save());
		return clone;
	}

	object ICloneable.Clone() => Clone();
}
