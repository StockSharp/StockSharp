namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Типы значения изменения в <see cref="PositionChangeMessage"/>.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public enum PositionChangeTypes
	{
		/// <summary>
		/// Начальное значение.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str253Key)]
		BeginValue,

		/// <summary>
		/// Текущая значение.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str254Key)]
		CurrentValue,

		/// <summary>
		/// Заблокировано.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str255Key)]
		BlockedValue,

		/// <summary>
		/// Стоимость позиции.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str256Key)]
		CurrentPrice,

		/// <summary>
		/// Средневзвешанная цена.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str257Key)]
		AveragePrice,

		/// <summary>
		/// Нереализованная прибыль.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str258Key)]
		UnrealizedPnL,

		/// <summary>
		/// Реализованная прибыль.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str259Key)]
		RealizedPnL,

		/// <summary>
		/// Вариационная маржа.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str260Key)]
		VariationMargin,

		/// <summary>
		/// Валюта.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.CurrencyKey)]
		Currency,

		/// <summary>
		/// Расширенная информация.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		ExtensionInfo,

		/// <summary>
		/// Плечо маржи.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str261Key)]
		Leverage,

		/// <summary>
		/// Общий размер комиссий.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str262Key)]
		Commission,

		/// <summary>
		/// Текущее значение (в лотах).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str263Key)]
		CurrentValueInLots,

		/// <summary>
		/// Название депозитария, где находится физически ценная бумага.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str264Key)]
		DepoName,

		/// <summary>
		/// Состояние портфеля.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str265Key)]
		State,
	}

	/// <summary>
	/// Сообщение, содержащее данные об изменениях позиции.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public sealed class PositionChangeMessage : BaseChangeMessage<PositionChangeTypes>
	{
		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Название портфеля.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.PortfolioNameKey)]
		[MainCategory]
		[ReadOnly(true)]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Название депозитария, где находится физически ценная бумага.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str264Key)]
		[DescriptionLoc(LocalizedStrings.DepoNameKey)]
		[MainCategory]
		public string DepoName { get; set; }

		/// <summary>
		/// Вид лимита для Т+ рынка.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str266Key)]
		[DescriptionLoc(LocalizedStrings.Str267Key)]
		[MainCategory]
		[Nullable]
		public TPlusLimits? LimitType { get; set; }

		/// <summary>
		/// Текстовое описание позиции.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DescriptionKey)]
		[DescriptionLoc(LocalizedStrings.Str269Key)]
		[MainCategory]
		public string Description { get; set; }

		/// <summary>
		/// Создать <see cref="PositionChangeMessage"/>.
		/// </summary>
		public PositionChangeMessage()
			: base(MessageTypes.PositionChange)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="PositionChangeMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var msg = new PositionChangeMessage
			{
				LocalTime = LocalTime,
				PortfolioName = PortfolioName,
				SecurityId = SecurityId,
				DepoName = DepoName,
				ServerTime = ServerTime,
				LimitType = LimitType,
				Description = Description,
			};

			msg.Changes.AddRange(Changes);
			this.CopyExtensionInfo(msg);

			return msg;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Sec={0},P={1},Changes={2}".Put(SecurityId, PortfolioName, Changes.Select(c => c.ToString()).Join(","));
		}
	}
}