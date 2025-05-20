namespace StockSharp.Algo.Statistics;

/// <summary>
/// Total number of error orders.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ErrorsKey,
	Description = LocalizedStrings.ErrorOrdersOnlyKey,
	GroupName = LocalizedStrings.OrdersKey,
	Order = 305
)]
public class OrderErrorCountParameter : BaseOrderStatisticParameter<int>
{
	/// <summary>
	/// Initialize <see cref="OrderErrorCountParameter"/>.
	/// </summary>
	public OrderErrorCountParameter()
		: base(StatisticParameterTypes.OrderErrorCount)
	{
	}

	/// <inheritdoc />
	public override void RegisterFailed(OrderFail fail) => Value++;
}
