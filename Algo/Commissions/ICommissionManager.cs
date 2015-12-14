#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Commissions.Algo
File: ICommissionManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Commissions
{
	using Ecng.Collections;

	/// <summary>
	/// The commission calculating manager interface.
	/// </summary>
	public interface ICommissionManager : ICommissionRule
	{
		/// <summary>
		/// The list of commission calculating rules.
		/// </summary>
		ISynchronizedCollection<ICommissionRule> Rules { get; }
	}
}