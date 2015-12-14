#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rss.Rss
File: RssTrader.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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