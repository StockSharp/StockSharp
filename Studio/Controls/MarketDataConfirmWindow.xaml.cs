#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: MarketDataConfirmWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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