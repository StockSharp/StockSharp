namespace StockSharp.Rss
{
	using System;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface <see cref="IConnector"/> implementation which provides a connection to RSS feed.
	/// </summary>
	public class RssTrader : Connector
    {
		private readonly RssMarketDataMessageAdapter _adapter;

		/// <summary>
		/// Initializes a new instance of the <see cref="RssTrader"/>.
		/// </summary>
		public RssTrader()
		{
			_adapter = new RssMarketDataMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(_adapter.ToChannel(this));
		}

		/// <summary>
		/// RSS feed address.
		/// </summary>
		public Uri Address
		{
			get { return _adapter.Address; }
			set { _adapter.Address = value; }
		}

		/// <summary>
		/// The format of dates. Should be fill in, if the format is different from ddd, dd MMM yyyy hh:mm:ss.
		/// </summary>
		public string CustomDateFormat
		{
			get { return _adapter.CustomDateFormat; }
			set { _adapter.CustomDateFormat = value; }
		}
    }
}