#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: CommissionRuleMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		public CommissionRule Rule { get; set; }

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