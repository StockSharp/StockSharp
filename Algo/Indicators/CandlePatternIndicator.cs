using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Ecng.Serialization;

using StockSharp.Algo.Candles.Patterns;
using StockSharp.Messages;
using StockSharp.Localization;

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
	/// </summary>
	public CandlePatternIndicatorValue(IIndicator indicator, bool value) : base(indicator, value) { }

	/// <summary>
	/// </summary>
	public CandlePatternIndicatorValue(IIndicator indicator) : base(indicator) { }
}

/// <summary>
/// </summary>
[DisplayName("Pattern")]
[DescriptionLoc(LocalizedStrings.PatternKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
public class CandlePatternIndicator : BaseIndicator
{
	private ICandlePatternProvider _candlePatternProvider;
	private ICandlePattern _pattern;
	private readonly List<ICandleMessage> _buffer = new();

	/// <summary>
	/// Candle pattern.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.PatternKey)]
	[DescriptionLoc(LocalizedStrings.PatternKey)]
	[CategoryLoc(LocalizedStrings.GeneralKey)]
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
	/// </summary>
	public CandlePatternIndicator()
	{
		EnsureProvider();
		Reset();
	}

	/// <inheritdoc />
	public sealed override void Reset()
	{
		Name = LocalizedStrings.Pattern + " " + Pattern?.Name;
		_buffer.Clear();
		base.Reset();
		IsFormed = true;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Pattern = storage.GetValue<SettingsStorage>(nameof(Pattern))?.LoadEntire<ICandlePattern>();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Pattern), Pattern?.SaveEntire(false));
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
}
