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

		/// <inheritdoc />
		public override void Reset()
		{
			_sma.Length = (Length - 2) * 2;
			base.Reset();
		}

		/// <inheritdoc />
		public override bool IsFormed => Buffer.Count >= Length;

		/// <inheritdoc />
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