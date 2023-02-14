#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: IIndicatorContainer.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;

	/// <summary>
	/// The interface of the container, storing indicator data.
	/// </summary>
	public interface IIndicatorContainer
	{
		/// <summary>
		/// The current number of saved values.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Add new values.
		/// </summary>
		/// <param name="input">The input value of the indicator.</param>
		/// <param name="result">The resulting value of the indicator.</param>
		void AddValue(IIndicatorValue input, IIndicatorValue result);

		/// <summary>
		/// To get all values of the identifier.
		/// </summary>
		/// <returns>All values of the identifier. The empty set, if there are no values.</returns>
		IEnumerable<(IIndicatorValue input, IIndicatorValue output)> GetValues();

		/// <summary>
		/// To get the indicator value by the index.
		/// </summary>
		/// <param name="index">The sequential number of value from the end.</param>
		/// <returns>Input and resulting values of the indicator.</returns>
		(IIndicatorValue input, IIndicatorValue output) GetValue(int index);

		/// <summary>
		/// To delete all values of the indicator.
		/// </summary>
		void ClearValues();
	}
}