namespace StockSharp.CQG
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// <see cref="CQG"/> order condition.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "CQG")]
	public class CQGOrderCondition : OrderCondition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CQGOrderCondition"/>.
		/// </summary>
		public CQGOrderCondition()
		{
		}

		/// <summary>
		/// Activation price, when reached an order will be placed.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.StopLossKey)]
		[DisplayNameLoc(LocalizedStrings.Str2455Key)]
		[DescriptionLoc(LocalizedStrings.Str3460Key)]
		public decimal? StopPrice 
		{
			get { return (decimal?)Parameters.TryGetValue("StopPrice"); }
			set { Parameters["StopPrice ="] = value; }
		}
	}
}