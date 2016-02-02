namespace StockSharp.Studio.Controls
{
	using System.IO;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Community;
	using StockSharp.Localization;
	using StockSharp.Studio.Core;

	public partial class StorageSettingsWindow
	{
		public static RoutedCommand OkCommand = new RoutedCommand();

		private MarketDataSettings _settings;

		public MarketDataSettings Settings
		{
			get { return _settings; }
			set
			{
				_settings = value;
				SettingsChanged(value);
			}
		}

		public StorageSettingsWindow()
		{
			InitializeComponent();
		}

		private void SettingsChanged(MarketDataSettings settings)
		{
			SettingsPanel.Path = string.Empty;
			SettingsPanel.Address = string.Empty;
			SetCredentials();

			if (settings == null)
			{
				SettingsPanel.IsEnabled = false;
				return;
			}

			SettingsPanel.IsEnabled = true;
			SettingsPanel.IsLocal = settings.UseLocal;
			//SettingsPanel.IsAlphabetic = settings.IsAlphabetic;

			if (settings.UseLocal)
				SettingsPanel.Path = settings.Path;
			else
				SettingsPanel.Address = settings.Path;

			SetCredentials(settings.Credentials);
		}

		private void SetCredentials(ServerCredentials credentials = null)
		{
			var serverCredentials = credentials ?? new ServerCredentials();

			SettingsPanel.Login = serverCredentials.Login;
			SettingsPanel.Password = serverCredentials.Password;
		}

		private void SettingsPanel_OnRemotePathChanged()
		{
			SetCredentials();
		}

		private void OkCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var settings = Settings;

			//if (SettingsPanel.IsLocal == settings.UseLocal &&
			//	//SettingsPanel.IsAlphabetic == settings.IsAlphabetic &&
			//	(SettingsPanel.IsLocal ? SettingsPanel.Path : SettingsPanel.Address) == settings.Path &&
			//	SettingsPanel.Login == settings.Credentials.Login &&
			//	SettingsPanel.Password == settings.Credentials.Password)
			//{
			//	return;
			//}

			settings.UseLocal = SettingsPanel.IsLocal;
			//settings.IsAlphabetic = SettingsPanel.IsAlphabetic;
			settings.Path = SettingsPanel.IsLocal ? SettingsPanel.Path : SettingsPanel.Address;
			settings.Credentials.Login = SettingsPanel.Login;
			settings.Credentials.Password = SettingsPanel.Password;

			if (SettingsPanel.IsLocal)
			{
				if (!Directory.Exists(settings.Path))
				{
					var res = new MessageBoxBuilder()
						.Owner(this)
						.Text(LocalizedStrings.Str3263)
						.Warning()
						.YesNo()
						.Show();

					if (res != MessageBoxResult.Yes)
						return;
				}
			}

			DialogResult = true;
			Close();
		}

		private void OkCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !(SettingsPanel.IsLocal ? SettingsPanel.Path : SettingsPanel.Address).IsEmpty();
		}
	}
}