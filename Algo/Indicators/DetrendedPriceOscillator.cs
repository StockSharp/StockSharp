#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: DetrendedPriceOscillator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Price oscillator without trend.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/dpo.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DPOKey,
		Description = LocalizedStrings.DetrendedPriceOscillatorKey)]
	[Doc("topics/api/indicators/list_of_indicators/dpo.html")]
	public class DetrendedPriceOscillator : LengthIndicator<decimal>
	{
		private readonly SimpleMovingAverage _sma;
		private int _lookBack;

		/// <summary>
		/// Initializes a new instance of the <see cref="DetrendedPriceOscillator"/>.
		/// </summary>
		public DetrendedPriceOscillator()
		{
			_sma = new SimpleMovingAverage();
			Length = 3;
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

		/// <inheritdoc />
		public override void Reset()
		{
			_sma.Length = Length;
			_lookBack = Length / 2 + 1;
			base.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var smaValue = _sma.Process(input);

			if (_sma.IsFormed && input.IsFinal)
				Buffer.PushBack(smaValue.GetValue<decimal>());

			if (!IsFormed)
				return new DecimalIndicatorValue(this);

			return new DecimalIndicatorValue(this, input.GetValue<decimal>() - Buffer[Math.Max(0, Buffer.Count - 1 - _lookBack)]);
		}
	}
}