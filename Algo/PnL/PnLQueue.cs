namespace StockSharp.Algo.PnL;

/// <summary>
/// The queue of profit calculation by messages stream.
/// </summary>
public class PnLQueue
{
	private Sides _openedPosSide;
	private readonly SynchronizedStack<RefPair<decimal, decimal>> _openedTrades = [];
	private decimal _multiplier;

	private decimal? _lastPrice;
	private decimal? _bidPrice;
	private decimal? _askPrice;

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

			if (_priceStep == value)
				return;

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

			if (_stepPrice == value)
				return;

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

			if (value == _leverage)
				return;

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

			if (value == _lotMultiplier)
				return;

			_lotMultiplier = value;
			UpdateMultiplier();
		}
	}

	private decimal? _unrealizedPnL;

	/// <summary>
	/// Unrealized profit.
	/// </summary>
	public decimal UnrealizedPnL
	{
		get
		{
			if (_unrealizedPnL is decimal unrealPnL)
				return unrealPnL;

			var price = (_openedPosSide == Sides.Buy ? _bidPrice : _askPrice) ?? _lastPrice;

			var sum = price == null
				? 0
				: _openedTrades.SyncGet(c => c.Sum(t => GetPnL(t.First, t.Second, _openedPosSide, price.Value)));

			unrealPnL = sum * _multiplier;
			_unrealizedPnL = unrealPnL;
			return unrealPnL;
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
		if (trade is null)
			throw new ArgumentNullException(nameof(trade));

		var closedVolume = 0m;
		var pnl = 0m;
		var volume = trade.SafeGetVolume();
		var price = trade.GetTradePrice();

		_unrealizedPnL = default;

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
	/// To update the information on the instrument.
	/// </summary>
	/// <param name="levelMsg"><see cref="Level1ChangeMessage"/></param>
	public void UpdateSecurity(Level1ChangeMessage levelMsg)
	{
		if (levelMsg is null)
			throw new ArgumentNullException(nameof(levelMsg));

		if (levelMsg.TryGetDecimal(Level1Fields.PriceStep) is decimal priceStep)
		{
			PriceStep = priceStep;
		}

		if (levelMsg.TryGetDecimal(Level1Fields.StepPrice) is decimal stepPrice)
		{
			StepPrice = stepPrice;
		}

		if (levelMsg.TryGetDecimal(Level1Fields.Multiplier) is decimal lotMultiplier)
		{
			LotMultiplier = lotMultiplier;
		}
	}

	/// <summary>
	/// To process the message, containing market data.
	/// </summary>
	/// <param name="levelMsg">The message, containing market data.</param>
	public void ProcessLevel1(Level1ChangeMessage levelMsg)
	{
		if (levelMsg is null)
			throw new ArgumentNullException(nameof(levelMsg));

		if (levelMsg.TryGetDecimal(Level1Fields.LastTradePrice) is decimal lastPrice
			&& _lastPrice != lastPrice)
		{
			_lastPrice = lastPrice;
			_bidPrice = default;
			_askPrice = default;
			_unrealizedPnL = default;
		}

		if (levelMsg.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bidPrice
			&& _bidPrice != bidPrice)
		{
			_bidPrice = bidPrice;
			_lastPrice = default;
			_unrealizedPnL = default;
		}

		if (levelMsg.TryGetDecimal(Level1Fields.BestAskPrice) is decimal askPrice
			&& _askPrice != askPrice)
		{
			_askPrice = askPrice;
			_lastPrice = default;
			_unrealizedPnL = default;
		}
	}

	/// <summary>
	/// To process <see cref="CandleMessage"/> message.
	/// </summary>
	/// <param name="candleMsg"><see cref="CandleMessage"/>.</param>
	public void ProcessCandle(CandleMessage candleMsg)
	{
		if (candleMsg is null)
			throw new ArgumentNullException(nameof(candleMsg));

		_lastPrice = candleMsg.ClosePrice;

		_unrealizedPnL = default;
	}

	/// <summary>
	/// To process the message, containing information on tick trade.
	/// </summary>
	/// <param name="execMsg">The message, containing information on tick trade.</param>
	public void ProcessExecution(ExecutionMessage execMsg)
	{
		if (execMsg is null)
			throw new ArgumentNullException(nameof(execMsg));

		if (execMsg.TradePrice == null)
			return;

		_lastPrice = execMsg.TradePrice.Value;

		_unrealizedPnL = default;
	}

	/// <summary>
	/// To process the message, containing data on order book.
	/// </summary>
	/// <param name="quoteMsg">The message, containing data on order book.</param>
	public void ProcessQuotes(QuoteChangeMessage quoteMsg)
	{
		if (quoteMsg is null)
			throw new ArgumentNullException(nameof(quoteMsg));

		_askPrice = quoteMsg.GetBestAsk()?.Price;
		_bidPrice = quoteMsg.GetBestBid()?.Price;

		_unrealizedPnL = default;
	}

	private void UpdateMultiplier()
	{
		var stepPrice = StepPrice;

		_multiplier = (stepPrice == null ? 1 : stepPrice.Value / PriceStep) * Leverage * LotMultiplier;
		_unrealizedPnL = default;
	}

	private static decimal GetPnL(decimal price, decimal volume, Sides side, decimal marketPrice)
	{
		return (price - marketPrice) * volume * (side == Sides.Sell ? 1 : -1);
	}
}