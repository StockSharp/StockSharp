namespace StockSharp.Samples.Strategies.LiveTerminal;

using System;
using System.IO;
using System.Windows;

using Ecng.Common;
using Ecng.Serialization;
using Ecng.Xaml;
using Ecng.Logging;

using StockSharp.Configuration;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
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

				AddStrategy(strategy);
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

		AddStrategy(quoting);

		SaveStrategy(quoting);
	}

	private void AddStrategy(Strategy strategy)
	{
		strategy.Connector = MainWindow.Instance.Connector;
		strategy.DisposeOnStop = false;

		Dashboard.Items.Add(new StrategiesDashboardItem(strategy)
		{
			SettingsCommand = new DelegateCommand(_ =>
			{
				var wnd = new StrategyEditWindow
				{
					Strategy = strategy.TypedClone(),
				};

				if (!wnd.ShowModal(this))
					return;

				var id = strategy.Id;
				strategy.Apply(wnd.Strategy);
				strategy.Id = id;
				SaveStrategy(strategy);
			}, _ => strategy.ProcessState == ProcessStates.Stopped)
		});
		MainWindow.Instance.LogManager.Sources.Add(strategy);
	}

	private void SaveStrategy(Strategy strategy)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		strategy.SaveEntire(false).Serialize(Path.Combine(_dir, $"{strategy.Id}{Paths.DefaultSettingsExt}"));
	}
}