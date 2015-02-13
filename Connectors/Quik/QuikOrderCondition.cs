namespace StockSharp.Quik
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Условия стоп-цены по отношению к цене последней сделки инструмента.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public enum QuikStopPriceConditions
	{
		/// <summary>
		/// Больше или равно.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1730Key)]
		[EnumMember]
		MoreOrEqual,

		/// <summary>
		/// Меньше или равно.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1731Key)]
		[EnumMember]
		LessOrEqual,
	}

	/// <summary>
	/// Типы условий заявок, специфичных для <see cref="QuikTrader"/>.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public enum QuikOrderConditionTypes
	{
		/// <summary>
		/// Две заявки по одному и тому же инструменту, одинаковые по направленности и объему. Первая заявка типа «Стоп-лимит», вторая – лимитированная заявка.
		/// При исполнении одной из заявок вторая снимается.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1732Key)]
		[EnumMember]
		LinkedOrder,

		/// <summary>
		/// Заявка типа «Стоп-лимит», условие стоп-цены которой проверяется по одному инструменту, а в исполняемой лимитированной заявке указывается другой инструмент.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.SecurityKey)]
		[EnumMember]
		OtherSecurity,

		/// <summary>
		/// Стоп-заявка, порождающая при исполнении лимитированную заявку.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1733Key)]
		[EnumMember]
		StopLimit,

		/// <summary>
		/// Заявка с условием вида «исполнить при ухудшении цены на заданную величину от достигнутого максимума (на продажу) или минимума (на покупку)».
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.TakeProfitKey)]
		[EnumMember]
		TakeProfit,

		/// <summary>
		/// Это заявка, имеющая два условия: «тэйк-профит», если цена последней сделки после достигнутого максимума ухудшится на величину, превышающую установленный «отступ»;
		/// «стоп-лимит», если цена последней сделки ухудшится до указанного уровня.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1735Key)]
		[EnumMember]
		TakeProfitStopLimit,
	}

	/// <summary>
	/// Результат исполнения заявки, специфичной для <see cref="QuikTrader"/>.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public enum QuikOrderConditionResults
	{
		/// <summary>
		/// Заявка принята торговой системой.
		/// </summary>
		[EnumMember]
		SentToTS,

		/// <summary>
		/// Заявка отвергнута торговой системой.
		/// </summary>
		[EnumMember]
		RejectedByTS,

		/// <summary>
		/// Заявка снята пользователем.
		/// </summary>
		[EnumMember]
		Killed,

		/// <summary>
		/// Недостаточно средств клиента для выполнения заявки.
		/// </summary>
		[EnumMember]
		LimitControlFailed,

		/// <summary>
		/// Лимитированная заявка, связанная со стоп-заявкой, была снята пользователем.
		/// </summary>
		[EnumMember]
		LinkedOrderKilled,

		/// <summary>
		/// Торговой системой была удовлетворена лимитированная заявка, связанная со стоп-заявкой.
		/// </summary>
		[EnumMember]
		LinkedOrderFilled,

		/// <summary>
		/// Условие активации не наступило. Параметр заявок типов «Тэйк-профит» и «по исполнению».
		/// </summary>
		[EnumMember]
		WaitingForActivation,

		/// <summary>
		/// Условие активации наступило, начат расчет минимума/максимума цены. Параметр заявок типов «Тэйк-профит» и «Тэйк-профит по заявке».
		/// </summary>
		[EnumMember]
		CalculateMinMax,

		/// <summary>
		/// Заявка активирована на неполный объем в результате частичного исполнения заявки-условия, начат расчет минимума/максимума цены.
		/// Параметр заявок типа «Тэйк профит по заявке» с включенным признаком «Частичное исполнение заявки учитывается».
		/// </summary>
		[EnumMember]
		CalculateMinMaxAndWaitForActivation,
	}

	/// <summary>
	/// Условие заявок, специфичных для <see cref="Quik"/>.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "Quik")]
	public class QuikOrderCondition : OrderCondition
	{
		/// <summary>
		/// Создать <see cref="QuikOrderCondition"/>.
		/// </summary>
		public QuikOrderCondition()
		{
			Type = default(QuikOrderConditionTypes);
			LinkedOrderCancel = false;
			LinkedOrderPrice = null;
			Offset = null;
			Spread = null;
			StopPriceCondition = default(QuikStopPriceConditions);
			StopPrice = null;
			StopLimitPrice = null;
			ActiveTime = null;
			IsMarketStopLimit = null;
			IsMarketTakeProfit = null;
			ConditionOrderPartiallyMatched = null;
			ConditionOrderUseMatchedBalance = null;
			Result = null;
			OtherSecurityId = null;
			ConditionOrderId = null;
			ConditionOrderSide = Sides.Buy;
		}

		/// <summary>
		/// Тип стоп-заявки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1737Key)]
		[DescriptionLoc(LocalizedStrings.Str1691Key)]
		public QuikOrderConditionTypes Type
		{
			get { return (QuikOrderConditionTypes)Parameters["Type"]; }
			set { Parameters["Type"] = value; }
		}

		///<summary>
		/// Результат исполнения стоп-заявки.
		///</summary>
		/// <remarks>
		/// Значение null означает, что условие стоп-заявки ещё не сработало - стоп-заявка активна.
		/// </remarks>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1738Key)]
		[DescriptionLoc(LocalizedStrings.Str1739Key)]
		public QuikOrderConditionResults? Result
		{
			get { return (QuikOrderConditionResults?)Parameters["Result"]; }
			set { Parameters["Result"] = value; }
		}

		/// <summary>
		/// Идентификатор инструмента для стоп-заявок с условием по другому инструменту.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.Str1740Key)]
		public SecurityId? OtherSecurityId
		{
			get { return (SecurityId?)Parameters["OtherSecurityId"]; }
			set { Parameters["OtherSecurityId"] = value; }
		}

		/// <summary>
		/// Условие стоп-цены. Используется для заявок типа «Стоп-цена по другой бумаге».
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1741Key)]
		[DescriptionLoc(LocalizedStrings.Str1742Key)]
		public QuikStopPriceConditions StopPriceCondition
		{
			get { return (QuikStopPriceConditions)Parameters["StopPriceCondition"]; }
			set { Parameters["StopPriceCondition"] = value; }
		}

		/// <summary>
		/// Стоп цена, которая задает условие срабатывания стоп-заявки. Например, для заявок типа «Стоп-цена по другой бумаге» условие имеет вид:
		/// «Если цена &lt;=» (или «&gt;=») и означает исполнение заявки, если цена последней сделки по другому инструменту пересечет указанное значение.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1743Key)]
		[DescriptionLoc(LocalizedStrings.Str1744Key)]
		public decimal? StopPrice
		{
			get { return (decimal?)Parameters["StopPrice"]; }
			set { Parameters["StopPrice"] = value; }
		}

		/// <summary>
		/// Стоп-лимит цена. Аналогична <see cref="StopPrice"/>, но используется только при типе заявки «Тэйк-профит и стоп-лимит».
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1745Key)]
		[DescriptionLoc(LocalizedStrings.Str1746Key)]
		public decimal? StopLimitPrice
		{
			get { return (decimal?)Parameters["StopLimitPrice"]; }
			set { Parameters["StopLimitPrice"] = value; }
		}

		/// <summary>
		/// Признак исполнения заявки «Стоп-лимит» по рыночной цене.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1747Key)]
		[DescriptionLoc(LocalizedStrings.Str1748Key)]
		public bool? IsMarketStopLimit
		{
			get { return (bool?)Parameters["IsMarketStopLimit"]; }
			set { Parameters["IsMarketStopLimit"] = value; }
		}

		/// <summary>
		/// Время проверки условий заявки только в течение заданного периода времени (если значение null, то не проверять).
		/// Используется для заявок типов «Тэйк-профит и стоп-лимит» и «Тэйк-профит и стоп-лимит по заявке».
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1749Key)]
		[DescriptionLoc(LocalizedStrings.Str1750Key)]
		public Range<DateTimeOffset> ActiveTime
		{
			get { return (Range<DateTimeOffset>)Parameters["ActiveTime"]; }
			set { Parameters["ActiveTime"] = value; }
		}

		/// <summary>
		/// Идентификатор заявки-условия.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1751Key)]
		[DescriptionLoc(LocalizedStrings.Str1751Key, true)]
		public long? ConditionOrderId
		{
			get { return (long?)Parameters["ConditionOrderId"]; }
			set { Parameters["ConditionOrderId"] = value; }
		}

		/// <summary>
		/// Направление заявки-условия.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1753Key)]
		[DescriptionLoc(LocalizedStrings.Str1754Key)]
		public Sides ConditionOrderSide
		{
			get { return (Sides)Parameters["ConditionOrderSide"]; }
			set { Parameters["ConditionOrderSide"] = value; }
		}

		/// <summary>
		/// Частичное исполнение заявки учитывается. Заявка «по исполнению» будет активирована при частичном исполнении заявки-условия <see cref="ConditionOrderId"/>.
		/// Если false (или null), то заявка «по исполнению» активируется только при полном исполнении заявки-условия <see cref="ConditionOrderId"/>. 
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1755Key)]
		[DescriptionLoc(LocalizedStrings.Str1756Key)]
		public bool? ConditionOrderPartiallyMatched
		{
			get { return (bool?)Parameters["ConditionOrderPartiallyMatched"]; }
			set { Parameters["ConditionOrderPartiallyMatched"] = value; }
		}

		/// <summary>
		/// Брать исполненный объем заявки в качестве количества выставляемой стоп-заявки. В качестве количества бумаг в заявке «по исполнению»
		/// принимается исполненный объем заявки-условия <see cref="ConditionOrderId"/>. Если false (или null), то объем заявки указывается явно в свойство <see cref="Order.Volume"/>.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1757Key)]
		[DescriptionLoc(LocalizedStrings.Str1758Key)]
		public bool? ConditionOrderUseMatchedBalance
		{
			get { return (bool?)Parameters["ConditionOrderUseMatchedBalance"]; }
			set { Parameters["ConditionOrderUseMatchedBalance"] = value; }
		}

		/// <summary>
		/// Цена связанной лимитированной заявки.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1759Key)]
		[DescriptionLoc(LocalizedStrings.Str1760Key)]
		public decimal? LinkedOrderPrice
		{
			get { return (decimal?)Parameters["LinkedOrderPrice"]; }
			set { Parameters["LinkedOrderPrice"] = value; }
		}

		/// <summary>
		/// Признак снятия стоп-заявки при частичном исполнении связанной лимитированной заявки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1761Key)]
		[DescriptionLoc(LocalizedStrings.Str1762Key)]
		public bool LinkedOrderCancel
		{
			get { return (bool)Parameters["LinkedOrderCancel"]; }
			set { Parameters["LinkedOrderCancel"] = value; }
		}

		/// <summary>
		/// Величина отступа от максимума (минимума) цены последней сделки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1763Key)]
		[DescriptionLoc(LocalizedStrings.Str1764Key)]
		public Unit Offset
		{
			get { return (Unit)Parameters["Offset"]; }
			set { Parameters["Offset"] = value; }
		}

		/// <summary>
		/// Величина защитного спрэда.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str3536Key)]
		[DescriptionLoc(LocalizedStrings.Str1766Key)]
		public Unit Spread
		{
			get { return (Unit)Parameters["Spread"]; }
			set { Parameters["Spread"] = value; }
		}

		/// <summary>
		/// Признак исполнения заявки «Тэйк-профит» по рыночной цене.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1767Key)]
		[DescriptionLoc(LocalizedStrings.Str1768Key)]
		public bool? IsMarketTakeProfit
		{
			get { return (bool?)Parameters["IsMarketTakeProfit"]; }
			set { Parameters["IsMarketTakeProfit"] = value; }
		}

		/// <summary>
		/// Создать копию условия (копирование параметров условия).
		/// </summary>
		/// <returns>Копия условия.</returns>
		public override OrderCondition Clone()
		{
			var clone = (QuikOrderCondition)base.Clone();
			clone.Offset = Offset.CloneNullable();
			clone.Spread = Spread.CloneNullable();
			clone.ActiveTime = ActiveTime.CloneNullable();
			return clone;
		}
	}
}