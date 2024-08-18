namespace StockSharp.Algo.Indicators
{
	using Ecng.ComponentModel;

	/// <summary>
	/// QStick.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/qstick.html
	/// </remarks>
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/qstick.html")]
	public class QStick : LengthIndicator<IIndicatorValue>
	{
		private readonly SimpleMovingAverage _sma;

		/// <summary>
		/// Initializes a new instance of the <see cref="QStick"/>.
		/// </summary>
		public QStick()
		{
			_sma = new SimpleMovingAverage();
			Length = 15;
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

		/// <inheritdoc />
		protected override bool CalcIsFormed() => _sma.IsFormed;

		/// <inheritdoc />
		public override void Reset()
		{
			_sma.Length = Length;
			base.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var (open, _, _, close) = input.GetOhlc();

			var val = _sma.Process(input.SetValue(this, open - close));
			return val.SetValue(this, val.GetValue<decimal>());
		}
	}
}
