#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: ProxySettingsEditor.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Windows;

	/// <summary>
	/// The panel for editing <see cref="ProxySettingsEditor.ProxySettings"/>.
	/// </summary>
	public partial class ProxySettingsEditor
	{
		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="ProxySettingsEditor.ProxySettings"/>.
		/// </summary>
		public static readonly DependencyProperty ProxySettingsProperty = DependencyProperty.Register(nameof(ProxySettings), typeof(ProxySettings), typeof(ProxySettingsEditor));

		/// <summary>
		/// Proxy-server settings.
		/// </summary>
		public ProxySettings ProxySettings
		{
			get { return (ProxySettings)GetValue(ProxySettingsProperty); }
			set { SetValue(ProxySettingsProperty, value); }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProxySettingsEditor"/>.
		/// </summary>
		public ProxySettingsEditor()
		{
			InitializeComponent();
		}
	}
}
