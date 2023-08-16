using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Candles.Patterns;
using StockSharp.Messages;
using StockSharp.Localization;
using StockSharp.Logging;

namespace StockSharp.Algo.Indicators;

/// <summary>
/// Candle pattern indicator value.
/// </summary>
public class CandlePatternIndicatorValue : SingleIndicatorValue<bool>
{
	/// <summary>
	/// Pattern candle times.
	/// </summary>
	public DateTimeOffset[] CandleOpenTimes { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CandlePatternIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <param name="value">Signal value.</param>
	public CandlePatternIndicatorValue(IIndicator indicator, bool value) : base(indicator, value) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="CandlePatternIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	public CandlePatternIndicatorValue(IIndicator indicator) : base(indicator) { }

	/// <summary>
	/// Cast object from <see cref="CandlePatternIndicatorValue"/> to <see cref="bool"/>.
	/// </summary>
	/// <param name="value">Object <see cref="CandlePatternIndicatorValue"/>.</param>
	/// <returns><see cref="bool"/> value.</returns>
	public static explicit operator bool(CandlePatternIndicatorValue value)
		=> value.Value;
}

/// <summary>
/// Indicator, based on <see cref="ICandlePattern"/>.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PatternKey,
	Description = LocalizedStrings.PatternKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
public class CandlePatternIndicator : BaseIndicator
{
	private ICandlePatternProvider _candlePatternProvider;
	private ICandlePattern _pattern;
	private readonly List<ICandleMessage> _buffer = new();

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
	/// Number of candles in the pattern.
	/// </summary>
	[Browsable(false)]
	public int PatternLength => Pattern?.CandlesCount ?? 0;

	/// <summary>
	/// Initializes a new instance of the <see cref="CandlePatternIndicator"/>.
	/// </summary>
	public CandlePatternIndicator()
	{
		EnsureProvider();
		Reset();
	}

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
			LogManager.Instance?.Application.AddErrorLog($"pattern '{patternName}' not found");

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
		if(!(Pattern?.CandlesCount > 0))
			return new CandlePatternIndicatorValue(this, false)
			{
				IsFinal = input.IsFinal,
				InputValue = input,
			};

		var candle = input.GetValue<ICandleMessage>();
		var recognized = false;
		DateTimeOffset[] times = null;

		try
		{
			_buffer.Add(candle);

			if (_buffer.Count == Pattern.CandlesCount)
			{
				recognized = Pattern.Recognize(CollectionsMarshal.AsSpan(_buffer));

				if (recognized)
				{
					times = new DateTimeOffset[Pattern.CandlesCount];

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

			return new CandlePatternIndicatorValue(this, recognized)
			{
				IsFinal = input.IsFinal,
				InputValue = input,
				CandleOpenTimes = times,
			};
		}
		finally
		{
			if(!input.IsFinal && _buffer.Count > 0)
				_buffer.RemoveAt(_buffer.Count - 1);
		}
	}

	/// <inheritdoc />
	public override IIndicatorValue CreateValue(IEnumerable<object> values)
		=> new CandlePatternIndicatorValue(this, values.First().To<bool>());
}
