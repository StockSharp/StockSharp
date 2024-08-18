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

	decimal? IStopLossOrderCondition.ClosePositionPrice { get; set; }
	decimal? IStopLossOrderCondition.ActivationPrice { get; set; }
	bool IStopLossOrderCondition.IsTrailing { get; set; }

	decimal? ITakeProfitOrderCondition.ClosePositionPrice { get; set; }
	decimal? ITakeProfitOrderCondition.ActivationPrice { get; set; }
}