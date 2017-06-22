#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleSync.SampleSyncPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleSync
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
	using StockSharp.Quik;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private readonly Dictionary<CandleSeries, ChartWindow> _chartWindows = new Dictionary<CandleSeries, ChartWindow>();
		private GuiConnector<QuikTrader> _connector;
		private ICandleManager _candleManager;

		public MainWindow()
		{
			InitializeComponent();

			// попробовать сразу найти месторасположение Quik по запущенному процессу
			QuikPath.Folder = QuikTerminal.GetDefaultPath();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			foreach (var pair in _chartWindows)
				pair.Value.DeleteHideable();

			if (_connector != null)
			{
				_connector.Dispose();
				_connector.Connector.Dispose();
			}

			base.OnClosing(e);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (QuikPath.Folder.IsEmpty())
				MessageBox.Show(this, LocalizedStrings.Str2969);
			else
			{
				// создаем подключение к Quik-у и синхронизуем его
				_connector = new GuiConnector<QuikTrader>(new QuikTrader(QuikPath.Folder));

				// или напрямую через конструктор GuiTrader
				// (пред. нужно закомментировать, это - раскомментировать)
				// new GuiTrader<QuikTrader>(new QuikTrader(Path.Text));

				Security.SecurityProvider = new FilterableSecurityProvider(_connector);

				// производим соединение
				_connector.Connect();

				// создаем менеджер свечек по синхронизированному подключению
				_candleManager = new CandleManager(_connector.Connector);

				ConnectBtn.IsEnabled = false;
			}
		}

		private void ShowChartClick(object sender, RoutedEventArgs e)
		{
			var security = Security.SelectedSecurity;
			var series = new CandleSeries(typeof(TimeFrameCandle), security, TimeSpan.FromMinutes(5));

			_chartWindows.SafeAdd(series, key =>
			{
				var wnd = new ChartWindow
				{
					Title = "{0} {1}".Put(security.Code, series.Arg)
				};

				wnd.MakeHideable();

				var area = new ChartArea();
				wnd.Chart.Areas.Add(area);

				var candlesElem = new ChartCandleElement();
				area.Elements.Add(candlesElem);

				_candleManager.Processing += (s, candle) => wnd.Chart.Draw(candlesElem, candle);

				return wnd;
			}).Show();

			_candleManager.Start(series);
		}

		private void OnSecuritySelected()
		{
			ShowChart.IsEnabled = Security.SelectedSecurity != null;
		}
	}
}