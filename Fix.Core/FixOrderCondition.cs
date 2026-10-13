namespace StockSharp.Fix;

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

/// <summary>
/// The stop orders types that are specific for <see cref="Fix"/>.
/// </summary>
[Serializable]
[DataContract]
public enum FixStopOrderTypes
{
	/// <summary>
	/// Stop-loss.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StopLossKey)]
	[EnumMember]
	StopLoss,

	/// <summary>
	/// Trailing stop-loss.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TrailingStopLossKey)]
	[EnumMember]
	StopLossTrailing,

	/// <summary>
	/// Take.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TakeProfitKey)]
	[EnumMember]
	TakeProfit,

	/// <summary>
	/// Trailing take-profit.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TrailingTakeProfitKey)]
	[EnumMember]
	TakeProfitTrailing,

	/// <summary>
	/// Market on close (pit).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarketOnPitCloseKey)]
	[EnumMember]
	MarketOnClose,

	/// <summary>
	/// With the market price when the condition is fulfilled.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarketOnTouchKey)]
	[EnumMember]
	MarketIfTouched,
}

/// <summary>
/// <see cref="Fix"/> order condition.
/// </summary>
[Serializable]
[DataContract]
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FIXKey)]
public class FixOrderCondition : OrderCondition, IStopLossOrderCondition, ITakeProfitOrderCondition
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FixOrderCondition"/>.
	/// </summary>
	public FixOrderCondition()
	{
		Type = FixStopOrderTypes.StopLoss;
	}

	/// <summary>
	/// Stop-order type.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TypeKey,
		Description = LocalizedStrings.StopOrderTypeDescKey,
		GroupName = LocalizedStrings.ParametersKey,
		Order = 0)]
	public FixStopOrderTypes? Type
	{
		get => (FixStopOrderTypes?)Parameters.TryGetValue(nameof(Type));
		set => Parameters[nameof(Type)] = value;
	}

	/// <summary>
	/// Stop-loss.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StopLossKey,
		Description = LocalizedStrings.StopLossActivationPriceKey,
		GroupName = LocalizedStrings.ParametersKey,
		Order = 1)]
	public decimal? StopLoss
	{
		get => (decimal?)Parameters.TryGetValue(nameof(StopLoss));
		set => Parameters[nameof(StopLoss)] = value;
	}

	/// <summary>
	/// Take-profit.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TakeProfitKey,
		Description = LocalizedStrings.TakeProfitDescKey,
		GroupName = LocalizedStrings.ParametersKey,
		Order = 1)]
	public decimal? TakeProfit
	{
		get => (decimal?)Parameters.TryGetValue(nameof(TakeProfit));
		set => Parameters[nameof(TakeProfit)] = value;
	}

	/// <summary>
	/// Offset.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceOffsetKey,
		Description = LocalizedStrings.PriceOffsetForOrderKey,
		GroupName = LocalizedStrings.ParametersKey,
		Order = 2)]
	public decimal? Offset
	{
		get => (decimal?)Parameters.TryGetValue(nameof(Offset));
		set => Parameters[nameof(Offset)] = value;
	}

	decimal? IStopLossOrderCondition.ClosePositionPrice
	{
		get => null;
		set { }
	}

	decimal? ITakeProfitOrderCondition.ActivationPrice
	{
		get => TakeProfit;
		set => TakeProfit = value;
	}

	decimal? ITakeProfitOrderCondition.ClosePositionPrice
	{
		get => null;
		set { }
	}

	decimal? IStopLossOrderCondition.ActivationPrice
	{
		get => StopLoss;
		set => StopLoss = value;
	}

	bool IStopLossOrderCondition.IsTrailing
	{
		get => Type == FixStopOrderTypes.StopLossTrailing;
		set => Type = value ? FixStopOrderTypes.StopLossTrailing : FixStopOrderTypes.StopLoss;
	}
}