namespace StockSharp.Algo.Statistics;

/// <summary>
/// Total number of registration error orders.
/// </summary>
[Display(
    ResourceType = typeof(LocalizedStrings),
    Name = LocalizedStrings.ErrorsKey,
    Description = LocalizedStrings.ErrorOrdersOnlyKey,
    GroupName = LocalizedStrings.OrdersKey,
    Order = 305
)]
public class OrderRegisterErrorCountParameter : BaseOrderStatisticParameter<int>
{
    /// <summary>
    /// Initialize <see cref="OrderRegisterErrorCountParameter"/>.
    /// </summary>
    public OrderRegisterErrorCountParameter()
        : base(StatisticParameterTypes.OrderErrorCount)
    {
    }

    /// <inheritdoc />
    public override void RegisterFailed(OrderFail fail) => Value++;
}
