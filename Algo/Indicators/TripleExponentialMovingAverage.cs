namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Тройная экспоненциальная скользящая средняя.
	/// </summary>
	/// <remarks>
	/// http://tradingsim.com/blog/triple-exponential-moving-average/
	/// (3 * EMA) – (3 * EMA of EMA) + EMA of EMA of EMA)
	/// </remarks>
	[DisplayName("TEMA")]
	[DescriptionLoc(LocalizedStrings.Str752Key)]
	public class TripleExponentialMovingAverage : LengthIndicator<decimal>
	{
		// http://www2.wealth-lab.com/WL5Wiki/GetFile.aspx?File=%2fTEMA.cs&Provider=ScrewTurn.Wiki.FilesStorageProvider

		private readonly ExponentialMovingAverage _ema1;
		private readonly ExponentialMovingAverage _ema2;
		private readonly ExponentialMovingAverage _ema3;

		/// <summary>
		/// Создать <see cref="TripleExponentialMovingAverage"/>.
		/// </summary>
		public TripleExponentialMovingAverage()
		{
			_ema1 = new ExponentialMovingAverage();
			_ema2 = new ExponentialMovingAverage();
			_ema3 = new ExponentialMovingAverage();

			Length = 32;
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return _ema1.IsFormed && _ema2.IsFormed && _ema3.IsFormed; }
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_ema3.Length = _ema2.Length = _ema1.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// Возможно ли обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns><see langword="true"/>, если возможно, иначе, <see langword="false"/>.</returns>
		public override bool CanProcess(IIndicatorValue input)
		{
			return _ema1.CanProcess(input) && _ema2.CanProcess(input) && _ema3.CanProcess(input);
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var ema1Value = _ema1.Process(input);

			if (!_ema1.IsFormed)
				return new DecimalIndicatorValue(this);

			var ema2Value = _ema2.Process(ema1Value);

			if (!_ema2.IsFormed)
				return new DecimalIndicatorValue(this);

			var ema3Value = _ema3.Process(ema2Value);

			return new DecimalIndicatorValue(this, 3 * ema1Value.GetValue<decimal>() - 3 * ema2Value.GetValue<decimal>() + ema3Value.GetValue<decimal>());
		}
	}
}