#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rss.Xaml.Rss
File: RssAddressComboBox.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Rss.Xaml
{
	using System;

	using Ecng.Xaml;

	/// <summary>
	/// The drop-down list to select the RSS feed address.
	/// </summary>
	public class RssAddressComboBox : AddressComboBox<Uri>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RssAddressComboBox"/>.
		/// </summary>
		public RssAddressComboBox()
		{
			AddAddress(RssAddresses.Reuters, "Reuters");
			AddAddress(RssAddresses.Bloomberg, "Bloomberg");
			AddAddress(RssAddresses.Nyse, "NYSE");
			AddAddress(RssAddresses.Nasdaq, "NASDAQ");
			AddAddress(RssAddresses.Moex, "MOEX");
			AddAddress(RssAddresses.TradingEconomics, "TradingEconomics");
			AddAddress(RssAddresses.TechnicalTraders, "TechnicalTraders");
			AddAddress(RssAddresses.DailyFX, "DailyFX");
			AddAddress(RssAddresses.TradingFloor, "TradingFloor");
			AddAddress(RssAddresses.MarketWatch, "MarketWatch");
			AddAddress(RssAddresses.SmartLab, "Smart-Lab");
			AddAddress(RssAddresses.H2T, "H2T");

			SelectedAddress = RssAddresses.Reuters;

			IsEditable = true;
		}
	}
}