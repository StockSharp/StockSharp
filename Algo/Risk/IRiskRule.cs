#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Risk.Algo
File: IRiskRule.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Risk
{
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing risk-rule.
	/// </summary>
	public interface IRiskRule : ILogSource, IPersistable
	{
		/// <summary>
		/// Header.
		/// </summary>
		string Title { get; }

		/// <summary>
		/// Action.
		/// </summary>
		RiskActions Action { get; set; }

		/// <summary>
		/// To reset the state.
		/// </summary>
		void Reset();

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns><see langword="true" />, if the rule is activated, otherwise, <see langword="false" />.</returns>
		bool ProcessMessage(Message message);
	}
}