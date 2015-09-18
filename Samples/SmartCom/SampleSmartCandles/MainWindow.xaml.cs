namespace SampleSmartCandles
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.SmartCom;
	using StockSharp.Xaml.Charting;
	using StockSharp.SmartCom.Native;
	using StockSharp.Localization;

	partial class MainWindow
	{
		private readonly Dictionary<CandleSeries, ChartWindow> _chartWindows = new Dictionary<CandleSeries, ChartWindow>();
		private SmartTrader _trader;
		private CandleManager _candleManager;
		
		public MainWindow()
		{
			InitializeComponent();

			HistoryInterval.ItemsSource = SmartComTimeFrames.AllTimeFrames;

			HistoryInterval.SelectedIndex = 2;
			From.Value = DateTime.Today - TimeSpan.FromDays(7);
			To.Value = DateTime.Now;
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (Login.Text.IsEmpty())
			{
				MessageBox.Show(this, LocalizedStrings.Str2974);
				return;
			}
			else if (Password.Password.IsEmpty())
			{
				MessageBox.Show(this, LocalizedStrings.Str2975);
				return;
			}

			// создаем подключение
			_trader = new SmartTrader
			{
				Login = Login.Text,
				Password = Password.Password,
				Address = Address.SelectedAddress,

				// применить нужную версию SmartCOM
				Version = IsSmartCom3.IsChecked == true ? SmartComVersions.V3 : SmartComVersions.V2,
			};

			// очищаем из текстового поля в целях безопасности
			//Password.Clear();

			// подписываемся на ошибку обработки данных (транзакций и маркет)
			_trader.Error += error =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			// подписываемся на ошибку подписки маркет-данных
			_trader.MarketDataSubscriptionFailed += (security, type, error) =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

			Security.SecurityProvider = new FilterableSecurityProvider(_trader);

			_candleManager = new CandleManager(_trader);

            _trader.Connect();
			ConnectBtn.IsEnabled = false;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			foreach (var pair in _chartWindows)
				pair.Value.DeleteHideable();

			if (_trader != null)
				_trader.Dispose();

			base.OnClosing(e);
		}

		private Security SelectedSecurity
		{
			get { return Security.SelectedSecurity; }
		}

		private void ShowChartClick(object sender, RoutedEventArgs e)
		{
			var security = SelectedSecurity;

			CandleSeries series;

			if (IsRealTime.IsChecked == true)
			{
				series = new CandleSeries(RealTimeSettings.Settings.CandleType, security, RealTimeSettings.Settings.Arg);
			}
			else
			{
				var timeFrame = (TimeSpan)HistoryInterval.SelectedItem;
				series = new CandleSeries(typeof(TimeFrameCandle), security, timeFrame);
			}

			_chartWindows.SafeAdd(series, key =>
			{
				var wnd = new ChartWindow
				{
					Title = "{0} {1} {2}".Put(security.Code, series.CandleType.Name.Replace("Candle", string.Empty), series.Arg)
				};

				wnd.MakeHideable();

				var area = new ChartArea();
				wnd.Chart.Areas.Add(area);

				var candlesElem = new ChartCandleElement();
				area.Elements.Add(candlesElem);

				series.ProcessCandle += candle => wnd.Chart.Draw(candlesElem, candle);

				return wnd;
			}).Show();

			if (IsRealTime.IsChecked == true)
				_candleManager.Start(series, DateTime.Today, DateTimeOffset.MaxValue);
			else
				_candleManager.Start(series, (DateTimeOffset)From.Value, (DateTimeOffset)To.Value);
		}

		private void OnSecuritySelected()
		{
			ShowChart.IsEnabled = SelectedSecurity != null;
		}

		private void OnChartTypeChanged(object sender, RoutedEventArgs e)
		{
			RealTimeSettings.IsEnabled = IsRealTime.IsChecked == true;
			HistorySettings.IsEnabled = IsHistory.IsChecked == true;
		}
	}
}