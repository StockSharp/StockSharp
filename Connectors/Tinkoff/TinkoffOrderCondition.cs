namespace StockSharp.Tinkoff;

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

/// <summary>
/// <see cref="Tinkoff"/> order condition.
/// </summary>
[Serializable]
[DataContract]
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TinkoffKey)]
public class TinkoffOrderCondition : OrderCondition, IStopLossOrderCondition, ITakeProfitOrderCondition
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TinkoffOrderCondition"/>.
	/// </summary>
	public TinkoffOrderCondition()
	{
	}

	/// <summary>
	/// Trigger price.
	/// </summary>
	[DataMember]
	public decimal? TriggerPrice
	{
		get => (decimal?)Parameters.TryGetValue(nameof(TriggerPrice));
		set => Parameters[nameof(TriggerPrice)] = value;
	}

	/// <summary>
	/// Close position price.
	/// </summary>
	[DataMember]
	public decimal? Price
	{
		get => (decimal?)Parameters.TryGetValue(nameof(Price));
		set => Parameters[nameof(Price)] = value;
	}

	decimal? IStopLossOrderCondition.ClosePositionPrice { get => Price; set => Price = value; }
	decimal? ITakeProfitOrderCondition.ClosePositionPrice { get => Price; set => Price = value; }

	decimal? IStopLossOrderCondition.ActivationPrice { get => TriggerPrice; set => TriggerPrice = value; }
	decimal? ITakeProfitOrderCondition.ActivationPrice { get => TriggerPrice; set => TriggerPrice = value; }

	/// <inheritdoc />
	[DataMember]
	public bool IsTrailing
	{
		get => (bool?)Parameters.TryGetValue(nameof(IsTrailing)) ?? false;
		set => Parameters[nameof(IsTrailing)] = value;
	}
}