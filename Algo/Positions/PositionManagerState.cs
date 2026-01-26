namespace StockSharp.Algo.Positions;

/// <summary>
/// Default implementation of <see cref="IPositionManagerState"/>.
/// </summary>
public class PositionManagerState : IPositionManagerState
{
	private class OrderInfo(SecurityId securityId, string portfolioName, Sides side, decimal volume, decimal balance)
	{
		public SecurityId SecurityId { get; } = securityId;
		public string PortfolioName { get; } = portfolioName;
		public Sides Side { get; } = side;
		public decimal Volume { get; } = volume;
		public decimal Balance { get; set; } = balance;
	}

	private readonly Lock _sync = new();
	private readonly Dictionary<long, OrderInfo> _orders = [];
	private readonly Dictionary<(SecurityId, string), decimal> _positions = [];

	/// <inheritdoc />
	public decimal AddOrGetOrder(long transactionId, SecurityId securityId, string portfolioName, Sides side, decimal volume, decimal balance)
	{
		using (_sync.EnterScope())
		{
			if (_orders.TryGetValue(transactionId, out var existing))
				return existing.Balance;

			_orders[transactionId] = new OrderInfo(securityId, portfolioName, side, volume, balance);
			return balance;
		}
	}

	/// <inheritdoc />
	public bool TryGetOrder(long transactionId, out SecurityId securityId, out string portfolioName, out Sides side, out decimal balance)
	{
		using (_sync.EnterScope())
		{
			if (_orders.TryGetValue(transactionId, out var info))
			{
				securityId = info.SecurityId;
				portfolioName = info.PortfolioName;
				side = info.Side;
				balance = info.Balance;
				return true;
			}

			securityId = default;
			portfolioName = null;
			side = default;
			balance = 0;
			return false;
		}
	}

	/// <inheritdoc />
	public void UpdateOrderBalance(long transactionId, decimal newBalance)
	{
		using (_sync.EnterScope())
		{
			if (_orders.TryGetValue(transactionId, out var info))
				info.Balance = newBalance;
		}
	}

	/// <inheritdoc />
	public void RemoveOrder(long transactionId)
	{
		using (_sync.EnterScope())
			_orders.Remove(transactionId);
	}

	/// <inheritdoc />
	public decimal UpdatePosition(SecurityId securityId, string portfolioName, decimal diff)
	{
		var key = (securityId, portfolioName.ToLowerInvariant());

		using (_sync.EnterScope())
		{
			if (!_positions.TryGetValue(key, out var current))
				current = 0;

			var newValue = current + diff;
			_positions[key] = newValue;
			return newValue;
		}
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_sync.EnterScope())
		{
			_orders.Clear();
			_positions.Clear();
		}
	}
}
