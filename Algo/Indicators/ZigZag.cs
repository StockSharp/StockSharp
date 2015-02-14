namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// ЗигЗаг.
	/// </summary>
	/// <remarks>
	/// ZigZag отслеживает и соединяет между собой крайние точки графика отстоящие
	/// друг от друга не менее чем на заданный процент по шкале цены.
	/// </remarks>
	[DisplayName("ZigZag")]
	[DescriptionLoc(LocalizedStrings.Str826Key)]
	public class ZigZag : BaseIndicator<decimal>
	{
		private readonly IList<Candle> _buffer = new List<Candle>();
		private readonly List<decimal> _lowBuffer = new List<decimal>();
		private readonly List<decimal> _highBuffer = new List<decimal>();
		private readonly List<decimal> _zigZagBuffer = new List<decimal>();

		private Func<Candle, decimal> _currentValue = candle => candle.ClosePrice;
		private int _depth;
		private int _backStep;
		private bool _needAdd = true;

		///<summary>
		/// Создать <see cref="ZigZag"/>
		///</summary>
		public ZigZag()
		{
			BackStep = 3;
			Depth = 12;
		}

		///<summary>
		/// Минимальное число баров между локальными максимумами, минимумами.
		///</summary>
		[DisplayNameLoc(LocalizedStrings.Str827Key)]
		[DescriptionLoc(LocalizedStrings.Str828Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int BackStep
		{
			get { return _backStep; }
			set
			{
				if (_backStep == value)
					return;

				_backStep = value;
				Reset();
			}
		}

		///<summary>
		/// Минимум баров, на котором Zigzag не будет строить второй максимум (или минимум),
		/// если тот меньше (или больше) на deviation предыдущего соответственно.
		///</summary>
		[DisplayNameLoc(LocalizedStrings.Str829Key)]
		[DescriptionLoc(LocalizedStrings.Str830Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Depth
		{
			get { return _depth; }
			set
			{
				if (_depth == value)
					return;

				_depth = value;
				Reset();
			}
		}

		private Unit _deviation = new Unit(0.45m, UnitTypes.Percent);
		///<summary>
		/// Минимальное количество пунктов между максимумами (минимумами) двух соседних баров для того
		/// чтобы индикатор Zigzag сформировал локальную вершину (локальный минимум).
		///</summary>
		[DisplayNameLoc(LocalizedStrings.Str831Key)]
		[DescriptionLoc(LocalizedStrings.Str832Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public Unit Deviation
		{
			get { return _deviation; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (_deviation == value)
					return;

				_deviation = value;
				Reset();
			}
		}

		private Func<Candle, decimal> _highValue = candle => candle.HighPrice;
		///<summary>
		/// Конвертер, который возвращает из свечи цену для поиска максимума.
		///</summary>
		[Browsable(false)]
		public Func<Candle, decimal> HighValueFunc
		{
			get { return _highValue; }
			set
			{
				_highValue = value;
				Reset();
			}
		}

		private Func<Candle, decimal> _lowValue = candle => candle.LowPrice;
		///<summary>
		/// Конвертер, который возвращает из свечи цену для поиска минимумв.
		///</summary>
		[Browsable(false)]
		public Func<Candle, decimal> LowValueFunc
		{
			get { return _lowValue; }
			set
			{
				_lowValue = value;
				Reset();
			}
		}

		///<summary>
		/// Конвертер, который возвращает из свечи цену для текущего значения.
		///</summary>
		[Browsable(false)]
		public Func<Candle, decimal> CurrentValueFunc
		{
			get { return _currentValue; }
			set
			{
				_currentValue = value;
				Reset();
			}
		}

		///<summary>
		/// Текущее значение индикатора
		///</summary>
		[Browsable(false)]
		public decimal CurrentValue { get; private set; }

		///<summary>
		/// Смещение для последнего значения индикатора
		///</summary>
		[Browsable(false)]
		public int LastValueShift { get; private set; }

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
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

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

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

			for (var shift = limit; shift >= 0; shift--)
			{
				//--- low
				var val = _buffer.Skip(shift).Take(Depth).Min(v => _lowValue(v));
				if (val == lastLow)
				{
					val = 0.0m;
				}
				else
				{
					lastLow = val;
					if (_lowValue(_buffer[shift]) - val > 0.0m * val / 100)
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
				if (_lowValue(_buffer[shift]) == val)
					_lowBuffer[shift] = val;
				else
					_lowBuffer[shift] = 0m;

				//--- high
				val = _buffer.Skip(shift).Take(Depth).Max(v => _highValue(v));
				if (val == lastHigh)
				{
					val = 0.0m;
				}
				else
				{
					lastHigh = val;
					if (val - _highValue(_buffer[shift]) > 0.0m * val / 100)
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
				if (_highValue(_buffer[shift]) == val)
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

			IsFormed = true;
			LastValueShift = valueId - 1;

			CurrentValue = _currentValue(_buffer[0]);

			return new DecimalIndicatorValue(this, _zigZagBuffer[LastValueShift]);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			BackStep = settings.GetValue<int>("BackStep");
			Depth = settings.GetValue<int>("Depth");
			Deviation.Type = settings.GetValue<UnitTypes>("DeviationType");
			Deviation.Value = settings.GetValue<decimal>("DeviationValue");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("BackStep", BackStep);
			settings.SetValue("Depth", Depth);
			settings.SetValue("DeviationType", Deviation.Type);
			settings.SetValue("DeviationValue", Deviation.Value);
		}
	}
}