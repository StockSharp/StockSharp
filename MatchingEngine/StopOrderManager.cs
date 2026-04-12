namespace StockSharp.MatchingEngine;

/// <summary>
/// Stop order manager implementation.
/// </summary>
public class StopOrderManager : IStopOrderManager
{
	private readonly Dictionary<long, StopOrderInfo> _stopOrders = [];
	private readonly Dictionary<SecurityId, List<long>> _bySecurityId = [];

	/// <inheritdoc />
	public void Register(StopOrderInfo info)
	{
		if (info is null)
			throw new ArgumentNullException(nameof(info));

		_stopOrders[info.TransactionId] = info;

		if (!_bySecurityId.TryGetValue(info.SecurityId, out var list))
			_bySecurityId[info.SecurityId] = list = [];

		list.Add(info.TransactionId);
	}

	/// <inheritdoc />
	public bool Cancel(long transactionId, out StopOrderInfo info)
	{
		if (!_stopOrders.TryGetValue(transactionId, out info))
			return false;

		_stopOrders.Remove(transactionId);

		if (_bySecurityId.TryGetValue(info.SecurityId, out var list))
		{
			list.Remove(transactionId);

			if (list.Count == 0)
				_bySecurityId.Remove(info.SecurityId);
		}

		return true;
	}

	/// <inheritdoc />
	public bool Replace(long origTransactionId, StopOrderInfo newInfo)
	{
		if (!Cancel(origTransactionId, out _))
			return false;

		Register(newInfo);
		return true;
	}

	/// <inheritdoc />
	public IReadOnlyList<StopOrderTrigger> CheckPrice(SecurityId securityId, decimal price, DateTime time)
	{
		if (!_bySecurityId.TryGetValue(securityId, out var list) || list.Count == 0)
			return [];

		List<StopOrderTrigger> triggered = null;

		for (var i = list.Count - 1; i >= 0; i--)
		{
			var transId = list[i];

			if (!_stopOrders.TryGetValue(transId, out var info))
			{
				list.RemoveAt(i);
				continue;
			}

			if (info.IsTrailing)
				UpdateTrailing(info, price);

			if (!IsTriggered(info, price))
				continue;

			triggered ??= [];
			triggered.Add(new(info, price, CreateResultingOrder(info, time)));

			_stopOrders.Remove(transId);
			list.RemoveAt(i);
		}

		if (list.Count == 0)
			_bySecurityId.Remove(securityId);

		return triggered ?? (IReadOnlyList<StopOrderTrigger>)[];
	}

	/// <inheritdoc />
	public void Clear()
	{
		_stopOrders.Clear();
		_bySecurityId.Clear();
	}

	private static void UpdateTrailing(StopOrderInfo info, decimal price)
	{
		if (info.Side == Sides.Sell)
		{
			// Trailing sell: track max price
			if (info.BestSeenPrice is null || price > info.BestSeenPrice)
			{
				info.BestSeenPrice = price;
				info.StopPrice = price - (info.TrailingOffset ?? 0);
			}
		}
		else
		{
			// Trailing buy: track min price
			if (info.BestSeenPrice is null || price < info.BestSeenPrice)
			{
				info.BestSeenPrice = price;
				info.StopPrice = price + (info.TrailingOffset ?? 0);
			}
		}
	}

	private static bool IsTriggered(StopOrderInfo info, decimal price)
	{
		if (info.InvertTrigger)
		{
			// TakeProfit: sell triggers when price rises, buy triggers when price falls
			return info.Side == Sides.Sell
				? price >= info.StopPrice
				: price <= info.StopPrice;
		}

		// StopLoss: buy stop triggers when price rises, sell stop triggers when price falls
		return info.Side == Sides.Buy
			? price >= info.StopPrice
			: price <= info.StopPrice;
	}

	private static OrderRegisterMessage CreateResultingOrder(StopOrderInfo info, DateTime time)
	{
		return new()
		{
			SecurityId = info.SecurityId,
			Side = info.Side,
			Volume = info.Volume,
			PortfolioName = info.PortfolioName,
			OrderType = info.LimitPrice is not null ? OrderTypes.Limit : OrderTypes.Market,
			Price = info.LimitPrice ?? 0,
			LocalTime = time,
		};
	}
}
