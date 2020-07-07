namespace SampleStrategies
{
	using System;
	using System.IO;
	using System.Windows;

	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Protective;
	using StockSharp.Algo.Strategies.Quoting;
	using StockSharp.BusinessEntities;
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

			var serializer = new XmlSerializer<SettingsStorage>();

			var connector = MainWindow.Instance.Connector;

			foreach (var xml in Directory.GetFiles(_dir, "*.xml"))
			{
				try
				{
					var strategy = serializer.Deserialize(xml).LoadEntire<Strategy>();
					strategy.Connector = connector;
					Dashboard.Items.Add(new StrategiesDashboardItem(strategy.Name, strategy, null));
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
			}
		}

		private void QuotingClick(object sender, RoutedEventArgs e)
		{
			var wnd = new StrategyAddWindow
			{
				//Security = SecurityPicker.SelectedSecurity,
			};

			if (!wnd.ShowModal(this))
				return;

			var security = wnd.Security;
			var portfolio = wnd.Portfolio;

			var quoting = new MarketQuotingStrategy(wnd.Side, wnd.Volume);

			if (wnd.TakeProfit > 0 || wnd.StopLoss > 0)
			{
				var tp = wnd.TakeProfit;
				var sl = wnd.StopLoss;

				quoting
					.WhenNewMyTrade()
					.Do(trade =>
					{
						var tpStrategy = tp == 0 ? null : new TakeProfitStrategy(trade, tp);
						var slStrategy = sl == 0 ? null : new StopLossStrategy(trade, sl);

						if (tpStrategy != null && slStrategy != null)
						{
							var strategy = new TakeProfitStopLossStrategy(tpStrategy, slStrategy);
							AddStrategy($"TPSL {trade.Trade.Price} Vol={trade.Trade.Volume}", strategy, security, portfolio);
						}
						else if (tpStrategy != null)
						{
							AddStrategy($"TP {trade.Trade.Price} Vol={trade.Trade.Volume}", tpStrategy, security, portfolio);
						}
						else if (slStrategy != null)
						{
							AddStrategy($"SL {trade.Trade.Price} Vol={trade.Trade.Volume}", slStrategy, security, portfolio);
						}
					})
					.Apply(quoting);
			}

			AddStrategy($"Quoting {quoting.Security} {wnd.Side} Vol={wnd.Volume}", quoting, security, portfolio);
		}

		private void AddStrategy(string name, Strategy strategy, Security security, Portfolio portfolio)
		{
			strategy.Security = security;
			strategy.Portfolio = portfolio;
			strategy.Connector = MainWindow.Instance.Connector;

			Dashboard.Items.Add(new StrategiesDashboardItem(name, strategy, null));
			MainWindow.Instance.LogManager.Sources.Add(strategy);

			SaveStrategy(strategy);
			//strategy.Start();
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

			new XmlSerializer<SettingsStorage>().Serialize(strategy.SaveEntire(false), Path.Combine(_dir, $"{strategy.Id}.xml"));
		}
	}
}