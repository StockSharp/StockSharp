namespace StockSharp.Algo.Storages.Backup.Xaml
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;

	using Xceed.Wpf.Toolkit.PropertyGrid;
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// The drop-down list to select the AWS region.
	/// </summary>
	public class AmazonRegionEditor : ITypeEditor
	{
		FrameworkElement ITypeEditor.ResolveEditor(PropertyItem propertyItem)
		{
			var comboBox = new AmazonRegionComboBox { IsEditable = false, Width = double.NaN };

			var binding = new Binding("Value")
			{
				Source = propertyItem,
				//Converter = new EndPointValueConverter(comboBox, SmartComAddresses.Matrix),
				Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
			};

			BindingOperations.SetBinding(comboBox, ComboBox.TextProperty, binding);
			return comboBox;
		}
	}
}