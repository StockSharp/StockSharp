namespace StockSharp.Hydra.Transaq
{
	using System;
	
	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Transaq;

	internal class ProxyItemsSource : IItemsSource
	{
		public ItemCollection GetValues()
		{
			var items = new ItemCollection();

			foreach (var s in Enum.GetNames(typeof (ProxyTypes)))
			{
				items.Add(s);
			}

			return items;
		}
	}
}
