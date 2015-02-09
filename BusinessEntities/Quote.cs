namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;
	using NetDataContract = System.Runtime.Serialization.DataContractAttribute;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Котировка стакана, представляющая бид или оффер.
	/// </summary>
	[Serializable]
	[NetDataContract]
	[Ignore(FieldName = "IsDisposed")]
	[DisplayNameLoc(LocalizedStrings.Str273Key)]
	[DescriptionLoc(LocalizedStrings.Str274Key)]
	[ExpandableObject]
	public class Quote : Cloneable<Quote>, IExtendableEntity
	{
		/// <summary>
		/// Создать <see cref="Quote"/>.
		/// </summary>
		public Quote()
		{
		}

		/// <summary>
		/// Создать <see cref="Quote"/>
		/// </summary>
		/// <param name="security">Инструмент, по которому получена котировка.</param>
		/// <param name="price">Цена котировки.</param>
		/// <param name="volume">Объем котировки.</param>
		/// <param name="direction">Направление (покупка или продажа).</param>
		public Quote(Security security, decimal price, decimal volume, Sides direction)
		{
			_security = security;
			_price = price;
			_volume = volume;
			_direction = direction;
		}

		private Security _security;

		/// <summary>
		/// Инструмент, по которому получена котировка.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public Security Security
		{
			get { return _security; }  
			set { _security = value; }
		}

		private decimal _price;

		/// <summary>
		/// Цена котировки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.Str275Key)]
		[MainCategory]
		public decimal Price
		{
			get { return _price; }
			set { _price = value; }
		}

		private decimal _volume;

		/// <summary>
		/// Объем котировки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str276Key)]
		[MainCategory]
		public decimal Volume
		{
			get { return _volume; }
			set { _volume = value; }
		}

		private Sides _direction;

		/// <summary>
		/// Направление (покупка или продажа).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str277Key)]
		[MainCategory]
		public Sides OrderDirection
		{
			get { return _direction; }
			set { _direction = value; }
		}

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Расширенная информация по котировке.
		/// </summary>
		/// <remarks>
		/// Необходима в случае хранения в программе дополнительной информации, ассоциированной с котировкой.
		/// Например, количество собственных контрактов в стакане, сумму лучшей покупки-продажи.
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
		/// Создать копию объекта <see cref="Quote" />.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Quote Clone()
		{
			return new Quote(_security, _price, _volume, _direction)
			{
				ExtensionInfo = ExtensionInfo,
			};
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "{0} {1} {2}".Put(OrderDirection == Sides.Buy ? LocalizedStrings.Bid : LocalizedStrings.Ask, Price, Volume);
		}
	}
}
