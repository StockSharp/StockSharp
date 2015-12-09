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
	/// Tick trade.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str506Key)]
	[DescriptionLoc(LocalizedStrings.TickTradeKey)]
	public class Trade : Cloneable<Trade>, IExtendableEntity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Trade"/>.
		/// </summary>
		public Trade()
		{
		}

		/// <summary>
		/// Trade ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str361Key)]
		[DescriptionLoc(LocalizedStrings.Str145Key)]
		[MainCategory]
		[Identity]
		[PropertyOrder(0)]
		public long Id { get; set; }

		/// <summary>
		/// Trade ID (as string, if electronic board does not use numeric order ID representation).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OrderIdStringKey)]
		[DescriptionLoc(LocalizedStrings.Str146Key)]
		[MainCategory]
		[PropertyOrder(1)]
		public string StringId { get; set; }

		/// <summary>
		/// The instrument, on which the trade was completed.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[XmlIgnore]
		[Browsable(false)]
		[PropertyOrder(2)]
		public Security Security { get; set; }

		/// <summary>
		/// Trade time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TimeKey)]
		[DescriptionLoc(LocalizedStrings.Str605Key)]
		[MainCategory]
		[PropertyOrder(3)]
		public DateTimeOffset Time { get; set; }

		/// <summary>
		/// Trade received local time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str514Key)]
		[DescriptionLoc(LocalizedStrings.Str606Key)]
		[MainCategory]
		[PropertyOrder(9)]
		public DateTimeOffset LocalTime { get; set; }

		/// <summary>
		/// Number of contracts in a trade.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.TradeVolumeKey)]
		[MainCategory]
		[PropertyOrder(4)]
		public decimal Volume { get; set; }

		/// <summary>
		/// Trade price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.Str147Key)]
		[MainCategory]
		[PropertyOrder(3)]
		public decimal Price { get; set; }

		/// <summary>
		/// Order side (buy or sell), which led to the trade.
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str608Key)]
		[MainCategory]
		[PropertyOrder(5)]
		public Sides? OrderDirection { get; set; }

		/// <summary>
		/// Is a system trade.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SystemTradeKey)]
		[DescriptionLoc(LocalizedStrings.IsSystemTradeKey)]
		[MainCategory]
		[PropertyOrder(6)]
		[Nullable]
		public bool? IsSystem { get; set; }

		/// <summary>
		/// System trade status.
		/// </summary>
		[Browsable(false)]
		[Nullable]
		public int? Status { get; set; }

		/// <summary>
		/// Number of open positions (open interest).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str150Key)]
		[DescriptionLoc(LocalizedStrings.Str151Key)]
		[MainCategory]
		[Nullable]
		[PropertyOrder(7)]
		public decimal? OpenInterest { get; set; }

		/// <summary>
		/// Is tick ascending or descending in price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str157Key)]
		[DescriptionLoc(LocalizedStrings.Str158Key)]
		[MainCategory]
		[Nullable]
		[PropertyOrder(8)]
		public bool? IsUpTick { get; set; }

		/// <summary>
		/// Trading security currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str382Key)]
		[MainCategory]
		[Nullable]
		public CurrencyTypes? Currency { get; set; }

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Extended trade info.
		/// </summary>
		/// <remarks>
		/// Required if additional information associated with the trade is stored in the program. For example, the operation that results to the trade.
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
		/// Create a copy of <see cref="Trade"/>.
		/// </summary>
		/// <returns>Copy.</returns>
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
				Currency = Currency,
			};
		}

		/// <summary>
		/// Get the hash code of the object <see cref="Trade"/>.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode()
		{
			return (Security != null ? Security.GetHashCode() : 0) ^ Id.GetHashCode();
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "{0} {1} {2} {3}".Put(Time, Id, Price, Volume);
		}
	}
}