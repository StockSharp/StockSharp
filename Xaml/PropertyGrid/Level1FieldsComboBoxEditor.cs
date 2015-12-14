#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.PropertyGrid.Xaml
File: Level1FieldsComboBoxEditor.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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