namespace StockSharp.Algo.Statistics;

/// <summary>
/// Total number of order cancellation errors.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ErrorCancellingKey,
	Description = LocalizedStrings.ErrorCancellingKey,
	GroupName = LocalizedStrings.OrdersKey,
	Order = 307
)]
public class OrderCancelErrorCountParameter : BaseOrderStatisticParameter<int>
{
	/// <summary>
	/// Initialize <see cref="OrderCancelErrorCountParameter"/>.
	/// </summary>
	public OrderCancelErrorCountParameter()
		: base(StatisticParameterTypes.OrderCancelErrorCount)
	{
	}

	/// <inheritdoc />
	public override void CancelFailed(OrderFail fail) => Value++;
}
