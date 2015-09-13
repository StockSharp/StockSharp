namespace StockSharp.Oanda
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// <see cref="Oanda"/> order condition.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "OANDA")]
	public class OandaOrderCondition : OrderCondition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OandaOrderCondition"/>.
		/// </summary>
		public OandaOrderCondition()
		{
		}

		/// <summary>
		/// If Market If Touched mode.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str3441Key)]
		[DescriptionLoc(LocalizedStrings.Str3442Key)]
		public bool IsMarket
		{
			get { return (bool?)Parameters.TryGetValue("IsMarket") ?? false; }
			set { Parameters["IsMarket"] = value; }
		}

		/// <summary>
		/// Minimum execution price.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str3443Key)]
		[DescriptionLoc(LocalizedStrings.Str3444Key)]
		public decimal? LowerBound
		{
			get { return (decimal?)Parameters.TryGetValue("LowerBound"); }
			set { Parameters["LowerBound"] = value; }
		}

		/// <summary>
		/// Maximum execution price.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str3407Key)]
		[DescriptionLoc(LocalizedStrings.Str3445Key)]
		public decimal? UpperBound
		{
			get { return (decimal?)Parameters.TryGetValue("UpperBound"); }
			set { Parameters["UpperBound"] = value; }
		}

		/// <summary>
		/// Stop-loss offset.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.StopLossKey)]
		[DescriptionLoc(LocalizedStrings.Str3384Key)]
		public decimal? StopLossOffset
		{
			get { return (decimal?)Parameters.TryGetValue("StopLossOffset"); }
			set { Parameters["StopLossOffset"] = value; }
		}

		/// <summary>
		/// Take-profit offset.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.TakeProfitKey)]
		[DescriptionLoc(LocalizedStrings.Str3386Key)]
		public decimal? TakeProfitOffset
		{
			get { return (decimal?)Parameters.TryGetValue("TakeProfitOffset"); }
			set { Parameters["TakeProfitOffset"] = value; }
		}

		/// <summary>
		/// Offset of a trailing stop-loss.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str3446Key)]
		[DescriptionLoc(LocalizedStrings.Str3447Key)]
		public int? TrailingStopLossOffset
		{
			get { return (int?)Parameters.TryGetValue("TrailingStopLossOffset"); }
			set { Parameters["TrailingStopLossOffset"] = value; }
		}
	}
}