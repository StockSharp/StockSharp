namespace StockSharp.Algo.Testing
{
	using StockSharp.Algo.Commissions;
	using StockSharp.Messages;

	/// <summary>
	/// Сообщение, содержащее в себе информацию о правиле расчета комиссии.
	/// </summary>
	public class CommissionRuleMessage : Message
	{
		/// <summary>
		/// Создать <see cref="CommissionRuleMessage"/>.
		/// </summary>
		public CommissionRuleMessage()
			: base(ExtendedMessageTypes.CommissionRule)
		{
		}

		/// <summary>
		/// Правило вычисления комиссии.
		/// </summary>
		public CommissionRule Rule { get; set; }

		/// <summary>
		/// Имя портфеля. Если оно задано, то <see cref="Rule"/> применяется к конктреному портфелю.
		/// </summary>
		public string PortfolioName { get; set; }
	}
}