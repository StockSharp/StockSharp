#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.InteractiveBrokers.InteractiveBrokers
File: FundamentalReports.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.InteractiveBrokers
{
	/// <summary>
	/// Financial reports types.
	/// </summary>
	public enum FundamentalReports
	{
		/// <summary>
		/// Overview.
		/// </summary>
		Overview,

		/// <summary>
		/// Statements.
		/// </summary>
		Statements,
		
		/// <summary>
		/// Summary.
		/// </summary>
		Summary,

		/// <summary>
		/// Ratio.
		/// </summary>
		Ratio,

		/// <summary>
		/// Estimates.
		/// </summary>
		Estimates,
		
		/// <summary>
		/// Chart.
		/// </summary>
		Calendar,
	}
}