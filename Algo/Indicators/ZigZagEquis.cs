namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;

	using StockSharp.Localization;

	///<summary>
	/// ЗигЗаг (Metastock).
	///</summary>
	/// <remarks>
	/// Индикатор Зиг-Заг (Zig Zag) фильтрует колебания цен или значений индикаторов, которые не выходят за определенную величину
	/// выраженную в % или абсолютных числах. Это делается для предварительного анализа графика на котором акцентированы только 
	/// достаточно большие изменения цен (значений индикатора).
	/// </remarks>
	[DisplayName("ZigZag Metastock")]
	[DescriptionLoc(LocalizedStrings.Str826Key)]
	public class ZigZagEquis : BaseIndicator<ShiftedIndicatorValue>
	{
		private readonly IList<decimal> _buffer = new List<decimal>();
		private readonly List<decimal> _zigZagBuffer = new List<decimal>();

		private bool _needAdd = true;

		/// <summary>
		/// Создать <see cref="ZigZagEquis"/>.
		/// </summary>
		public ZigZagEquis()
		{
		}

		private decimal _deviation = 0.45m * 0.01m;

		///<summary>
		/// Процент изменения.
		///</summary>
		/// <remarks>Указывается в диапазоне от 0 до 1</remarks>
		[DisplayNameLoc(LocalizedStrings.Str833Key)]
		[DescriptionLoc(LocalizedStrings.Str834Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Deviation
		{
			get { return _deviation; }
			set
			{
				if (value == 0)
					throw new ArgumentOutOfRangeException("value");

				if (_deviation == value)
					return;

				_deviation = value;
				Reset();
			}
		}

		private Func<Candle, decimal> _byPrice = candle => candle.ClosePrice;

		///<summary>
		/// Конвертер, который возвращает из свечи цену для расчетов.
		///</summary>
		[Browsable(false)]
		public Func<Candle, decimal> ByPrice
		{
			get { return _byPrice; }
			set
			{
				_byPrice = value;
				Reset();
			}
		}

		///<summary>
		/// Текущее значение индикатора.
		///</summary>
		[Browsable(false)]
		public decimal CurrentValue { get; private set; }

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
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
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
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
				return Container.Count > 1 ? this.GetCurrentValue() : new ShiftedIndicatorValue(this);

			IsFormed = true;
			CurrentValue = last;

			return new ShiftedIndicatorValue(this, valueId - 1, input.SetValue(this, lastButOne));
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Deviation = settings.GetValue<decimal>("Deviation");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Deviation", Deviation);
		}
	}
}