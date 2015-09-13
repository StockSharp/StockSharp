namespace StockSharp.LMAX
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// <see cref="LMAX"/> order condition.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "LMAX")]
	public class LmaxOrderCondition : OrderCondition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LmaxOrderCondition"/>.
		/// </summary>
		public LmaxOrderCondition()
		{
		}

		/// <summary>
		/// Stop-loss offset.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str3383Key)]
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
		[DisplayNameLoc(LocalizedStrings.Str3385Key)]
		[DescriptionLoc(LocalizedStrings.Str3386Key)]
		public decimal? TakeProfitOffset
		{
			get { return (decimal?)Parameters.TryGetValue("TakeProfitOffset"); }
			set { Parameters["TakeProfitOffset"] = value; }
		}
	}
}