namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Commodity Channel Index.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/cci.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CCIKey,
		Description = LocalizedStrings.CommodityChannelIndexKey)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/cci.html")]
	public class CommodityChannelIndex : LengthIndicator<decimal>
	{
		private readonly MeanDeviation _mean = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="CommodityChannelIndex"/>.
		/// </summary>
		public CommodityChannelIndex()
		{
			Length = 15;
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

		/// <inheritdoc />
		public override void Reset()
		{
			_mean.Length = Length;
			base.Reset();
		}

		/// <inheritdoc />
		protected override bool CalcIsFormed() => _mean.IsFormed;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var (_, high, low, close) = input.GetOhlc();

			var aveP = (high + low + close) / 3m;

			var meanValue = _mean.Process(new DecimalIndicatorValue(this, aveP) { IsFinal = input.IsFinal });

			if (IsFormed && meanValue.GetValue<decimal>() != 0)
				return new DecimalIndicatorValue(this, ((aveP - _mean.Sma.GetCurrentValue()) / (0.015m * meanValue.GetValue<decimal>())));

			return new DecimalIndicatorValue(this);
		}
	}
}