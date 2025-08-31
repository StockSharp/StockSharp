namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// Pure functional engine that receives input data and returns action recommendations.
/// </summary>
public class QuotingEngine
{
	private readonly Security _security;
	private readonly Portfolio _portfolio;
	private readonly Sides _quotingSide;
	private readonly decimal _quotingVolume;
	private readonly decimal _maxOrderVolume;
	private readonly IQuotingBehavior _behavior;
	private readonly TimeSpan _timeOut;
	private readonly IMarketDataProvider _mdProvider;
	private readonly DateTimeOffset _startTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="QuotingEngine"/> class.
	/// </summary>
	/// <param name="behavior">The behavior defining the quoting logic.</param>
	/// <param name="security">Security to quote.</param>
	/// <param name="portfolio">Portfolio for orders.</param>
	/// <param name="quotingSide">The direction of quoting (Buy or Sell).</param>
	/// <param name="quotingVolume">The total volume to be quoted.</param>
	/// <param name="maxOrderVolume">Maximum volume of a single order.</param>
	/// <param name="timeOut">The time limit for quoting completion.</param>
	/// <param name="mdProvider">Market data provider.</param>
	/// <param name="startTime">Start time for timeout calculation.</param>
	public QuotingEngine(
		IQuotingBehavior behavior,
		Security security,
		Portfolio portfolio,
		Sides quotingSide,
		decimal quotingVolume,
		decimal maxOrderVolume,
		TimeSpan timeOut,
		IMarketDataProvider mdProvider,
		DateTimeOffset startTime)
	{
		if (quotingVolume <= 0)
			throw new ArgumentOutOfRangeException(nameof(quotingVolume), quotingVolume, "Invalid value");

		if (maxOrderVolume <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxOrderVolume), maxOrderVolume, "Invalid value");

		_security = security ?? throw new ArgumentNullException(nameof(security));
		_portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
		_quotingSide = quotingSide;
		_quotingVolume = quotingVolume;
		_maxOrderVolume = maxOrderVolume;
		_behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
		_timeOut = timeOut;
		_mdProvider = mdProvider ?? throw new ArgumentNullException(nameof(mdProvider));
		_startTime = startTime;
	}

	/// <summary>
	/// Calculate remaining volume to quote.
	/// </summary>
	public decimal GetLeftVolume(decimal position)
	{
		var sign = _quotingSide == Sides.Buy ? 1 : -1;
		return Math.Max(0, _quotingVolume - position * sign);
	}

	/// <summary>
	/// Check if timeout has occurred.
	/// </summary>
	public bool IsTimeOut(DateTimeOffset currentTime) =>
		_timeOut != TimeSpan.Zero && (currentTime - _startTime) >= _timeOut;

	/// <summary>
	/// Process input data and return recommended action.
	/// </summary>
	/// <param name="input">Input market data and state.</param>
	/// <returns>Recommended action.</returns>
	public QuotingAction ProcessQuoting(QuotingInput input)
	{
		// Check timeout
		if (IsTimeOut(input.CurrentTime))
		{
			return QuotingAction.Finish(false, "Timeout reached");
		}

		// Check if target volume is reached
		var leftVolume = GetLeftVolume(input.Position);
		if (leftVolume <= 0)
		{
			return QuotingAction.Finish(true, "Target volume reached");
		}

		// If order is pending, wait
		if (input.CurrentOrder?.IsPending == true)
		{
			return QuotingAction.None("Order is pending");
		}

		// Calculate desired volume for new order
		var newVolume = Math.Min(_maxOrderVolume, leftVolume);

		// Calculate best price using behavior
		var bestPrice = _behavior.CalculateBestPrice(
			_security, _mdProvider, _quotingSide,
			input.BestBidPrice, input.BestAskPrice, input.LastTradePrice,
			input.Bids, input.Asks);

		if (bestPrice == null)
		{
			return QuotingAction.None("No market data available");
		}

		// Check if quoting is needed using behavior
		var currentPrice = input.CurrentOrder?.Price;
		var currentVolume = input.CurrentOrder?.Volume ?? 0;

		var qp = _behavior.NeedQuoting(
			_security, _mdProvider, input.CurrentTime,
			currentPrice, currentVolume, newVolume, bestPrice);

		if (qp is not decimal quotingPrice)
		{
			return QuotingAction.None("No quoting needed");
		}

		// Determine action based on current order state
		if (input.CurrentOrder == null)
		{
			// No current order - register new one
			if (!input.IsTradingAllowed)
			{
				return QuotingAction.None("Trading not allowed");
			}

			var orderType = quotingPrice == 0 ? OrderTypes.Market : OrderTypes.Limit;
			return QuotingAction.Register(quotingPrice, newVolume, orderType,
				$"Registering {_quotingSide} order for {newVolume} at {quotingPrice.ToString() ?? "market"}");
		}
		else
		{
			// Current order exists - check if modification needed
			var needsPriceChange = currentPrice != quotingPrice;
			var needsVolumeChange = currentVolume != newVolume;

			if (needsPriceChange || needsVolumeChange)
			{
				if (!input.IsCancellationAllowed)
				{
					return QuotingAction.None("Cancellation not allowed");
				}

				// For simplicity, always cancel first, then register new
				// In real implementation, you might want to use modify if supported
				return QuotingAction.Cancel($"Price/volume change needed: {currentPrice}->{quotingPrice}, {currentVolume}->{newVolume}");
			}

			return QuotingAction.None("Current order is optimal");
		}
	}

	/// <summary>
	/// Process the result of an order registration.
	/// </summary>
	/// <param name="isSuccess">Whether registration was successful.</param>
	/// <param name="input">Current input state.</param>
	/// <returns>Next recommended action.</returns>
	public QuotingAction ProcessOrderResult(bool isSuccess, QuotingInput input)
	{
		if (!isSuccess)
		{
			// Order registration failed, try again if possible
			return ProcessQuoting(input);
		}

		// Order registered successfully, continue normal processing
		return ProcessQuoting(input);
	}

	/// <summary>
	/// Process the result of an order cancellation.
	/// </summary>
	/// <param name="isSuccess">Whether cancellation was successful.</param>
	/// <param name="input">Current input state.</param>
	/// <returns>Next recommended action.</returns>
	public QuotingAction ProcessCancellationResult(bool isSuccess, QuotingInput input)
	{
		// After cancellation (successful or not), reprocess to potentially register new order
		return ProcessQuoting(input);
	}

	/// <summary>
	/// Process a trade execution.
	/// </summary>
	/// <param name="tradeVolume">Volume of the executed trade.</param>
	/// <param name="input">Current input state.</param>
	/// <returns>Next recommended action.</returns>
	public QuotingAction ProcessTrade(decimal tradeVolume, QuotingInput input)
	{
		// After trade, check if we need to continue quoting
		return ProcessQuoting(input);
	}
}
