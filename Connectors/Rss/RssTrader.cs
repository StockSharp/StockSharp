namespace StockSharp.Rss
{
	using System;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

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
			_adapter = new RssMarketDataMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(_adapter.ToChannel(this));
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