namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Фракталы.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/fractal
	/// </remarks>
	[DisplayName("Fractals")]
	[DescriptionLoc(LocalizedStrings.Str844Key)]
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
		/// Создать <see cref="Fractals"/>.
		/// </summary>
		public Fractals()
			: this(5, new FractalPart { Name = "Up" }, new FractalPart { Name = "Down" })
		{
		}

		/// <summary>
		/// Создать <see cref="Fractals"/>.
		/// </summary>
		/// <param name="length">Длина периода.</param>
		/// <param name="up">Фрактал вверх.</param>
		/// <param name="down">Фрактал вниз.</param>
		public Fractals(int length, FractalPart up, FractalPart down)
			: base(up, down)
		{
			if (length % 2 == 0)
			{
				throw new ArgumentOutOfRangeException("length", length, LocalizedStrings.Str845);
			}

			_length = length;
			_numCenter = length / 2;
			Up = up;
			Down = down;
		}

		private int _length;

		/// <summary>
		/// Длина периода.
		/// </summary>
		public int Length
		{
			get { return _length; }
			set
			{
				_length = value;
				_numCenter = value / 2;
				Reset();
			}
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return _buffer.Count >= Length && base.IsFormed; }
		}

		/// <summary>
		/// Фрактал вверх.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str846Key)]
		[DescriptionLoc(LocalizedStrings.Str847Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public FractalPart Up { get; private set; }

		/// <summary>
		/// Фрактал вниз.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str848Key)]
		[DescriptionLoc(LocalizedStrings.Str849Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public FractalPart Down { get; private set; }

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_buffer.Clear();
			base.Reset();
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
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
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Length = settings.GetValue<int>("Length");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Length", Length);
		}
	}
}