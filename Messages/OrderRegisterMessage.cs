#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderRegisterMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The message containing the information for the order registration.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class OrderRegisterMessage : OrderMessage
	{
		/// <summary>
		/// Order price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.OrderPriceKey)]
		[MainCategory]
		public decimal Price { get; set; }

		/// <summary>
		/// Number of contracts in the order.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.OrderVolumeKey)]
		[MainCategory]
		public decimal Volume { get; set; }

		/// <summary>
		/// Visible quantity of contracts in order.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VisibleVolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str127Key)]
		[MainCategory]
		[Nullable]
		public decimal? VisibleVolume { get; set; }

		/// <summary>
		/// Order side (buy or sell).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str129Key)]
		[MainCategory]
		public Sides Side { get; set; }

		/// <summary>
		/// Order expiry time. The default is <see langword="null" />, which mean (GTC).
		/// </summary>
		/// <remarks>
		/// If the value is equal <see langword="null" />, order will be GTC (good til cancel). Or uses exact date.
		/// </remarks>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str141Key)]
		[DescriptionLoc(LocalizedStrings.Str142Key)]
		[MainCategory]
		public DateTimeOffset? TillDate { get; set; }

		/// <summary>
		/// Limit order time in force.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TimeInForceKey)]
		[DescriptionLoc(LocalizedStrings.Str232Key)]
		[MainCategory]
		[Nullable]
		public TimeInForce? TimeInForce { get; set; }

		/// <summary>
		/// Is the order of market-maker.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarketMakerKey)]
		[DescriptionLoc(LocalizedStrings.MarketMakerOrderKey, true)]
		[MainCategory]
		public bool? IsMarketMaker { get; set; }

		/// <summary>
		/// Slippage in trade price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str163Key)]
		[DescriptionLoc(LocalizedStrings.Str164Key)]
		public decimal? Slippage { get; set; }

		/// <summary>
		/// Is order manual.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ManualKey)]
		[DescriptionLoc(LocalizedStrings.IsOrderManualKey)]
		public bool? IsManual { get; set; }

		/// <summary>
		/// Minimum quantity of an order to be executed.
		/// </summary>
		[DataMember]
		public decimal? MinOrderVolume { get; set; }

		/// <summary>
		/// Position effect.
		/// </summary>
		[DataMember]
		public OrderPositionEffects? PositionEffect { get; set; }

		/// <summary>
		/// Post-only order.
		/// </summary>
		[DataMember]
		public bool? PostOnly { get; set; }

		/// <summary>
		/// Margin leverage.
		/// </summary>
		[DataMember]
		public int? Leverage { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderRegisterMessage"/>.
		/// </summary>
		public OrderRegisterMessage()
			: base(MessageTypes.OrderRegister)
		{
		}

		/// <summary>
		/// Initialize <see cref="OrderRegisterMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected OrderRegisterMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="OrderRegisterMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new OrderRegisterMessage(Type);
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public void CopyTo(OrderRegisterMessage destination)
		{
			base.CopyTo(destination);

			destination.Price = Price;
			destination.Volume = Volume;
			destination.VisibleVolume = VisibleVolume;
			destination.Side = Side;
			destination.TillDate = TillDate;
			destination.TimeInForce = TimeInForce;
			destination.IsMarketMaker = IsMarketMaker;
			destination.Slippage = Slippage;
			destination.IsManual = IsManual;
			destination.MinOrderVolume = MinOrderVolume;
			destination.PositionEffect = PositionEffect;
			destination.PostOnly = PostOnly;
			destination.Leverage = Leverage;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",Price={Price},Side={Side},Vol={Volume}/{VisibleVolume}/{MinOrderVolume},Till={TillDate},TIF={TimeInForce},MM={IsMarketMaker},SLP={Slippage},MN={IsManual}";

			if (PositionEffect != null)
				str += $",PosEffect={PositionEffect.Value}";

			if (PostOnly != null)
				str += $",PostOnly={PostOnly.Value}";

			if (Leverage != null)
				str += $",Leverage={Leverage.Value}";

			return str;
		}
	}
}