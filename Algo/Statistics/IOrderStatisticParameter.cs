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