namespace SampleSync
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Controls;

	using Ookii.Dialogs.Wpf;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
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
			Path.Text = QuikTerminal.GetDefaultPath();
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
				// создаем подключение к Quik-у и синхронизуем его
				_connector = new QuikTrader(Path.Text).GuiSyncTrader();

				// или напрямую через конструктор GuiTrader
				// (пред. нужно закомментировать, это - раскомментировать)
				// new GuiTrader<QuikTrader>(new QuikTrader(Path.Text));

				// теперь можно обратиться к элементу окна 'Security' (это выпадающий список) без конструкции Sync
				_connector.NewSecurities += securities => Security.ItemsSource = _connector.Securities;

				// производим соединение
				_connector.Connect();

				// создаем менеджер свечек по синхронизованному подключению
				_candleManager = new CandleManager(_connector);

				ConnectBtn.IsEnabled = false;
			}
		}

		private void ShowChartClick(object sender, RoutedEventArgs e)
		{
			var security = (Security)Security.SelectedValue;
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

				series.ProcessCandle += candle => wnd.Chart.Draw(candlesElem, candle);

				return wnd;
			}).Show();

			_candleManager.Start(series);
		}

		private void SecuritySelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Security.SelectedIndex != -1)
				ShowChart.IsEnabled = true;
		}
	}
}