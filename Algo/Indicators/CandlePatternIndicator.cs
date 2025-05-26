namespace StockSharp.Algo.Indicators;

using System.Runtime.InteropServices;

using StockSharp.Algo.Candles.Patterns;

/// <summary>
/// <see cref="CandlePatternIndicator"/> value.
/// </summary>
public class CandlePatternIndicatorValue : SingleIndicatorValue<bool>
{
	private DateTimeOffset[] _candleOpenTimes;

	/// <summary>
	/// Pattern candle times.
	/// </summary>
	public DateTimeOffset[] CandleOpenTimes
	{
		get => _candleOpenTimes;
		set => _candleOpenTimes = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CandlePatternIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <param name="value">Signal value.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public CandlePatternIndicatorValue(IIndicator indicator, bool value, DateTimeOffset time)
		: base(indicator, value, time)
	{ }

	/// <summary>
	/// Initializes a new instance of the <see cref="CandlePatternIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public CandlePatternIndicatorValue(IIndicator indicator, DateTimeOffset time)
		: base(indicator, time)
	{ }

	/// <summary>
	/// Cast object from <see cref="CandlePatternIndicatorValue"/> to <see cref="bool"/>.
	/// </summary>
	/// <param name="value">Object <see cref="CandlePatternIndicatorValue"/>.</param>
	/// <returns><see cref="bool"/> value.</returns>
	public static explicit operator bool(CandlePatternIndicatorValue value)
		=> value.Value;

	/// <inheritdoc />
	public override IEnumerable<object> ToValues()
	{
		foreach (var v in base.ToValues())
			yield return v;

		if (IsEmpty)
			yield break;

		foreach (var time in CandleOpenTimes)
			yield return time.UtcDateTime;
	}

	/// <inheritdoc />
	public override void FromValues(object[] values)
	{
		base.FromValues(values);

		if (IsEmpty)
			return;

		CandleOpenTimes = [.. values.Skip(1).Select(v => (DateTimeOffset)v.To<DateTime>().UtcKind())];
	}
}

/// <summary>
/// Indicator, based on <see cref="ICandlePattern"/>.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/patterns.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PatternKey,
	Description = LocalizedStrings.PatternKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[IndicatorOut(typeof(CandlePatternIndicatorValue))]
[Doc("topics/api/patterns.html")]
public class CandlePatternIndicator : BaseIndicator
{
	private ICandlePatternProvider _candlePatternProvider;
	private ICandlePattern _pattern;
	private readonly List<ICandleMessage> _buffer = [];

	/// <summary>
	/// Candle pattern.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PatternKey,
		Description = LocalizedStrings.PatternKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public ICandlePattern Pattern
	{
		get => _pattern;
		set
		{
			_pattern = value;
			Reset();
		}
	}

	private void EnsureProvider()
	{
		if(_candlePatternProvider != null)
			return;

		_candlePatternProvider = ServicesRegistry.TryCandlePatternProvider;
		if (_candlePatternProvider == null)
			return;

		_candlePatternProvider.PatternReplaced += (oldPattern, newPattern) =>
		{
			if(oldPattern == Pattern)
				Pattern = newPattern;
		};

		_candlePatternProvider.PatternDeleted += (pattern) =>
		{
			if(pattern == Pattern)
				Pattern = null;
		};
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CandlePatternIndicator"/>.
	/// </summary>
	public CandlePatternIndicator()
	{
		EnsureProvider();
		Reset();
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => Pattern?.CandlesCount ?? 0;

	/// <inheritdoc />
	public sealed override void Reset()
	{
		Name = (Pattern?.Name).IsEmpty(LocalizedStrings.Pattern);
		_buffer.Clear();
		base.Reset();
		IsFormed = true;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		EnsureProvider();
		var patternName = storage.GetValue<string>(nameof(Pattern));

		if(patternName.IsEmptyOrWhiteSpace())
			return;

		if(_candlePatternProvider == null)
			throw new InvalidOperationException($"unable to load pattern '{patternName}'. candle pattern provider is not initialized.");

		if(!_candlePatternProvider.TryFind(patternName, out var pattern))
			LogManager.Instance?.Application.LogError($"pattern '{patternName}' not found");

		Pattern = pattern;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		if(Pattern != null)
			storage.SetValue(nameof(Pattern), Pattern.Name);
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candlesCount = NumValuesToInitialize;

		if (candlesCount == 0)
		{
			return new CandlePatternIndicatorValue(this, input.Time)
			{
				IsFinal = input.IsFinal
			};
		}

		try
		{
			var candle = input.ToCandle();
		
			_buffer.Add(candle);

			var recognized = false;
			var times = Array.Empty<DateTimeOffset>();

			if (_buffer.Count == candlesCount)
			{
				recognized = Pattern.Recognize(CollectionsMarshal.AsSpan(_buffer));

				if (recognized)
				{
					times = new DateTimeOffset[candlesCount];

					for (var i = 0; i < times.Length; ++i)
						times[i] = _buffer[i].OpenTime;
				}

				if (input.IsFinal)
				{
					if (recognized)
						_buffer.Clear();
					else
						_buffer.RemoveAt(0); // shift buffer by one candle
				}
			}

			return new CandlePatternIndicatorValue(this, recognized, input.Time)
			{
				IsFinal = input.IsFinal,
				CandleOpenTimes = times,
			};
		}
		finally
		{
			if(!input.IsFinal && _buffer.Count > 0)
				_buffer.RemoveAt(_buffer.Count - 1);
		}
	}
}