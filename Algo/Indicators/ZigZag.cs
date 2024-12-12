namespace StockSharp.Algo.Indicators;

/// <summary>
/// Zig Zag.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/zigzag.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ZigZagKey,
	Description = LocalizedStrings.ZigZagDescKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[IndicatorOut(typeof(ShiftedIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/zigzag.html")]
public class ZigZag : LengthIndicator<decimal>
{
	private readonly List<decimal> _zigZagBuffer = [];

	private bool _needAdd = true;

	/// <summary>
	/// Initializes a new instance of the <see cref="ZigZag"/>.
	/// </summary>
	public ZigZag()
	{
		Length = 2;
	}

	private decimal _deviation = 0.45m * 0.01m;

	/// <summary>
	/// Percentage change.
	/// </summary>
	/// <remarks>
	/// It is specified in the range from 0 to 1.
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PercentageChangeKey,
		Description = LocalizedStrings.PercentageChangeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal Deviation
	{
		get => _deviation;
		set
		{
			if (value <= 0 || value > 1)
				throw new ArgumentOutOfRangeException(nameof(value));

			if (_deviation == value)
				return;

			_deviation = value;
			Reset();
		}
	}

	/// <summary>
	/// The indicator current value.
	/// </summary>
	[Browsable(false)]
	public decimal CurrentValue { get; private set; }

	/// <inheritdoc />
	public override void Reset()
	{
		_needAdd = true;
		_zigZagBuffer.Clear();
		CurrentValue = 0;

		base.Reset();
	}

	/// <summary>
	/// Get the price from the input value.
	/// </summary>
	/// <param name="input"><see cref="IIndicatorValue"/></param>
	/// <returns><see cref="decimal"/></returns>
	protected virtual decimal GetPrice(IIndicatorValue input)
		=> input.ToDecimal();

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var value = GetPrice(input);

		if (_needAdd)
		{
			Buffer.PushBack(value);
			_zigZagBuffer.Add(0);
		}
		else
		{
			Buffer[^1] = value;
			_zigZagBuffer[^1] = 0;
		}

		const int level = 3;
		int limit = 0, count = 0;
		while (count < level && limit >= 0)
		{
			var res = _zigZagBuffer[limit];
			if (res != 0)
			{
				count++;
			}
			limit--;
		}
		limit++;

		var min = Buffer[limit];
		var max = min;
		int action = 0, j = 0;
		for (var i = limit + 1; i < Buffer.Count; i++)
		{
			if (Buffer[i] > max)
			{
				max = Buffer[i];
				if (action != 2) //action=1:building the down-point (min) of ZigZag
				{
					if (max - min >= _deviation * min) //min (action!=2) end,max (action=2) begin
					{
						action = 2;
						_zigZagBuffer[i] = max;
						j = i;
						min = max;
					}
					else
						_zigZagBuffer[i] = 0.0m; //max-min=miser,(action!=2) continue
				}
				else //max (action=2) continue
				{
					_zigZagBuffer[j] = 0.0m;
					_zigZagBuffer[i] = max;
					j = i;
					min = max;
				}
			}
			else if (Buffer[i] < min)
			{
				min = Buffer[i];
				if (action != 1) //action=2:building the up-point (max) of ZigZag
				{
					if (max - min >= _deviation * max) //max (action!=1) end,min (action=1) begin
					{
						action = 1;
						_zigZagBuffer[i] = min;
						j = i;
						max = min;
					}
					else
						_zigZagBuffer[i] = 0.0m; //max-min=miser,(action!=1) continue
				}
				else //min (action=1) continue
				{
					_zigZagBuffer[j] = 0.0m;
					_zigZagBuffer[i] = min;
					j = i;
					max = min;
				}
			}
			else
				_zigZagBuffer[i] = 0.0m;
		}

		int valuesCount = 0, valueId = 0;
		decimal last = 0, lastButOne = 0;
		for (var i = _zigZagBuffer.Count - 1; i > 0 && valuesCount < 2; i--, valueId++)
		{
			if (_zigZagBuffer[i] == 0)
				continue;

			valuesCount++;

			if (valuesCount == 1)
				last = _zigZagBuffer[i];
			else
				lastButOne = _zigZagBuffer[i];
		}

		_needAdd = input.IsFinal;

		if (valuesCount != 2)
			return Container.Count > 1 ? this.GetCurrentValue<ShiftedIndicatorValue>() : new ShiftedIndicatorValue(this, input.Time);

		if (input.IsFinal)
			IsFormed = true;

		CurrentValue = last;

		return new ShiftedIndicatorValue(this, lastButOne, valueId - 1, input.Time);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Deviation = storage.GetValue<decimal>(nameof(Deviation));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Deviation), Deviation);
	}
}
