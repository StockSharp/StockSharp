namespace StockSharp.Algo.Indicators;

/// <summary>
/// Sum of N last values.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/sum_n.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SumKey,
	Description = LocalizedStrings.SumNLastValuesKey)]
[Doc("topics/api/indicators/list_of_indicators/sum_n.html")]
public class Sum : DecimalLengthIndicator
{
	private decimal _sum;
	private decimal _correction;
	private int _nonZeroCount;

	/// <summary>
	/// Initializes a new instance of the <see cref="Sum"/>.
	/// </summary>
	public Sum()
	{
		Length = 15;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Volume;

	/// <inheritdoc />
	public override void Reset()
	{
		_sum = default;
		_correction = default;
		_nonZeroCount = default;

		base.Reset();
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal(Source);
		var sum = _sum;
		var correction = _correction;
		var nonZeroCount = _nonZeroCount;

		if (Buffer.Count == Length)
		{
			var removed = Buffer[0];

			if (ShouldAddFirst(sum, removed))
			{
				Add(newValue, ref sum, ref correction, ref nonZeroCount);
				Remove(removed, ref sum, ref correction, ref nonZeroCount);
			}
			else
			{
				Remove(removed, ref sum, ref correction, ref nonZeroCount);
				Add(newValue, ref sum, ref correction, ref nonZeroCount);
			}
		}
		else
			Add(newValue, ref sum, ref correction, ref nonZeroCount);

		if (input.IsFinal)
		{
			Buffer.PushBack(newValue);
			_sum = sum;
			_correction = correction;
			_nonZeroCount = nonZeroCount;
		}

		return sum + correction;
	}

	private static void Add(decimal value, ref decimal sum, ref decimal correction, ref int nonZeroCount)
	{
		AddCompensated(value, ref sum, ref correction);

		if (value != 0)
			nonZeroCount++;
	}

	private static void Remove(decimal value, ref decimal sum, ref decimal correction, ref int nonZeroCount)
	{
		SubtractCompensated(value, ref sum, ref correction);

		if (value != 0)
			nonZeroCount--;

		if (nonZeroCount == 0)
			sum = correction = 0;
	}

	private static void AddCompensated(decimal value, ref decimal sum, ref decimal correction)
	{
		var next = sum + value;

		correction += Math.Abs(sum) >= Math.Abs(value)
			? (sum - next) + value
			: (value - next) + sum;

		sum = next;
	}

	private static void SubtractCompensated(decimal value, ref decimal sum, ref decimal correction)
	{
		var next = sum - value;

		correction += Math.Abs(sum) >= Math.Abs(value)
			? (sum - next) - value
			: sum - (value + next);

		sum = next;
	}

	private static bool ShouldAddFirst(decimal sum, decimal removed)
		=> (sum > 0 && removed < 0) || (sum < 0 && removed > 0);
}
