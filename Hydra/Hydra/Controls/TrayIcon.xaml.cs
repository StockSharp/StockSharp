namespace StockSharp.Hydra.Controls
{
	using System;
	using System.Reflection;
	using System.Windows;
	using System.Windows.Forms;
	using Ecng.Common;
	using ContextMenu = System.Windows.Controls.ContextMenu;
	using MenuItem = System.Windows.Controls.MenuItem;
	using StockSharp.Localization;

	public partial class TrayIcon
	{
		private NotifyIcon _trayIcon;
		private ContextMenu _trayMenu;

		private MainWindow _window;

		public TrayIcon()
		{
			InitializeComponent();
		}

		public void Show(MainWindow window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			_window = window;
			_window.IsVisibleChanged += WindowOnIsVisibleChanged;

			_trayIcon = new NotifyIcon { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().ManifestModule.Name), Text = "Hydra" };

			_trayMenu = (ContextMenu)Resources["TrayMenu"];

			_trayIcon.Click += (s1, e1) =>
			{
				if (((MouseEventArgs)e1).Button == MouseButtons.Left)
				{
					ShowHideMainWindow(null, null);
				}
				else
				{
					_trayMenu.IsOpen = true;
					_window.Activate();
				}
			};

			_trayIcon.Visible = true;
		}

		public void UpdateState(bool isStarted)
		{
			var showHideItem = (MenuItem)_trayMenu.Items[2];
			var settingsItem = (MenuItem)_trayMenu.Items[3];

			showHideItem.Header = isStarted ? LocalizedStrings.Str242 : LocalizedStrings.Str2421;
			//settingsItem.IsEnabled = !isStarted; TODO: включить когда добавится меню с настройками в трей (amu)
		}

		public void ShowErrorMessage(Exception error)
		{
			if (error == null)
				throw new ArgumentNullException("error");

			_trayIcon.BalloonTipTitle = error.Message;
			_trayIcon.BalloonTipText = error.ToString();
			_trayIcon.BalloonTipIcon = ToolTipIcon.Error;
		}

		public void Close()
		{
			_window.IsVisibleChanged -= WindowOnIsVisibleChanged;
			_trayIcon.Visible = false;
		}

		private void WindowOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			((MenuItem)_trayMenu.Items[0]).Header = _window.IsVisible ? LocalizedStrings.Str1479 : LocalizedStrings.Str2933;
		}

		private void ShowHideMainWindow(object sender, RoutedEventArgs e)
		{
			_trayMenu.IsOpen = false;

			if (_window.IsVisible)
			{
				_window.Hide();
			}
			else
			{
				_window.Show();
				_window.WindowState = _window.CurrentWindowState;
				_window.Activate();
			}
		}

		private void MenuExitClick(object sender, RoutedEventArgs e)
		{
			_window.Close();
		}

		public event Action StartStop;
		public event Action Logs;

		private void StartStopClick(object sender, RoutedEventArgs e)
		{
			StartStop.SafeInvoke();
		}

		private void LogsClick(object sender, RoutedEventArgs e)
		{
			Logs.SafeInvoke();
		}
	}
}