namespace StockSharp.PrizmBit;

/// <summary>
/// <see cref="PrizmBit"/> order condition.
/// </summary>
[Serializable]
[DataContract]
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PrizmBitKey)]
public class PrizmBitOrderCondition : BaseWithdrawOrderCondition, ITakeProfitOrderCondition, IStopLossOrderCondition
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PrizmBitOrderCondition"/>.
	/// </summary>
	public PrizmBitOrderCondition()
	{
	}

	/// <inheritdoc />
	decimal? IStopLossOrderCondition.ClosePositionPrice
	{
		get => StopLossClosePositionPrice;
		set => StopLossClosePositionPrice = value;
	}

	decimal? IStopLossOrderCondition.ActivationPrice
	{
		get => StopLossActivationPrice;
		set => StopLossActivationPrice = value;
	}

	bool IStopLossOrderCondition.IsTrailing
	{
		get => IsStopLossTrailing;
		set => IsStopLossTrailing = value;
	}

	decimal? ITakeProfitOrderCondition.ActivationPrice
	{
		get => TakeProfitActivationPrice;
		set => TakeProfitActivationPrice = value;
	}

	/// <summary>
	/// Close position price. <see langword="null"/> means close by market.
	/// </summary>
	public decimal? StopLossClosePositionPrice
	{
		get => (decimal?)Parameters.TryGetValue(nameof(StopLossClosePositionPrice));
		set => Parameters[nameof(StopLossClosePositionPrice)] = value;
	}

	/// <summary>
	/// The absolute value of the price when the one is reached the protective strategy is activated.
	/// </summary>
	public decimal? StopLossActivationPrice
	{
		get => (decimal?)Parameters.TryGetValue(nameof(StopLossActivationPrice));
		set => Parameters[nameof(StopLossActivationPrice)] = value;
	}

	/// <summary>
	/// The absolute value of the price when the one is reached the protective strategy is activated.
	/// </summary>
	public decimal? TakeProfitActivationPrice
	{
		get => (decimal?)Parameters.TryGetValue(nameof(TakeProfitActivationPrice));
		set => Parameters[nameof(TakeProfitActivationPrice)] = value;
	}

	/// <summary>
	/// Trailing stop-loss.
	/// </summary>
	public bool IsStopLossTrailing
	{
		get => (bool)Parameters.TryGetValue(nameof(IsStopLossTrailing));
		set => Parameters[nameof(IsStopLossTrailing)] = value;
	}

	decimal? ITakeProfitOrderCondition.ClosePositionPrice
	{
		get => null;
		set { }
	}
}