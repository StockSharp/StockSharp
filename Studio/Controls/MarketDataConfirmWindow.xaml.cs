namespace StockSharp.Studio.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Messages;

	public partial class MarketDataConfirmWindow
	{
		private readonly PairSet<CheckBox, SecurityTypes> _checkBoxes = new PairSet<CheckBox, SecurityTypes>();

		public MarketDataConfirmWindow()
		{
			InitializeComponent();

			foreach (var type in Enumerator.GetValues<SecurityTypes>())
			{
				var checkBox = new CheckBox { Content = type.GetDisplayName() };
				checkBox.Checked += CheckBoxOnChecked;
				checkBox.Unchecked += CheckBoxOnChecked;

				SecTypes.Children.Add(checkBox);
				_checkBoxes.Add(checkBox, type);
			}
		}

		private void CheckBoxOnChecked(object sender, RoutedEventArgs e)
		{
			OkBtn.IsEnabled = SecurityTypes.Any();
		}

		public IEnumerable<SecurityTypes> SecurityTypes
		{
			get { return _checkBoxes.Where(p => p.Key.IsChecked == true).Select(p => p.Value); }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_checkBoxes.ForEach(p => p.Key.IsChecked = false);

				foreach (var type in value)
					_checkBoxes[type].IsChecked = true;
			}
		}
	}
}