namespace StockSharp.Algo.Indicators;

/// <summary>
/// Psychological Line.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PSYKey,
	Description = LocalizedStrings.PsychologicalLineKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/psychological_line.html")]
public class PsychologicalLine : LengthIndicator<decimal>
{
	private int _upCount;

	/// <summary>
	/// Initializes a new instance of the <see cref="PsychologicalLine"/>.
	/// </summary>
	public PsychologicalLine()
	{
		Length = 20;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		decimal tempUpCount;

		if (input.IsFinal)
		{
			if (Buffer.Count == Length)
			{
				if (Buffer[0] < Buffer[^1])
					_upCount--;
			}

			if (Buffer.Count > 0 && price > Buffer[^1])
				_upCount++;

			Buffer.PushBack(price);

			tempUpCount = _upCount;
		}
		else
		{
			tempUpCount = _upCount;

			if (Buffer.Count == Length)
			{
				if (price > Buffer[^1])
					tempUpCount++;
			}
			else if (Buffer.Count > 0 && price > Buffer[^1])
			{
				tempUpCount++;
			}
		}

		if (IsFormed)
		{
			var pl = tempUpCount / Length;
			return pl;
		}

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_upCount = 0;
		base.Reset();
	}
}