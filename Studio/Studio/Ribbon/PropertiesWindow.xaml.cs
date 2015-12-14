#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: PropertiesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Ribbon
{
	using System.Windows;

	using Ecng.Serialization;

	public partial class PropertiesWindow
	{
		public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register("SelectedObject", typeof(IPersistable), typeof(PropertiesWindow),
			new FrameworkPropertyMetadata(null));

		public IPersistable SelectedObject
		{
			get { return (IPersistable)GetValue(SelectedObjectProperty); }
			set { SetValue(SelectedObjectProperty, value); }
		}

		public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PropertiesWindow),
			new FrameworkPropertyMetadata(false));

		public bool IsReadOnly
		{
			get { return (bool)GetValue(IsReadOnlyProperty); }
			set { SetValue(IsReadOnlyProperty, value); }
		}

		public PropertiesWindow()
		{
			InitializeComponent();
		}
	}
}
