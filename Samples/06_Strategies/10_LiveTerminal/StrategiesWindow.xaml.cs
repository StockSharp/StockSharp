namespace StockSharp.Samples.Strategies.LiveTerminal;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Ecng.Common;
using Ecng.Serialization;
using Ecng.Xaml;
using Ecng.Logging;
using Ecng.ComponentModel;

using StockSharp.Configuration;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
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

	public async Task LoadStrategiesAsync(string path, CancellationToken cancellationToken)
	{
		_dir = Path.Combine(path, "Strategies");

		var fs = MainWindow.Instance.FileSystem;
		fs.CreateDirectory(_dir);

		foreach (var xml in _dir.EnumerateConfigs(fs))
		{
			try
			{
				var storage = await xml.DeserializeAsync<SettingsStorage>(fs, cancellationToken);

				if (storage is null)
					continue;

				var strategy = await storage.LoadEntireAsync<Strategy>(cancellationToken);

				AddStrategy(strategy);
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}
	}

	private async void QuotingClick(object sender, RoutedEventArgs e)
	{
		var quoting = new MarketQuotingProcessorStrategy();

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

		await SaveStrategyAsync(quoting, default);
	}

	private void AddStrategy(Strategy strategy)
	{
		strategy.Connector = MainWindow.Instance.Connector;
		strategy.DisposeOnStop = false;

		Dashboard.Items.Add(new StrategiesDashboardItem(strategy)
		{
			SettingsCommand = new AsyncCommand(() => EditStrategyAsync(strategy), () => strategy.ProcessState == ProcessStates.Stopped)
		});
		MainWindow.Instance.LogManager.Sources.Add(strategy);
	}

	private async Task EditStrategyAsync(Strategy strategy)
	{
		try
		{
			var wnd = new StrategyEditWindow
			{
				Strategy = await strategy.CloneAsync(default),
			};

			if (!wnd.ShowModal(this))
				return;

			var id = strategy.Id;
			await strategy.ApplyAsync(wnd.Strategy);
			strategy.Id = id;
			await SaveStrategyAsync(strategy, default);
		}
		catch (Exception ex)
		{
			ex.LogError();
		}
	}

	private async Task SaveStrategyAsync(Strategy strategy, CancellationToken cancellationToken)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		var fs = MainWindow.Instance.FileSystem;
		var storage = await strategy.SaveEntireAsync(false, cancellationToken);
		await storage.SerializeAsync(fs, Path.Combine(_dir, $"{strategy.Id}{Paths.DefaultSettingsExt}"), true, cancellationToken);
	}
}