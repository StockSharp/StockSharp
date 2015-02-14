namespace StockSharp.Rss.Xaml
{
	using System;

	using Ecng.Xaml;

	/// <summary>
	/// Выпадающий список для выбора адреса RSS фида.
	/// </summary>
	public class RssAddressComboBox : AddressComboBox<Uri>
	{
		/// <summary>
		/// Создать <see cref="RssAddressComboBox"/>.
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
			AddAddress(RssAddresses.TrueFlipper, "True-Flipper");
			AddAddress(RssAddresses.SmartLab, "Smart-Lab");
			AddAddress(RssAddresses.H2T, "H2T");

			SelectedAddress = RssAddresses.Reuters;

			IsEditable = true;
		}
	}
}