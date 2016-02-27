#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StrategyRunner.StrategyRunnerPublic
File: StrategyContent.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.StrategyRunner
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Media;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Xaml.Diagram;

	public partial class StrategyContent
	{
		private DiagramStrategy _strategy;
		private DateTimeOffset _lastPnlTime;

		private readonly ICollection<EquityData> _totalPnL;
		private readonly ICollection<EquityData> _unrealizedPnL;
		private readonly ICollection<EquityData> _commission;

		public static readonly DependencyProperty StrategyProperty = DependencyProperty.Register(nameof(Strategy), typeof(DiagramStrategy), typeof(StrategyContent),
			new PropertyMetadata(StrategyChanged));

		private static void StrategyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((StrategyContent)sender).OnStrategyChanged( (DiagramStrategy)args.NewValue);
		}

		public DiagramStrategy Strategy
		{
			get { return _strategy; }
			set { SetValue(StrategyProperty, value); }
		}

		public StrategyContent()
		{
			InitializeComponent();

			_totalPnL = EquityCurveChart.CreateCurve(LocalizedStrings.PnL, Colors.Green, Colors.Red, EquityCurveChartStyles.Area);
			_unrealizedPnL = EquityCurveChart.CreateCurve(LocalizedStrings.PnLUnreal, Colors.Black);
			_commission = EquityCurveChart.CreateCurve(LocalizedStrings.Str159, Colors.Red, EquityCurveChartStyles.DashedLine);
		}

		private void OnStrategyChanged(DiagramStrategy strategy)
		{
			_strategy = strategy;

			_strategy.OrderRegistering += OnStrategyOrderRegistering;
			_strategy.OrderReRegistering += OnStrategyOrderReRegistering;
			_strategy.OrderRegisterFailed += OnStrategyOrderRegisterFailed;

			_strategy.StopOrderRegistering += OnStrategyOrderRegistering;
			_strategy.StopOrderReRegistering += OnStrategyOrderReRegistering;
			_strategy.StopOrderRegisterFailed += OnStrategyOrderRegisterFailed;

			_strategy.NewMyTrades += OnStrategyNewMyTrade;

			_strategy.PositionManager.NewPosition += OnStrategyNewPosition;
			_strategy.PositionManager.Positions.ForEach(OnStrategyNewPosition);

			_strategy.PnLChanged += OnStrategyPnLChanged;
			_strategy.Reseted += OnStrategyReseted;

			_strategy.SetChart(ChartPanel);

			PropertyGrid.SelectedObject = _strategy;
			StatisticParameterGrid.StatisticManager = _strategy.StatisticManager;
		}

		private void OnStrategyOrderRegisterFailed(OrderFail fail)
		{
			OrderGrid.AddRegistrationFail(fail);
		}

		private void OnStrategyOrderReRegistering(Order oldOrder, Order newOrder)
		{
			OrderGrid.Orders.Add(newOrder);
		}

		private void OnStrategyOrderRegistering(Order order)
		{
			OrderGrid.Orders.Add(order);
		}

		private void OnStrategyNewMyTrade(IEnumerable<MyTrade> trades)
		{
			MyTradeGrid.Trades.AddRange(trades);
		}

		private void OnStrategyNewPosition(KeyValuePair<Tuple<SecurityId, string>, decimal> position)
		{
			//PositionGrid.Positions.Add(position);
		}

		private void OnStrategyPnLChanged()
		{
			var time = _strategy.CurrentTime;

			if (time < _lastPnlTime)
				return; // TODO нужен перевод стратегий на месседжи
			//throw new InvalidOperationException("Новое значение даты для PnL {0} меньше ранее добавленного {1}.".Put(time, _lastPnlTime));

			_lastPnlTime = time;

			_totalPnL.Add(new EquityData { Time = time, Value = _strategy.PnL - (_strategy.Commission ?? 0) });
			_unrealizedPnL.Add(new EquityData { Time = time, Value = _strategy.PnLManager.UnrealizedPnL });
			_commission.Add(new EquityData { Time = time, Value = _strategy.Commission ?? 0 });
		}

		private void OnStrategyReseted()
		{
			_lastPnlTime = DateTimeOffset.MinValue;

			_totalPnL.Clear();
			_unrealizedPnL.Clear();
			_commission.Clear();

			OrderGrid.Orders.Clear();
			MyTradeGrid.Trades.Clear();
			PositionGrid.Positions.Clear();

			ChartPanel.Reset(ChartPanel.Elements);
		}
	}
}
