namespace StockSharp.Studio.Configuration
{
	using System.Configuration;

	class CandleElementCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new CandleElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			var elem = (CandleElement)element;
			return elem.Type;
		}
	}
}