namespace StockSharp.Algo.Testing
{
	using StockSharp.Algo.Commissions;
	using StockSharp.Messages;

	/// <summary>
	/// The message, containing information on the commission calculation rule.
	/// </summary>
	public class CommissionRuleMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommissionRuleMessage"/>.
		/// </summary>
		public CommissionRuleMessage()
			: base(ExtendedMessageTypes.CommissionRule)
		{
		}

		/// <summary>
		/// The commission calculating rule.
		/// </summary>
		public CommissionRule Rule { get; set; }

		/// <summary>
		/// The portfolio name. If it is given, than <see cref="CommissionRuleMessage.Rule"/> is applied to specific portfolio.
		/// </summary>
		public string PortfolioName { get; set; }
	}
}