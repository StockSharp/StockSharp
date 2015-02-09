namespace StockSharp.Xaml.PropertyGrid
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> для <see cref="Level1FieldsComboBox"/>.
	/// </summary>
	public class Level1FieldsComboBoxEditor : TypeEditor<Level1FieldsComboBox>
	{
		/// <summary>
		/// Установить <see cref="TypeEditor{T}.ValueProperty"/> значением <see cref="Level1FieldsComboBox.SelectedFieldsProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = Level1FieldsComboBox.SelectedFieldsProperty;
		}
	}
}