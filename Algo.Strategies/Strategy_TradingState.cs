namespace StockSharp.Algo.Strategies;

using StockSharp.Localization;

partial class Strategy
{
	/// <summary>
	/// <see cref="IsFormed"/> and <see cref="IsOnline"/>.
	/// </summary>
	/// <returns>Check result.</returns>
	public bool IsFormedAndOnline() => IsFormed && IsOnline;

	/// <summary>
	/// <see cref="IsFormedAndOnline"/> and <see cref="TradingMode"/>.
	/// </summary>
	/// <param name="required">Required action.</param>
	/// <returns>Check result.</returns>
	public bool IsFormedAndOnlineAndAllowTrading(StrategyTradingModes required = StrategyTradingModes.Full)
	{
		if (!IsFormedAndOnline() || TradingMode == StrategyTradingModes.Disabled)
			return false;

		return required switch
		{
			StrategyTradingModes.Full => TradingMode == StrategyTradingModes.Full,
			StrategyTradingModes.CancelOrdersOnly => true,
			StrategyTradingModes.ReducePositionOnly => TradingMode != StrategyTradingModes.CancelOrdersOnly,
			_ => throw new ArgumentOutOfRangeException(nameof(required), required, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// The method is called when the strategy has entered the stopped state.
	/// </summary>
	protected virtual void OnStopped() { }

	/// <summary>
	/// The history period required before the strategy can start, or <see langword="null"/>.
	/// </summary>
	protected virtual TimeSpan? HistoryCalculated => null;

	/// <summary>
	/// Order book sources used by the strategy.
	/// </summary>
	public virtual IEnumerable<IOrderBookSource> OrderBookSources => [];
}
