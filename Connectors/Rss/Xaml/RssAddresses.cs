namespace StockSharp.Rss.Xaml
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// јдреса RSS попул€рных фидов.
	/// </summary>
	public static class RssAddresses
	{
		/// <summary>
		/// Reuters (ecomony).
		/// </summary>
		public static readonly Uri Reuters = "http://feeds.reuters.com/news/economy?format=xml".To<Uri>();

		/// <summary>
		/// Bloomberg.
		/// </summary>
		public static readonly Uri Bloomberg = "http://www.bloomberg.com/feed/podcast/etf-report.xml".To<Uri>();

		/// <summary>
		/// NYSE.
		/// </summary>
		public static readonly Uri Nyse = "http://markets.nyx.com/content/msa_traderupdates/all/all/rss.xml".To<Uri>();

		/// <summary>
		/// NASDAQ (Stocks).
		/// </summary>
		public static readonly Uri Nasdaq = "http://articlefeeds.nasdaq.com/nasdaq/categories?category=Stocks&format=xml".To<Uri>();

		/// <summary>
		/// MOEX.
		/// </summary>
		public static readonly Uri Moex = "http://moex.com/export/news.aspx".To<Uri>();

		/// <summary>
		/// Trading Economics (Russia).
		/// </summary>
		public static readonly Uri TradingEconomics = "http://www.tradingeconomics.com/russia/rss".To<Uri>();

		/// <summary>
		/// Technical Traders.
		/// </summary>
		public static readonly Uri TechnicalTraders = "http://www.thetechnicaltraders.com/feed/".To<Uri>();

		/// <summary>
		/// DailyFX.
		/// </summary>
		public static readonly Uri DailyFX = "http://www.dailyfx.com/feeds/all".To<Uri>();

		/// <summary>
		/// Trading Floor.
		/// </summary>
		public static readonly Uri TradingFloor = "http://www.tradingfloor.com/blogs/rss/extract".To<Uri>();

		/// <summary>
		/// MarketWatch.
		/// </summary>
		public static readonly Uri MarketWatch = "http://feeds.marketwatch.com/marketwatch/bulletins?format=xml".To<Uri>();

		/// <summary>
		/// True-Flipper.
		/// </summary>
		public static readonly Uri TrueFlipper = "http://true-flipper.livejournal.com/data/rss".To<Uri>();

		/// <summary>
		/// Smart-Lab.
		/// </summary>
		public static readonly Uri SmartLab = "http://smart-lab.ru/allsignals/rss/".To<Uri>();

		/// <summary>
		/// H2T.
		/// </summary>
		public static readonly Uri H2T = "http://www.h2t.ru/rss/".To<Uri>();
	}
}