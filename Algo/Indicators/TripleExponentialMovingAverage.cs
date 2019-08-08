#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: TripleExponentialMovingAverage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Triple Exponential Moving Average.
	/// </summary>
	/// <remarks>
	/// http://tradingsim.com/blog/triple-exponential-moving-average/ (3 * EMA) – (3 * EMA of EMA) + EMA of EMA of EMA).
	/// </remarks>
	[DisplayName("TEMA")]
	[DescriptionLoc(LocalizedStrings.Str752Key)]
	public class TripleExponentialMovingAverage : LengthIndicator<decimal>
	{
		// http://www2.wealth-lab.com/WL5Wiki/GetFile.aspx?File=%2fTEMA.cs&Provider=ScrewTurn.Wiki.FilesStorageProvider

		private readonly ExponentialMovingAverage _ema1;
		private readonly ExponentialMovingAverage _ema2;
		private readonly ExponentialMovingAverage _ema3;

		/// <summary>
		/// Initializes a new instance of the <see cref="TripleExponentialMovingAverage"/>.
		/// </summary>
		public TripleExponentialMovingAverage()
		{
			_ema1 = new ExponentialMovingAverage();
			_ema2 = new ExponentialMovingAverage();
			_ema3 = new ExponentialMovingAverage();

			Length = 32;
		}

		/// <inheritdoc />
		public override bool IsFormed => _ema1.IsFormed && _ema2.IsFormed && _ema3.IsFormed;

		/// <inheritdoc />
		public override void Reset()
		{
			_ema3.Length = _ema2.Length = _ema1.Length = Length;
			base.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var ema1Value = _ema1.Process(input);

			if (!_ema1.IsFormed)
				return new DecimalIndicatorValue(this);

			var ema2Value = _ema2.Process(ema1Value);

			if (!_ema2.IsFormed)
				return new DecimalIndicatorValue(this);

			var ema3Value = _ema3.Process(ema2Value);

			return new DecimalIndicatorValue(this, 3 * ema1Value.GetValue<decimal>() - 3 * ema2Value.GetValue<decimal>() + ema3Value.GetValue<decimal>());
		}
	}
}