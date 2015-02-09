namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Котировка стакана, представляющая бид или оффер.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Ignore(FieldName = "IsDisposed")]
	[DisplayNameLoc(LocalizedStrings.Str273Key)]
	[DescriptionLoc(LocalizedStrings.Str274Key)]
	public class QuoteChange : Equatable<QuoteChange>, IExtendableEntity
	{
		/// <summary>
		/// Создать <see cref="QuoteChange"/>.
		/// </summary>
		public QuoteChange()
		{
		}

		/// <summary>
		/// Создать <see cref="QuoteChange"/>.
		/// </summary>
		/// <param name="side">Направление (покупка или продажа).</param>
		/// <param name="price">Цена котировки.</param>
		/// <param name="volume">Объем котировки.</param>
		public QuoteChange(Sides side, decimal price, decimal volume)
		{
			Side = side;
			Price = price;
			Volume = volume;
		}

		/// <summary>
		/// Цена котировки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.Str275Key)]
		[MainCategory]
		public decimal Price { get; set; }

		/// <summary>
		/// Объем котировки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str276Key)]
		[MainCategory]
		public decimal Volume { get; set; }

		/// <summary>
		/// Направление (покупка или продажа).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str277Key)]
		[MainCategory]
		public Sides Side { get; set; }

		/// <summary>
		/// Код электронной площадки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string BoardCode { get; set; }

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
		/// Создать копию объекта <see cref="QuoteChange" />.
		/// </summary>
		/// <returns>Копия.</returns>
		public override QuoteChange Clone()
		{
			var clone = new QuoteChange(Side, Price, Volume);
			this.CopyExtensionInfo(clone);
			return clone;
		}

		/// <summary>
		/// Сравнить две котировки на эквивалентность.
		/// </summary>
		/// <param name="other">Другая котировки, с которой необходимо сравнивать.</param>
		/// <returns><see langword="true"/>, если другая котировки равна текущей, иначе, <see langword="false"/>.</returns>
		protected override bool OnEquals(QuoteChange other)
		{
			return Price == other.Price && Side == other.Side;
		}

		/// <summary>
		/// Рассчитать хеш-код объекта <see cref="QuoteChange" />.
		/// </summary>
		/// <returns>Хеш-код.</returns>
		public override int GetHashCode()
		{
			return Price.GetHashCode() ^ Side.GetHashCode();
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "{0} {1} {2}".Put(Side == Sides.Buy ? LocalizedStrings.Bid : LocalizedStrings.Ask, Price, Volume);
		}
	}
}