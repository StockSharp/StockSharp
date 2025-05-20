namespace StockSharp.Algo.Statistics;

/// <summary>
/// Total number of insufficient fund error orders.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.IFEKey,
	Description = LocalizedStrings.InsufficientFundErrorKey,
	GroupName = LocalizedStrings.OrdersKey,
	Order = 306
)]
public class OrderInsufficientFundErrorCountParameter : BaseOrderStatisticParameter<int>
{
	/// <summary>
	/// Initialize <see cref="OrderInsufficientFundErrorCountParameter"/>.
	/// </summary>
	public OrderInsufficientFundErrorCountParameter()
		: base(StatisticParameterTypes.OrderInsufficientFundErrorCount)
	{
	}

	/// <inheritdoc />
	public override void RegisterFailed(OrderFail fail)
	{
		if (fail.Error is InsufficientFundException)
			Value++;
	}
}