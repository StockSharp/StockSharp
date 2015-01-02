namespace StockSharp.LicenseTool
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Security;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Xaml;

	using StockSharp.Licensing;
	using StockSharp.Community;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();
			Title += " v" + typeof(MainWindow).Assembly.GetName().Version;
		}

		private void WindowLoaded(object sender, RoutedEventArgs e)
		{
			string hwId = null;

			var worker = new BackgroundWorker();

			worker.DoWork += (o, ea) => hwId = HardwareInfo.Instance.Id;

			worker.RunWorkerCompleted += (o, ea) =>
			{
				HardwareIdCtrl.Text = hwId;
				BusyIndicator.IsBusy = false;
			};

			worker.RunWorkerAsync();
		}

		private void LoginCtrlTextChanged(object sender, TextChangedEventArgs e)
		{
			EnableButton();
		}

		private void PasswordCtrlPasswordChanged(object sender, RoutedEventArgs e)
		{
			EnableButton();
		}

		private void EnableButton()
		{
			DoCtrl.IsEnabled = !HardwareIdCtrl.Text.IsEmpty() && !LoginCtrl.Text.IsEmpty() && !PasswordCtrl.Password.IsEmpty();
		}

		private void DoCtrlClick(object sender, RoutedEventArgs e)
		{
			var worker = new BackgroundWorker();

			BusyIndicator.BusyContent = LocalizedStrings.ObtainingLicense;

			AuthenticationClient.Instance.Credentials.Login = LoginCtrl.Text;
			AuthenticationClient.Instance.Credentials.Password = PasswordCtrl.Password.To<SecureString>();

			worker.DoWork += (o, ea) =>
			{
				using (var client = new LicenseClient())
				{
					var lic = client.GetFullLicense();
					lic.Save();
				}
			};

			worker.RunWorkerCompleted += (o, ea) =>
			{
				BusyIndicator.IsBusy = false;

				if (ea.Error == null)
				{
					new MessageBoxBuilder()
						.Owner(this)
						.Text(LocalizedStrings.Str3028Params.Put(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StockSharp")))
						.Info()
						.Show();
				}
				else
				{
					new MessageBoxBuilder()
						.Owner(this)
						.Text(ea.Error.Message)
						.Error()
						.Show();
				}
			};

			BusyIndicator.IsBusy = true;

			worker.RunWorkerAsync();
		}

		private void CanExecuteCopyId(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
			e.Handled = true;
		}

		private void ExecutedCopyId(object sender, ExecutedRoutedEventArgs e)
		{
			Clipboard.SetData(DataFormats.UnicodeText, HardwareIdCtrl.Text);
		}

		private void LicenseViewClick(object sender, RoutedEventArgs e)
		{
			new LicenseWindow { LicensePanel = { Licenses = LicenseHelper.Licenses } }.ShowModal(this);
		}

		private void ProxySettingsClick(object sender, RoutedEventArgs e)
		{
			var wnd = new ProxyEditorWindow();

			if (wnd.ShowModal(this))
				wnd.ProxySettings.ApplyProxySettings();
		}
	}
}