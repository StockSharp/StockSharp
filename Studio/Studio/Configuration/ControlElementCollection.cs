namespace StockSharp.Studio.Configuration
{
	using System.Configuration;

	class ControlElementCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new ControlElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			var elem = (ControlElement)element;
			return elem.Type;
		}
	}
}