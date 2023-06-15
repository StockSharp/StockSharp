#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: Trade.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Tick trade.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str506Key)]
	[DescriptionLoc(LocalizedStrings.TickTradeKey)]
	public class Trade : Cloneable<Trade>, ITickTradeMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Trade"/>.
		/// </summary>
		public Trade()
		{
		}

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str361Key,
			Description = LocalizedStrings.Str145Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public long Id { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.OrderIdStringKey,
			Description = LocalizedStrings.Str146Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 1)]
		public string StringId { get; set; }

		/// <inheritdoc />
		SecurityId ISecurityIdMessage.SecurityId
		{
			get => Security?.Id.ToSecurityId() ?? default;
			set => throw new NotSupportedException();
		}

		/// <summary>
		/// The instrument, on which the trade was completed.
		/// </summary>
		[XmlIgnore]
		[Browsable(false)]
		public Security Security { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TimeKey,
			Description = LocalizedStrings.Str605Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 3)]
		public DateTimeOffset ServerTime { get; set; }

		/// <inheritdoc />
		[Browsable(false)]
		public DateTimeOffset Time
		{
			get => ServerTime;
			set => ServerTime = value;
		}

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str514Key,
			Description = LocalizedStrings.Str606Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 9)]
		public DateTimeOffset LocalTime { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.VolumeKey,
			Description = LocalizedStrings.TradeVolumeKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 4)]
		public decimal Volume { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PriceKey,
			Description = LocalizedStrings.Str147Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 3)]
		public decimal Price { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str128Key,
			Description = LocalizedStrings.Str608Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 5)]
		public Sides? OriginSide { get; set; }

		/// <inheritdoc />
		[Browsable(false)]
		public Sides? OrderDirection
		{
			get => OriginSide;
			set => OriginSide = value;
		}

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SystemTradeKey,
			Description = LocalizedStrings.IsSystemTradeKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 6)]
		public bool? IsSystem { get; set; }

		/// <inheritdoc />
		[Browsable(false)]
		public int? Status { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str150Key,
			Description = LocalizedStrings.Str151Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 10)]
		public decimal? OpenInterest { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str157Key,
			Description = LocalizedStrings.Str158Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 11)]
		public bool? IsUpTick { get; set; }

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CurrencyKey,
			Description = LocalizedStrings.Str382Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 7)]
		public CurrencyTypes? Currency { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long SeqNum { get; set; }

		/// <inheritdoc />
		public Messages.DataType BuildFrom { get; set; }

		/// <inheritdoc />
		[DataMember]
		public decimal? Yield { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long? OrderBuyId { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long? OrderSellId { get; set; }

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
				ServerTime = ServerTime,
				LocalTime = LocalTime,
				OriginSide = OriginSide,
				Security = Security,
				IsSystem = IsSystem,
				Status = Status,
				OpenInterest = OpenInterest,
				IsUpTick = IsUpTick,
				Currency = Currency,
				SeqNum = SeqNum,
				BuildFrom = BuildFrom,
				Yield = Yield,
				OrderBuyId = OrderBuyId,
				OrderSellId = OrderSellId,
			};
		}

		/// <summary>
		/// Get the hash code of the object <see cref="Trade"/>.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode()
		{
			return (Security?.GetHashCode() ?? 0) ^ Id.GetHashCode();
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var idStr = Id == 0 ? StringId : Id.To<string>();
			return $"{ServerTime} {idStr} {Price} {Volume}";
		}
	}
}