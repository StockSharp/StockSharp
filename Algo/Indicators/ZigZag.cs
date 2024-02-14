#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: ZigZag.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// ZigZag.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/IndicatorZigZag.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ZigZagKey,
		Description = LocalizedStrings.ZigZagDescKey)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/IndicatorZigZag.html")]
	public class ZigZag : BaseIndicator
	{
		private readonly IList<ICandleMessage> _buffer = new List<ICandleMessage>();
		private readonly List<decimal> _lowBuffer = new();
		private readonly List<decimal> _highBuffer = new();
		private readonly List<decimal> _zigZagBuffer = new();

		private int _depth;
		private int _backStep;
		private bool _needAdd = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="ZigZag"/>.
		/// </summary>
		public ZigZag()
		{
			BackStep = 3;
			Depth = 12;
		}

		/// <inheritdoc />
		public override int NumValuesToInitialize => 2;

		/// <summary>
		/// Minimum number of candles between local maximums, minimums.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CandlesKey,
			Description = LocalizedStrings.BackStepKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public int BackStep
		{
			get => _backStep;
			set
			{
				if (_backStep == value)
					return;

				_backStep = value;
				Reset();
			}
		}

		/// <summary>
		/// Candles minimum, on which Zigzag will not build a second maximum (or minimum), if it is smaller (or larger) by a deviation of the previous respectively.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.DepthKey,
			Description = LocalizedStrings.ZigZagDepthKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public int Depth
		{
			get => _depth;
			set
			{
				if (_depth == value)
					return;

				_depth = value;
				Reset();
			}
		}

		private Unit _deviation = new(0.45m, UnitTypes.Percent);

		/// <summary>
		/// Minimum number of points between maximums (minimums) of two adjacent candles used by Zigzag indicator to form a local peak (local trough).
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.MinimumChangeKey,
			Description = LocalizedStrings.MinimumChangeDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public Unit Deviation
		{
			get => _deviation;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (_deviation == value)
					return;

				_deviation = value;
				Reset();
			}
		}

		private Func<ICandleMessage, decimal> _getHighValue = candle => candle.HighPrice;
		/// <summary>
		/// The converter, returning from the candle a price for search of maximum.
		/// </summary>
		[Browsable(false)]
		public Func<ICandleMessage, decimal> GetHighValue
		{
			get => _getHighValue;
			set
			{
				_getHighValue = value ?? throw new ArgumentNullException(nameof(value));
				Reset();
			}
		}

		private Func<ICandleMessage, decimal> _getLowValue = candle => candle.LowPrice;
		/// <summary>
		/// The converter, returning from the candle a price for search of minimum.
		/// </summary>
		[Browsable(false)]
		public Func<ICandleMessage, decimal> GetLowValue
		{
			get => _getLowValue;
			set
			{
				_getLowValue = value ?? throw new ArgumentNullException(nameof(value));
				Reset();
			}
		}

		private Func<ICandleMessage, decimal> _getCurrentValue = candle => candle.ClosePrice;
		/// <summary>
		/// The converter, returning from the candle a price for the current value.
		/// </summary>
		[Browsable(false)]
		public Func<ICandleMessage, decimal> GetCurrentValue
		{
			get => _getCurrentValue;
			set
			{
				_getCurrentValue = value ?? throw new ArgumentNullException(nameof(value));
				Reset();
			}
		}

		/// <summary>
		/// The indicator current value.
		/// </summary>
		[Browsable(false)]
		public decimal CurrentValue { get; private set; }

		/// <summary>
		/// Shift for the last indicator value.
		/// </summary>
		[Browsable(false)]
		public int LastValueShift { get; private set; }

		/// <inheritdoc />
		public override void Reset()
		{
			_needAdd = true;
			_buffer.Clear();
			_lowBuffer.Clear();
			_highBuffer.Clear();
			_zigZagBuffer.Clear();
			CurrentValue = 0;
			LastValueShift = 0;
			base.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			if (_needAdd)
			{
				_buffer.Insert(0, candle);
				_lowBuffer.Insert(0, 0);
				_highBuffer.Insert(0, 0);
				_zigZagBuffer.Insert(0, 0);
			}
			else
			{
				_buffer[0] = candle;
				_lowBuffer[0] = 0;
				_highBuffer[0] = 0;
				_zigZagBuffer[0] = 0;
			}

			const int level = 3;
			int limit;
			decimal lastHigh = 0;
			decimal lastLow = 0;

			if (_buffer.Count - 1 == 0)
			{
				limit = _buffer.Count - Depth;
			}
			else
			{
				int i = 0, count = 0;
				while (count < level && i < _buffer.Count - Depth)
				{
					var res = _zigZagBuffer[i];
					if (res != 0)
					{
						count++;
					}
					i++;
				}
				limit = --i;
			}

			var getLowValue = GetLowValue;
			var getHighValue = GetHighValue;

			for (var shift = limit; shift >= 0; shift--)
			{
				//--- low
				var val = _buffer.Skip(shift).Take(Depth).Min(getLowValue);
				if (val == lastLow)
				{
					val = 0.0m;
				}
				else
				{
					lastLow = val;
					if (getLowValue(_buffer[shift]) - val > 0.0m * val / 100)
					{
						val = 0.0m;
					}
					else
					{
						for (var back = 1; back <= BackStep; back++)
						{
							var res = _lowBuffer[shift + back];
							if (res != 0 && res > val)
							{
								_lowBuffer[shift + back] = 0.0m;
							}
						}
					}
				}
				if (getLowValue(_buffer[shift]) == val)
					_lowBuffer[shift] = val;
				else
					_lowBuffer[shift] = 0m;

				//--- high
				val = _buffer.Skip(shift).Take(Depth).Max(getHighValue);
				if (val == lastHigh)
				{
					val = 0.0m;
				}
				else
				{
					lastHigh = val;
					if (val - getHighValue(_buffer[shift]) > 0.0m * val / 100)
					{
						val = 0.0m;
					}
					else
					{
						for (var back = 1; back <= BackStep; back++)
						{
							var res = _highBuffer[shift + back];
							if (res != 0 && res < val)
							{
								_highBuffer[shift + back] = 0.0m;
							}
						}
					}
				}
				if (getHighValue(_buffer[shift]) == val)
					_highBuffer[shift] = val;
				else
					_highBuffer[shift] = 0m;
			}

			// final cutting
			lastHigh = -1;
			lastLow = -1;
			var lastHighPos = -1;
			var lastLowPos = -1;

			for (var shift = limit; shift >= 0; shift--)
			{
				var curLow = _lowBuffer[shift];
				var curHigh = _highBuffer[shift];

				if ((curLow == 0) && (curHigh == 0))
					continue;

				//---
				if (curHigh != 0)
				{
					if (lastHigh > 0)
					{
						if (lastHigh < curHigh)
						{
							_highBuffer[lastHighPos] = 0;
						}
						else
						{
							_highBuffer[shift] = 0;
						}
					}
					//---
					if (lastHigh < curHigh || lastHigh < 0)
					{
						lastHigh = curHigh;
						lastHighPos = shift;
					}
					lastLow = -1;
				}

				//----
				if (curLow != 0)
				{
					if (lastLow > 0)
					{
						if (lastLow > curLow)
						{
							_lowBuffer[lastLowPos] = 0;
						}
						else
						{
							_lowBuffer[shift] = 0;
						}
					}
					//---
					if ((curLow < lastLow) || (lastLow < 0))
					{
						lastLow = curLow;
						lastLowPos = shift;
					}
					lastHigh = -1;
				}
			}

			for (var shift = limit; shift >= 0; shift--)
			{
				if (shift >= _buffer.Count - Depth)
				{
					_zigZagBuffer[shift] = 0.0m;
				}
				else
				{
					var res = _highBuffer[shift];
					if (res != 0.0m)
					{
						_zigZagBuffer[shift] = res;
					}
					else
					{
						_zigZagBuffer[shift] = _lowBuffer[shift];
					}
				}
			}

			int valuesCount = 0, valueId = 0;

			for (; valueId < _zigZagBuffer.Count && valuesCount < 2; valueId++)
			{
				if (_zigZagBuffer[valueId] != 0)
					valuesCount++;
			}

			_needAdd = input.IsFinal;

			if (valuesCount != 2)
				return new DecimalIndicatorValue(this);

			if (input.IsFinal)
				IsFormed = true;

			LastValueShift = valueId - 1;

			CurrentValue = GetCurrentValue(_buffer[0]);

			return new DecimalIndicatorValue(this, _zigZagBuffer[LastValueShift]);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			BackStep = storage.GetValue<int>(nameof(BackStep));
			Depth = storage.GetValue<int>(nameof(Depth));
			Deviation.Load(storage, nameof(Deviation));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(BackStep), BackStep);
			storage.SetValue(nameof(Depth), Depth);
			storage.SetValue(nameof(Deviation), Deviation.Save());
		}
	}
}
