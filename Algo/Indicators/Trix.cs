#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Trix.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Triple Exponential Moving Average.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/IndicatorTrix.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TrixKey,
		Description = LocalizedStrings.TripleExponentialMovingAverageKey)]
	[Doc("topics/IndicatorTrix.html")]
	public class Trix : LengthIndicator<IIndicatorValue>
	{
		private readonly ExponentialMovingAverage _ema1;
		private readonly ExponentialMovingAverage _ema2;
		private readonly ExponentialMovingAverage _ema3;
		private readonly RateOfChange _roc;

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeWeightedMovingAverage"/>.
		/// </summary>
		public Trix()
		{
			_ema1 = new ExponentialMovingAverage();
			_ema2 = new ExponentialMovingAverage();
			_ema3 = new ExponentialMovingAverage();
			_roc = new RateOfChange { Length = 1 };
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

		/// <summary>
		/// The length of period <see cref="RateOfChange"/>.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ROCKey,
			Description = LocalizedStrings.RocLengthKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public int RocLength
		{
			get => _roc.Length;
			set => _roc.Length = value;
		}

		/// <inheritdoc />
		public override void Reset()
		{
			_ema3.Length = _ema2.Length = _ema1.Length = Length;
			_roc.Reset();
			base.Reset();
		}

		/// <inheritdoc />
		protected override bool CalcIsFormed() => _ema1.IsFormed && _ema2.IsFormed && _ema3.IsFormed && _roc.IsFormed;

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

			return _ema3.IsFormed ?
				new DecimalIndicatorValue(this, 10m * _roc.Process(ema3Value).GetValue<decimal>()) :
				new DecimalIndicatorValue(this);
		}
	}
}