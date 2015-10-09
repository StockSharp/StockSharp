namespace StockSharp.Rss
{
	using System;
	using System.Linq;

	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface <see cref="IConnector"/> implementation which provides a connection to RSS feed.
	/// </summary>
	[Icon("Rss_logo.png")]
	public class RssTrader : Connector
    {
		/// <summary>
		/// Initializes a new instance of the <see cref="RssTrader"/>.
		/// </summary>
		public RssTrader()
		{
			Adapter.InnerAdapters.Add(new RssMarketDataMessageAdapter(TransactionIdGenerator));
		}

		private RssMarketDataMessageAdapter NativeAdapter
		{
			get { return Adapter.InnerAdapters.OfType<RssMarketDataMessageAdapter>().First(); }
		}

		/// <summary>
		/// RSS feed address.
		/// </summary>
		public Uri Address
		{
			get { return NativeAdapter.Address; }
			set { NativeAdapter.Address = value; }
		}

		/// <summary>
		/// The format of dates. Should be fill in, if the format is different from ddd, dd MMM yyyy hh:mm:ss.
		/// </summary>
		public string CustomDateFormat
		{
			get { return NativeAdapter.CustomDateFormat; }
			set { NativeAdapter.CustomDateFormat = value; }
		}
    }
}