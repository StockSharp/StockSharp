namespace StockSharp.Xaml.PropertyGrid
{
	using System;

	/// <summary>
	/// Интерфейс, описывающий тип с переменным количеством свойств.
	/// </summary>
	public interface INotifyPropertiesChanged
	{
		/// <summary>
		/// Событие изменения доступных свойств.
		/// </summary>
		event Action PropertiesChanged;
	}
}