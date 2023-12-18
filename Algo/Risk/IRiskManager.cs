#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Risk.Algo
File: IRiskManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Risk
{
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing risks control manager.
	/// </summary>
	public interface IRiskManager : ILogSource, IPersistable
	{
		/// <summary>
		/// Rule list.
		/// </summary>
		INotifyList<IRiskRule> Rules { get; }

		/// <summary>
		/// To reset the state.
		/// </summary>
		void Reset();

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns>List of rules, activated by the message.</returns>
		IEnumerable<IRiskRule> ProcessRules(Message message);
	}
}