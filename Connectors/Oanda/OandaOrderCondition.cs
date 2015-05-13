namespace StockSharp.Oanda
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Условия заявок, специфичных для <see cref="Oanda"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "OANDA")]
	public class OandaOrderCondition : OrderCondition
	{
		/// <summary>
		/// Создать <see cref="OandaOrderCondition"/>.
		/// </summary>
		public OandaOrderCondition()
		{
		}

		/// <summary>
		/// Режим If Market If Touched.
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
		/// Минимальная цена исполнения.
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
		/// Максимальная цена исполнения.
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
		/// Отступ стоп-лосса.
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
		/// Отступ тейк-профита.
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
		/// Отступ скользящего стоп-лосса.
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