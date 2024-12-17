namespace StockSharp.Algo.Indicators;

/// <summary>
/// Covariance.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/covariation.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.COVKey,
	Description = LocalizedStrings.CovarianceKey)]
[Doc("topics/api/indicators/list_of_indicators/covariation.html")]
[IndicatorIn(typeof(PairIndicatorValue<decimal>))]
[IndicatorHidden]
public class Covariance : LengthIndicator<(decimal, decimal)>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Covariance"/>.
	/// </summary>
	public Covariance()
	{
		Length = 20;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var value = input.GetValue<(decimal, decimal)>();

		(decimal, decimal)? first = null;

		if (input.IsFinal)
		{
			Buffer.PushBack(value);
		}
		else
		{
			first = Buffer.Count == Length ? Buffer[0] : default;
			Buffer.PushBack(value);
		}

		decimal avgSource = 0;
		decimal avgOther = 0;

		foreach (var tuple in Buffer)
		{
			avgSource += tuple.Item1;
			avgOther += tuple.Item2;
		}

		var len = Buffer.Count;

		avgSource /= len;
		avgOther /= len;

		var covariance = 0m;

		foreach (var tuple in Buffer)
		{
			covariance += (tuple.Item1 - avgSource) * (tuple.Item2 - avgOther);
		}

		if (!input.IsFinal)
		{
			if (first != null)
				Buffer.PushFront(first.Value);

			Buffer.PopBack();
		}

		return new DecimalIndicatorValue(this, covariance / len, input.Time);
	}
}