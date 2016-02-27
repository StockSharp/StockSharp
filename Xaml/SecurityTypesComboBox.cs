#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: SecurityTypesComboBox.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit;
	using Xceed.Wpf.Toolkit.Primitives;

	using ComboItem = System.Collections.Generic.KeyValuePair<StockSharp.Messages.SecurityTypes, string>;

	/// <summary>
	/// The drop-down list to select the instrument types.
	/// </summary>
	public class SecurityTypesComboBox : CheckComboBox
	{
		private static readonly IDictionary<SecurityTypes, string> _allTypes;

		static SecurityTypesComboBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SecurityTypesComboBox), new FrameworkPropertyMetadata(typeof(SecurityTypesComboBox)));

			_allTypes = Enumerator.GetValues<SecurityTypes>().ToDictionary(t => t, t => t.GetDisplayName());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityTypesComboBox"/>.
		/// </summary>
		public SecurityTypesComboBox()
		{
			ValueMemberPath = "Key";
			DisplayMemberPath = "Value";

			ItemsSource = _allTypes;

			SelectedTypes = Enumerable.Empty<SecurityTypes>();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="SecurityTypesComboBox.SelectedTypes"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedTypesProperty = DependencyProperty.Register(nameof(SelectedTypes),
			typeof(IEnumerable<SecurityTypes>), typeof(SecurityTypesComboBox), new UIPropertyMetadata((s, e) =>
			{
				var comboBox = s as SecurityTypesComboBox;

				if (comboBox == null)
					return;

				var count = 0;

				var prevItems = comboBox.SelectedItems.Cast<ComboItem>().ToList();

				foreach (var type in (IEnumerable<SecurityTypes>)e.NewValue)
				{
					var item = new ComboItem(type, type.GetDisplayName());

					if (!comboBox.SelectedItems.Contains(item))
						comboBox.SelectedItems.Add(item);

					prevItems.Remove(item);

					count++;
				}

				prevItems.ForEach(item => comboBox.SelectedItems.Remove(item));

				comboBox.Text = LocalizedStrings.Str1544 + count;
			}));

		/// <summary>
		/// List of selected types.
		/// </summary>
		public IEnumerable<SecurityTypes> SelectedTypes
		{
			get { return (IEnumerable<SecurityTypes>)GetValue(SelectedTypesProperty); }
			set { SetValue(SelectedTypesProperty, value.ToArray()); }
		}

		/// <summary>
		/// The method that is called when the value of the item is changed.
		/// </summary>
		/// <param name="e">Arguments.</param>
		protected override void OnItemSelectionChanged(ItemSelectionChangedEventArgs e)
		{
			base.OnItemSelectionChanged(e);
			SelectedTypes = SelectedItems.Cast<ComboItem>().Select(p => p.Key);
		}
	}
}