namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// Часть индикатора <see cref="DirectionalIndex"/>.
	/// </summary>
	public abstract class DiPart : LengthIndicator<decimal>
	{
		private readonly AverageTrueRange _averageTrueRange;
		private readonly LengthIndicator<decimal> _movingAverage;
		private Candle _lastCandle;
		private bool _isFormed;

		/// <summary>
		/// Инициализировать <see cref="DiPart"/>.
		/// </summary>
		protected DiPart()
		{
			_averageTrueRange = new AverageTrueRange(new WilderMovingAverage(), new TrueRange());
			_movingAverage = new WilderMovingAverage();

			Length = 5;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_averageTrueRange.Length = Length;
			_movingAverage.Length = Length;

			_lastCandle = null;
			_isFormed = false;
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return _isFormed; }
		}

		/// <summary>
		/// Возможно ли обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns><see langword="true"/>, если возможно, иначе, <see langword="false"/>.</returns>
		public override bool CanProcess(IIndicatorValue input)
		{
			return _movingAverage.CanProcess(input) && _averageTrueRange.CanProcess(input);
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			decimal? result = null;

			var candle = input.GetValue<Candle>();

			// задержка в 1 период
			_isFormed = _averageTrueRange.IsFormed && _movingAverage.IsFormed;

			_averageTrueRange.Process(input);

			if (_lastCandle != null)
			{
				var trValue = _averageTrueRange.GetCurrentValue<decimal>();

				// не вносить в тернарный оператор! 
				var maValue = _movingAverage.Process(new DecimalIndicatorValue(this, GetValue(candle, _lastCandle)) { IsFinal = input.IsFinal });

				if (!maValue.IsEmpty)
					result = (trValue != 0m) ? (100m * maValue.GetValue<decimal>() / trValue) : 0m;
			}

			if (input.IsFinal)
				_lastCandle = candle;

			return result == null ? new DecimalIndicatorValue(this) : new DecimalIndicatorValue(this, result.Value);
		}

		/// <summary>
		/// Получить значение части.
		/// </summary>
		/// <param name="current">Текущая свеча.</param>
		/// <param name="prev">Предыдущая свеча.</param>
		/// <returns>Значение.</returns>
		protected abstract decimal GetValue(Candle current, Candle prev);
	}
}