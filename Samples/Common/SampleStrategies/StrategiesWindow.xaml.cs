namespace SampleStrategies
{
	using System;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
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

		public StrategiesWindow()
		{
			InitializeComponent();

			Dashboard.ItemsSource = _items;
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

			MainWindow.Instance.LogManager.Sources.Add(quoting);

			_items.Add(new StrategyItem($"Quoting {quoting.Security} {wnd.Side} Vol={wnd.Volume}", quoting));

			quoting.Start();
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