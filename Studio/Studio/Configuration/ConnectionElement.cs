namespace StockSharp.Studio.Configuration
{
	using System.Configuration;

	class ConnectionElement : ConfigurationElement
	{
		private const string _transactionAdapterKey = "transactionAdapter";

		[ConfigurationProperty(_transactionAdapterKey)]
		public string TransactionAdapter
		{
			get { return (string)this[_transactionAdapterKey]; }
			set { this[_transactionAdapterKey] = value; }
		}

		private const string _marketDataAdapterKey = "marketDataAdapter";

		[ConfigurationProperty(_marketDataAdapterKey)]
		public string MarketDataAdapter
		{
			get { return (string)this[_marketDataAdapterKey]; }
			set { this[_marketDataAdapterKey] = value; }
		}
	}
}