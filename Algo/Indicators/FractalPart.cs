#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: FractalPart.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using Ecng.Common;

	/// <summary>
	/// Part <see cref="Fractals"/>.
	/// </summary>
	[Browsable(false)]
	[IndicatorOut(typeof(ShiftedIndicatorValue))]
	public class FractalPart : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FractalPart"/>.
		/// </summary>
		public FractalPart()
		{
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			return input.To<ShiftedIndicatorValue>();
		}
	}
}