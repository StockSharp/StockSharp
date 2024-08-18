namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// The average true range <see cref="Indicators.TrueRange"/>.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/atr.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ATRKey,
		Description = LocalizedStrings.AverageTrueRangeKey)]
	[Doc("topics/api/indicators/list_of_indicators/atr.html")]
	public class AverageTrueRange : LengthIndicator<IIndicatorValue>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AverageTrueRange"/>.
		/// </summary>
		public AverageTrueRange()
			: this(new WilderMovingAverage(), new TrueRange())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AverageTrueRange"/>.
		/// </summary>
		/// <param name="movingAverage">Moving Average.</param>
		/// <param name="trueRange">True range.</param>
		public AverageTrueRange(LengthIndicator<decimal> movingAverage, TrueRange trueRange)
		{
			MovingAverage = movingAverage ?? throw new ArgumentNullException(nameof(movingAverage));
			TrueRange = trueRange ?? throw new ArgumentNullException(nameof(trueRange));
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

		/// <summary>
		/// Moving Average.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> MovingAverage { get; }

		/// <summary>
		/// True range.
		/// </summary>
		[Browsable(false)]
		public TrueRange TrueRange { get; }

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();

			MovingAverage.Length = Length;
			TrueRange.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			// используем дополнительную переменную IsFormed,
			// т.к. нужна задержка в один период для корректной инициализации скользящей средней
			if (MovingAverage.IsFormed)
				IsFormed = true;

			var val = MovingAverage.Process(TrueRange.Process(input));
			return val.SetValue(this, val.GetValue<decimal>());
		}
	}
}
