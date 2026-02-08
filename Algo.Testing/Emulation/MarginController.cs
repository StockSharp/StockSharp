namespace StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Default implementation of <see cref="IMarginController"/>.
/// Leverage is taken from <see cref="PositionInfo.Leverage"/> (default 1).
/// Margin call/stop-out levels are taken from <see cref="IPortfolio"/>.
/// </summary>
public class MarginController : IMarginController
{
	/// <inheritdoc />
	public decimal GetRequiredMargin(decimal price, decimal volume, PositionInfo position)
	{
		var leverage = position?.Leverage ?? 1m;
		if (leverage < 1)
			leverage = 1;

		return price * volume / leverage;
	}

	/// <inheritdoc />
	public InvalidOperationException ValidateOrder(IPortfolio portfolio, decimal price, decimal volume, PositionInfo position)
	{
		if (portfolio is null)
			throw new ArgumentNullException(nameof(portfolio));

		var needMoney = GetRequiredMargin(price, volume, position);

		if (portfolio.AvailableMoney < needMoney)
			return new InvalidOperationException($"Insufficient funds: need {needMoney}, available {portfolio.AvailableMoney}");

		return null;
	}

	/// <inheritdoc />
	public decimal CheckMarginLevel(IPortfolio portfolio, decimal unrealizedPnL)
	{
		if (portfolio is null)
			throw new ArgumentNullException(nameof(portfolio));

		if (portfolio.BlockedMoney <= 0)
			return decimal.MaxValue;

		var equity = portfolio.CurrentMoney + unrealizedPnL;
		return equity / portfolio.BlockedMoney;
	}

	/// <inheritdoc />
	public bool IsMarginCall(IPortfolio portfolio, decimal unrealizedPnL)
		=> CheckMarginLevel(portfolio, unrealizedPnL) <= portfolio.MarginCallLevel;

	/// <inheritdoc />
	public bool IsStopOut(IPortfolio portfolio, decimal unrealizedPnL)
		=> portfolio.EnableStopOut && CheckMarginLevel(portfolio, unrealizedPnL) <= portfolio.StopOutLevel;
}
