#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleAsyncTransactions.SampleAsyncTransactionsPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleAsyncTransactions
{
	using System.ComponentModel;
	using System.Windows;

	using Ookii.Dialogs.Wpf;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Quik;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class MainWindow
	{
		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			_securitiesWindow.MakeHideable();

			// попробовать сразу найти месторасположение Quik по запущенному процессу
			Path.Text = QuikTerminal.GetDefaultPath();
		}

		public QuikTrader Trader { get; private set; }

		public static MainWindow Instance { get; private set; }

		public Portfolio Portfolio => Portfolios.SelectedPortfolio;

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

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (Path.Text.IsEmpty())
				MessageBox.Show(this, LocalizedStrings.Str2969);
			else
			{
				Trader = new QuikTrader(Path.Text) { IsDde = true, IsAsyncMode = true };

				Portfolios.Portfolios = new PortfolioDataSource(Trader);

				Trader.NewSecurity += security => _securitiesWindow.SecurityPicker.Securities.Add(security);

				// подписываемся на событие о неудачной регистрации заявок
				Trader.OrderRegisterFailed += OrderFailed;
				// подписываемся на событие о неудачном снятии заявок
				Trader.OrderCancelFailed += OrderFailed;

				// подписываемся на событие о неудачной регистрации стоп-заявок
				Trader.StopOrderRegisterFailed += OrderFailed;
				// подписываемся на событие о неудачном снятии стоп-заявок
				Trader.StopOrderCancelFailed += OrderFailed;

				// добавляем экспорт дополнительных колонок из стакана (своя покупка и продажа)
				Trader.QuotesTable.Columns.Add(DdeQuoteColumns.OwnBidVolume);
				Trader.QuotesTable.Columns.Add(DdeQuoteColumns.OwnAskVolume);

				Trader.Connect();

				ShowSecurities.IsEnabled = true;
				ConnectBtn.IsEnabled = false;
			}
		}

		private void OrderFailed(OrderFail fail)
		{
			this.GuiAsync(() =>
			{
				MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str153);
			});
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			if (_securitiesWindow.Visibility == Visibility.Visible)
				_securitiesWindow.Hide();
			else
				_securitiesWindow.Show();
		}
	}
}