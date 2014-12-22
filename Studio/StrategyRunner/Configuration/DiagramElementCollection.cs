namespace StockSharp.Studio.StrategyRunner.Configuration
{
	using System.Configuration;

	class DiagramElementCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new DiagramElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			var elem = (DiagramElement)element;
			return elem.Type;
		}
	}
}