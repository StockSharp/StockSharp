namespace StockSharp.MatchingEngine;

/// <summary>
/// Per-security state for matching engine.
/// Contains order book, order manager, and security definition.
/// </summary>
public class SecurityState(SecurityId securityId)
{
	private SecurityMessage _securityDefinition;

	// What the venue never stated, taken from the first thing it quoted. Kept here rather than
	// written into its own definition of the instrument, which belongs to whoever sent it.
	private decimal? _guessedPriceStep;
	private decimal? _guessedVolumeStep;
	private long? _depthSubscription;

	// Built on the first incremental frame. A venue that publishes whole books never needs one.
	private OrderBookIncrementBuilder _incrementBuilder;

	// The builder has handed over a whole book, so the increments after it are folded straight into the engine's.
	private bool _isBookStated;

	// The best quotes the market had when it closed: what an open position is valued at until the market quotes again.
	private decimal? _closingBid;
	private decimal? _closingAsk;

	/// <summary>
	/// Security identifier.
	/// </summary>
	public SecurityId SecurityId { get; } = securityId;

	/// <summary>
	/// Order book for this security.
	/// </summary>
	public OrderBook OrderBook { get; } = new(securityId);

	/// <summary>
	/// Active order manager.
	/// </summary>
	public OrderLifecycleManager OrderManager { get; } = new();

	/// <summary>
	/// Price step from security definition or auto-detected.
	/// </summary>
	public decimal PriceStep => _securityDefinition?.PriceStep ?? _guessedPriceStep ?? 0.01m;

	/// <summary>
	/// Volume step from security definition or auto-detected.
	/// </summary>
	public decimal VolumeStep => _securityDefinition?.VolumeStep ?? _guessedVolumeStep ?? 1m;

	/// <summary>
	/// Whether depth subscription is active.
	/// </summary>
	public bool HasDepthSubscription => _depthSubscription.HasValue;

	/// <summary>
	/// Trading state as the venue last stated it, or <see langword="null"/> when it never has.
	/// </summary>
	public SecurityStates? TradingState { get; private set; }

	/// <summary>
	/// The price the instrument last traded at, or <see langword="null"/> when the venue has stated
	/// none. This is the price a stop is measured against, so it is also what a distance stated as a
	/// percent of the market is resolved from.
	/// </summary>
	public decimal? LastTradePrice { get; private set; }

	/// <summary>
	/// Whether the market of this security is closed: its book holds only the orders resting in it, and an open position is
	/// valued at the quotes the market closed with.
	/// </summary>
	public bool IsMarketClosed { get; private set; }

	/// <summary>
	/// The definition the venue stated for this security, or <see langword="null"/> when it has
	/// stated none. What the engine matches against is derived from it; whoever has to answer what
	/// the venue lists reads it here rather than keeping a second copy.
	/// </summary>
	public SecurityMessage Definition => _securityDefinition;

	/// <summary>
	/// Process security definition message.
	/// </summary>
	public void ProcessSecurity(SecurityMessage msg)
	{
		_securityDefinition = msg;
	}

	/// <summary>
	/// Record the trading state the venue reports for this security.
	/// </summary>
	/// <param name="state">Trading state.</param>
	public void ProcessTradingState(SecurityStates state)
	{
		TradingState = state;
	}

	/// <summary>
	/// Record the price the venue reports the instrument traded at.
	/// </summary>
	/// <param name="price">Traded price.</param>
	public void ProcessTradePrice(decimal price)
	{
		LastTradePrice = price;
	}

	/// <summary>
	/// Process quote change (snapshot) — update order book.
	/// </summary>
	public void ProcessQuoteChange(QuoteChangeMessage msg, List<Message> results)
	{
		if ((msg.GetBestBid() ?? msg.GetBestAsk()) is { } quote)
			UpdateSteps(quote.Price, quote.Volume);

		// A venue either states the whole book in every frame, or states it once and sends what
		// changed. The engine holds the book, so folding the second kind is its own work: dropping
		// those frames would leave it matching against a market that never moves.
		if (msg.State is null)
		{
			OrderBook.SetSnapshot(msg.Bids ?? [], msg.Asks ?? []);
			ReopenMarket();
		}
		else if (msg.State == QuoteChangeStates.Increment && !msg.HasPositions && _isBookStated)
		{
			// Only the levels an increment names change. Rebuilding the whole book for each one costs as
			// much as the book is deep, and a venue that sends increments keeps thousands of levels.
			foreach (var bid in msg.Bids ?? [])
				OrderBook.UpdateLevel(Sides.Buy, bid.Price, bid.Volume);

			foreach (var ask in msg.Asks ?? [])
				OrderBook.UpdateLevel(Sides.Sell, ask.Price, ask.Volume);
		}
		else
		{
			_incrementBuilder ??= new(SecurityId);

			// A snapshot stated in parts is no book until its last part, and an increment folded into
			// one that is not whole yet would be lost when it is.
			var full = _incrementBuilder.TryApply(msg);
			_isBookStated = full is not null;

			if (full is not null)
			{
				OrderBook.SetSnapshot(full.Bids ?? [], full.Asks ?? []);
				ReopenMarket();
			}
		}

		if (_depthSubscription.HasValue)
		{
			results.Add(OrderBook.ToMessage(msg.LocalTime, msg.ServerTime));
		}
	}

	/// <summary>
	/// Forgets the book stated so far, so an incremental feed has to state a whole one again before
	/// its increments mean anything. What the engine currently holds is left standing.
	/// </summary>
	public void ForgetBook()
	{
		_incrementBuilder = null;
		_isBookStated = false;
	}

	/// <summary>
	/// Close the market of this security: what the market offered leaves the book and the orders resting in it stay, while
	/// open positions keep the value of the best quotes it had until it quotes again.
	/// </summary>
	public void CloseMarket()
	{
		// A market closed again before it quoted keeps the quotes it first closed with: its book holds no market by then.
		if (!IsMarketClosed)
		{
			_closingBid = OrderBook.BestBid?.price;
			_closingAsk = OrderBook.BestAsk?.price;
			IsMarketClosed = true;
		}

		OrderBook.SetSnapshot([], []);
		ForgetBook();
	}

	/// <summary>
	/// The market of this security quotes again: its book values open positions from now on.
	/// </summary>
	public void ReopenMarket()
	{
		if (!IsMarketClosed)
			return;

		IsMarketClosed = false;
		_closingBid = null;
		_closingAsk = null;
	}

	/// <summary>
	/// The price a position in this security can be closed at by an order of <paramref name="closeSide"/>: the best quote on
	/// the side that order takes from, or the one the market closed with while it is closed.
	/// </summary>
	/// <param name="closeSide">Side of the order that would close the position.</param>
	/// <returns>The price, or <see langword="null"/> when the market has not named one.</returns>
	public decimal? TryGetClosePrice(Sides closeSide)
	{
		if (IsMarketClosed)
			return closeSide == Sides.Sell ? _closingBid : _closingAsk;

		return closeSide == Sides.Sell ? OrderBook.BestBid?.price : OrderBook.BestAsk?.price;
	}

	/// <summary>
	/// Process market data subscriptions (depth only).
	/// </summary>
	public void ProcessMarketData(MarketDataMessage msg)
	{
		if (msg.IsSubscribe)
		{
			if (msg.DataType2 == DataType.MarketDepth)
				_depthSubscription = msg.TransactionId;
		}
		else
		{
			if (_depthSubscription == msg.OriginalTransactionId)
				_depthSubscription = null;
		}
	}

	/// <summary>
	/// Takes the steps from what the market quotes, for an instrument whose venue never stated them.
	/// </summary>
	/// <param name="price">Quoted price.</param>
	/// <param name="volume">Quoted volume.</param>
	public void UpdateSteps(decimal price, decimal? volume)
	{
		if (_guessedPriceStep is null && price > 0)
			_guessedPriceStep = price.GetDecimalInfo().EffectiveScale.GetPriceStep();

		if (_guessedVolumeStep is null && volume > 0)
			_guessedVolumeStep = volume.Value.GetDecimalInfo().EffectiveScale.GetPriceStep();
	}
}
