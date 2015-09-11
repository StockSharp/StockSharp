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
		public static readonly DependencyProperty ProxySettingsProperty = DependencyProperty.Register("ProxySettings", typeof(ProxySettings), typeof(ProxyEditorWindow));

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
