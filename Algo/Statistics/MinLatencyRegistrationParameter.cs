namespace StockSharp.Algo.Statistics;

/// <summary>
/// The minimal value of order registration delay.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MinLatencyRegistrationKey,
	Description = LocalizedStrings.MinLatencyRegistrationDescKey,
	GroupName = LocalizedStrings.OrdersKey,
	Order = 302
)]
public class MinLatencyRegistrationParameter : BaseOrderStatisticParameter<TimeSpan>
{
	/// <summary>
	/// Initialize <see cref="MinLatencyRegistrationParameter"/>.
	/// </summary>
	public MinLatencyRegistrationParameter()
		: base(StatisticParameterTypes.MinLatencyRegistration)
	{
	}

	/// <inheritdoc />
	public override void New(Order order)
	{
		if (order.LatencyRegistration is TimeSpan latency)
			Value = Value == default ? latency : Value.Min(latency);
	}
}
