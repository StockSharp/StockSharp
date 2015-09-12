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