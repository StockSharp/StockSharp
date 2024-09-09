﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// McGinley Dynamic.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MGDKey,
	Description = LocalizedStrings.McGinleyDynamicKey)]
[Doc("topics/indicators/mcginley_dynamic.html")]
public class McGinleyDynamic : LengthIndicator<decimal>
{
	private decimal _prevMd;

	/// <summary>
	/// Initializes a new instance of the <see cref="McGinleyDynamic"/>.
	/// </summary>
	public McGinleyDynamic()
	{
		Length = 14;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		if (!IsFormed)
		{
			if (input.IsFinal)
			{
				Buffer.PushBack(price);

				if (Buffer.Count == Length)
				{
					_prevMd = Buffer.Average();

					return new DecimalIndicatorValue(this, _prevMd, input.Time);
				}
			}
		}
		else
		{
			var md = _prevMd + (price - _prevMd) / (0.6m * Length * (decimal)Math.Pow((double)(price / _prevMd), 4));

			if (input.IsFinal)
				_prevMd = md;

			return new DecimalIndicatorValue(this, md, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevMd = default;

		base.Reset();
	}
}