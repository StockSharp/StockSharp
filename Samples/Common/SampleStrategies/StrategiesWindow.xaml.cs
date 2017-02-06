namespace SampleStrategies
{
	using System;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Protective;
	using StockSharp.Algo.Strategies.Quoting;

	public partial class StrategiesWindow
	{
		private class StrategyItem
		{
			public string Name { get; }
			public Strategy Strategy { get; }

			public StrategyItem(string name, Strategy strategy)
			{
				if (name.IsEmpty())
					throw new ArgumentNullException(nameof(name));

				if (strategy == null)
					throw new ArgumentNullException(nameof(strategy));

				Name = name;
				Strategy = strategy;
			}
		}

		private readonly ObservableCollectionEx<StrategyItem> _items = new ObservableCollectionEx<StrategyItem>();
		private readonly ThreadSafeObservableCollection<StrategyItem> _itemsTs;

		public StrategiesWindow()
		{
			InitializeComponent();

			Dashboard.ItemsSource = _items;
			TakeProfit.EditValue = StopLoss.EditValue = 0m;

			_itemsTs = new ThreadSafeObservableCollection<StrategyItem>(_items);
		}

		private void QuotingClick(object sender, RoutedEventArgs e)
		{
			var wnd = new QuotingWindow
			{
				//Security = SecurityPicker.SelectedSecurity,
			};

			if (!wnd.ShowModal(this))
				return;

			var quoting = new MarketQuotingStrategy(wnd.Side, wnd.Volume)
			{
				Security = wnd.Security,
				Portfolio = wnd.Portfolio,
				Connector = MainWindow.Instance.Connector
			};

			if ((decimal?)TakeProfit.EditValue > 0 || (decimal?)StopLoss.EditValue > 0)
			{
				var tp = (decimal?)TakeProfit.EditValue;
				var sl = (decimal?)StopLoss.EditValue;

				quoting
					.WhenNewMyTrade()
					.Do(trade =>
					{
						var tpStrategy = tp == null ? null : new TakeProfitStrategy(trade, tp.Value);
						var slStrategy = sl == null ? null : new StopLossStrategy(trade, sl.Value);

						if (tpStrategy != null && slStrategy != null)
						{
							var strategy = new TakeProfitStopLossStrategy(tpStrategy, slStrategy);
							AddStrategy($"TPSL {trade.Trade.Price} Vol={trade.Trade.Volume}", strategy);
						}
						else if (tpStrategy != null)
						{
							AddStrategy($"TP {trade.Trade.Price} Vol={trade.Trade.Volume}", tpStrategy);
						}
						else if (slStrategy != null)
						{
							AddStrategy($"SL {trade.Trade.Price} Vol={trade.Trade.Volume}", slStrategy);
						}
					})
					.Apply(quoting);
			}

			AddStrategy($"Quoting {quoting.Security} {wnd.Side} Vol={wnd.Volume}", quoting);
		}

		private void AddStrategy(string name, Strategy strategy)
		{
			_itemsTs.Add(new StrategyItem(name, strategy));
			MainWindow.Instance.LogManager.Sources.Add(strategy);
			strategy.Start();
		}

		private bool Dashboard_OnCanExecuteStart(object arg)
		{
			return ((StrategyItem)arg).Strategy.ProcessState == ProcessStates.Stopped;
		}

		private bool Dashboard_OnCanExecuteStop(object arg)
		{
			return ((StrategyItem)arg).Strategy.ProcessState == ProcessStates.Started;
		}

		private void Dashboard_OnExecuteStart(object arg)
		{
			((StrategyItem)arg).Strategy.Start();
		}

		private void Dashboard_OnExecuteStop(object arg)
		{
			((StrategyItem)arg).Strategy.Stop();
		}
	}
}