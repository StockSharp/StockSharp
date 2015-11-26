namespace StockSharp.Rithmic
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// <see cref="Rithmic"/> order condition.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "Rithmic")]
	public class RithmicOrderCondition : OrderCondition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RithmicOrderCondition"/>.
		/// </summary>
		public RithmicOrderCondition()
		{
		}

		/// <summary>
		/// Activation price, when reached an order will be placed.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.StopLossKey)]
		[DisplayNameLoc(LocalizedStrings.Str2455Key)]
		[DescriptionLoc(LocalizedStrings.Str3460Key)]
		public decimal? TriggerPrice 
		{
			get { return (decimal?)Parameters.TryGetValue("TriggerPrice"); }
			set { Parameters["TriggerPrice ="] = value; }
		}
	}
}