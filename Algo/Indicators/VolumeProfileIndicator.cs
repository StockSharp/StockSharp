namespace StockSharp.Algo.Indicators;

/// <summary>
/// Volume profile.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/volume_profile.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.VolumeProfileKey,
	Description = LocalizedStrings.VolumeProfileKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[IndicatorOut(typeof(VolumeProfileIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/volume_profile.html")]
public class VolumeProfileIndicator : BaseIndicator
{
	/// <summary>
	/// The indicator value <see cref="VolumeProfileIndicator"/>, derived in result of calculation.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="VolumeProfileIndicatorValue"/>.
	/// </remarks>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public class VolumeProfileIndicatorValue(IIndicator indicator, DateTimeOffset time) : SingleIndicatorValue<IDictionary<decimal, decimal>>(indicator, time)
	{
		/// <summary>
		/// Embedded values.
		/// </summary>
		public IDictionary<decimal, decimal> Levels { get; } = new Dictionary<decimal, decimal>();

		/// <inheritdoc />
		public override T GetValue<T>(Level1Fields? field) => throw new NotSupportedException();

		/// <inheritdoc />
		public override IEnumerable<object> ToValues()
		{
			if (IsEmpty)
				yield break;

			foreach (var level in Levels)
			{
				yield return level.Key;
				yield return level.Value;
			}
		}

		/// <inheritdoc />
		public override void FromValues(object[] values)
		{
			if (values.Length == 0)
			{
				IsEmpty = true;
				return;
			}

			IsEmpty = false;

			Levels.Clear();

			for (var i = 0; i < values.Length; i += 2)
				Levels.Add(values[i].To<decimal>(), values[i + 1].To<decimal>());
		}
	}

	private readonly Dictionary<decimal, decimal> _levels = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="VolumeProfileIndicator"/>.
	/// </summary>
	public VolumeProfileIndicator()
	{
		Step = 1;
	}

	private decimal _step;

	/// <summary>
	/// The grouping increment.
	/// </summary>
	public decimal Step
	{
		get => _step;
		set
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_step = value;

			Reset();
		}
	}

	private bool _useTotalVolume;

	/// <summary>
	/// To use aggregate volume in calculations (when candles do not contain VolumeProfile).
	/// </summary>
	public bool UseTotalVolume
	{
		get => _useTotalVolume;
		set
		{
			_useTotalVolume = value;

			Reset();
		}
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_levels.Clear();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new VolumeProfileIndicatorValue(this, input.Time);

		if (!input.IsFinal)
			return result;

		IsFormed = true;

		var candle = input.ToCandle();

		if (!UseTotalVolume)
		{
			if (candle.PriceLevels != null)
			{
				foreach (var priceLevel in candle.PriceLevels)
					AddVolume(priceLevel.Price, priceLevel.TotalVolume);
			}
		}
		else
			AddVolume(candle.ClosePrice, candle.TotalVolume);

		foreach (var level in _levels)
		{
			result.Levels.Add(level.Key, level.Value);
		}

		return result;
	}

	private void AddVolume(decimal price, decimal volume)
	{
		var level = (int)(price / Step) * Step;
		var currentValue = _levels.TryGetValue(level);

		_levels[level] = currentValue + volume;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Step), Step);
		storage.SetValue(nameof(UseTotalVolume), UseTotalVolume);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Step = storage.GetValue<decimal>(nameof(Step));
		UseTotalVolume = storage.GetValue<bool>(nameof(UseTotalVolume));
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()}, S={Step}, V={UseTotalVolume}";
}
