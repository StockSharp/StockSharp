#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: ExtensionInfoPicker.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Windows;

	using Ecng.Xaml;

	/// <summary>
	/// The button activating the window <see cref="ExtensionInfoWindow"/>.
	/// </summary>
	public partial class ExtensionInfoPicker
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ExtensionInfoPicker"/>.
		/// </summary>
		public ExtensionInfoPicker()
		{
			InitializeComponent();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="ExtensionInfoPicker.SelectedExtensionInfo"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedExtensionInfoProperty =
			 DependencyProperty.Register(nameof(SelectedExtensionInfo), typeof(IDictionary<object, object>), typeof(ExtensionInfoPicker),
				new FrameworkPropertyMetadata(null, OnSelectedExtensionInfoPropertyChanged));

		/// <summary>
		/// Extended information.
		/// </summary>
		public IDictionary<object, object> SelectedExtensionInfo
		{
			get { return (IDictionary<object, object>)GetValue(SelectedExtensionInfoProperty); }
			set { SetValue(SelectedExtensionInfoProperty, value); }
		}

		private static void OnSelectedExtensionInfoPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
		}

		private void ButtonClick(object sender, RoutedEventArgs e)
		{
			new ExtensionInfoWindow { Data = SelectedExtensionInfo }.ShowModal(this);
		}
	}
}