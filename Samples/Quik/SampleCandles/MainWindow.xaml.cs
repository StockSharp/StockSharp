namespace SampleCandles
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Net;
	using System.Security;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Quik;
	using StockSharp.Xaml.Charting;

	partial class MainWindow
	{
		private readonly Dictionary<CandleSeries, ChartWindow> _chartWindows = new Dictionary<CandleSeries, ChartWindow>();
		private QuikTrader _trader;
		private CandleManager _candleManager;
		private readonly LogManager _logManager;

		public MainWindow()
		{
			InitializeComponent();

			// попробовать сразу найти месторасположение Quik по запущенному процессу
			Path.Text = QuikTerminal.GetDefaultPath();

			//Добавим логирование
			_logManager = new LogManager
			{
				Application = { LogLevel = LogLevels.Debug }
			};

			_logManager.Listeners.Add(new FileLogListener
			{
				LogDirectory = @"Logs\",
				SeparateByDates = SeparateByDateModes.SubDirectories,
				Append = false,
			});
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
			var isLua = IsLua.IsChecked == true;

			if (isLua)
			{
				if (Address.Text.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2977);
					return;
				}

				if (Login.Text.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2978);
					return;
				}

				if (Password.Password.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2979);
					return;
				}
			}
			else
			{
				if (Path.Text.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2983);
					return;
				}
			}

			if (_trader == null)
			{
				// создаем подключение
				_trader = isLua
					? new QuikTrader
					{
						LuaFixServerAddress = Address.Text.To<EndPoint>(),
						LuaLogin = Login.Text,
						LuaPassword = Password.Password.To<SecureString>()
					}
					: new QuikTrader(Path.Text) { IsDde = true };

				if (_trader.IsDde)
				{
					_trader.DdeTables = new[] { _trader.SecuritiesTable, _trader.TradesTable };
				}

				_logManager.Sources.Add(_trader);
				// подписываемся на событие об успешном восстановлении соединения
				_trader.Restored += () => this.GuiAsync(() => MessageBox.Show(this, LocalizedStrings.Str2958));

				// подписываемся на событие разрыва соединения
				_trader.ConnectionError += error => this.GuiAsync(() => MessageBox.Show(this, error.ToString()));

				// подписываемся на ошибку обработки данных (транзакций и маркет)
				_trader.Error += error =>
					this.GuiAsync(() => MessageBox.Show(this, error.ToString(), "Ошибка обработки данных"));

				// подписываемся на ошибку подписки маркет-данных
				_trader.MarketDataSubscriptionFailed += (security, type, error) =>
					this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));
				
				Security.SecurityProvider = new FilterableSecurityProvider(_trader);

				_trader.Connect();

				_candleManager = new CandleManager(_trader);
				_candleManager.Processing += DrawCandle;

				ConnectBtn.IsEnabled = false;
			}
		}

		private void DrawCandle(CandleSeries series, Candle candle)
		{
			var wnd = _chartWindows.TryGetValue(series);

			if (wnd != null)
				wnd.Chart.Draw((ChartCandleElement)wnd.Chart.Areas[0].Elements[0], candle);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			foreach (var pair in _chartWindows)
				pair.Value.DeleteHideable();

			if (_trader != null)
			{
				_trader.Dispose();
			}

			base.OnClosing(e);
		}

		private Security SelectedSecurity
		{
			get { return Security.SelectedSecurity; }
		}

		private void OnSecuritySelected()
		{
			ShowChart.IsEnabled = SelectedSecurity != null;
		}

		private void ShowChartClick(object sender, RoutedEventArgs e)
		{
			var security = SelectedSecurity;

			var series = new CandleSeries(CandlesSettings.Settings.CandleType, security, CandlesSettings.Settings.Arg);

			_chartWindows.SafeAdd(series, key =>
			{
				var wnd = new ChartWindow
				{
					Title = "{0} {1} {2}".Put(security.Code, series.CandleType.Name, series.Arg)
				};

				wnd.MakeHideable();

				var area = new ChartArea();
				wnd.Chart.Areas.Add(area);

				var candlesElem = new ChartCandleElement();
				area.Elements.Add(candlesElem);

				return wnd;
			}).Show();

			_candleManager.Start(series);
		}
	}
}