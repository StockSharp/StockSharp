namespace StockSharp.Algo.Statistics;

/// <summary>
/// The minimal value of order cancelling delay.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MinLatencyCancellationKey,
	Description = LocalizedStrings.MinLatencyCancellationDescKey,
	GroupName = LocalizedStrings.OrdersKey,
	Order = 303
)]
public class MinLatencyCancellationParameter : BaseOrderStatisticParameter<TimeSpan>
{
	/// <summary>
	/// Initialize <see cref="MinLatencyCancellationParameter"/>.
	/// </summary>
	public MinLatencyCancellationParameter()
		: base(StatisticParameterTypes.MinLatencyCancellation)
	{
	}

	/// <inheritdoc />
	public override void Changed(Order order)
	{
		if (order.LatencyCancellation is TimeSpan latency)
			Value = Value == default ? latency : Value.Min(latency);
	}
}
