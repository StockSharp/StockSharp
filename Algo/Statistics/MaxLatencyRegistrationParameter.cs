namespace StockSharp.Algo.Statistics;

/// <summary>
/// The maximal value of the order registration delay.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxLatencyRegistrationKey,
	Description = LocalizedStrings.MaxLatencyRegistrationDescKey,
	GroupName = LocalizedStrings.OrdersKey,
	Order = 300
)]
public class MaxLatencyRegistrationParameter : BaseOrderStatisticParameter<TimeSpan>
{
	/// <summary>
	/// Initialize <see cref="MaxLatencyRegistrationParameter"/>.
	/// </summary>
	public MaxLatencyRegistrationParameter()
		: base(StatisticParameterTypes.MaxLatencyRegistration)
	{
	}

	/// <inheritdoc />
	public override void New(Order order)
	{
		if (order.LatencyRegistration is TimeSpan latency)
			Value = Value == default ? latency : Value.Max(latency);
	}
}