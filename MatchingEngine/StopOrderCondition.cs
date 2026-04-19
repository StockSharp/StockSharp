namespace StockSharp.MatchingEngine;

/// <summary>
/// Stop order condition for matching engine.
/// </summary>
[System.Runtime.Serialization.DataContract]
[Serializable]
public class StopOrderCondition : OrderCondition, IStopLossOrderCondition, IPercentStopOrderCondition
{
	/// <summary>
	/// Stop activation price.
	/// </summary>
	public decimal? ActivationPrice
	{
		get => (decimal?)Parameters.TryGetValue(nameof(ActivationPrice));
		set => Parameters[nameof(ActivationPrice)] = value;
	}

	/// <summary>
	/// Close position price. <see langword="null"/> means close by market.
	/// </summary>
	public decimal? ClosePositionPrice
	{
		get => (decimal?)Parameters.TryGetValue(nameof(ClosePositionPrice));
		set => Parameters[nameof(ClosePositionPrice)] = value;
	}

	/// <summary>
	/// Trailing stop flag.
	/// </summary>
	public bool IsTrailing
	{
		get => Parameters.TryGetValue(nameof(IsTrailing)) is true;
		set => Parameters[nameof(IsTrailing)] = value;
	}

	/// <summary>
	/// Trailing stop offset from extremum.
	/// </summary>
	public decimal? TrailingOffset
	{
		get => (decimal?)Parameters.TryGetValue(nameof(TrailingOffset));
		set => Parameters[nameof(TrailingOffset)] = value;
	}

	/// <inheritdoc />
	public bool IsActivationPricePercent
	{
		get => Parameters.TryGetValue(nameof(IsActivationPricePercent)) is true;
		set => Parameters[nameof(IsActivationPricePercent)] = value;
	}

	/// <inheritdoc />
	public bool IsClosePositionPricePercent
	{
		get => Parameters.TryGetValue(nameof(IsClosePositionPricePercent)) is true;
		set => Parameters[nameof(IsClosePositionPricePercent)] = value;
	}

	/// <inheritdoc />
	public bool IsTrailingOffsetPercent
	{
		get => Parameters.TryGetValue(nameof(IsTrailingOffsetPercent)) is true;
		set => Parameters[nameof(IsTrailingOffsetPercent)] = value;
	}
}
