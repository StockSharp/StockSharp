namespace StockSharp.Algo.Strategies.Protective;

/// <summary>
/// Protective controller for the specified position.
/// </summary>
public interface IProtectivePositionController
{
	/// <summary>
	/// <see cref="SecurityId"/>
	/// </summary>
	SecurityId SecurityId { get; }

	/// <summary>
	/// Portfolio name.
	/// </summary>
    string PortfolioName { get; }

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
	/// Try activate local stop orders.
	/// </summary>
	/// <param name="price">Current price.</param>
	/// <param name="time">Current time.</param>
	/// <returns>Registration order info.</returns>
	(bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition)?
		TryActivate(decimal price, DateTimeOffset time);
}