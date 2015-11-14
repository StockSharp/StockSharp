namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Price oscillator without trend.
	/// </summary>
	[DisplayName("DPO")]
	[DescriptionLoc(LocalizedStrings.Str761Key)]
	public class DetrendedPriceOscillator : LengthIndicator<decimal>
	{
		private readonly SimpleMovingAverage _sma;

		/// <summary>
		/// Initializes a new instance of the <see cref="DetrendedPriceOscillator"/>.
		/// </summary>
		public DetrendedPriceOscillator()
		{
			_sma = new SimpleMovingAverage();
			Length = 3;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_sma.Length = (Length - 2) * 2;
			base.Reset();
		}

		/// <summary>
		/// The indicator is formed.
		/// </summary>
		public override bool IsFormed => Buffer.Count >= Length;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var smaValue = _sma.Process(input);

			if (_sma.IsFormed && input.IsFinal)
				Buffer.Add(smaValue.GetValue<decimal>());

			if (!IsFormed)
				return new DecimalIndicatorValue(this);

			if (Buffer.Count > Length)
				Buffer.RemoveAt(0);

			return new DecimalIndicatorValue(this, input.GetValue<decimal>() - Buffer[0]);
		}
	}
}