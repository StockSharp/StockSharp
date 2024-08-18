namespace StockSharp.Algo.Testing
{
	using Ecng.Serialization;

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
		public ICommissionRule Rule { get; set; }

		/// <summary>
		/// The portfolio name. If it is given, then <see cref="Rule"/> is applied to specific portfolio.
		/// </summary>
		public string PortfolioName { get; set; }

		/// <summary>
		/// Create a copy of <see cref="CommissionRuleMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new CommissionRuleMessage
			{
				Rule = Rule?.Clone(),
				PortfolioName = PortfolioName,
			};
		}
	}
}