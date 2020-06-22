#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: Quote.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

	/// <summary>
	/// Market depth quote representing bid or ask.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str273Key)]
	[DescriptionLoc(LocalizedStrings.Str274Key)]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Obsolete("Use QuoteChange type.")]
	public class Quote : Cloneable<Quote>, IExtendableEntity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Quote"/>.
		/// </summary>
		public Quote()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Quote"/>.
		/// </summary>
		/// <param name="security">The instrument by which the quote is received.</param>
		/// <param name="price">Quote price.</param>
		/// <param name="volume">Quote volume.</param>
		/// <param name="side">Direction (buy or sell).</param>
		/// <param name="ordersCount">Orders count.</param>
		/// <param name="condition">Condition.</param>
		public Quote(Security security, decimal price, decimal volume, Sides side, int? ordersCount = null, QuoteConditions condition = default)
		{
			Security = security;
			Price = price;
			Volume = volume;
			OrderDirection = side;
			OrdersCount = ordersCount;
			Condition = condition;
		}

		/// <summary>
		/// The instrument by which the quote is received.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public Security Security { get; set; }

		/// <summary>
		/// Quote price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.Str275Key)]
		[MainCategory]
		public decimal Price { get; set; }

		/// <summary>
		/// Quote volume.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str276Key)]
		[MainCategory]
		public decimal Volume { get; set; }

		/// <summary>
		/// Direction (buy or sell).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str277Key)]
		[MainCategory]
		public Sides OrderDirection { get; set; }

		[field: NonSerialized]
		private IDictionary<string, object> _extensionInfo;

		/// <inheritdoc />
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		[Obsolete]
		public IDictionary<string, object> ExtensionInfo
		{
			get => _extensionInfo;
			set => _extensionInfo = value;
		}

		/// <summary>
		/// Orders count.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? OrdersCount { get; set; }

		/// <summary>
		/// Quote condition.
		/// </summary>
		[DataMember]
		public QuoteConditions Condition { get; set; }

		/// <summary>
		/// Create a copy of <see cref="Quote"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Quote Clone()
		{
			var clone = new Quote(Security, Price, Volume, OrderDirection, OrdersCount, Condition);
			this.CopyExtensionInfo(clone);
			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var type = OrderDirection == Sides.Buy ? LocalizedStrings.Bid : LocalizedStrings.Ask;
			return $"{type} {Price} {Volume}";
		}
	}
}
