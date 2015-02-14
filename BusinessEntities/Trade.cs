namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Тиковая сделка.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str506Key)]
	[DescriptionLoc(LocalizedStrings.TickTradeKey)]
	public class Trade : Cloneable<Trade>, IExtendableEntity
	{
		/// <summary>
		/// Создать <see cref="Trade"/>.
		/// </summary>
		public Trade()
		{
		}

		/// <summary>
		/// Идентификатор сделки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str361Key)]
		[DescriptionLoc(LocalizedStrings.Str145Key)]
		[MainCategory]
		[Identity]
		[PropertyOrder(0)]
		public long Id { get; set; }

		/// <summary>
		/// Идентификатор сделки (ввиде строки, если электронная площадка не использует числовое представление идентификатора сделки).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OrderIdStringKey)]
		[DescriptionLoc(LocalizedStrings.Str146Key)]
		[MainCategory]
		[PropertyOrder(1)]
		public string StringId { get; set; }

		/// <summary>
		/// Инструмент, по которому была совершена сделка.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[XmlIgnore]
		[Browsable(false)]
		[PropertyOrder(2)]
		public Security Security { get; set; }

		/// <summary>
		/// Время совершения сделки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str219Key)]
		[DescriptionLoc(LocalizedStrings.Str605Key)]
		[MainCategory]
		[PropertyOrder(3)]
		public DateTimeOffset Time { get; set; }

		/// <summary>
		/// Локальное время получения сделки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str514Key)]
		[DescriptionLoc(LocalizedStrings.Str606Key)]
		[MainCategory]
		[PropertyOrder(9)]
		public DateTime LocalTime { get; set; }

		/// <summary>
		/// Количество контрактов в сделке.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.TradeVolumeKey)]
		[MainCategory]
		[PropertyOrder(4)]
		public decimal Volume { get; set; }

		/// <summary>
		/// Цена сделки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.Str147Key)]
		[MainCategory]
		[PropertyOrder(3)]
		public decimal Price { get; set; }

		/// <summary>
		/// Направление заявки (покупка или продажа), которая привела к сделке.
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str608Key)]
		[MainCategory]
		[PropertyOrder(5)]
		public Sides? OrderDirection { get; set; }

		private bool _isSystem = true;

		/// <summary>
		/// Является ли сделка системной.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SystemTradeKey)]
		[DescriptionLoc(LocalizedStrings.IsSystemTradeKey)]
		[MainCategory]
		[PropertyOrder(6)]
		public bool IsSystem
		{
			get { return _isSystem; }
			set { _isSystem = value; }
		}

		/// <summary>
		/// Системный статус сделки.
		/// </summary>
		[Browsable(false)]
		public int Status { get; set; }

		/// <summary>
		/// Количество открытых позиций (открытый интерес).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str150Key)]
		[DescriptionLoc(LocalizedStrings.Str151Key)]
		[MainCategory]
		[Nullable]
		[PropertyOrder(7)]
		public decimal? OpenInterest { get; set; }

		/// <summary>
		/// Является ли тик восходящим или нисходящим в цене.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str157Key)]
		[DescriptionLoc(LocalizedStrings.Str158Key)]
		[MainCategory]
		[Nullable]
		[PropertyOrder(8)]
		public bool? IsUpTick { get; set; }

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Расширенная информация по сделке.
		/// </summary>
		/// <remarks>
		/// Необходима в случае хранения в программе дополнительной информации, ассоциированной со сделкой.
		/// Например, операция, преведшая к сделке.
		/// </remarks>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set { _extensionInfo = value; }
		}

		/// <summary>
		/// Создать копию объекта <see cref="Trade"/>.
		/// </summary>
		/// <returns>Копия объекта.</returns>
		public override Trade Clone()
		{
			return new Trade
			{
				Id = Id,
				StringId = StringId,
				Volume = Volume,
				Price = Price,
				Time = Time,
				LocalTime = LocalTime,
				OrderDirection = OrderDirection,
				Security = Security,
				IsSystem = IsSystem,
				Status = Status,
				OpenInterest = OpenInterest,
				IsUpTick = IsUpTick,
			};
		}

		/// <summary>
		/// Рассчитать хеш-код объекта <see cref="Trade"/>.
		/// </summary>
		/// <returns>Хеш-код.</returns>
		public override int GetHashCode()
		{
			return (Security != null ? Security.GetHashCode() : 0) ^ Id.GetHashCode();
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "{0} {1} {2} {3}".Put(Time, Id, Price, Volume);
		}
	}
}