#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Transaq
File: TransaqOrderCondition.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	// Из доки(стр. 23 низ): Защитный спред, объем quantity, цену заявки для stop loss и коррекцию можно задавать как в аболютной влеичине,
	// так и в процентах(от цены, либо от позиции клиента по смылсу).

	/// <summary>
	/// Типы стоп-заявок заявок.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public enum TransaqOrderConditionTypes
	{
		/// <summary>
		/// SL предназначен для закрытия позиции с целью ограничения убытков от удержания позиции при неблагоприятном движении цены на рынке.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str242Key)]
		[EnumMember]
		StopLoss,

		/// <summary>
		/// TP предназначен для закрытия позиции с фиксацие прибыли.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.TakeProfitKey)]
		[EnumMember]
		TakeProfit,

		/// <summary>
		/// TP + SL. При выполнении условия для одной части, вторая часть снимается.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3515Key)]
		[EnumMember]
		TakeProfitStopLoss,

		/// <summary>
		/// Алгоритмическая заявка.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str2490Key)]
		[EnumMember]
		Algo,
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
		[EnumDisplayNameLoc(LocalizedStrings.TimeKey)]
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
		[EnumDisplayNameLoc(LocalizedStrings.DateKey)]
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
		[EnumDisplayNameLoc(LocalizedStrings.GTCKey)]
		[EnumMember]
		TillCancelled
	}

	/// <summary>
	/// Условия стоп-заявок, специфичных для <see cref="Transaq"/>.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "Transaq")]
	[CategoryOrderLoc(LocalizedStrings.Str225Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.StopLossKey, 1)]
	[CategoryOrderLoc(LocalizedStrings.TakeProfitKey, 2)]
	[CategoryOrderLoc(LocalizedStrings.Str2490Key, 3)]
	public class TransaqOrderCondition : OrderCondition
	{
		/// <summary>
		/// Создать <see cref="TransaqOrderCondition"/>.
		/// </summary>
		public TransaqOrderCondition()
		{
			Type = TransaqOrderConditionTypes.StopLoss;
		}

		/// <summary>
		/// Тип стоп-заявки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1737Key)]
		[DescriptionLoc(LocalizedStrings.Str1691Key)]
		public TransaqOrderConditionTypes Type
		{
			get { return (TransaqOrderConditionTypes)Parameters["Type"]; }
			set { Parameters["Type"] = value; }
		}

		/// <summary>
		/// Идентификатор связанной заявки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str532Key)]
		[DescriptionLoc(LocalizedStrings.Str3517Key)]
		public long? LinkedOrderId
		{
			get { return (long?)Parameters.TryGetValue("LinkedOrderId"); }
			set { Parameters["LinkedOrderId"] = value; }
		}

		/// <summary>
		/// Заявка действительна до.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str3518Key)]
		[DescriptionLoc(LocalizedStrings.Str3519Key)]
		public DateTimeOffset? ValidFor
		{
			get { return (DateTimeOffset?)Parameters.TryGetValue("ValidFor"); }
			set { Parameters["ValidFor"] = value; }
		}

		#region SL

		/// <summary>
		/// Цена активации, при достижении которой будет выставлена заявка по цене указанной в <see cref="StopLossOrderPrice"/>.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.StopLossKey)]
		[DisplayNameLoc(LocalizedStrings.Str2455Key)]
		[DescriptionLoc(LocalizedStrings.Str3520Key)]
		public decimal? StopLossActivationPrice
		{
			get { return (decimal?)Parameters.TryGetValue("StopLossActivationPrice"); }
			set { Parameters["StopLossActivationPrice"] = value; }
		}

		/// <summary>
		/// Цена выставляемой заявки, которая будет отправлена на биржу при активации по цене указанной в <see cref="StopLossActivationPrice"/>. 
		/// Абсолютное значение, или в процентах.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.StopLossKey)]
		[DisplayNameLoc(LocalizedStrings.Str1341Key)]
		[DescriptionLoc(LocalizedStrings.Str3521Key)]
		public Unit StopLossOrderPrice
		{
			get { return (Unit)Parameters.TryGetValue("StopLossOrderPrice"); }
			set { Parameters["StopLossOrderPrice"] = value; }
		}

		/// <summary>
		/// Выставить заявку по рынку (в этом случае <see cref="StopLossOrderPrice"/> игнорируется).
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.StopLossKey)]
		[DisplayNameLoc(LocalizedStrings.Str3522Key)]
		[DescriptionLoc(LocalizedStrings.Str3523Key)]
		public bool? StopLossByMarket
		{
			get { return (bool?)Parameters.TryGetValue("StopLossByMarket"); }
			set { Parameters["StopLossByMarket"] = value; }
		}

		/// <summary>
		/// Объем (абсолютное значение или в процентах).
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.StopLossKey)]
		[DisplayNameLoc(LocalizedStrings.Str3524Key)]
		[DescriptionLoc(LocalizedStrings.Str3525Key)]
		public Unit StopLossVolume
		{
			get { return (Unit)Parameters.TryGetValue("StopLossVolume"); }
			set { Parameters["StopLossVolume"] = value; }
		}

		/// <summary>
		/// Использовать кредит.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.StopLossKey)]
		[DisplayNameLoc(LocalizedStrings.Str3526Key)]
		[DescriptionLoc(LocalizedStrings.Str3526Key, true)]
		public bool? StopLossUseCredit
		{
			get { return (bool?)Parameters.TryGetValue("StopLossUseCredit"); }
			set { Parameters["StopLossUseCredit"] = value; }
		}

		/// <summary>
		/// Защитное время, в сек. Защитное время позволяет предотвратить исполнение при "проколах" на рынке.
		/// Т.е. в таких ситуациях, когда цены на рынке лишь кратковременно достигают уровня <see cref="StopLossActivationPrice"/>, и вскоре возвращаются обратно. 
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.StopLossKey)]
		[DisplayNameLoc(LocalizedStrings.Str3528Key)]
		[DescriptionLoc(LocalizedStrings.Str3529Key)]
		public int? StopLossProtectionTime
		{
			get { return (int?)Parameters.TryGetValue("StopLossProtectionTime"); }
			set { Parameters["StopLossProtectionTime"] = value; }
		}

		/// <summary>
		/// Примечание.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.StopLossKey)]
		[DisplayNameLoc(LocalizedStrings.Str135Key)]
		[DescriptionLoc(LocalizedStrings.Str135Key, true)]
		public string StopLossComment
		{
			get { return (string)Parameters.TryGetValue("StopLossComment"); }
			set { Parameters["StopLossComment"] = value; }
		}

		#endregion

		#region TP

		/// <summary>
		/// Цена активации, при достижении которой будет отправлена заявка на биржу с указанной ценой, с учетом <see cref="TakeProfitProtectionSpread"/>.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.TakeProfitKey)]
		[DisplayNameLoc(LocalizedStrings.Str2455Key)]
		[DescriptionLoc(LocalizedStrings.Str3531Key)]
		public decimal? TakeProfitActivationPrice
		{
			get { return (decimal?)Parameters.TryGetValue("TakeProfitActivationPrice"); }
			set { Parameters["TakeProfitActivationPrice"] = value; }
		}

		/// <summary>
		/// Выставить заявку по рынку.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.TakeProfitKey)]
		[DisplayNameLoc(LocalizedStrings.Str3522Key)]
		[DescriptionLoc(LocalizedStrings.Str3532Key)]
		public bool? TakeProfitByMarket
		{
			get { return (bool?)Parameters.TryGetValue("TakeProfitByMarket"); }
			set { Parameters["TakeProfitByMarket"] = value; }
		}

		/// <summary>
		/// Объем. 
		/// Абсолютное значение, или в процентах.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.TakeProfitKey)]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str3533Key)]
		public Unit TakeProfitVolume
		{
			get { return (Unit)Parameters.TryGetValue("TakeProfitVolume"); }
			set { Parameters["TakeProfitVolume"] = value; }
		}

		/// <summary>
		/// Использовать кредит.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.TakeProfitKey)]
		[DisplayNameLoc(LocalizedStrings.Str3526Key)]
		[DescriptionLoc(LocalizedStrings.Str3526Key, true)]
		public bool? TakeProfitUseCredit
		{
			get { return (bool?)Parameters.TryGetValue("TakeProfitUseCredit"); }
			set { Parameters["TakeProfitUseCredit"] = value; }
		}

		/// <summary>
		/// Защитное время, в сек. Защитное время позволяет предотвратить исполнение при "проколах" на рынке.
		/// Т.е. в таких ситуациях, когда цены на рынке лишь кратковременно достигают уровня <see cref="StopLossActivationPrice"/>, и вскоре возвращаются обратно.
		/// Нужно при использовании трейлинга, при выставленном значении <see cref="TakeProfitCorrection"/>.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.TakeProfitKey)]
		[DisplayNameLoc(LocalizedStrings.Str3528Key)]
		[DescriptionLoc(LocalizedStrings.Str3529Key)]
		public int? TakeProfitProtectionTime
		{
			get { return (int?)Parameters.TryGetValue("TakeProfitProtectionTime"); }
			set { Parameters["TakeProfitProtectionTime"] = value; }
		}

		/// <summary>
		/// Примечание.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.TakeProfitKey)]
		[DisplayNameLoc(LocalizedStrings.Str135Key)]
		[DescriptionLoc(LocalizedStrings.Str135Key, true)]
		public string TakeProfitComment
		{
			get { return (string)Parameters.TryGetValue("TakeProfitComment"); }
			set { Parameters["TakeProfitComment"] = value; }
		}

		/// <summary>
		/// Коррекция. Если задано, то после активации заявки по <see cref="TakeProfitActivationPrice"/> и снижении цены (для TP на продажу)
		/// или повышения цены (для TP на покупку) будет послана заявка по цене, с учетом <see cref="TakeProfitProtectionSpread"/>.
		/// Абсолютное значение, или в процентах.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.TakeProfitKey)]
		[DisplayNameLoc(LocalizedStrings.Str3534Key)]
		[DescriptionLoc(LocalizedStrings.Str3535Key)]
		public Unit TakeProfitCorrection
		{
			get { return (Unit)Parameters.TryGetValue("TakeProfitCorrection"); }
			set { Parameters["TakeProfitCorrection"] = value; }
		}

		/// <summary>
		/// Защитный спред. Величина, которя будет прибавлятся (при TP на покупку) или отниматься (при TP на продажу)
		/// к цене <see cref="TakeProfitActivationPrice"/>, при отравке заявки на биржу.
		/// Абсолютное значение, или в процентах.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.TakeProfitKey)]
		[DisplayNameLoc(LocalizedStrings.Str3536Key)]
		[DescriptionLoc(LocalizedStrings.Str3537Key)]
		public Unit TakeProfitProtectionSpread
		{
			get { return (Unit)Parameters.TryGetValue("TakeProfitProtectionSpread"); }
			set { Parameters["TakeProfitProtectionSpread"] = value; }
		}

		#endregion TP

		#region Algo

		/// <summary>
		/// Условие.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str2490Key)]
		[DisplayNameLoc(LocalizedStrings.Str154Key)]
		[DescriptionLoc(LocalizedStrings.Str3552Key)]
		public TransaqAlgoOrderConditionTypes? AlgoType
		{
			get { return (TransaqAlgoOrderConditionTypes?)Parameters.TryGetValue("AlgoType"); }
			set { Parameters["AlgoType"] = value; }
		}

		/// <summary>
		/// Цена для заявки, либо обеспеченность в процентах.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str2490Key)]
		[DisplayNameLoc(LocalizedStrings.Str1341Key)]
		[DescriptionLoc(LocalizedStrings.Str3553Key)]
		public decimal? AlgoValue
		{
			get { return (decimal?)Parameters.TryGetValue("AlgoValue"); }
			set { Parameters["AlgoValue"] = value; }
		}

		/// <summary>
		/// Условие действительности заявки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str2490Key)]
		[DisplayNameLoc(LocalizedStrings.Str3554Key)]
		[DescriptionLoc(LocalizedStrings.Str3555Key)]
		public TransaqAlgoOrderValidTypes? AlgoValidAfterType
		{
			get { return (TransaqAlgoOrderValidTypes?)Parameters.TryGetValue("AlgoValidAfterType"); }
			set { Parameters["AlgoValidAfterType"] = value; }
		}

		/// <summary>
		/// С какого момента времени действительна.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str2490Key)]
		[DisplayNameLoc(LocalizedStrings.Str3556Key)]
		[DescriptionLoc(LocalizedStrings.Str3557Key)]
		public DateTimeOffset? AlgoValidAfter
		{
			get { return (DateTimeOffset?)Parameters.TryGetValue("AlgoValidAfter"); }
			set { Parameters["AlgoValidAfter"] = value; }
		}

		/// <summary>
		/// Условие действительности заявки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str2490Key)]
		[DisplayNameLoc(LocalizedStrings.Str3558Key)]
		[DescriptionLoc(LocalizedStrings.Str3555Key)]
		public TransaqAlgoOrderValidTypes? AlgoValidBeforeType
		{
			get { return (TransaqAlgoOrderValidTypes?)Parameters.TryGetValue("AlgoValidBeforeType"); }
			set { Parameters["AlgoValidBeforeType"] = value; }
		}

		/// <summary>
		/// До какого момента времени действительна.
		/// </summary>
		[DataMember]
		[Nullable]
		[CategoryLoc(LocalizedStrings.Str2490Key)]
		[DisplayNameLoc(LocalizedStrings.Str3518Key)]
		[DescriptionLoc(LocalizedStrings.Str3519Key)]
		public DateTimeOffset? AlgoValidBefore
		{
			get { return (DateTimeOffset?)Parameters.TryGetValue("AlgoValidBefore"); }
			set { Parameters["AlgoValidBefore"] = value; }
		}

		#endregion
	}
}