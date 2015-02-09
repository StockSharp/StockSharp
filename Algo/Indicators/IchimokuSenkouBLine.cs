namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using MoreLinq;

	using StockSharp.Algo.Candles;

	/// <summary>
	/// Линия Senkou Span B.
	/// </summary>
	public class IchimokuSenkouBLine : LengthIndicator<decimal>
	{
		private readonly List<Candle> _buffer = new List<Candle>();

		/// <summary>
		/// Создать <see cref="IchimokuLine"/>.
		/// </summary>
		/// <param name="kijun">Линия Kijun.</param>
		public IchimokuSenkouBLine(IchimokuLine kijun)
			: base(typeof(Candle))
		{
			if (kijun == null)
				throw new ArgumentNullException("kijun");

			Kijun = kijun;
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
			get { return _buffer.Count >= Length && Buffer.Count >= Kijun.Length; }
		}
		//_buffer.Count >= Length &&
		
		/// <summary>
		/// Линия Kijun.
		/// </summary>
		[Browsable(false)]
		public IchimokuLine Kijun { get; private set; }

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();
			
			decimal? result = null;
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

			if (buff.Count >= Length)
			{
				// рассчитываем значение
				var max = buff.Max(t => t.HighPrice);
				var min = buff.Min(t => t.LowPrice);

				if (Kijun.IsFormed && input.IsFinal)
				    Buffer.Add((max + min) / 2);

				if (Buffer.Count >= Kijun.Length)
					result = Buffer[0];

				if (Buffer.Count > Kijun.Length)
				{
					Buffer.RemoveAt(0);
				}
			}

			return result == null ? new DecimalIndicatorValue(this) : new DecimalIndicatorValue(this, result.Value);
		}
	}
}
