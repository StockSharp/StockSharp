namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;

	using StockSharp.Algo.Candles;

	/// <summary>
	/// Средневзвешанная часть индикатора <see cref="RelativeVigorIndex"/>.
	/// </summary>
	public class RelativeVigorIndexAverage : LengthIndicator<decimal>
	{
		private readonly List<Candle> _buffer = new List<Candle>();

		/// <summary>
		/// Создать <see cref="RelativeVigorIndexAverage"/>.
		/// </summary>
		public RelativeVigorIndexAverage()
			: base(typeof(Candle))
		{
			Length = 4;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_buffer.Clear();
			Buffer.Clear();
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<Candle>();

			if (input.IsFinal)
			{
				_buffer.Add(newValue);

				if (_buffer.Count > Length)
					_buffer.RemoveAt(0);
			}

			if (IsFormed)
			{
				decimal valueUp, valueDn;

				if (input.IsFinal)
				{
					valueUp = ((_buffer[0].ClosePrice - _buffer[0].OpenPrice) +
					           2*(_buffer[1].ClosePrice - _buffer[1].OpenPrice) +
					           2*(_buffer[2].ClosePrice - _buffer[2].OpenPrice) +
					           (_buffer[3].ClosePrice - _buffer[3].OpenPrice))/6m;

					valueDn = ((_buffer[0].HighPrice - _buffer[0].LowPrice) +
					           2*(_buffer[1].HighPrice - _buffer[1].LowPrice) +
					           2*(_buffer[2].HighPrice - _buffer[2].LowPrice) +
					           (_buffer[3].HighPrice - _buffer[3].LowPrice))/6m;
				}
				else
				{
					valueUp = ((_buffer[1].ClosePrice - _buffer[1].OpenPrice) +
					           2*(_buffer[2].ClosePrice - _buffer[2].OpenPrice) +
					           2*(_buffer[3].ClosePrice - _buffer[3].OpenPrice) +
							   (newValue.ClosePrice - newValue.OpenPrice)) / 6m;

					valueDn = ((_buffer[1].HighPrice - _buffer[1].LowPrice) +
					           2*(_buffer[2].HighPrice - _buffer[2].LowPrice) +
					           2*(_buffer[3].HighPrice - _buffer[3].LowPrice) +
							   (newValue.HighPrice - newValue.LowPrice)) / 6m;
				}

				return new DecimalIndicatorValue(this, valueDn == decimal.Zero 
					? valueUp 
					: valueUp / valueDn);
			}

			return new DecimalIndicatorValue(this);
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return _buffer.Count >= Length; }
		}
	}
}