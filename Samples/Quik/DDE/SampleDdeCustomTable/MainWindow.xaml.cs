#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDdeCustomTable.SampleDdeCustomTablePublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleDdeCustomTable
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;

	using Ookii.Dialogs.Wpf;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Quik;

	using StockSharp.Localization;

	public partial class MainWindow
	{
		public QuikTrader Trader;

		private readonly CandlesWindow _candlesWindow = new CandlesWindow();
		private DdeCustomTable _table;

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			_candlesWindow.MakeHideable();

			// попробовать сразу найти месторасположение Quik по запущенному процессу
			Path.Text = QuikTerminal.GetDefaultPath();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_candlesWindow.DeleteHideable();
			_candlesWindow.Close();

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

						// добавляем тип QuikCandle для преобразования строчки из таблица Исторические свечи в объект QuikCandle
						_table = new DdeCustomTable(typeof(QuikCandle));
						Trader.CustomTables.Add(_table);

						Trader.NewCustomTables += (type, objects) =>
						{
							// нас интересует только QuikCandle
							if (type == typeof(QuikCandle))
								_candlesWindow.Candles.AddRange(objects.Cast<QuikCandle>());
						};

						Trader.Connected += () => this.GuiAsync(() =>
						{
							ShowCandles.IsEnabled = true;
							ExportDde.IsEnabled = true;

							_isConnected = true;
							ConnectBtn.Content = LocalizedStrings.Disconnect;
						});

						Trader.Disconnected += () => this.GuiAsync(() =>
						{
							_isConnected = false;
							ConnectBtn.Content = LocalizedStrings.Connect;
						});
					}
					
					Trader.Connect();
				}
			}
			else
				Trader.Disconnect();
		}

		private void ShowCandlesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_candlesWindow);
		}

		private static void ShowOrHide(Window window)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		private bool _isDdeStarted;

		private void StartDde()
		{
			Trader.StartExport(_table);
			_isDdeStarted = true;
		}

		private void StopDde()
		{
			Trader.StopExport(_table);
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