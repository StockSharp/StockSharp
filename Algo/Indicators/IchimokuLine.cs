namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.Linq;

	using MoreLinq;

	using StockSharp.Algo.Candles;

	/// <summary>
	/// Реализация одной из линий индикатора Ишимоку Кинко Хайо (Tenkan, Kijun, Senkou Span B).
	/// </summary>
	public class IchimokuLine : LengthIndicator<decimal>
	{
		private readonly List<Candle> _buffer = new List<Candle>();

		/// <summary>
		/// Создать <see cref="IchimokuLine"/>.
		/// </summary>
		public IchimokuLine()
			: base(typeof(Candle))
		{
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			_buffer.Clear();
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return _buffer.Count >= Length; }
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();
			var buff = _buffer;

			if (input.IsFinal)
			{
				_buffer.Add(candle);

				// если буффер стал достаточно большим (стал больше длины)
				if (_buffer.Count > Length)
					_buffer.RemoveAt(0);
			}
			else
				buff = _buffer.Skip(1).Concat(candle).ToList();

			if (IsFormed)
			{
				// рассчитываем значение
				var max = buff.Max(t => t.HighPrice);
				var min = buff.Min(t => t.LowPrice);

				return new DecimalIndicatorValue(this, (max + min) / 2);
			}
				
			return new DecimalIndicatorValue(this);
		}
	}
}