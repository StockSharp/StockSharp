namespace StockSharp.Xaml
{
	using System.Windows;

	/// <summary>
	/// Диалоговое окно для редактирования <see cref="ProxySettings"/>.
	/// </summary>
	public partial class ProxyEditorWindow
	{
		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="ProxySettings"/>.
		/// </summary>
		public static readonly DependencyProperty ProxySettingsProperty = DependencyProperty.Register("ProxySettings", typeof(ProxySettings), typeof(ProxyEditorWindow));

		/// <summary>
		/// Настройки прокси-сервера.
		/// </summary>
		public ProxySettings ProxySettings
		{
			get { return (ProxySettings)GetValue(ProxySettingsProperty); }
			set { SetValue(ProxySettingsProperty, value); }
		}

		/// <summary>
		/// Создать <see cref="ProxyEditorWindow"/>.
		/// </summary>
		public ProxyEditorWindow()
		{
			InitializeComponent();
			ProxySettings = ProxySettings.GetProxySettings();
		}
	}
}
