namespace StockSharp.Xaml
{
	using System.Windows;

	/// <summary>
	/// Панель для редактирования <see cref="ProxySettings"/>.
	/// </summary>
	public partial class ProxySettingsEditor
	{
		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="ProxySettings"/>.
		/// </summary>
		public static readonly DependencyProperty ProxySettingsProperty = DependencyProperty.Register("ProxySettings", typeof(ProxySettings), typeof(ProxySettingsEditor));

		/// <summary>
		/// Настройки прокси-сервера.
		/// </summary>
		public ProxySettings ProxySettings
		{
			get { return (ProxySettings)GetValue(ProxySettingsProperty); }
			set { SetValue(ProxySettingsProperty, value); }
		}

		/// <summary>
		/// Создать <see cref="ProxySettingsEditor"/>.
		/// </summary>
		public ProxySettingsEditor()
		{
			InitializeComponent();
		}
	}
}
