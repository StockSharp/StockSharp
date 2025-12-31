namespace StockSharp.Algo.Testing;

/// <summary>
/// <see cref="IMarketEmulator"/> order condition.
/// </summary>
[Serializable]
[DataContract]
[Display(ResourceType = typeof(LocalizedStrings), Name = "Emulator")]
public class EmulationOrderCondition : OrderCondition, IStopLossOrderCondition, ITakeProfitOrderCondition
{
	/// <summary>
	/// Is take profit.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TakeProfitKey,
		Description = LocalizedStrings.TakeProfitKey,
		GroupName = LocalizedStrings.ParametersKey,
		Order = 0)]
	public bool IsTakeProfit
	{
		get => (bool?)Parameters.TryGetValue(nameof(IsTakeProfit)) == true;
		set => Parameters[nameof(IsTakeProfit)] = value;
	}

	/// <summary>
	/// Stop-price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StopPriceKey,
		Description = LocalizedStrings.StopPriceValueKey,
		GroupName = LocalizedStrings.ParametersKey,
		Order = 1)]
	public decimal? StopPrice
	{
		get => (decimal?)Parameters.TryGetValue(nameof(StopPrice));
		set => Parameters[nameof(StopPrice)] = value;
	}

	decimal? IStopLossOrderCondition.ClosePositionPrice
	{
		get => (decimal?)Parameters.TryGetValue(nameof(IStopLossOrderCondition.ClosePositionPrice));
		set => Parameters[nameof(IStopLossOrderCondition.ClosePositionPrice)] = value;
	}

	decimal? IStopLossOrderCondition.ActivationPrice
	{
		get => (decimal?)Parameters.TryGetValue(nameof(IStopLossOrderCondition.ActivationPrice));
		set => Parameters[nameof(IStopLossOrderCondition.ActivationPrice)] = value;
	}

	bool IStopLossOrderCondition.IsTrailing
	{
		get => (bool?)Parameters.TryGetValue(nameof(IStopLossOrderCondition.IsTrailing)) == true;
		set => Parameters[nameof(IStopLossOrderCondition.IsTrailing)] = value;
	}

	decimal? ITakeProfitOrderCondition.ClosePositionPrice
	{
		get => (decimal?)Parameters.TryGetValue(nameof(ITakeProfitOrderCondition.ClosePositionPrice));
		set => Parameters[nameof(ITakeProfitOrderCondition.ClosePositionPrice)] = value;
	}

	decimal? ITakeProfitOrderCondition.ActivationPrice
	{
		get => (decimal?)Parameters.TryGetValue(nameof(ITakeProfitOrderCondition.ActivationPrice));
		set => Parameters[nameof(ITakeProfitOrderCondition.ActivationPrice)] = value;
	}
}