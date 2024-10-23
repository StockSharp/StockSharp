namespace StockSharp.Algo.Indicators;

/// <summary>
/// The container, storing indicators data.
/// </summary>
public class IndicatorContainer : IIndicatorContainer
{
	private readonly CircularBuffer<(IIndicatorValue, IIndicatorValue)> _values = new(100);

	/// <inheritdoc />
	public int Count => _values.Count;

	/// <inheritdoc />
	public virtual void AddValue(IIndicatorValue input, IIndicatorValue result)
		=> _values.PushFront((input, result));

	/// <inheritdoc />
	public virtual IEnumerable<(IIndicatorValue, IIndicatorValue)> GetValues()
		=> [.. _values];

	/// <inheritdoc />
	public virtual (IIndicatorValue, IIndicatorValue) GetValue(int index)
		=> _values[index];

	/// <inheritdoc />
	public virtual void ClearValues()
		=> _values.Clear();
}