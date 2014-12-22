namespace SampleDdeExtendedInfo
{
	using System;
	using System.ComponentModel;
	using System.Windows;

	using Ookii.Dialogs.Wpf;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Quik;

	using StockSharp.Localization;

	public partial class MainWindow
	{
		public QuikTrader Trader;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();

		public MainWindow()
		{
			InitializeComponent();
			MainWindow.Instance = this;

			_securitiesWindow.MakeHideable();

			// попробовать сразу найти месторасположение Quik по запущенному процессу)
			Path.Text = QuikTerminal.GetDefaultPath();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_securitiesWindow.DeleteHideable();
			_securitiesWindow.Close();

			if (Trader != null)
			{
				if (_isDdeStarted)
					StopDde();

				Trader.Dispose();
			}

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void FindPathClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (!Path.Text.IsEmpty())
				dlg.SelectedPath = Path.Text;

			if (dlg.ShowDialog(this) == true)
			{
				Path.Text = dlg.SelectedPath;
			}
		}

		private bool _isConnected;

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				if (Path.Text.IsEmpty())
					MessageBox.Show(this, LocalizedStrings.Str2969);
				else
				{
					if (Trader == null)
					{
						// создаем подключение
						Trader = new QuikTrader(Path.Text) { IsDde = true };

						// возводим флаг, что соединение установлено
						_isConnected = true;

						// подписываемся на событие ошибки соединения
						Trader.ConnectionError += error => this.GuiAsync(() => MessageBox.Show(this, error.ToString()));

						Trader.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);

						Trader.ProcessDataError += error => System.Diagnostics.Debug.WriteLine(error);

						// добавляем на экспорт необходимые колонки
						Trader.SecuritiesTable.Columns.Add(DdeSecurityColumns.ImpliedVolatility);
						Trader.SecuritiesTable.Columns.Add(DdeSecurityColumns.TheorPrice);
						Trader.SecuritiesTable.Columns.Add(DdeSecurityColumns.UnderlyingSecurity);
						Trader.SecuritiesTable.Columns.Add(DdeSecurityColumns.StepPrice);

						// добавляем экспорт дополнительных колонок из стакана (своя продажа и покупка)
						Trader.QuotesTable.Columns.Add(DdeQuoteColumns.OwnAskVolume);
						Trader.QuotesTable.Columns.Add(DdeQuoteColumns.OwnBidVolume);

						Trader.Connected += () => this.GuiAsync(() =>
						{
							ShowSecurities.IsEnabled = true;
							ExportDde.IsEnabled = true;

							_isConnected = true;
							ConnectBtn.Content = LocalizedStrings.Str2961;
						});

						Trader.Disconnected += () => this.GuiAsync(() =>
						{
							_isConnected = false;
							ConnectBtn.Content = LocalizedStrings.Str2962;
						});
					}
					
					Trader.Connect();
				}
			}
			else
				Trader.Disconnect();
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private static void ShowOrHide(Window window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		private bool _isDdeStarted;

		private void StartDde()
		{
			Trader.StartExport(new[] { Trader.SecuritiesTable });
			_isDdeStarted = true;
		}

		private void StopDde()
		{
			Trader.StopExport(new[] { Trader.SecuritiesTable });
			_isDdeStarted = false;
		}

		private void ExportDdeClick(object sender, RoutedEventArgs e)
		{
			if (_isDdeStarted)
				StopDde();
			else
				StartDde();
		}
	}
}