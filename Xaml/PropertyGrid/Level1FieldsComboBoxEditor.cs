namespace StockSharp.Xaml.PropertyGrid
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> for <see cref="Level1FieldsComboBox"/>.
	/// </summary>
	public class Level1FieldsComboBoxEditor : TypeEditor<Level1FieldsComboBox>
	{
		/// <summary>
		/// To set <see cref="TypeEditor{T}.ValueProperty"/> with the <see cref="Level1FieldsComboBox.SelectedFieldsProperty"/> value.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = Level1FieldsComboBox.SelectedFieldsProperty;
		}
	}
}