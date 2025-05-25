namespace StockSharp.Algo.Strategies.Protective;

/// <summary>
/// Position protection behaviour.
/// </summary>
public interface IProtectiveBehaviour : ILogSource
{
	/// <summary>
	/// Current position value.
	/// </summary>
	decimal Position { get; }

	/// <summary>
	/// Update position difference.
	/// </summary>
	/// <param name="price">Position difference price.</param>
	/// <param name="value">Position difference value.</param>
	/// <param name="time">Current time.</param>
	/// <returns>Registration order info.</returns>
	(bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition)?
		Update(decimal price, decimal value, DateTimeOffset time);

	/// <summary>
	/// Try activate protection.
	/// </summary>
	/// <param name="price">Current price.</param>
	/// <param name="time">Current time.</param>
	/// <returns>Registration order info.</returns>
	(bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition)?
		TryActivate(decimal price, DateTimeOffset time);
}

/// <summary>
/// Base implementation of <see cref="IProtectiveBehaviour"/>.
/// </summary>
public abstract class BaseProtectiveBehaviour : BaseLogReceiver, IProtectiveBehaviour
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BaseProtectiveBehaviour"/>.
	/// </summary>
	/// <param name="takeValue">Take offset.</param>
	/// <param name="stopValue">Stop offset.</param>
	/// <param name="isStopTrailing">Whether to use a trailing technique.</param>
	/// <param name="takeTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="stopTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="useMarketOrders">Whether to use market orders.</param>
	protected BaseProtectiveBehaviour(
		Unit takeValue, Unit stopValue,
		bool isStopTrailing,
		TimeSpan takeTimeout, TimeSpan stopTimeout,
		bool useMarketOrders)
    {
		if (takeTimeout < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(takeTimeout), takeTimeout, LocalizedStrings.InvalidValue);

		if (stopTimeout < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(stopTimeout), stopTimeout, LocalizedStrings.InvalidValue);

		TakeValue = takeValue ?? throw new ArgumentNullException(nameof(takeValue));
		StopValue = stopValue ?? throw new ArgumentNullException(nameof(stopValue));
		IsStopTrailing = isStopTrailing;
		TakeTimeout = takeTimeout;
		StopTimeout = stopTimeout;
		UseMarketOrders = useMarketOrders;
	}

	/// <summary>
	/// Take offset.
	/// </summary>
	protected Unit TakeValue { get; }

	/// <summary>
	/// Stop offset.
	/// </summary>
	protected Unit StopValue { get; }

	/// <summary>
	/// Whether to use a trailing technique.
	/// </summary>
	protected bool IsStopTrailing { get; }

	/// <summary>
	/// Time limit. If protection has not worked by this time, the position will be closed on the market.
	/// </summary>
	protected TimeSpan TakeTimeout { get; }

	/// <summary>
	/// Time limit. If protection has not worked by this time, the position will be closed on the market.
	/// </summary>
	protected TimeSpan StopTimeout { get; }

	/// <summary>
	/// Whether to use market orders.
	/// </summary>
	protected bool UseMarketOrders { get; }

	/// <inheritdoc />
	public abstract decimal Position { get; }

	/// <inheritdoc />
	public abstract (bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition)? Update(decimal price, decimal value, DateTimeOffset time);

	/// <inheritdoc />
	public abstract (bool, Sides, decimal, decimal, OrderCondition)? TryActivate(decimal price, DateTimeOffset time);
}