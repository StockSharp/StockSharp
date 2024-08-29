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
public class Aroon : BaseComplexIndicator
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
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UpKey,
		Description = LocalizedStrings.AroonUpKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public AroonUp Up { get; }

	/// <summary>
	/// Aroon Down.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DownKey,
		Description = LocalizedStrings.AroonDownKey,
		GroupName = LocalizedStrings.GeneralKey)]
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
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var (_, high, _, _) = input.GetOhlc();

		//decimal tempMaxValue;
		decimal tempMaxValueAge;

		if (input.IsFinal)
		{
			if (high >= _maxValue)
			{
				_maxValue = high;
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
					_maxValue = high;
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

			Buffer.PushBack(high);

			//tempMaxValue = _maxValue;
			tempMaxValueAge = _maxValueAge;
		}
		else
		{
			//tempMaxValue = _maxValue;
			tempMaxValueAge = _maxValueAge;

			if (high > _maxValue)
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
			return new DecimalIndicatorValue(this, value, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
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
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var (_, _, low, _) = input.GetOhlc();

		//decimal tempMinValue;
		decimal tempMinValueAge;

		if (input.IsFinal)
		{
			if (low <= _minValue)
			{
				_minValue = low;
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
					_minValue = low;
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

			Buffer.PushBack(low);

			//tempMinValue = _minValue;
			tempMinValueAge = _minValueAge;
		}
		else
		{
			//tempMinValue = _minValue;
			tempMinValueAge = _minValueAge;

			if (low < _minValue)
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
			return new DecimalIndicatorValue(this, value, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_minValue = decimal.MaxValue;
		_minValueAge = default;

		base.Reset();
	}
}