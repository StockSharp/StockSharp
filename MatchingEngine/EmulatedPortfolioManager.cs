namespace StockSharp.MatchingEngine;

/// <summary>
/// Emulated portfolio implementation that tracks positions and money in-memory.
/// </summary>
public class EmulatedPortfolio
{
	private readonly Dictionary<SecurityId, PositionInfo> _positions = [];
	private decimal _beginMoney;
	private decimal _realizedPnL;
	private decimal _totalBlockedMoney;
	private decimal _commission;
	private readonly IMarkPrices _markPrices;

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	/// <param name="name">Portfolio name.</param>
	/// <param name="markPrices">Where the prices to revalue open positions come from.</param>
	public EmulatedPortfolio(string name, IMarkPrices markPrices)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		_markPrices = markPrices ?? throw new ArgumentNullException(nameof(markPrices));
	}

	/// <summary>
	/// Portfolio name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Initial money amount.
	/// </summary>
	public decimal BeginMoney => _beginMoney;

	/// <summary>
	/// Current money (begin + PnL).
	/// </summary>
	public decimal CurrentMoney => _beginMoney + TotalPnL;

	/// <summary>
	/// Available money (current - blocked).
	/// </summary>
	public decimal AvailableMoney => CurrentMoney - _totalBlockedMoney;

	/// <summary>
	/// Total realized PnL.
	/// </summary>
	public decimal RealizedPnL => _realizedPnL;

	/// <summary>
	/// Total PnL (realized - commission).
	/// </summary>
	public decimal TotalPnL => _realizedPnL - _commission + UnrealizedPnL;

	/// <summary>
	/// What the open positions have gained or lost since they were taken, at the prices they could be
	/// closed at now. A position the market has not priced counts for nothing.
	/// </summary>
	public decimal UnrealizedPnL
	{
		get
		{
			var total = 0m;

			foreach (var (securityId, pos) in _positions)
			{
				var volume = pos.CurrentValue;

				if (volume == 0)
					continue;

				// A long is closed by selling into the bid, a short by buying from the ask.
				var price = _markPrices.TryGetClosePrice(securityId, volume > 0 ? Sides.Sell : Sides.Buy);

				if (price is null)
					continue;

				total += (price.Value - pos.AveragePrice) * volume;
			}

			return total;
		}
	}

	/// <summary>
	/// Blocked money for pending orders.
	/// </summary>
	public decimal BlockedMoney => _totalBlockedMoney;

	/// <summary>
	/// Total commission paid.
	/// </summary>
	public decimal Commission => _commission;

	/// <summary>
	/// Margin call level threshold. When margin level falls to this value, a warning is triggered.
	/// </summary>
	public decimal MarginCallLevel { get; set; } = 0.5m;

	/// <summary>
	/// Stop-out level threshold. When margin level falls to this value, positions are liquidated.
	/// </summary>
	public decimal StopOutLevel { get; set; } = 0.2m;

	/// <summary>
	/// Enable automatic position liquidation on stop-out.
	/// </summary>
	public bool EnableStopOut { get; set; }

	/// <summary>
	/// Set initial money.
	/// </summary>
	/// <param name="money">Money amount.</param>
	public void SetMoney(decimal money)
	{
		_beginMoney = money;
	}

	/// <summary>
	/// Set initial position.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="volume">Position volume.</param>
	/// <param name="avgPrice">Average entry price.</param>
	public void SetPosition(SecurityId securityId, decimal volume, decimal avgPrice = 0)
	{
		var pos = GetOrCreatePosition(securityId);
		pos.BeginValue = volume;
		pos.Diff = 0;
		pos.AveragePrice = avgPrice;
	}

	private PositionInfo GetOrCreatePosition(SecurityId securityId)
	{
		if (!_positions.TryGetValue(securityId, out var pos))
		{
			pos = new PositionInfo(securityId);
			_positions[securityId] = pos;
		}
		return pos;
	}

	/// <summary>
	/// Get position for security.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <returns>Position info or null.</returns>
	public PositionInfo GetPosition(SecurityId securityId)
	{
		return _positions.TryGetValue(securityId, out var pos) ? pos : null;
	}

	/// <summary>
	/// Process a trade execution.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="side">Trade side.</param>
	/// <param name="price">Trade price.</param>
	/// <param name="volume">Trade volume.</param>
	/// <param name="commission">Commission amount.</param>
	/// <returns>Trade processing result.</returns>
	public TradeProcessingResult ProcessTrade(SecurityId securityId, Sides side, decimal price, decimal volume, decimal? commission = null)
	{
		var pos = GetOrCreatePosition(securityId);

		// Update commission
		if (commission.HasValue)
			_commission += commission.Value;

		// Calculate position change
		var positionDelta = side == Sides.Buy ? volume : -volume;
		var prevPos = pos.CurrentValue;
		var prevAvgPrice = pos.AveragePrice;

		pos.Diff += positionDelta;

		var currPos = pos.CurrentValue;
		var tradeRealizedPnL = 0m;

		// Calculate AveragePrice and RealizedPnL
		if (currPos == 0)
		{
			// Position closed completely
			if (prevPos != 0)
			{
				// Realized PnL = (exit price - entry price) * volume * direction
				tradeRealizedPnL = (price - prevAvgPrice) * prevPos.Abs() * Math.Sign(prevPos);
				_realizedPnL += tradeRealizedPnL;
			}
			pos.AveragePrice = 0;
		}
		else if (prevPos == 0)
		{
			// New position opened
			pos.AveragePrice = price;
		}
		else if (Math.Sign(prevPos) == Math.Sign(currPos))
		{
			// Position increased or partially closed
			if (currPos.Abs() > prevPos.Abs())
			{
				// Position increased - recalculate average price
				pos.AveragePrice = (prevAvgPrice * prevPos.Abs() + price * volume) / currPos.Abs();
			}
			else
			{
				// Position partially closed - realize PnL for closed portion
				var closedVolume = prevPos.Abs() - currPos.Abs();
				tradeRealizedPnL = (price - prevAvgPrice) * closedVolume * Math.Sign(prevPos);
				_realizedPnL += tradeRealizedPnL;
				// Average price remains the same for remaining position
			}
		}
		else
		{
			// Position flipped (was long, now short or vice versa)
			// First close old position completely
			tradeRealizedPnL = (price - prevAvgPrice) * prevPos.Abs() * Math.Sign(prevPos);
			_realizedPnL += tradeRealizedPnL;
			// Then open new position at current price
			pos.AveragePrice = price;
		}

		// Update blocked volume/value for active orders (order was executed)
		// Use the average blocked price, not the trade price, to properly unblock
		if (side == Sides.Buy)
		{
			var avgBlockedPrice = pos.TotalBidsVolume > 0 ? pos.TotalBidsValue / pos.TotalBidsVolume : price;
			var blockedValue = volume * avgBlockedPrice;
			pos.TotalBidsVolume -= volume;
			pos.TotalBidsValue -= blockedValue;
		}
		else
		{
			var avgBlockedPrice = pos.TotalAsksVolume > 0 ? pos.TotalAsksValue / pos.TotalAsksVolume : price;
			var blockedValue = volume * avgBlockedPrice;
			pos.TotalAsksVolume -= volume;
			pos.TotalAsksValue -= blockedValue;
		}

		UpdateBlockedMoney();

		return new TradeProcessingResult(tradeRealizedPnL, positionDelta, pos);
	}

	/// <summary>
	/// Process order registration (block funds).
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="side">Order side.</param>
	/// <param name="volume">Order volume.</param>
	/// <param name="price">Order price for margin calculation.</param>
	public void ProcessOrderRegistration(SecurityId securityId, Sides side, decimal volume, decimal price)
	{
		var pos = GetOrCreatePosition(securityId);
		var value = volume * price;

		if (side == Sides.Buy)
		{
			pos.TotalBidsVolume += volume;
			pos.TotalBidsValue += value;
		}
		else
		{
			pos.TotalAsksVolume += volume;
			pos.TotalAsksValue += value;
		}

		UpdateBlockedMoney();
	}

	/// <summary>
	/// Process order cancellation (unblock funds).
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="side">Order side.</param>
	/// <param name="volume">Cancelled volume.</param>
	/// <param name="price">Price used for margin calculation.</param>
	public void ProcessOrderCancellation(SecurityId securityId, Sides side, decimal volume, decimal price = 0)
	{
		var pos = GetOrCreatePosition(securityId);
		var value = volume * price;

		if (side == Sides.Buy)
		{
			pos.TotalBidsVolume -= volume;
			pos.TotalBidsValue -= value;
		}
		else
		{
			pos.TotalAsksVolume -= volume;
			pos.TotalAsksValue -= value;
		}

		UpdateBlockedMoney();
	}

	private void UpdateBlockedMoney()
	{
		_totalBlockedMoney = 0;
		foreach (var pos in _positions.Values)
		{
			// TotalPrice logic:
			// - If no position: blocked = buys + sells
			// - If long position: blocked = max(position + buys, sells)
			// - If short position: blocked = max(position + sells, buys)
			var positionValue = pos.CurrentValue.Abs() * pos.AveragePrice;
			var buyOrderValue = pos.TotalBidsValue;
			var sellOrderValue = pos.TotalAsksValue;

			decimal blocked;
			if (positionValue == 0)
			{
				blocked = buyOrderValue + sellOrderValue;
			}
			else if (pos.CurrentValue > 0)
			{
				// Long position: max(position + buys, sells)
				blocked = (positionValue + buyOrderValue).Max(sellOrderValue);
			}
			else
			{
				// Short position: max(position + sells, buys)
				blocked = (positionValue + sellOrderValue).Max(buyOrderValue);
			}

			_totalBlockedMoney += blocked;
		}
	}

	/// <summary>
	/// Get all positions.
	/// </summary>
	/// <returns>Enumeration of positions.</returns>
	public IEnumerable<(SecurityId securityId, decimal volume, decimal avgPrice)> GetPositions()
	{
		return _positions.Select(kvp => (kvp.Key, kvp.Value.CurrentValue, kvp.Value.AveragePrice));
	}

	/// <summary>
	/// Get all position info objects.
	/// </summary>
	public IEnumerable<PositionInfo> GetAllPositions() => _positions.Values;

	/// <summary>
	/// Calculate unrealized PnL across all positions.
	/// </summary>
	/// <param name="getCurrentPrice">Function to get current market price for a security. Returns null if price unavailable.</param>
	/// <returns>Total unrealized PnL.</returns>
	public decimal CalculateUnrealizedPnL(Func<SecurityId, decimal?> getCurrentPrice)
	{
		if (getCurrentPrice is null)
			throw new ArgumentNullException(nameof(getCurrentPrice));

		var total = 0m;

		foreach (var pos in _positions.Values)
		{
			if (pos.CurrentValue == 0)
				continue;

			var price = getCurrentPrice(pos.SecurityId);
			if (price is null)
				continue;

			total += (price.Value - pos.AveragePrice) * pos.CurrentValue;
		}

		return total;
	}

	/// <summary>
	/// Clear all state (used by Reset).
	/// </summary>
	internal void Clear()
	{
		_positions.Clear();
		_beginMoney = 0;
		_realizedPnL = 0;
		_totalBlockedMoney = 0;
		_commission = 0;
	}
}

/// <summary>
/// Portfolio manager that creates emulated portfolios in-memory.
/// </summary>
public class EmulatedPortfolioManager : IMarkPrices
{
	// One account, one balance: the name is compared the way the rest of the engine compares it,
	// or an order spelling it differently opens a second, empty portfolio next to the funded one.
	private readonly Dictionary<string, EmulatedPortfolio> _portfolios = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Margin controller for order validation.
	/// </summary>
	public IMarginController MarginController { get; set; }

	/// <summary>
	/// Where the prices to revalue open positions come from. Until the engine supplies them every
	/// position is worth what it cost.
	/// </summary>
	public IMarkPrices MarkPrices { get; set; }

	decimal? IMarkPrices.TryGetClosePrice(SecurityId securityId, Sides closeSide)
		=> MarkPrices?.TryGetClosePrice(securityId, closeSide);

	/// <summary>
	/// Get or create a portfolio by name.
	/// </summary>
	/// <param name="name">Portfolio name.</param>
	/// <returns>Portfolio instance.</returns>
	public EmulatedPortfolio GetPortfolio(string name)
	{
		if (!_portfolios.TryGetValue(name, out var portfolio))
		{
			portfolio = new EmulatedPortfolio(name, this);
			_portfolios[name] = portfolio;
		}
		return portfolio;
	}

	/// <summary>
	/// Look an account up without opening one, unlike <see cref="GetPortfolio"/>, which creates the
	/// account when the name is unknown.
	/// </summary>
	/// <param name="name">Portfolio name.</param>
	/// <param name="portfolio">The account, or <see langword="null"/> when no account carries that name.</param>
	/// <returns><see langword="true"/> when the account exists.</returns>
	public virtual bool TryGetPortfolio(string name, out EmulatedPortfolio portfolio)
		=> _portfolios.TryGetValue(name, out portfolio);

	/// <summary>
	/// Check if portfolio exists.
	/// </summary>
	/// <param name="name">Portfolio name.</param>
	/// <returns>True if exists.</returns>
	public bool HasPortfolio(string name)
	{
		return _portfolios.ContainsKey(name);
	}

	/// <summary>
	/// Get all portfolios.
	/// </summary>
	public IEnumerable<EmulatedPortfolio> GetAllPortfolios()
	{
		return _portfolios.Values;
	}

	/// <summary>
	/// Validate that portfolio has sufficient funds for order registration.
	/// </summary>
	/// <param name="portfolioName">Portfolio name.</param>
	/// <param name="securityId">Security ID (for per-position leverage).</param>
	/// <param name="price">Order price.</param>
	/// <param name="volume">Order volume.</param>
	/// <returns>Error if insufficient funds, null otherwise.</returns>
	public InvalidOperationException ValidateFunds(string portfolioName, SecurityId securityId, decimal price, decimal volume)
	{
		if (!HasPortfolio(portfolioName))
			return null;

		var portfolio = GetPortfolio(portfolioName);
		var position = portfolio.GetPosition(securityId);

		if (MarginController is not null)
			return MarginController.ValidateOrder(portfolio, price, volume, position);

		var needMoney = price * volume;

		if (portfolio.AvailableMoney < needMoney)
			return new InvalidOperationException($"Insufficient funds: need {needMoney}, available {portfolio.AvailableMoney}");

		return null;
	}

	/// <summary>
	/// Clear all portfolio state.
	/// </summary>
	public void Clear()
	{
		_portfolios.Clear();
	}
}
