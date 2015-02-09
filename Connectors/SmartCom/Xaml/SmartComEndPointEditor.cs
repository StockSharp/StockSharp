namespace StockSharp.SmartCom.Xaml
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;

	using Ecng.Xaml;

	using StockSharp.SmartCom;

	using Xceed.Wpf.Toolkit.PropertyGrid;
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// ¬ыпадающий список дл€ выбора адреса сервера SmartCOM.
	/// </summary>
	public class SmartComEndPointEditor : ITypeEditor
	{
		FrameworkElement ITypeEditor.ResolveEditor(PropertyItem propertyItem)
		{
			var comboBox = new SmartComAddressComboBox { IsEditable = true, Width = double.NaN };

			var binding = new Binding("Value")
			{
				Source = propertyItem,
				Converter = new EndPointValueConverter(comboBox, SmartComAddresses.Matrix),
				Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
			};

			BindingOperations.SetBinding(comboBox, ComboBox.TextProperty, binding);
			return comboBox;
		}
	}
}