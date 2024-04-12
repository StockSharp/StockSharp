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
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Covariance.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/covariation.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.COVKey,
		Description = LocalizedStrings.CovarianceKey)]
	[Doc("topics/api/indicators/list_of_indicators/covariation.html")]
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

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = input.GetValue<Tuple<decimal, decimal>>();

			Tuple<decimal, decimal> first = null;

			if (input.IsFinal)
			{
				Buffer.PushBack(value);
			}
			else
			{
				first = Buffer.Count == Length ? Buffer[0] : default;
				Buffer.PushBack(value);
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
					Buffer.PushFront(first);

				Buffer.PopBack();
			}

			return new DecimalIndicatorValue(this, covariance / len);
		}
	}
}