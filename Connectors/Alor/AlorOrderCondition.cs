#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alor.Alor
File: AlorOrderCondition.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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