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
			 DependencyProperty.Register("SelectedExtensionInfo", typeof(IDictionary<object, object>), typeof(ExtensionInfoPicker),
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