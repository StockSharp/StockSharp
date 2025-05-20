namespace StockSharp.Algo.Statistics;

/// <summary>
/// Total number of orders.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TotalOrdersKey,
	Description = LocalizedStrings.OrdersCountKey,
	GroupName = LocalizedStrings.OrdersKey,
	Order = 304
)]
public class OrderCountParameter : BaseOrderStatisticParameter<int>
{
	/// <summary>
	/// Initialize <see cref="OrderCountParameter"/>.
	/// </summary>
	public OrderCountParameter()
		: base(StatisticParameterTypes.OrderCount)
	{
	}

	/// <inheritdoc />
	public override void New(Order order) => Value++;
}
