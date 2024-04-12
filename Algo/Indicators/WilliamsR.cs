#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: WilliamsR.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Williams Percent Range.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/%r.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.WRKey,
		Description = LocalizedStrings.WilliamsRKey)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/%r.html")]
	public class WilliamsR : LengthIndicator<decimal>
	{
		private readonly Lowest _low;
		private readonly Highest _high;

		/// <summary>
		/// Initializes a new instance of the <see cref="WilliamsR"/>.
		/// </summary>
		public WilliamsR()
		{
			_low = new Lowest();
			_high = new Highest();
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

		/// <inheritdoc />
		protected override bool CalcIsFormed() => _low.IsFormed;

		/// <inheritdoc />
		public override void Reset()
		{
			_high.Length = _low.Length = Length;
			base.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			var lowValue = _low.Process(input.SetValue(this, candle.LowPrice)).GetValue<decimal>();
			var highValue = _high.Process(input.SetValue(this, candle.HighPrice)).GetValue<decimal>();

			if ((highValue - lowValue) != 0)
				return new DecimalIndicatorValue(this, -100m * (highValue - candle.ClosePrice) / (highValue - lowValue));
				
			return new DecimalIndicatorValue(this);
		}
	}
}