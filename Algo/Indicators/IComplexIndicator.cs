#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: IComplexIndicator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;

	/// <summary>
	/// The interface of indicator, built as combination of several indicators.
	/// </summary>
	public interface IComplexIndicator : IIndicator
	{
		/// <summary>
		/// Embedded indicators.
		/// </summary>
		IEnumerable<IIndicator> InnerIndicators { get; }
	}
}