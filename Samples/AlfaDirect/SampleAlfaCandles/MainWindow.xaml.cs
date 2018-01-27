#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleAlfaCandles.SampleAlfaCandlesPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleAlfaCandles
{
	using System;
	using System.ComponentModel;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.AlfaDirect;
	using StockSharp.Algo;
	using StockSharp.Logging;
	using StockSharp.Localization;

	partial class MainWindow
	{
		private AlfaTrader _trader;

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();

			HistoryInterval.ItemsSource = AlfaTimeFrames.AllTimeFrames;

			HistoryInterval.SelectedIndex = 2;
			From.EditValue = DateTime.Today - TimeSpan.FromDays(7);
			To.EditValue = DateTime.Now;

			_logManager.Listeners.Add(new FileLogListener());
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			// создаем подключение
			_trader = new AlfaTrader { LogLevel = LogLevels.Debug };

			_logManager.Sources.Add(_trader);

			// подписываемся на ошибку обработки данных (транзакций и маркет)
			_trader.Error += error =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			// подписываемся на ошибку подписки маркет-данных
			_trader.MarketDataSubscriptionFailed += (security, msg, error) =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));

			_trader.NewSecurity += security =>
			{
				// начинаем получать текущие сделки (для построения свечек в реальном времени)

				// альфа не выдержит нагрузки получения сделок по всем инструментам
				// нужно подписываться только на те, которые необходимы
				// securities.ForEach(_trader.RegisterTrades);
			};

			Security.SecurityProvider = new FilterableSecurityProvider(_trader);

			_trader.Connected += () =>
			{
				this.GuiAsync(() => ConnectBtn.IsEnabled = false);
			};

			_trader.ConnectionError += error =>
			{
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959));
			};

			_trader.Connect();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (_trader != null)
			{
				_logManager.Sources.Remove(_trader);
				_trader.Dispose();
			}

			base.OnClosing(e);
		}

		private Security SelectedSecurity => Security.SelectedSecurity;

		private void ShowChartClick(object sender, RoutedEventArgs e)
		{
			var security = SelectedSecurity;
			_trader.RegisterTrades(security);

			var timeFrame = (TimeSpan)HistoryInterval.SelectedItem;

			var from = (DateTime?)From.EditValue;
			var to = RealTime.IsChecked == true ? null : (DateTime?)To.EditValue;

			if (from > to)
			{
				return;
			}

			var wnd = new ChartWindow
			{
				Title = "{0}, {1}, {2} - {3}".Put(security.Code, HistoryInterval.SelectedItem, from, to)
			};

			wnd.Show();

			var series = new CandleSeries
			{
				Security = security,
				Arg = timeFrame,
				CandleType = typeof(TimeFrameCandle)
			};

			_trader.CandleSeriesProcessing += (candleSeries, candle) =>
			{
				_trader.AddInfoLog("New сandle({0})", candle);

				if (candleSeries == series)
					wnd.DrawCandles(candle);
			};

			_trader.SubscribeCandles(series, from, to);
		}

		private void OnSelectedSecurity()
		{
			ShowChart.IsEnabled = SelectedSecurity != null;
		}

		private void RealTime_Checked(object sender, RoutedEventArgs e)
		{
			To.IsEnabled = RealTime.IsChecked == false;
		}
	}
}
