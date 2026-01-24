namespace StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Emulated portfolio implementation that tracks positions and money in-memory.
/// </summary>
public class EmulatedPortfolio : IPortfolio
{
	private readonly Dictionary<SecurityId, PositionInfo> _positions = [];
	private decimal _beginMoney;
	private decimal _realizedPnL;
	private decimal _totalBlockedMoney;
	private decimal _commission;

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	/// <param name="name">Portfolio name.</param>
	public EmulatedPortfolio(string name)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
	}

	/// <inheritdoc />
	public string Name { get; }

	/// <inheritdoc />
	public decimal BeginMoney => _beginMoney;

	/// <inheritdoc />
	public decimal CurrentMoney => _beginMoney + TotalPnL;

	/// <inheritdoc />
	public decimal AvailableMoney => CurrentMoney - _totalBlockedMoney;

	/// <inheritdoc />
	public decimal RealizedPnL => _realizedPnL;

	/// <inheritdoc />
	public decimal TotalPnL => _realizedPnL - _commission;

	/// <inheritdoc />
	public decimal BlockedMoney => _totalBlockedMoney;

	/// <inheritdoc />
	public decimal Commission => _commission;

	/// <inheritdoc />
	public void SetMoney(decimal money)
	{
		_beginMoney = money;
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	public PositionInfo GetPosition(SecurityId securityId)
	{
		return _positions.TryGetValue(securityId, out var pos) ? pos : null;
	}

	/// <inheritdoc />
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
				tradeRealizedPnL = (price - prevAvgPrice) * Math.Abs(prevPos) * Math.Sign(prevPos);
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
			if (Math.Abs(currPos) > Math.Abs(prevPos))
			{
				// Position increased - recalculate average price
				pos.AveragePrice = (prevAvgPrice * Math.Abs(prevPos) + price * volume) / Math.Abs(currPos);
			}
			else
			{
				// Position partially closed - realize PnL for closed portion
				var closedVolume = Math.Abs(prevPos) - Math.Abs(currPos);
				tradeRealizedPnL = (price - prevAvgPrice) * closedVolume * Math.Sign(prevPos);
				_realizedPnL += tradeRealizedPnL;
				// Average price remains the same for remaining position
			}
		}
		else
		{
			// Position flipped (was long, now short or vice versa)
			// First close old position completely
			tradeRealizedPnL = (price - prevAvgPrice) * Math.Abs(prevPos) * Math.Sign(prevPos);
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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
			var positionValue = Math.Abs(pos.CurrentValue) * pos.AveragePrice;
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
				blocked = Math.Max(positionValue + buyOrderValue, sellOrderValue);
			}
			else
			{
				// Short position: max(position + sells, buys)
				blocked = Math.Max(positionValue + sellOrderValue, buyOrderValue);
			}

			_totalBlockedMoney += blocked;
		}
	}

	/// <inheritdoc />
	public IEnumerable<(SecurityId securityId, decimal volume, decimal avgPrice)> GetPositions()
	{
		return _positions.Select(kvp => (kvp.Key, kvp.Value.CurrentValue, kvp.Value.AveragePrice));
	}

	/// <inheritdoc />
	public IEnumerable<PositionInfo> GetAllPositions() => _positions.Values;

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
public class EmulatedPortfolioManager : IPortfolioManager
{
	private readonly Dictionary<string, EmulatedPortfolio> _portfolios = [];

	/// <inheritdoc />
	public IPortfolio GetPortfolio(string name)
	{
		if (!_portfolios.TryGetValue(name, out var portfolio))
		{
			portfolio = new EmulatedPortfolio(name);
			_portfolios[name] = portfolio;
		}
		return portfolio;
	}

	/// <inheritdoc />
	public bool HasPortfolio(string name)
	{
		return _portfolios.ContainsKey(name);
	}

	/// <inheritdoc />
	public IEnumerable<IPortfolio> GetAllPortfolios()
	{
		return _portfolios.Values;
	}

	/// <inheritdoc />
	public void Clear()
	{
		_portfolios.Clear();
	}
}
