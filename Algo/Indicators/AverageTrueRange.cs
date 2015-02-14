namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Средний истинный диапазон <see cref="TrueRange"/>.
	/// </summary>
	[DisplayName("ATR")]
	[DescriptionLoc(LocalizedStrings.Str758Key)]
	public class AverageTrueRange : LengthIndicator<IIndicatorValue>
	{
		private bool _isFormed;

		/// <summary>
		/// Создать <see cref="AverageTrueRange"/>.
		/// </summary>
		public AverageTrueRange()
			: this(new WilderMovingAverage(), new TrueRange())
		{
		}

		/// <summary>
		/// Создать <see cref="AverageTrueRange"/>.
		/// </summary>
		/// <param name="movingAverage">Скользящая средняя.</param>
		/// <param name="trueRange">Истинный диапазон.</param>
		public AverageTrueRange(LengthIndicator<decimal> movingAverage, TrueRange trueRange)
		{
			if (movingAverage == null)
				throw new ArgumentNullException("movingAverage");

			if (trueRange == null)
				throw new ArgumentNullException("trueRange");

			MovingAverage = movingAverage;
			TrueRange = trueRange;
		}

		/// <summary>
		/// Скользящая средняя.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> MovingAverage { get; private set; }

		/// <summary>
		/// Истинный диапазон.
		/// </summary>
		[Browsable(false)]
		public TrueRange TrueRange { get; private set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return _isFormed; }
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_isFormed = false;

			MovingAverage.Length = Length;
			TrueRange.Reset();
		}

		/// <summary>
		/// Возможно ли обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns><see langword="true"/>, если возможно, иначе, <see langword="false"/>.</returns>
		public override bool CanProcess(IIndicatorValue input)
		{
			return TrueRange.CanProcess(input) && MovingAverage.CanProcess(input);
		}
		
		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			// используем дополнительную переменную IsFormed, 
			// т.к. нужна задержка в один период для корректной инициализации скользящей средней
			_isFormed = MovingAverage.IsFormed;

			return MovingAverage.Process(TrueRange.Process(input));
		}
	}
}