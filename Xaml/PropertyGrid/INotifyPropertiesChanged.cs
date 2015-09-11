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