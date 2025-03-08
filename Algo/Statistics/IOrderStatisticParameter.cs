namespace StockSharp.Algo.Statistics;

/// <summary>
/// The interface, describing statistic parameter, calculated based on orders.
/// </summary>
public interface IOrderStatisticParameter : IStatisticParameter
{
	/// <summary>
	/// To add to the parameter an information on new order.
	/// </summary>
	/// <param name="order">New order.</param>
	void New(Order order);

	/// <summary>
	/// To add to the parameter an information on changed order.
	/// </summary>
	/// <param name="order">The changed order.</param>
	void Changed(Order order);

	/// <summary>
	/// To add to the parameter an information on error of order registration.
	/// </summary>
	/// <param name="fail">Error registering order.</param>
	void RegisterFailed(OrderFail fail);

	/// <summary>
	/// To add to the parameter an information on error of order cancelling.
	/// </summary>
	/// <param name="fail">Error cancelling order.</param>
	void CancelFailed(OrderFail fail);
}

/// <summary>
/// The base statistic parameter, calculated based on orders.
/// </summary>
/// <typeparam name="TValue">The type of the parameter value.</typeparam>
/// <remarks>
/// Initialize <see cref="BaseOrderStatisticParameter{T}"/>.
/// </remarks>
/// <param name="type"><see cref="IStatisticParameter.Type"/></param>
public abstract class BaseOrderStatisticParameter<TValue>(StatisticParameterTypes type) : BaseStatisticParameter<TValue>(type), IOrderStatisticParameter
	where TValue : IComparable<TValue>
{
	/// <inheritdoc />
	public virtual void New(Order order)
	{
	}

	/// <inheritdoc />
	public virtual void Changed(Order order)
	{
	}

	/// <inheritdoc />
	public virtual void RegisterFailed(OrderFail fail)
	{
	}

	/// <inheritdoc />
	public virtual void CancelFailed(OrderFail fail)
	{
	}
}

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
		if (order.LatencyRegistration is not null)
			Value = Value.Max(order.LatencyRegistration.Value);
	}
}

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
		if (order.LatencyCancellation is not null)
			Value = Value.Max(order.LatencyCancellation.Value);
	}
}

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
		if (order.LatencyRegistration is not null)
			Value = Value.Min(order.LatencyRegistration.Value);
	}
}

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
		if (order.LatencyCancellation is not null)
			Value = Value.Min(order.LatencyCancellation.Value);
	}
}

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