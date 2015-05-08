namespace StockSharp.Studio.Configuration
{
	using System.Configuration;

	class ConnectionElementCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new ConnectionElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			var elem = (ConnectionElement)element;
			return elem.TransactionAdapter ?? elem.MarketDataAdapter;
		}
	}
}