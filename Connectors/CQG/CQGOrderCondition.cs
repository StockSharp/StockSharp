namespace StockSharp.CQG
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Условия заявок, специфичных для <see cref="CQG"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "CQG")]
	public class CQGOrderCondition : OrderCondition
	{
		/// <summary>
		/// Создать <see cref="CQGOrderCondition"/>.
		/// </summary>
		public CQGOrderCondition()
		{
		}

		/// <summary>
		/// Цена активации, при достижении которой будет выставлена заявка.
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