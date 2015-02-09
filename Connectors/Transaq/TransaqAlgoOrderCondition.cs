namespace StockSharp.Transaq
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Условие заявок, специфичных для <see cref="Transaq"/>.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "Transaq")]
	public class TransaqAlgoOrderCondition : OrderCondition
	{
		/// <summary>
		/// Создать <see cref="TransaqAlgoOrderCondition"/>.
		/// </summary>
		public TransaqAlgoOrderCondition()
		{
			Type = TransaqAlgoOrderConditionTypes.None;
			Value = 0;
			ValidBeforeType = TransaqAlgoOrderValidTypes.TillCancelled;
			ValidAfterType = TransaqAlgoOrderValidTypes.TillCancelled;
		}

		/// <summary>
		/// Условие.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str154Key)]
		[DescriptionLoc(LocalizedStrings.Str3552Key)]
		public TransaqAlgoOrderConditionTypes Type 
		{
			get { return (TransaqAlgoOrderConditionTypes)Parameters["Type"]; }
			set { Parameters["Type"] = value; }
		}

		/// <summary>
		/// Цена для заявки, либо обеспеченность в процентах.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1341Key)]
		[DescriptionLoc(LocalizedStrings.Str3553Key)]
		public decimal Value 
		{
			get { return (decimal)Parameters["Value"]; }
			set { Parameters["Value"] = value; }
		}

		/// <summary>
		/// Условие действительности заявки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str3554Key)]
		[DescriptionLoc(LocalizedStrings.Str3555Key)]
		public TransaqAlgoOrderValidTypes ValidAfterType 
		{
			get { return (TransaqAlgoOrderValidTypes)Parameters["ValidAfterType"]; }
			set { Parameters["ValidAfterType"] = value; }
		}

		/// <summary>
		/// С какого момента времени действительна.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str3556Key)]
		[DescriptionLoc(LocalizedStrings.Str3557Key)]
		public DateTime? ValidAfter 
		{
			get { return (DateTime?)Parameters.TryGetValue("ValidAfter"); }
			set { Parameters["ValidAfter"] = value; }
		}

		/// <summary>
		/// Условие действительности заявки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str3558Key)]
		[DescriptionLoc(LocalizedStrings.Str3555Key)]
		public TransaqAlgoOrderValidTypes ValidBeforeType
		{
			get { return (TransaqAlgoOrderValidTypes)Parameters["ValidBeforeType"]; }
			set { Parameters["ValidBeforeType"] = value; }
		}

		/// <summary>
		/// До какого момента времени действительна.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str3518Key)]
		[DescriptionLoc(LocalizedStrings.Str3519Key)]
		public DateTime? ValidBefore 
		{
			get { return (DateTime?)Parameters.TryGetValue("ValidBefore"); }
			set { Parameters["ValidBefore"] = value; }
		}
	}

	/// <summary>
	/// Допустимые типы условия.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public enum TransaqAlgoOrderConditionTypes
	{
		/// <summary>
		/// Нет.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3559Key)]
		[EnumMember]
		None,

		/// <summary>
		/// Лучшая цена покупки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.BidKey)]
		[EnumMember]
		Bid,

		/// <summary>
		/// Лучшая цена покупки или сделка по заданной цене и выше.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3560Key)]
		[EnumMember]
		BidOrLast,

		/// <summary>
		/// Лучшая цена продажи.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.AskKey)]
		[EnumMember]
		Ask,

		/// <summary>
		/// Лучшая цена продажи или сделка по заданной цене и ниже.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3561Key)]
		[EnumMember]
		AskOrLast,

		/// <summary>
		/// Время выставления заявки на Биржу.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str219Key)]
		[EnumMember]
		Time,

		/// <summary>
		/// Обеспеченность ниже заданной.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3562Key)]
		[EnumMember]
		CovDown,

		/// <summary>
		/// Обеспеченность выше заданной.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3563Key)]
		[EnumMember]
		CovUp,

		/// <summary>
		/// Сделка на рынке по заданной цене или выше.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3564Key)]
		[EnumMember]
		LastUp,

		/// <summary>
		/// Сделка на рынке по заданной цене или ниже.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3565Key)]
		[EnumMember]
		LastDown
	}

	/// <summary>
	/// Условие действительности заявки.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public enum TransaqAlgoOrderValidTypes
	{
		/// <summary>
		/// По дате и времени.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str2857Key)]
		[EnumMember]
		Date,

		/// <summary>
		/// Немедленно.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3566Key)]
		[EnumMember]
		Immediately,

		/// <summary>
		/// До отмены.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1539Key)]
		[EnumMember]
		TillCancelled
	}
}