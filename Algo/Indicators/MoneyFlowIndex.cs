namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Money Flow Index.
	/// </summary>
	[DisplayName("MFI")]
	[DescriptionLoc(LocalizedStrings.MoneyFlowIndexKey)]
	public class MoneyFlowIndex : LengthIndicator<decimal>
	{
		private decimal _previousPrice;
		private readonly Sum _positiveFlow = new Sum();
		private readonly Sum _negativeFlow = new Sum();

		/// <summary>
		/// Initializes a new instance of the <see cref="MoneyFlowIndex"/>.
		/// </summary>
		public MoneyFlowIndex()
		{
		    Length = 14;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="MoneyFlowIndex"/> using a specified length.
		/// </summary>
		public MoneyFlowIndex(int length)
		{
		    Length = length;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_positiveFlow.Length = _negativeFlow.Length = Length;
			_previousPrice = 0;
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _positiveFlow.IsFormed && _negativeFlow.IsFormed;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3.0m;
			var moneyFlow = typicalPrice * candle.TotalVolume;
			
			var positiveFlow = _positiveFlow.Process(input.SetValue(this, typicalPrice > _previousPrice ? moneyFlow : 0.0m)).GetValue<decimal>();
			var negativeFlow = _negativeFlow.Process(input.SetValue(this, typicalPrice < _previousPrice ? moneyFlow : 0.0m)).GetValue<decimal>();

			_previousPrice = typicalPrice;
			
			if (negativeFlow == 0)
				return new DecimalIndicatorValue(this, 100m);
			
			if (positiveFlow / negativeFlow == 1)
				return new DecimalIndicatorValue(this, 0m);

			return negativeFlow != 0 
				? new DecimalIndicatorValue(this, 100m - 100m / (1m + positiveFlow / negativeFlow))
				: new DecimalIndicatorValue(this);
		}
	}
}
