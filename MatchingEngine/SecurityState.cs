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
		}
		else
		{
			_incrementBuilder ??= new(SecurityId);

			if (_incrementBuilder.TryApply(msg) is QuoteChangeMessage full)
				OrderBook.SetSnapshot(full.Bids ?? [], full.Asks ?? []);
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
		=> _incrementBuilder = null;

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
