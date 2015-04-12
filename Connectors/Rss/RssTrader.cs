namespace StockSharp.Rss
{
	using System;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с RSS фидами.
	/// </summary>
	public class RssTrader : Connector
    {
		private readonly RssMarketDataMessageAdapter _adapter;

		/// <summary>
		/// Создать <see cref="RssTrader"/>.
		/// </summary>
		public RssTrader()
		{
			TransactionAdapter = new PassThroughMessageAdapter(TransactionIdGenerator) { IsMarketDataEnabled = false };
			
			_adapter = new RssMarketDataMessageAdapter(TransactionIdGenerator);
			MarketDataAdapter = _adapter.ToChannel(this);
		}

		/// <summary>
		/// Адрес RSS фида.
		/// </summary>
		public Uri Address
		{
			get { return _adapter.Address; }
			set { _adapter.Address = value; }
		}

		/// <summary>
		/// Формат дат. Необходимо заполнить, если формат RSS потока отличается от ddd, dd MMM yyyy hh:mm:ss.
		/// </summary>
		public string CustomDateFormat
		{
			get { return _adapter.CustomDateFormat; }
			set { _adapter.CustomDateFormat = value; }
		}
    }
}