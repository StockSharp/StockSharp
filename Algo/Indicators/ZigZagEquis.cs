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
	using System.ComponentModel.DataAnnotations;

	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Zig Zag (Metastock).
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/zigzag_metastock.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ZigZagMetaStockKey,
		Description = LocalizedStrings.ZigZagDescKey)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[IndicatorOut(typeof(ShiftedIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/zigzag_metastock.html")]
	public class ZigZagEquis : BaseIndicator
	{
		private readonly IList<decimal> _buffer = new List<decimal>();
		private readonly List<decimal> _zigZagBuffer = new();

		private bool _needAdd = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="ZigZagEquis"/>.
		/// </summary>
		public ZigZagEquis()
		{
		}

		/// <inheritdoc />
		public override int NumValuesToInitialize => 2;

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
				if (value == 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (_deviation == value)
					return;

				_deviation = value;
				Reset();
			}
		}

		private Level1Fields _priceField = Level1Fields.ClosePrice;

		/// <summary>
		/// The converter, returning from the candle a price for calculations.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ClosingPriceKey,
			Description = LocalizedStrings.ClosingPriceKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public Level1Fields PriceField
		{
			get => _priceField;
			set
			{
				_priceField = value;
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
			_buffer.Clear();
			_zigZagBuffer.Clear();
			CurrentValue = 0;
			base.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = input.GetValue<decimal>(PriceField);
			if (_needAdd)
			{
				_buffer.Add(value);
				_zigZagBuffer.Add(0);
			}
			else
			{
				_buffer[_buffer.Count - 1] = value;
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

			return new ShiftedIndicatorValue(this, lastButOne, valueId - 1);
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
}
