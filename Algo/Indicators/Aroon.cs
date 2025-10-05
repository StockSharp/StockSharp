namespace StockSharp.Algo.Indicators;

/// <summary>
/// Aroon indicator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/aroon.html
/// </remarks>
[Display(	
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.AroonKey,
	Description = LocalizedStrings.AroonDescriptionKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/aroon.html")]
[IndicatorOut(typeof(IAroonValue))]
public class Aroon : BaseComplexIndicator<IAroonValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Aroon"/>.
	/// </summary>
	public Aroon()
		: this(new AroonUp(), new AroonDown())
	{
		Length = 14;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Aroon"/>.
	/// </summary>
	/// <param name="up">Aroon Up.</param>
	/// <param name="down">Aroon Down.</param>
	public Aroon(AroonUp up, AroonDown down)
		: base(up, down)
	{
		Up = up;
		Down = down;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <summary>
	/// Period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => Up.Length;
		set
		{
			Up.Length = Down.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// Aroon Up.
	/// </summary>
	[Browsable(false)]
	public AroonUp Up { get; }

	/// <summary>
	/// Aroon Down.
	/// </summary>
	[Browsable(false)]
	public AroonDown Down { get; }

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" L={Length}";

	/// <inheritdoc />
	protected override IAroonValue CreateValue(DateTimeOffset time)
		=> new AroonValue(this, time);
}

/// <summary>
/// Aroon Up.
/// </summary>
[IndicatorHidden]
public class AroonUp : LengthIndicator<decimal>
{
	private decimal _maxValue;
	private int _maxValueAge;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		//decimal tempMaxValue;
		decimal tempMaxValueAge;

		if (input.IsFinal)
		{
			if (candle.HighPrice >= _maxValue)
			{
				_maxValue = candle.HighPrice;
				_maxValueAge = 0;
			}
			else
			{
				_maxValueAge++;
			}

			if (Buffer.Count == Length)
			{
				var removedValue = Buffer[0];
				if (removedValue == _maxValue)
				{
					_maxValue = candle.HighPrice;
					_maxValueAge = 0;

					for (var i = 1; i < Length; i++)
					{
						if (Buffer[i] > _maxValue)
						{
							_maxValue = Buffer[i];
							_maxValueAge = i;
						}
					}
				}
			}

			Buffer.PushBack(candle.HighPrice);

			//tempMaxValue = _maxValue;
			tempMaxValueAge = _maxValueAge;
		}
		else
		{
			//tempMaxValue = _maxValue;
			tempMaxValueAge = _maxValueAge;

			if (candle.HighPrice > _maxValue)
			{
				//tempMaxValue = high;
				tempMaxValueAge = 0;
			}
			else
			{
				tempMaxValueAge++;
			}
		}

		if (IsFormed)
		{
			var value = 100m * (Length - tempMaxValueAge) / Length;
			return value;
		}

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_maxValue = decimal.MinValue;
		_maxValueAge = default;

		base.Reset();
	}
}

/// <summary>
/// Aroon Down.
/// </summary>
[IndicatorHidden]
public class AroonDown : LengthIndicator<decimal>
{
	private decimal _minValue;
	private int _minValueAge;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		//decimal tempMinValue;
		decimal tempMinValueAge;

		if (input.IsFinal)
		{
			if (candle.LowPrice <= _minValue)
			{
				_minValue = candle.LowPrice;
				_minValueAge = 0;
			}
			else
			{
				_minValueAge++;
			}

			if (Buffer.Count == Length)
			{
				var removedValue = Buffer[0];
				if (removedValue == _minValue)
				{
					_minValue = candle.LowPrice;
					_minValueAge = 0;

					for (var i = 1; i < Length; i++)
					{
						if (Buffer[i] < _minValue)
						{
							_minValue = Buffer[i];
							_minValueAge = i;
						}
					}
				}
			}

			Buffer.PushBack(candle.LowPrice);

			//tempMinValue = _minValue;
			tempMinValueAge = _minValueAge;
		}
		else
		{
			//tempMinValue = _minValue;
			tempMinValueAge = _minValueAge;

			if (candle.LowPrice < _minValue)
			{
				//tempMinValue = low;
				tempMinValueAge = 0;
			}
			else
			{
				tempMinValueAge++;
			}
		}

		if (IsFormed)
		{
			var value = 100m * (Length - tempMinValueAge) / Length;
			return value;
		}

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_minValue = decimal.MaxValue;
		_minValueAge = default;

		base.Reset();
	}
}

/// <summary>
/// <see cref="Aroon"/> indicator value.
/// </summary>
public interface IAroonValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the <see cref="Aroon.Up"/> value.
	/// </summary>
	IIndicatorValue UpValue { get; }

	/// <summary>
	/// Gets the <see cref="Aroon.Up"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Up { get; }

	/// <summary>
	/// Gets the <see cref="Aroon.Down"/> value.
	/// </summary>
	IIndicatorValue DownValue { get; }

	/// <summary>
	/// Gets the <see cref="Aroon.Down"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Down { get; }
}

/// <summary>
/// Aroon indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AroonValue"/> class.
/// </remarks>
/// <param name="indicator">The parent Aroon indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class AroonValue(Aroon indicator, DateTimeOffset time) : ComplexIndicatorValue<Aroon>(indicator, time), IAroonValue
{
	/// <inheritdoc />
	public IIndicatorValue UpValue => this[TypedIndicator.Up];
	/// <inheritdoc />
	public decimal? Up => UpValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue DownValue => this[TypedIndicator.Down];
	/// <inheritdoc />
	public decimal? Down => DownValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"Up={Up}, Down={Down}";
}
