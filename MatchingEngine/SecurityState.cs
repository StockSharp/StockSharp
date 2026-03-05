namespace StockSharp.MatchingEngine;

/// <summary>
/// Per-security state for matching engine.
/// Contains order book, order manager, and security definition.
/// </summary>
public class SecurityState(SecurityId securityId)
{
	private SecurityMessage _securityDefinition;

	private bool _priceStepUpdated;
	private bool _volumeStepUpdated;
	private long? _depthSubscription;

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
	public decimal PriceStep => _securityDefinition?.PriceStep ?? 0.01m;

	/// <summary>
	/// Volume step from security definition or auto-detected.
	/// </summary>
	public decimal VolumeStep => _securityDefinition?.VolumeStep ?? 1m;

	/// <summary>
	/// Whether depth subscription is active.
	/// </summary>
	public bool HasDepthSubscription => _depthSubscription.HasValue;

	/// <summary>
	/// Process security definition message.
	/// </summary>
	public void ProcessSecurity(SecurityMessage msg)
	{
		_securityDefinition = msg;
	}

	/// <summary>
	/// Process quote change (snapshot) — update order book.
	/// </summary>
	public void ProcessQuoteChange(QuoteChangeMessage msg, List<Message> results)
	{
		if (!_priceStepUpdated || !_volumeStepUpdated)
		{
			var quote = msg.GetBestBid() ?? msg.GetBestAsk();
			if (quote != null)
				UpdateSteps(quote.Value.Price, quote.Value.Volume);
		}

		if (msg.State is null)
		{
			OrderBook.SetSnapshot(msg.Bids, msg.Asks);
		}

		if (_depthSubscription.HasValue)
		{
			results.Add(OrderBook.ToMessage(msg.LocalTime, msg.ServerTime));
		}
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
	/// Auto-detect price/volume step from market data.
	/// </summary>
	public void UpdateSteps(decimal price, decimal? volume)
	{
		if (!_priceStepUpdated && price > 0)
		{
			_securityDefinition ??= new SecurityMessage { SecurityId = SecurityId };
			_securityDefinition.PriceStep = price.GetDecimalInfo().EffectiveScale.GetPriceStep();
			_priceStepUpdated = true;
		}

		if (!_volumeStepUpdated && volume > 0)
		{
			_securityDefinition ??= new SecurityMessage { SecurityId = SecurityId };
			_securityDefinition.VolumeStep = volume.Value.GetDecimalInfo().EffectiveScale.GetPriceStep();
			_volumeStepUpdated = true;
		}
	}
}
