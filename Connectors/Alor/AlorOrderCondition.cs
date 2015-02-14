namespace StockSharp.Alor
{
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Типы условий стоп-заявок, специфичных для <see cref="AlorTrader"/>.
	/// </summary>
	public enum AlorOrderConditionTypes
	{
        /// <summary>
        /// Не активна.
        /// </summary>
        Inactive,

		/// <summary>
		/// Стоп-лосс.
		/// </summary>
		StopLoss,

		/// <summary>
		/// Тейк-профит.
		/// </summary>
		TakeProfit,
	}

	/// <summary>
	/// Условие заявок, специфичных для <see cref="Alor"/>.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "Alor")]
	public class AlorOrderCondition : OrderCondition
	{
		/// <summary>
		/// Создать <see cref="AlorOrderCondition"/>.
		/// </summary>
		public AlorOrderCondition()
		{
			Type = AlorOrderConditionTypes.Inactive;
			StopPrice = 0;
		}

		/// <summary>
		/// Тип условия.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str132Key)]
		[DescriptionLoc(LocalizedStrings.Str1691Key)]
		public AlorOrderConditionTypes Type
		{
			get { return (AlorOrderConditionTypes)Parameters["StopPrice"]; }
			set { Parameters["StopPrice"] = value; }
		}

		/// <summary>
		/// Стоп-цена.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.StopPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str1693Key)]
		public decimal StopPrice
		{
			get { return (decimal)Parameters["StopPrice"]; }
			set { Parameters["StopPrice"] = value; }
		}
	}
}