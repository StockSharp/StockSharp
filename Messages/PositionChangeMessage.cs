#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: PositionChangeMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Type of the changes in <see cref="PositionChangeMessage"/>.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public enum PositionChangeTypes
	{
		/// <summary>
		/// Initial value.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str253Key)]
		BeginValue,

		/// <summary>
		/// Current value.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str254Key)]
		CurrentValue,

		/// <summary>
		/// Blocked.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str255Key)]
		BlockedValue,

		/// <summary>
		/// Position price.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str256Key)]
		CurrentPrice,

		/// <summary>
		/// Average price.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AveragePriceKey)]
		AveragePrice,

		/// <summary>
		/// Unrealized profit.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str258Key)]
		UnrealizedPnL,

		/// <summary>
		/// Realized profit.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str259Key)]
		RealizedPnL,

		/// <summary>
		/// Variation margin.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str260Key)]
		VariationMargin,

		/// <summary>
		/// Currency.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CurrencyKey)]
		Currency,

		/// <summary>
		/// Extended information.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ExtendedInfoKey)]
		[Obsolete]
		ExtensionInfo,

		/// <summary>
		/// Margin leverage.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str261Key)]
		Leverage,

		/// <summary>
		/// Total commission.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str262Key)]
		Commission,

		/// <summary>
		/// Current value (in lots).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str263Key)]
		CurrentValueInLots,

		/// <summary>
		/// The depositary where the physical security.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str264Key)]
		[Obsolete]
		DepoName,

		/// <summary>
		/// Portfolio state.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str265Key)]
		State,

		/// <summary>
		/// Expiration date.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ExpiryDateKey)]
		ExpirationDate,

		/// <summary>
		/// Commission (taker).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CommissionTakerKey)]
		CommissionTaker,

		/// <summary>
		/// Commission (maker).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CommissionMakerKey)]
		CommissionMaker,

		/// <summary>
		/// Settlement price.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str312Key)]
		SettlementPrice,

		/// <summary>
		/// Orders (bids).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrdersBidsKey)]
		BuyOrdersCount,
		
		/// <summary>
		/// Orders (asks).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrdersAsksKey)]
		SellOrdersCount,
		
		/// <summary>
		/// Margin (buy).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str304Key)]
		BuyOrdersMargin,
		
		/// <summary>
		/// Margin (sell).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str305Key)]
		SellOrdersMargin,
		
		/// <summary>
		/// Orders (margin).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrdersMarginKey)]
		OrdersMargin,

		/// <summary>
		/// Orders.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str668Key)]
		OrdersCount,

		/// <summary>
		/// Trades.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str985Key)]
		TradesCount,
	}

	/// <summary>
	/// The message contains information about the position changes.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.Str862Key)]
	[DescriptionLoc(LocalizedStrings.PositionDescKey)]
	public class PositionChangeMessage : BaseChangeMessage<PositionChangeMessage,
		PositionChangeTypes>, IPortfolioNameMessage, ISecurityIdMessage, IStrategyIdMessage
	{
		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str247Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[DataMember]
		[MainCategory]
		[DisplayNameLoc(LocalizedStrings.ClientCodeKey)]
		[DescriptionLoc(LocalizedStrings.ClientCodeDescKey)]
		public string ClientCode { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// The depositary where the physical security.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str264Key)]
		[DescriptionLoc(LocalizedStrings.DepoNameKey)]
		[MainCategory]
		public string DepoName { get; set; }

		/// <summary>
		/// Limit type for Т+ market.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str266Key)]
		[DescriptionLoc(LocalizedStrings.Str267Key)]
		[MainCategory]
		[Nullable]
		public TPlusLimits? LimitType { get; set; }

		/// <summary>
		/// Text position description.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DescriptionKey)]
		[DescriptionLoc(LocalizedStrings.Str269Key)]
		[MainCategory]
		public string Description { get; set; }

		/// <summary>
		/// Electronic board code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey, true)]
		[MainCategory]
		public string BoardCode { get; set; }

		/// <inheritdoc />
		[DataMember]
		public string StrategyId { get; set; }

		/// <summary>
		/// Side.
		/// </summary>
		[DataMember]
		public Sides? Side { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionChangeMessage"/>.
		/// </summary>
		public PositionChangeMessage()
			: this(MessageTypes.PositionChange)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionChangeMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected PositionChangeMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.PositionChanges;

		/// <summary>
		/// Create a copy of <see cref="PositionChangeMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new PositionChangeMessage();

			CopyTo(clone);

			return clone;
		}

		/// <inheritdoc />
		public override void CopyTo(PositionChangeMessage destination)
		{
			base.CopyTo(destination);

			destination.SecurityId = SecurityId;
			destination.DepoName = DepoName;
			destination.LimitType = LimitType;
			destination.Description = Description;
			destination.PortfolioName = PortfolioName;
			destination.ClientCode = ClientCode;
			destination.BoardCode = BoardCode;
			destination.StrategyId = StrategyId;
			destination.Side = Side;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",Sec={SecurityId},P={PortfolioName},CL={ClientCode},L={LimitType},Changes={Changes.Select(c => c.ToString()).JoinComma()}";

			if (!StrategyId.IsEmpty())
				str += $",Strategy={StrategyId}";

			if (!Description.IsEmpty())
				str += $",Description={Description}";

			if (Side != null)
				str += $",Side={Side}";

			return str;
		}
	}
}