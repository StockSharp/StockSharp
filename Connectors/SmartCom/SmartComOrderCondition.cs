namespace StockSharp.SmartCom
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Условие заявок, специфичных для <see cref="SmartCom"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "SmartCOM")]
	public class SmartComOrderCondition : OrderCondition
	{
		/// <summary>
		/// Создать <see cref="SmartComOrderCondition"/>.
		/// </summary>
		public SmartComOrderCondition()
		{
			StopPrice = 0;
		}

		/// <summary>
		/// Стоп-цена заявки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1885Key)]
		[DescriptionLoc(LocalizedStrings.Str1886Key)]
		public decimal StopPrice
		{
			get { return (decimal)Parameters["StopPrice"]; }
			set { Parameters["StopPrice"] = value; }
		}
	}
}