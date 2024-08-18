namespace StockSharp.Algo.Indicators;

/// <summary>
/// The interface of the container, storing indicator data.
/// </summary>
public interface IIndicatorContainer
{
	/// <summary>
	/// The current number of saved values.
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Add new values.
	/// </summary>
	/// <param name="input">The input value of the indicator.</param>
	/// <param name="result">The resulting value of the indicator.</param>
	void AddValue(IIndicatorValue input, IIndicatorValue result);

	/// <summary>
	/// To get all values of the identifier.
	/// </summary>
	/// <returns>All values of the identifier. The empty set, if there are no values.</returns>
	IEnumerable<(IIndicatorValue input, IIndicatorValue output)> GetValues();

	/// <summary>
	/// To get the indicator value by the index.
	/// </summary>
	/// <param name="index">The sequential number of value from the end.</param>
	/// <returns>Input and resulting values of the indicator.</returns>
	(IIndicatorValue input, IIndicatorValue output) GetValue(int index);

	/// <summary>
	/// To delete all values of the indicator.
	/// </summary>
	void ClearValues();
}