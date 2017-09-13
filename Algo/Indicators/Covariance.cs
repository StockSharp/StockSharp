#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Covariance.cs
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
	/// Covariance.
	/// </summary>
	/// <remarks>
	/// https://en.wikipedia.org/wiki/Covariance.
	/// </remarks>
	[DisplayName("COV")]
	[DescriptionLoc(LocalizedStrings.CovarianceKey, true)]
	[IndicatorIn(typeof(PairIndicatorValue<decimal>))]
	public class Covariance : LengthIndicator<Tuple<decimal, decimal>>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Covariance"/>.
		/// </summary>
		public Covariance()
		{
			Length = 20;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = input.GetValue<Tuple<decimal, decimal>>();

			Buffer.Add(value);

			Tuple<decimal, decimal> first = null;

			if (input.IsFinal)
			{
				if (Buffer.Count > Length)
					Buffer.RemoveAt(0);
			}
			else
			{
				if (Buffer.Count > Length)
				{
					first = Buffer[0];
					Buffer.RemoveAt(0);
				}
			}

			decimal avgSource = 0;
			decimal avgOther = 0;

			foreach (var tuple in Buffer)
			{
				avgSource += tuple.Item1;
				avgOther += tuple.Item2;
			}

			var len = Buffer.Count;

			avgSource /= len;
			avgOther /= len;

			var covariance = 0m;

			foreach (var tuple in Buffer)
			{
				covariance += (tuple.Item1 - avgSource) * (tuple.Item2 - avgOther);
			}

			if (!input.IsFinal)
			{
				if (first != null)
					Buffer.Insert(0, first);

				Buffer.RemoveAt(len - 1);
			}

			return new DecimalIndicatorValue(this, covariance / len);
		}
	}
}