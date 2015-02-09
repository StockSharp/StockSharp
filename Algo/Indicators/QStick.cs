namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// QStick.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/QStick.ashx
	/// </remarks>
	public class QStick : LengthIndicator<IIndicatorValue>
	{
		private readonly SimpleMovingAverage _sma;

		/// <summary>
		/// —оздать <see cref="QStick"/>.
		/// </summary>
		public QStick()
			: base(typeof(Candle))
		{
			_sma = new SimpleMovingAverage();
			Length = 15;
		}

		/// <summary>
		/// —формирован ли индикатор.
		/// </summary>
		public override bool IsFormed { get { return _sma.IsFormed; } }

		/// <summary>
		/// —бросить состо€ние индикатора на первоначальное. ћетод вызываетс€ каждый раз, когда мен€ютс€ первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_sma.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// ќбработать входное значение.
		/// </summary>
		/// <param name="input">¬ходное значение.</param>
		/// <returns>–езультирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();
			return _sma.Process(input.SetValue(this, candle.OpenPrice - candle.ClosePrice));
		}
	}
}