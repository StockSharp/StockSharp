#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Transaq.TransaqPublic
File: ProxyItemsSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Transaq
{
	using Ecng.Common;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Transaq;

	internal class ProxyItemsSource : IItemsSource
	{
		public ItemCollection GetValues()
		{
			var items = new ItemCollection();

			foreach (var s in Enumerator.GetNames<ProxyTypes>())
				items.Add(s);

			return items;
		}
	}
}
