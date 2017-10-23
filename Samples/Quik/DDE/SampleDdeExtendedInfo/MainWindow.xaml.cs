#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDdeExtendedInfo.SampleDdeExtendedInfoPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
			Instance = this;

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

						Trader.NewSecurity += security => _securitiesWindow.SecurityPicker.Securities.Add(security);

						Trader.Error += error => System.Diagnostics.Debug.WriteLine(error);

						// добавляем на экспорт необходимые колонки
						Trader.SecuritiesTable.Columns.Add(DdeSecurityColumns.ImpliedVolatility);
						Trader.SecuritiesTable.Columns.Add(DdeSecurityColumns.TheorPrice);
						Trader.SecuritiesTable.Columns.Add(DdeSecurityColumns.UnderlyingSecurity);
						Trader.SecuritiesTable.Columns.Add(DdeSecurityColumns.StepPrice);

						// добавляем экспорт дополнительных колонок из стакана (своя продажа и покупка)
						Trader.QuotesTable.Columns.Add(DdeQuoteColumns.OwnAskVolume);
						Trader.QuotesTable.Columns.Add(DdeQuoteColumns.OwnBidVolume);

						Trader.DdeTables = new[] { Trader.SecuritiesTable };

						Trader.Connected += () => this.GuiAsync(() =>
						{
							ShowSecurities.IsEnabled = true;

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

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
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
	}
}