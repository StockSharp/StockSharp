namespace SampleStrategies
{
	using System;
	using System.IO;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Configuration;
	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Quoting;
	using StockSharp.Logging;
	using StockSharp.Xaml;

	public partial class StrategiesWindow
	{
		private string _dir;

		public StrategiesWindow()
		{
			InitializeComponent();

			var connector = MainWindow.Instance.Connector;

			Dashboard.SecurityProvider = connector;
			Dashboard.Portfolios = new PortfolioDataSource(connector);
		}

		public void LoadStrategies(string path)
		{
			_dir = Path.Combine(path, "Strategies");

			Directory.CreateDirectory(_dir);

			foreach (var xml in _dir.EnumerateConfigs())
			{
				try
				{
					var strategy = xml.Deserialize<SettingsStorage>()?.LoadEntire<Strategy>();

					if (strategy is null)
						continue;

					AddStrategy(strategy.Name, strategy);
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
			}
		}

		private void QuotingClick(object sender, RoutedEventArgs e)
		{
			var quoting = new MarketQuotingStrategy();

			var wnd = new StrategyEditWindow
			{
				Strategy = quoting,
			};

			if (!wnd.ShowModal(this))
				return;

			//if (wnd.TakeProfit > 0 || wnd.StopLoss > 0)
			//{
			//	var tp = wnd.TakeProfit;
			//	var sl = wnd.StopLoss;

			//	quoting
			//		.WhenNewMyTrade()
			//		.Do(trade =>
			//		{
			//			var tpStrategy = tp == 0 ? null : new TakeProfitStrategy(trade, tp);
			//			var slStrategy = sl == 0 ? null : new StopLossStrategy(trade, sl);

			//			if (tpStrategy != null && slStrategy != null)
			//			{
			//				var strategy = new TakeProfitStopLossStrategy(tpStrategy, slStrategy);
			//				AddStrategy($"TPSL {trade.Trade.Price} Vol={trade.Trade.Volume}", strategy, security, portfolio);
			//			}
			//			else if (tpStrategy != null)
			//			{
			//				AddStrategy($"TP {trade.Trade.Price} Vol={trade.Trade.Volume}", tpStrategy, security, portfolio);
			//			}
			//			else if (slStrategy != null)
			//			{
			//				AddStrategy($"SL {trade.Trade.Price} Vol={trade.Trade.Volume}", slStrategy, security, portfolio);
			//			}
			//		})
			//		.Apply(quoting);
			//}

			AddStrategy($"Quoting {quoting.Security} {quoting.QuotingDirection} Vol={quoting.QuotingVolume}", quoting);

			SaveStrategy(quoting);
		}

		private void AddStrategy(string name, Strategy strategy)
		{
			strategy.Connector = MainWindow.Instance.Connector;
			strategy.DisposeOnStop = false;

			Dashboard.Items.Add(new StrategiesDashboardItem(name, strategy, null));
			MainWindow.Instance.LogManager.Sources.Add(strategy);
		}

		private bool Dashboard_OnCanExecuteStart(StrategiesDashboardItem item)
		{
			return item.Strategy?.ProcessState == ProcessStates.Stopped;
		}

		private bool Dashboard_OnCanExecuteStop(StrategiesDashboardItem item)
		{
			return item.Strategy?.ProcessState == ProcessStates.Started;
		}

		private void Dashboard_OnExecuteStart(StrategiesDashboardItem item)
		{
			SaveStrategy(item.Strategy);
			item.Strategy.Start();
		}

		private void Dashboard_OnExecuteStop(StrategiesDashboardItem item)
		{
			item.Strategy.Stop();
		}

		private void SaveStrategy(Strategy strategy)
		{
			if (strategy is null)
				throw new ArgumentNullException(nameof(strategy));

			strategy.SaveEntire(false).Serialize(Path.Combine(_dir, $"{strategy.Id}{Paths.DefaultSettingsExt}"));
		}

		private bool Dashboard_OnCanExecuteSettings(StrategiesDashboardItem item)
		{
			return item.Strategy.ProcessState == ProcessStates.Stopped;
		}

		private void Dashboard_OnExecuteSettings(StrategiesDashboardItem item)
		{
			var wnd = new StrategyEditWindow
			{
				Strategy = item.Strategy.TypedClone(),
			};

			if (!wnd.ShowModal(this))
				return;

			var id = item.Strategy.Id;
			item.Strategy.Apply(wnd.Strategy);
			item.Strategy.Id = id;
			SaveStrategy(item.Strategy);
		}
	}
}