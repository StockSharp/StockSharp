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
		public static readonly DependencyProperty ProxySettingsProperty = DependencyProperty.Register("ProxySettings", typeof(ProxySettings), typeof(ProxySettingsEditor));

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
