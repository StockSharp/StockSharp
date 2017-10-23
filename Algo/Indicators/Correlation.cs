#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Correlation.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Correlation.
	/// </summary>
	/// <remarks>
	/// https://en.wikipedia.org/wiki/Correlation_and_dependence.
	/// </remarks>
	[DisplayName("COR")]
	[DescriptionLoc(LocalizedStrings.CorrelationKey, true)]
	[IndicatorIn(typeof(PairIndicatorValue<decimal>))]
	public class Correlation : Covariance
	{
		private readonly StandardDeviation _source;
		private readonly StandardDeviation _other;

		/// <summary>
		/// Initializes a new instance of the <see cref="Correlation"/>.
		/// </summary>
		public Correlation()
		{
			_source = new StandardDeviation();
			_other = new StandardDeviation();

			Length = 20;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			if (_source != null)
				_source.Length = Length;

			if (_other != null)
				_other.Length = Length;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var cov = base.OnProcess(input);

			var value = input.GetValue<Tuple<decimal, decimal>>();

			var sourceDev = _source.Process(value.Item1);
			var otherDev = _other.Process(value.Item2);

			var v = sourceDev.GetValue<decimal>() * otherDev.GetValue<decimal>();

			if (v != 0)
				v = cov.GetValue<decimal>() / v;

			return new DecimalIndicatorValue(this, v);
		}
	}
}