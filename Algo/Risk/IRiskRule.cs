namespace StockSharp.Algo.Risk
{
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing risk-rule.
	/// </summary>
	public interface IRiskRule : IPersistable
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