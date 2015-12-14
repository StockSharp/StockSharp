#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: PropertiesButton.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Ribbon
{
	using System.Windows;

	using ActiproSoftware.Windows.Controls.Ribbon.Controls;

	using Ecng.Serialization;
	using Ecng.Xaml;

	public partial class PropertiesButton
	{
		public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register("SelectedObject", typeof(IPersistable), typeof(PropertiesButton),
			new FrameworkPropertyMetadata(null));

		public IPersistable SelectedObject
		{
			get { return (IPersistable)GetValue(SelectedObjectProperty); }
			set { SetValue(SelectedObjectProperty, value); }
		}

		public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PropertiesButton),
			new FrameworkPropertyMetadata(false));

		public bool IsReadOnly
		{
			get { return (bool)GetValue(IsReadOnlyProperty); }
			set { SetValue(IsReadOnlyProperty, value); }
		}

		public PropertiesButton()
		{
			InitializeComponent();
		}

		private void Properties_OnClick(object sender, ExecuteRoutedEventArgs e)
		{
			var wnd = new PropertiesWindow
			{
				SelectedObject = SelectedObject.Clone(),
				IsReadOnly = IsReadOnly
			};

			if (wnd.ShowModal(this))
				SelectedObject.Load(wnd.SelectedObject.Save());
		}
	}
}
