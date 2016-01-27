#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: EmulationControl.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using Ecng.Common;
using Ecng.Configuration;
using Ecng.Serialization;
using Ecng.Xaml;

using StockSharp.Terminal.Layout;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Logging;
using StockSharp.Messages;
using StockSharp.Xaml.Charting;
using System.Diagnostics;
using StockSharp.Studio.Controls;
using Xceed.Wpf.AvalonDock.Layout;

namespace StockSharp.Terminal.Controls
{
	public partial class WorkAreaControl
	{
		//private class TradeCandleBuilderSourceEx : TradeCandleBuilderSource
		//{
		//	public TradeCandleBuilderSourceEx(IConnector connector)
		//		: base(connector)
		//	{
		//	}

		//	protected override void RegisterSecurity(Security security)
		//	{
		//	}

		//	protected override void UnRegisterSecurity(Security security)
		//	{
		//	}
		//}

		#region DependencyProperty

		//public static readonly DependencyProperty StrategyProperty = DependencyProperty.Register("Strategy", typeof(EmulationDiagramStrategy), 
		//	typeof(WorkAreaControl), new PropertyMetadata(null, OnStrategyPropertyChanged));

		//private static void OnStrategyPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
		//{
		//	((WorkAreaControl)s).OnStrategyChanged((EmulationDiagramStrategy)e.OldValue, (EmulationDiagramStrategy)e.NewValue);
		//}

		//public EmulationDiagramStrategy Strategy
		//{
		//	get { return (EmulationDiagramStrategy)GetValue(StrategyProperty); }
		//	set { SetValue(StrategyProperty, value); }
		//}

		#endregion

		//private readonly BufferedChart _bufferedChart;
		private readonly LayoutManager _layoutManager;
		//private readonly ICollection<EquityData> _pnlCurve;
		//private readonly ICollection<EquityData> _unrealizedPnLCurve;
		//private readonly ICollection<EquityData> _commissionCurve;
		//private readonly ICollection<EquityData> _posItems;

		//private HistoryEmulationConnector _connector;

		//public override string Key => Strategy.Id.ToString();

		public ICommand StartCommand { get; private set; }

		public ICommand StopCommand { get; private set; }

		//public ICommand AddBreakpointCommand => DiagramDebuggerControl.AddBreakpointCommand;

		//public ICommand RemoveBreakpointCommand => DiagramDebuggerControl.RemoveBreakpointCommand;

		//public ICommand StepNextCommand => DiagramDebuggerControl.StepNextCommand;

		//public ICommand StepToOutParamCommand => DiagramDebuggerControl.StepToOutParamCommand;

		//public ICommand StepIntoCommand => DiagramDebuggerControl.StepIntoCommand;

		//public ICommand StepOutCommand => DiagramDebuggerControl.StepOutCommand;

		//public ICommand ContinueCommand => DiagramDebuggerControl.ContinueCommand;

		private Dictionary<string, Type> _controls;

		public WorkAreaControl()
		{
			InitializeCommands();
            InitializeComponent();

			_controls = new Dictionary<string, Type>()
			{
				{ "TradesPanel", typeof(TradesPanel) },
				{ "OrdersPanel", typeof(OrdersPanel) },
				{ "SecuritiesPanel", typeof(SecuritiesPanel) },
				{ "Level2Panel", typeof(Level2Panel) },
				{ "NewsPanel", typeof(NewsPanel) },
				{ "PortfoliosPanel", typeof(PortfoliosPanel) },
				{ "CandleChartPanel", typeof(CandleChartPanel) },
			};

			//_bufferedChart = new BufferedChart(Chart);
			_layoutManager = new LayoutManager(DockingManager);

			//WorkArea.PropertyChanging += WorkArea_PropertyChanging;
			//WorkArea.ChildrenCollectionChanged += WorkArea_ChildrenCollectionChanged;
			//WorkArea.ChildrenTreeChanged += WorkArea_ChildrenTreeChanged;

			//_pnlCurve = Curve.CreateCurve(LocalizedStrings.PnL, Colors.DarkGreen, EquityCurveChartStyles.Area);
			//_unrealizedPnLCurve = Curve.CreateCurve(LocalizedStrings.PnLUnreal, Colors.Black);
			//_commissionCurve = Curve.CreateCurve(LocalizedStrings.Str159, Colors.Red, EquityCurveChartStyles.DashedLine);

			//_posItems = PositionCurve.CreateCurve(LocalizedStrings.Str862, Colors.DarkGreen);
		}

		public void AddControl(string controlName)
		{
			Type controlType = null;

			if (!_controls.TryGetValue(controlName, out controlType))
				return;

			var control = Activator.CreateInstance(controlType);
			var anchor = new LayoutAnchorable()
			{
				Title = controlName,
				CanClose = false
			};
			var pane = new LayoutAnchorablePane();

			anchor.Content = control;
			pane.Children.Add(anchor);

			DockingManagerGroup.Children.Add(pane);
		}

		//private void WorkArea_ChildrenTreeChanged(object sender, Xceed.Wpf.AvalonDock.Layout.ChildrenTreeChangedEventArgs e)
		//{
		//	Debug.WriteLine("ChildrenTreeChanged");
		//}

		//private void WorkArea_ChildrenCollectionChanged(object sender, EventArgs e)
		//{
		//	Debug.WriteLine("ChildrenCollectionChanged");
		//}

		//private void WorkArea_PropertyChanging(object sender, System.ComponentModel.PropertyChangingEventArgs e)
		//{
		//	Debug.WriteLine("PropertyChanging");
		//}

		public override bool CanClose()
		{
			//if (_connector == null || _connector.State == EmulationStates.Stopped)
			//	return true;

			//new MessageBoxBuilder()
			//	.Owner(this)
			//	.Caption(Title)
			//	.Text(LocalizedStrings.Str3617Params.Put(Title))
			//	.Icon(MessageBoxImage.Warning)
			//	.Button(MessageBoxButton.OK)
			//	.Show();

			return true;
		}

		private void InitializeCommands()
		{
			//StartCommand = new DelegateCommand(
			//	obj => StartEmulation(),
			//	obj => _connector == null || _connector.State == EmulationStates.Stopped);

			//StopCommand = new DelegateCommand(
			//	obj => StopEmulation(),
			//	obj => _connector != null && _connector.State == EmulationStates.Started);
		}

		private void StartEmulation()
		{
			//if (_connector != null && _connector.State != EmulationStates.Stopped)
			//	throw new InvalidOperationException(LocalizedStrings.Str3015);

			//if (Strategy == null)
			//	throw new InvalidOperationException("Strategy not selected.");

			//var strategy = Strategy;

			//if (strategy.DataPath.IsEmpty() || !Directory.Exists(strategy.DataPath))
			//	throw new InvalidOperationException(LocalizedStrings.Str3014);

			//strategy.Reset();

			//OrderGrid.Orders.Clear();
			//MyTradeGrid.Trades.Clear();

			//_pnlCurve.Clear();
			//_unrealizedPnLCurve.Clear();
			//_commissionCurve.Clear();
			//_posItems.Clear();

			//var secGen = new SecurityIdGenerator();
			//var secIdParts = secGen.Split(strategy.SecurityId);
			//var secCode = secIdParts.SecurityCode;
			//var board = ExchangeBoard.GetOrCreateBoard(secIdParts.BoardCode);
			//var timeFrame = strategy.CandlesTimeFrame;
			//var useCandles = strategy.MarketDataSource == MarketDataSource.Candles;

			// create test security
			//var security = new Security
			//{
			//	Id = strategy.SecurityId, // sec id has the same name as folder with historical data
			//	Code = secCode,
			//	Board = board,
			//};

			// storage to historical data
			//var storageRegistry = new StorageRegistry
			//{
			//	// set historical path
			//	DefaultDrive = new LocalMarketDataDrive(strategy.DataPath)
			//};

			//var startTime = strategy.StartDate.ChangeKind(DateTimeKind.Utc);
			//var stopTime = strategy.StopDate.ChangeKind(DateTimeKind.Utc);

			// ProgressBar refresh step
			//var progressStep = ((stopTime - startTime).Ticks / 100).To<TimeSpan>();

			// set ProgressBar bounds
			//TicksAndDepthsProgress.Value = 0;
			//TicksAndDepthsProgress.Maximum = 100;

			//var level1Info = new Level1ChangeMessage
			//{
			//	SecurityId = security.ToSecurityId(),
			//	ServerTime = startTime,
			//}
			//	.TryAdd(Level1Fields.PriceStep, secIdParts.SecurityCode == "RIZ2" ? 10m : 1)
			//	.TryAdd(Level1Fields.StepPrice, 6m)
			//	.TryAdd(Level1Fields.MinPrice, 10m)
			//	.TryAdd(Level1Fields.MaxPrice, 1000000m)
			//	.TryAdd(Level1Fields.MarginBuy, 10000m)
			//	.TryAdd(Level1Fields.MarginSell, 10000m);

			// test portfolio
			//var portfolio = new Portfolio
			//{
			//	Name = "test account",
			//	BeginValue = 1000000,
			//};

			// create backtesting connector
			//_connector = new HistoryEmulationConnector(
			//	new[] { security },
			//	new[] { portfolio })
			//{
			//	EmulationAdapter =
			//	{
			//		Emulator =
			//		{
			//			Settings =
			//			{
			//				// match order if historical price touched our limit order price. 
			//				// It is terned off, and price should go through limit order price level
			//				// (more "severe" test mode)
			//				MatchOnTouch = false,
			//			}
			//		}
			//	},

			//	UseExternalCandleSource = useCandles,

			//	HistoryMessageAdapter =
			//	{
			//		StorageRegistry = storageRegistry,

			//		// set history range
			//		StartDate = startTime,
			//		StopDate = stopTime,
			//	},

			//	// set market time freq as time frame
			//	MarketTimeChangedInterval = timeFrame,
			//};

			//((ILogSource)_connector).LogLevel = DebugLogCheckBox.IsChecked == true ? LogLevels.Debug : LogLevels.Info;

			//ConfigManager.GetService<LogManager>().Sources.Add(_connector);

			//var candleManager = !useCandles
			//	                    ? new CandleManager(new TradeCandleBuilderSourceEx(_connector))
			//	                    : new CandleManager(_connector);

			//strategy.Volume = 1;
			//strategy.Portfolio = portfolio;
			//strategy.Security = security;
			//strategy.Connector = _connector;
			//LogLevel = DebugLogCheckBox.IsChecked == true ? LogLevels.Debug : LogLevels.Info,

			// by default interval is 1 min,
			// it is excessively for time range with several months
			//strategy.UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>();

			//strategy.SetChart(_bufferedChart);
			//strategy.SetCandleManager(candleManager);

			//_connector.NewSecurities += securities =>
			//{
			//	if (securities.All(s => s != security))
			//		return;

			//	// fill level1 values
			//	_connector.SendInMessage(level1Info);

			//	//_connector.RegisterMarketDepth(security);
			//	if (!useCandles)
			//		_connector.RegisterTrades(security);

			//	// start strategy before emulation started
			//	strategy.Start();

			//	// start historical data loading when connection established successfully and all data subscribed
			//	_connector.Start();
			//};

			//var nextTime = startTime + progressStep;

			// handle historical time for update ProgressBar
			//_connector.MarketTimeChanged += d =>
			//{
			//	if (_connector.CurrentTime < nextTime && _connector.CurrentTime < stopTime)
			//		return;

			//	var steps = (_connector.CurrentTime - startTime).Ticks / progressStep.Ticks + 1;
			//	nextTime = startTime + (steps * progressStep.Ticks).To<TimeSpan>();
			//	//this.GuiAsync(() => TicksAndDepthsProgress.Value = steps);
			//};

			//_connector.StateChanged += () =>
			//{
			//	switch (_connector.State)
			//	{
			//		case EmulationStates.Stopped:
			//			strategy.Stop();

			//			this.GuiAsync(() =>
			//			{
			//				//if (_connector.IsFinished)
			//				//	TicksAndDepthsProgress.Value = TicksAndDepthsProgress.Maximum;
			//			});
			//			break;
			//		case EmulationStates.Started:
			//			break;
			//	}
			//};

			//TicksAndDepthsProgress.Value = 0;

			// raise NewSecurities and NewPortfolio for full fill strategy properties
			//_connector.Connect();

			// 1 cent commission for trade
			//_connector.SendInMessage(new CommissionRuleMessage
			//{
			//	Rule = new CommissionPerTradeRule
			//	{
			//		Value = 0.01m
			//	}
			//});
		}

		private void StopEmulation()
		{
			//_connector.Disconnect();

			//if (DiagramDebuggerControl.Debugger.IsWaiting)
			//	DiagramDebuggerControl.Debugger.Continue();
		}

		private void OnStrategyOrderRegisterFailed(OrderFail fail)
		{
			//OrderGrid.AddRegistrationFail(fail);
		}

		private void OnStrategyOrderReRegistering(Order oldOrder, Order newOrder)
		{
			//OrderGrid.Orders.Add(newOrder);
		}

		private void OnStrategyOrderRegistering(Order order)
		{
			//OrderGrid.Orders.Add(order);
		}

		private void OnStrategyNewMyTrade(IEnumerable<MyTrade> trades)
		{
			//MyTradeGrid.Trades.AddRange(trades);
		}

		private void OnDiagramDebuggerControlChanged()
		{
			RaiseChanged();
		}

		#region IPersistable

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			//var compositionId = storage.GetValue<Guid>("CompositionId");

			//var registry = ConfigManager.GetService<StrategiesRegistry>();
			//var composition = (CompositionDiagramElement)registry.Strategies.FirstOrDefault(c => c.TypeId == compositionId);

			//Strategy = new EmulationDiagramStrategy
			//{
			//	Id = storage.GetValue<Guid>("StrategyId"),
			//	DataPath = storage.GetValue<string>("DataPath"),
			//	StartDate = storage.GetValue<DateTime>("StartDate"),
			//	StopDate = storage.GetValue<DateTime>("StopDate"),
			//	SecurityId = storage.GetValue<string>("SecurityId"),
			//	MarketDataSource = storage.GetValue<MarketDataSource>("MarketDataSource"),
			//	CandlesTimeFrame = storage.GetValue<TimeSpan>("CandlesTimeFrame"),
			//	//Composition = registry.Clone(composition)
			//};

            //DiagramDebuggerControl.Debugger.Load(storage.GetValue<SettingsStorage>("Debugger"));

			//_layoutManager.LoadLayout(storage.GetValue<string>("Layout"));
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			//if (Strategy == null)
			//	return;

			//storage.SetValue("CompositionId", Strategy.Composition.TypeId);
			//storage.SetValue("StrategyId", Strategy.Id);
			//storage.SetValue("DataPath", Strategy.DataPath);
			//storage.SetValue("StartDate", Strategy.StartDate);
			//storage.SetValue("StopDate", Strategy.StopDate);
			//storage.SetValue("SecurityId", Strategy.SecurityId);
			//storage.SetValue("MarketDataSource", Strategy.MarketDataSource);
			//storage.SetValue("CandlesTimeFrame", Strategy.CandlesTimeFrame);

			//storage.SetValue("Debugger", DiagramDebuggerControl.Debugger.Save());

			//storage.SetValue("Layout", _layoutManager.SaveLayout());
		}

		#endregion
	}

	public sealed class EmulationTitleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return $"{ LocalizedStrings.Str1174 } { value }";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
