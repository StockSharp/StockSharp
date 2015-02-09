namespace StockSharp.Messages
{
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Типы значения изменения в <see cref="PositionChangeMessage"/>.
	/// </summary>
	public enum PositionChangeTypes
	{
		/// <summary>
		/// Начальное значение.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str253Key)]
		BeginValue,

		/// <summary>
		/// Текущая значение.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str254Key)]
		CurrentValue,

		/// <summary>
		/// Заблокировано.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str255Key)]
		BlockedValue,

		/// <summary>
		/// Стоимость позиции.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str256Key)]
		CurrentPrice,

		/// <summary>
		/// Средневзвешанная цена.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str257Key)]
		AveragePrice,

		/// <summary>
		/// Нереализованная прибыль.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str258Key)]
		UnrealizedPnL,

		/// <summary>
		/// Реализованная прибыль.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str259Key)]
		RealizedPnL,

		/// <summary>
		/// Вариационная маржа.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str260Key)]
		VariationMargin,

		/// <summary>
		/// Валюта.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str250Key)]
		Currency,

		/// <summary>
		/// Расширенная информация.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		ExtensionInfo,

		/// <summary>
		/// Плечо маржи.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str261Key)]
		Leverage,

		/// <summary>
		/// Общий размер комиссий.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str262Key)]
		Commission,

		/// <summary>
		/// Текущее значение (в лотах).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str263Key)]
		CurrentValueInLots,

		/// <summary>
		/// Название депозитария, где находится физически ценная бумага.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str264Key)]
		DepoName,

		/// <summary>
		/// Состояние портфеля.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str265Key)]
		State,
	}

	/// <summary>
	/// Сообщение, содержащее данные об изменениях позиции.
	/// </summary>
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
		/// Создать копию объекта.
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