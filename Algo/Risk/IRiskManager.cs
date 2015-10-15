namespace StockSharp.Algo.Risk
{
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing risks control manager.
	/// </summary>
	public interface IRiskManager : IRiskRule
	{
		/// <summary>
		/// Rule list.
		/// </summary>
		SynchronizedSet<IRiskRule> Rules { get; }

		/// <summary>
		/// To process the trade message.
		/// </summary>
		/// <param name="message">The trade message.</param>
		/// <returns>List of rules, activated by the message.</returns>
		IEnumerable<IRiskRule> ProcessRules(Message message);
	}
}