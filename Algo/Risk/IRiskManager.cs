namespace StockSharp.Algo.Risk;

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