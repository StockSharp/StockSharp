namespace StockSharp.Algo.Slippage;

/// <summary>
/// Default implementation of <see cref="ISlippageManagerState"/>.
/// </summary>
public class SlippageManagerState : ISlippageManagerState
{
	private readonly Lock _sync = new();
	private readonly Dictionary<SecurityId, (DateTime time, decimal bid, decimal ask)> _bestPrices = [];
	private readonly Dictionary<long, (Sides side, decimal price)> _plannedPrices = [];

	private decimal _slippage;

	/// <inheritdoc />
	public decimal Slippage
	{
		get
		{
			using (_sync.EnterScope())
				return _slippage;
		}
	}

	/// <inheritdoc />
	public void AddSlippage(decimal amount)
	{
		using (_sync.EnterScope())
			_slippage += amount;
	}

	/// <inheritdoc />
	public void UpdateBestPrices(SecurityId securityId, decimal? bidPrice, decimal? askPrice, DateTime time)
	{
		if (bidPrice is null && askPrice is null)
			return;

		using (_sync.EnterScope())
		{
			if (_bestPrices.TryGetValue(securityId, out var current) && time < current.time)
				return;

			_bestPrices[securityId] = (
				time,
				bidPrice ?? current.bid,
				askPrice ?? current.ask
			);
		}
	}

	/// <inheritdoc />
	public bool TryGetBestPrice(SecurityId securityId, Sides side, out decimal price)
	{
		using (_sync.EnterScope())
		{
			if (_bestPrices.TryGetValue(securityId, out var prices))
			{
				price = side == Sides.Buy ? prices.ask : prices.bid;
				return price != 0;
			}

			price = 0;
			return false;
		}
	}

	/// <inheritdoc />
	public void AddPlannedPrice(long transactionId, Sides side, decimal price)
	{
		using (_sync.EnterScope())
			_plannedPrices[transactionId] = (side, price);
	}

	/// <inheritdoc />
	public bool TryGetPlannedPrice(long transactionId, out Sides side, out decimal price)
	{
		using (_sync.EnterScope())
		{
			if (_plannedPrices.TryGetValue(transactionId, out var t))
			{
				side = t.side;
				price = t.price;
				return true;
			}

			side = default;
			price = 0;
			return false;
		}
	}

	/// <inheritdoc />
	public void RemovePlannedPrice(long transactionId)
	{
		using (_sync.EnterScope())
			_plannedPrices.Remove(transactionId);
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_sync.EnterScope())
		{
			_slippage = 0;
			_bestPrices.Clear();
			_plannedPrices.Clear();
		}
	}
}
