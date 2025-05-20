namespace StockSharp.Algo.Statistics;

/// <summary>
/// The maximal value of the order cancelling delay.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxLatencyCancellationKey,
	Description = LocalizedStrings.MaxLatencyCancellationDescKey,
	GroupName = LocalizedStrings.OrdersKey,
	Order = 301
)]
public class MaxLatencyCancellationParameter : BaseOrderStatisticParameter<TimeSpan>
{
	/// <summary>
	/// Initialize <see cref="MaxLatencyCancellationParameter"/>.
	/// </summary>
	public MaxLatencyCancellationParameter()
		: base(StatisticParameterTypes.MaxLatencyCancellation)
	{
	}

	/// <inheritdoc />
	public override void Changed(Order order)
	{
		if (order.LatencyCancellation is TimeSpan latency)
			Value = Value == default ? latency : Value.Max(latency);
	}
}
