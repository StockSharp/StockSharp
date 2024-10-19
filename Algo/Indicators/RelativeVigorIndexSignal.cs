namespace StockSharp.Algo.Indicators;

/// <summary>
/// The signaling part of indicator <see cref="RelativeVigorIndex"/>.
/// </summary>
[IndicatorHidden]
public class RelativeVigorIndexSignal : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RelativeVigorIndexSignal"/>.
	/// </summary>
	public RelativeVigorIndexSignal()
	{
		Length = 4;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(newValue);
		}

		if (IsFormed)
		{
			return input.IsFinal
				? new DecimalIndicatorValue(this, (Buffer[0] + 2 * Buffer[1] + 2 * Buffer[2] + Buffer[3]) / 6m, input.Time)
				: new DecimalIndicatorValue(this, (Buffer[1] + 2 * Buffer[2] + 2 * Buffer[3] + newValue) / 6m, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}
}