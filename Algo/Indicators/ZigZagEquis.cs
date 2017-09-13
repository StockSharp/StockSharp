#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: ZigZagEquis.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Zig Zag (Metastock).
	/// </summary>
	/// <remarks>
	/// Zig Zag indicator filters fluctuations of prices or indicator values, which are not beyond specific value, presented in % or in absolute numbers. It is done for preliminary analysis of chart, emphasizing only sufficiently big price changes (indicator values).
	/// </remarks>
	[DisplayName("ZigZag Metastock")]
	[DescriptionLoc(LocalizedStrings.Str826Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[IndicatorOut(typeof(ShiftedIndicatorValue))]
	public class ZigZagEquis : BaseIndicator
	{
		private readonly IList<decimal> _buffer = new List<decimal>();
		private readonly List<decimal> _zigZagBuffer = new List<decimal>();

		private bool _needAdd = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="ZigZagEquis"/>.
		/// </summary>
		public ZigZagEquis()
		{
		}

		private decimal _deviation = 0.45m * 0.01m;

		/// <summary>
		/// Percentage change.
		/// </summary>
		/// <remarks>
		/// It is specified in the range from 0 to 1.
		/// </remarks>
		[DisplayNameLoc(LocalizedStrings.Str833Key)]
		[DescriptionLoc(LocalizedStrings.Str834Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Deviation
		{
			get => _deviation;
			set
			{
				if (value == 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (_deviation == value)
					return;

				_deviation = value;
				Reset();
			}
		}

		private Func<Candle, decimal> _byPrice = candle => candle.ClosePrice;

		/// <summary>
		/// The converter, returning from the candle a price for calculations.
		/// </summary>
		[Browsable(false)]
		public Func<Candle, decimal> ByPrice
		{
			get => _byPrice;
			set
			{
				_byPrice = value;
				Reset();
			}
		}

		/// <summary>
		/// The indicator current value.
		/// </summary>
		[Browsable(false)]
		public decimal CurrentValue { get; private set; }

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_needAdd = true;
			_buffer.Clear();
			_zigZagBuffer.Clear();
			CurrentValue = 0;
			base.Reset();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = _byPrice(input.GetValue<Candle>());
			if (_needAdd)
			{
				_buffer.Add(value);
				_zigZagBuffer.Add(0);
			}
			else
			{
				_buffer[_buffer.Count - 1] = value;
				_zigZagBuffer[_zigZagBuffer.Count - 1] = 0;
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

			var min = _buffer[limit];
			var max = min;
			int action = 0, j = 0;
			for (var i = limit + 1; i < _buffer.Count; i++)
			{
				if (_buffer[i] > max)
				{
					max = _buffer[i];
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
				else if (_buffer[i] < min)
				{
					min = _buffer[i];
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
				return Container.Count > 1 ? this.GetCurrentValue<ShiftedIndicatorValue>() : new ShiftedIndicatorValue(this);

			if (input.IsFinal)
				IsFormed = true;

			CurrentValue = last;

			return new ShiftedIndicatorValue(this, valueId - 1, input.SetValue(this, lastButOne));
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			Deviation = settings.GetValue<decimal>(nameof(Deviation));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue(nameof(Deviation), Deviation);
		}
	}
}