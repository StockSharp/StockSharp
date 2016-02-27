#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: ProxyEditorWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Windows;

	/// <summary>
	/// The dialog box for editing <see cref="ProxyEditorWindow.ProxySettings"/>.
	/// </summary>
	public partial class ProxyEditorWindow
	{
		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="ProxyEditorWindow.ProxySettings"/>.
		/// </summary>
		public static readonly DependencyProperty ProxySettingsProperty = DependencyProperty.Register(nameof(ProxySettings), typeof(ProxySettings), typeof(ProxyEditorWindow));

		/// <summary>
		/// Proxy-server settings.
		/// </summary>
		public ProxySettings ProxySettings
		{
			get { return (ProxySettings)GetValue(ProxySettingsProperty); }
			set { SetValue(ProxySettingsProperty, value); }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProxyEditorWindow"/>.
		/// </summary>
		public ProxyEditorWindow()
		{
			InitializeComponent();
			ProxySettings = ProxySettings.GetProxySettings();
		}
	}
}
