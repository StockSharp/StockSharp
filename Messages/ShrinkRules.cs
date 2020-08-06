#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	/// <summary>
	/// Price rounding rules.
	/// </summary>
	public enum ShrinkRules
	{
		/// <summary>
		/// Automatically to determine rounding to lesser or to bigger value.
		/// </summary>
		Auto,

		/// <summary>
		/// To round to lesser value.
		/// </summary>
		Less,

		/// <summary>
		/// To round to bigger value.
		/// </summary>
		More,
	}
}