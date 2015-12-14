#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.PropertyGrid.Xaml
File: INotifyPropertiesChanged.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml.PropertyGrid
{
	using System;

	/// <summary>
	/// The interface describing the type with a variable number of properties.
	/// </summary>
	public interface INotifyPropertiesChanged
	{
		/// <summary>
		/// The available properties change event.
		/// </summary>
		event Action PropertiesChanged;
	}
}