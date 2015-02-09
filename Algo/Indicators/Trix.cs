namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Тройная экспоненциальная скользящая средняя.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/TRIX.ashx
	/// http://www.incrediblecharts.com/indicators/trix_indicator.php
	/// </remarks>
	[DisplayName("Trix")]
	[DescriptionLoc(LocalizedStrings.Str752Key)]
	public class Trix : LengthIndicator<IIndicatorValue>
	{
		private readonly ExponentialMovingAverage _ema1;
		private readonly ExponentialMovingAverage _ema2;
		private readonly ExponentialMovingAverage _ema3;
		private readonly RateOfChange _roc;

		/// <summary>
		/// Создать <see cref="VolumeWeightedMovingAverage"/>.
		/// </summary>
		public Trix()
		{
			_ema1 = new ExponentialMovingAverage();
			_ema2 = new ExponentialMovingAverage();
			_ema3 = new ExponentialMovingAverage();
			_roc = new RateOfChange { Length = 1 };
		}

		/// <summary>
		/// Длина периода <see cref="RateOfChange"/>.
		/// </summary>
		[DisplayName("ROC")]
		[DescriptionLoc(LocalizedStrings.Str753Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int RocLength
		{
			get { return _roc.Length; }
			set { _roc.Length = value; }
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_ema3.Length = _ema2.Length = _ema1.Length = Length;
			_roc.Reset();
			base.Reset();
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return _ema1.IsFormed && _ema2.IsFormed && _ema3.IsFormed && _roc.IsFormed; }
		}

		/// <summary>
		/// Возможно ли обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns><see langword="true"/>, если возможно, иначе, <see langword="false"/>.</returns>
		public override bool CanProcess(IIndicatorValue input)
		{
			return _ema1.CanProcess(input) && _ema2.CanProcess(input) && _ema3.CanProcess(input) && _roc.CanProcess(input);
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var ema1Value = _ema1.Process(input);

			if (_ema1.IsFormed)
			{
				var ema2Value = _ema2.Process(ema1Value);

				if (_ema2.IsFormed)
				{
					var ema3Value = _ema3.Process(ema2Value);

					if (_ema3.IsFormed)
					{
						return _roc.Process(ema3Value);
					}
				}
			}

			return input;
		}
	}
}