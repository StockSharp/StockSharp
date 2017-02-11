namespace SampleStrategies
{
	using System.Windows;

	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Protective;
	using StockSharp.Algo.Strategies.Quoting;
	using StockSharp.BusinessEntities;
	using StockSharp.Xaml;

	public partial class StrategiesWindow
	{
		public StrategiesWindow()
		{
			InitializeComponent();
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
			strategy.Start();
		}

		private bool Dashboard_OnCanExecuteStart(StrategiesDashboardItem item)
		{
			return item?.Strategy.ProcessState == ProcessStates.Stopped;
		}

		private bool Dashboard_OnCanExecuteStop(StrategiesDashboardItem item)
		{
			return item?.Strategy.ProcessState == ProcessStates.Started;
		}

		private void Dashboard_OnExecuteStart(StrategiesDashboardItem item)
		{
			item.Strategy.Start();
		}

		private void Dashboard_OnExecuteStop(StrategiesDashboardItem item)
		{
			item.Strategy.Stop();
		}
	}
}