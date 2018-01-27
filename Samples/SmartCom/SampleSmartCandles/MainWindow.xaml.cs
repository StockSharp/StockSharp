#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleSmartCandles.SampleSmartCandlesPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleSmartCandles
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.Algo;
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
			From.EditValue = DateTime.Today - TimeSpan.FromDays(7);
			To.EditValue = DateTime.Now;
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
				Version = IsSmartCom4.IsChecked == true ? SmartComVersions.V4 : SmartComVersions.V3,
			};

			// очищаем из текстового поля в целях безопасности
			//Password.Clear();

			// подписываемся на ошибку обработки данных (транзакций и маркет)
			_trader.Error += error =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			// подписываемся на ошибку подписки маркет-данных
			_trader.MarketDataSubscriptionFailed += (security, msg, error) =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));

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

		private Security SelectedSecurity => Security.SelectedSecurity;

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
					Title = "{0} {1} {2}".Put(security.Code, series.CandleType.Name.Remove("Candle"), series.Arg)
				};

				wnd.MakeHideable();

				var area = new ChartArea();
				wnd.Chart.Areas.Add(area);

				var candlesElem = new ChartCandleElement();
				area.Elements.Add(candlesElem);

				_candleManager.Processing += (s, candle) =>
				{
					if (s == series)
						wnd.Chart.Draw(candlesElem, candle);
				};

				return wnd;
			}).Show();

			if (IsRealTime.IsChecked == true)
				_candleManager.Start(series, DateTime.Today, null);
			else
				_candleManager.Start(series, (DateTime?)From.EditValue, (DateTime?)To.EditValue);
		}

		private void OnSecuritySelected()
		{
			ShowChart.IsEnabled = SelectedSecurity != null;
		}

		private void OnChartTypeChanged(object sender, RoutedEventArgs e)
		{
			CandleSettingsEditorGrid.IsEnabled = IsRealTime.IsChecked == true;
			HistorySettings.IsEnabled = IsHistory.IsChecked == true;
		}
	}
}