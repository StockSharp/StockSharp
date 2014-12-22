namespace StockSharp.Hydra.Configuration
{
	using System.Configuration;

	class IndicatorElementCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new IndicatorElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			var elem = (IndicatorElement)element;
			return elem.Type;
		}
	}
}