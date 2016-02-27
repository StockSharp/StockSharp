#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: Level1FieldsComboBox.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Linq;
	using System.Windows;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit;
	using Xceed.Wpf.Toolkit.Primitives;

	using ComboItem = System.Collections.Generic.KeyValuePair<StockSharp.Messages.Level1Fields, string>;

	/// <summary>
	/// The drop-down list to select a set of fields <see cref="Level1Fields"/>.
	/// </summary>
	public class Level1FieldsComboBox : CheckComboBox
	{
		private static readonly IDictionary<Level1Fields, string> _allFields;

		static Level1FieldsComboBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Level1FieldsComboBox), new FrameworkPropertyMetadata(typeof(Level1FieldsComboBox)));

			_allFields = Enumerator.GetValues<Level1Fields>().ToDictionary(t => t, t => t.GetDisplayName());

			DefaultFields = _allFields.Keys.Except(new[]
			{
				Level1Fields.LastTrade,
				Level1Fields.LastTradeId,
				Level1Fields.LastTradeOrigin,
				Level1Fields.LastTradePrice,
				Level1Fields.LastTradeTime,
				Level1Fields.LastTradeUpDown,
				Level1Fields.LastTradeVolume,
				Level1Fields.BestBid,
				Level1Fields.BestBidPrice,
				Level1Fields.BestBidTime,
				Level1Fields.BestBidVolume,
				Level1Fields.BestAsk,
				Level1Fields.BestAskPrice,
				Level1Fields.BestAskTime,
				Level1Fields.BestAskVolume
			}).ToArray();

			SelectedFieldsProperty = DependencyProperty.Register(nameof(SelectedFields), typeof(IEnumerable<Level1Fields>), typeof(Level1FieldsComboBox), new UIPropertyMetadata(DefaultFields, (s, e) =>
			{
				var comboBox = s as Level1FieldsComboBox;

				if (comboBox == null)
					return;

				var count = 0;

				var prevItems = comboBox.SelectedItems.Cast<ComboItem>().ToList();

				foreach (var field in (IEnumerable<Level1Fields>)e.NewValue)
				{
					var item = new ComboItem(field, field.GetDisplayName());

					if (!comboBox.SelectedItems.Contains(item))
						comboBox.SelectedItems.Add(item);

					prevItems.Remove(item);

					count++;
				}

				prevItems.ForEach(item => comboBox.SelectedItems.Remove(item));

				comboBox.Text = LocalizedStrings.Str1544 + count;
			}));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Level1FieldsComboBox"/>.
		/// </summary>
		public Level1FieldsComboBox()
		{
			ValueMemberPath = "Key";
			DisplayMemberPath = "Value";

			ItemsSource = _allFields;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Level1FieldsComboBox.SelectedFields"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedFieldsProperty;

		/// <summary>
		/// The list of selected fields.
		/// </summary>
		public IEnumerable<Level1Fields> SelectedFields
		{
			get { return (IEnumerable<Level1Fields>)GetValue(SelectedFieldsProperty); }
			set { SetValue(SelectedFieldsProperty, value.ToArray()); }
		}

		/// <summary>
		/// The set of selected default fields.
		/// </summary>
		public static IEnumerable<Level1Fields> DefaultFields { get; }

		/// <summary>
		/// The method that is called when the value of the item is changed.
		/// </summary>
		/// <param name="e">Arguments.</param>
		protected override void OnItemSelectionChanged(ItemSelectionChangedEventArgs e)
		{
			base.OnItemSelectionChanged(e);
			SelectedFields = SelectedItems.Cast<ComboItem>().Select(p => p.Key);
		}
	}
}