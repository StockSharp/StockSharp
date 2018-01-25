#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Fractals.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Fractals.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/fractal.
	/// </remarks>
	[DisplayName("Fractals")]
	[DescriptionLoc(LocalizedStrings.Str844Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class Fractals : BaseComplexIndicator
	{
		private readonly List<Candle> _buffer = new List<Candle>();

		// Номер центральной свечи
		private int _numCenter;

		// Флаг роста low'a до буфера
		private bool _wasLowUp;

		// Флаг снижения high'я до буфера
		private bool _wasHighDown;

		/// <summary>
		/// Initializes a new instance of the <see cref="Fractals"/>.
		/// </summary>
		public Fractals()
			: this(5, new FractalPart { Name = "Up" }, new FractalPart { Name = "Down" })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Fractals"/>.
		/// </summary>
		/// <param name="length">Period length.</param>
		/// <param name="up">Fractal up.</param>
		/// <param name="down">Fractal down.</param>
		public Fractals(int length, FractalPart up, FractalPart down)
			: base(up, down)
		{
			if (length % 2 == 0)
			{
				throw new ArgumentOutOfRangeException(nameof(length), length, LocalizedStrings.Str845);
			}

			_length = length;
			_numCenter = length / 2;
			Up = up;
			Down = down;
		}

		private int _length;

		/// <summary>
		/// Period length.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str736Key)]
		[DescriptionLoc(LocalizedStrings.Str778Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Length
		{
			get => _length;
			set
			{
				_length = value;
				_numCenter = value / 2;
				Reset();
			}
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _buffer.Count >= Length && base.IsFormed;

		/// <summary>
		/// Fractal up.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayNameLoc(LocalizedStrings.Str846Key)]
		[DescriptionLoc(LocalizedStrings.Str846Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public FractalPart Up { get; }

		/// <summary>
		/// Fractal down.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayNameLoc(LocalizedStrings.Str848Key)]
		[DescriptionLoc(LocalizedStrings.Str848Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public FractalPart Down { get; }

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_buffer.Clear();
			base.Reset();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();
			var buffer = input.IsFinal ? _buffer : _buffer.ToList();

			buffer.Add(candle);

			// если буфер стал достаточно большим (стал больше длины)
			if (buffer.Count > Length)
			{
				// Запоминаем интересующие изменения свечек до буфера
				if (buffer[0].HighPrice > buffer[1].HighPrice)
					_wasHighDown = true;
				else if (buffer[0].HighPrice < buffer[1].HighPrice)
					_wasHighDown = false;

				if (buffer[0].LowPrice < buffer[1].LowPrice)
					_wasLowUp = true;
				else if (buffer[0].LowPrice > buffer[1].LowPrice)
					_wasLowUp = false;

				buffer.RemoveAt(0);
			}

			// Логика расчета: последующие/предыдущие значения должны быть меньше/больше центрального значения
			if (buffer.Count >= Length)
			{
				// флаги для расчета фракталов (если флаг равен false, то на центральной свече нет фрактала)
				var isMax = true;
				var isMin = true;

				var centerHighPrice = buffer[_numCenter].HighPrice;
				var centerLowPrice = buffer[_numCenter].LowPrice;

				for (var i = 0; i < buffer.Count; i++)
				{
					if (i == _numCenter)
						continue;

					// Все значения до и после центральной свечи должны быть
					// больше для фрактала вверх и меньше для фрактала вниз
					if (buffer[i].HighPrice > centerHighPrice)
					{
						isMax = false;
					}

					if (buffer[i].LowPrice < centerLowPrice)
					{
						isMin = false;
					}
				}

				// Фильтр для ситуаций где цена у экстремума одинакова за период больше длины буфера
				if (isMax && _wasHighDown && buffer.GetRange(0, _numCenter).Min(i => i.HighPrice) == centerHighPrice)
				{
					isMax = false;
				}
					
				if (isMin && _wasLowUp && buffer.GetRange(0, _numCenter).Max(i => i.LowPrice) == centerLowPrice)
				{
					isMin = false;
				}

				var shift = buffer.Count - _numCenter - 1;

				var upValue = isMax ? new ShiftedIndicatorValue(this, shift, new DecimalIndicatorValue(this, centerHighPrice)) : new ShiftedIndicatorValue(this);
				var downValue = isMin ? new ShiftedIndicatorValue(this, shift, new DecimalIndicatorValue(this, centerLowPrice)) : new ShiftedIndicatorValue(this);

				upValue.IsFinal = input.IsFinal;
				downValue.IsFinal = input.IsFinal;

				var complexValue = new ComplexIndicatorValue(this);
				complexValue.InnerValues.Add(Up, Up.Process(upValue));
				complexValue.InnerValues.Add(Down, Down.Process(downValue));

				return complexValue;
			}

			return base.OnProcess(new ShiftedIndicatorValue(this, 0, input.SetValue(this, 0)));
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Length = settings.GetValue<int>(nameof(Length));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue(nameof(Length), Length);
		}
	}
}