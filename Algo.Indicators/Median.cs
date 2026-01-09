namespace StockSharp.Algo.Indicators;

/// <summary>
/// Moving Median.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/median.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MedianKey,
	Description = LocalizedStrings.MovingMedianKey)]
[Doc("topics/api/indicators/list_of_indicators/median.html")]
public class Median : DecimalLengthIndicator
{
	private readonly Queue<decimal> _window = [];
	private readonly List<decimal> _sorted = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="Median"/>.
	/// </summary>
	public Median()
	{
		Length = 5;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_window.Clear();
		_sorted.Clear();
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _window.Count == Length;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var val = input.ToDecimal(Source);

		if (input.IsFinal)
		{
			// If window full remove the oldest element
			if (_window.Count == Length)
				RemoveValue(_window.Dequeue(), _sorted);

			_window.Enqueue(val);
			InsertValue(val, _sorted);
			return new DecimalIndicatorValue(this, CalcMedian(_sorted), input.Time);
		}

		// Preview (non-final): virtually replace oldest (if full) or add new value
		if (_window.Count == 0)
			return new DecimalIndicatorValue(this, input.Time);

		List<decimal> temp = [.. _sorted];

		if (_window.Count == Length)
		{
			// Copy, remove oldest, insert new value
			RemoveValue(_window.Peek(), temp); // remove exactly one instance of oldest
			InsertValue(val, temp);
		}
		else
		{
			InsertValue(val, temp);
		}

		return new DecimalIndicatorValue(this, CalcMedian(temp), input.Time);
	}

	private static void InsertValue(decimal v, List<decimal> list)
	{
		var idx = list.BinarySearch(v);

		if (idx < 0)
			idx = ~idx;

		list.Insert(idx, v);
	}

	private static void RemoveValue(decimal v, List<decimal> list)
	{
		var idx = list.BinarySearch(v);

		if (idx >= 0)
			list.RemoveAt(idx);
	}

	private static decimal CalcMedian(List<decimal> data)
	{
		var n = data.Count;

		if (n == 0)
			return 0m;

		return (n & 1) == 1
			? data[n / 2]
			: (data[n / 2 - 1] + data[n / 2]) / 2m;
	}
}