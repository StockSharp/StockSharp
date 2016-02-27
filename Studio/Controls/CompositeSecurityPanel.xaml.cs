#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: CompositeSecurityPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Data;
	using System.Windows.Input;
	using System.Windows.Media;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.ComponentModel;
	using Ecng.Xaml;
	using Ecng.Xaml.Converters;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Configuration;
	using StockSharp.Logging;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	public partial class CompositeSecurityPanel
	{
		public class CandlePartIndicator : BaseIndicator
		{
			private readonly Security _security;

			public CandlePartIndicator()
			{
			}

			public CandlePartIndicator(Security security)
			{
				_security = security;
			}

			protected override IIndicatorValue OnProcess(IIndicatorValue input)
			{
				var candle = input.GetValue<Candle>();
				IsFormed = _security == null || candle.Security == _security;
				return new CandleIndicatorValue(this, candle, c => c.ClosePrice);
			}
		}

		public static readonly RoutedCommand SaveSecurityCommand = new RoutedCommand();
		public static readonly RoutedCommand DrawSecurityCommand = new RoutedCommand();

		#region DependencyProperty

		public static readonly DependencyProperty MarketDataSettingsProperty = DependencyProperty.Register(nameof(MarketDataSettings), typeof(MarketDataSettings), typeof(CompositeSecurityPanel),
			new PropertyMetadata(OnMarketDataSettingsChanged));

		private static void OnMarketDataSettingsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var ctrl = (CompositeSecurityPanel)sender;

			ctrl._candleManager.Sources.OfType<StorageCandleSource>().Single().StorageRegistry = new StudioStorageRegistry { MarketDataSettings = (MarketDataSettings)args.NewValue };
			ctrl.RaiseChangedCommand();
		}

		public MarketDataSettings MarketDataSettings
		{
			get { return (MarketDataSettings)GetValue(MarketDataSettingsProperty); }
			set { SetValue(MarketDataSettingsProperty, value); }
		}

		public static readonly DependencyProperty DateFromProperty = DependencyProperty.Register(nameof(DateFrom), typeof(DateTime), typeof(CompositeSecurityPanel),
			new PropertyMetadata(OnPropertyChanged));

		public DateTime DateFrom
		{
			get { return (DateTime)GetValue(DateFromProperty); }
			set { SetValue(DateFromProperty, value); }
		}

		public static readonly DependencyProperty DateToProperty = DependencyProperty.Register(nameof(DateTo), typeof(DateTime), typeof(CompositeSecurityPanel),
			new PropertyMetadata(OnPropertyChanged));

		public DateTime DateTo
		{
			get { return (DateTime)GetValue(DateToProperty); }
			set { SetValue(DateToProperty, value); }
		}

		public static readonly DependencyProperty SecurityCodeProperty = DependencyProperty.Register(nameof(SecurityCode), typeof(string), typeof(CompositeSecurityPanel),
			new PropertyMetadata(string.Empty));

		public string SecurityCode
		{
			get { return (string)GetValue(SecurityCodeProperty); }
			set { SetValue(SecurityCodeProperty, value); }
		}

		public static readonly DependencyProperty BoardProperty = DependencyProperty.Register(nameof(Board), typeof(ExchangeBoard), typeof(CompositeSecurityPanel),
			new PropertyMetadata(ExchangeBoard.Associated));

		public ExchangeBoard Board
		{
			get { return (ExchangeBoard)GetValue(BoardProperty); }
			set { SetValue(BoardProperty, value); }
		}

		public static readonly DependencyProperty CanEditProperty = DependencyProperty.Register(nameof(CanEdit), typeof(bool), typeof(CompositeSecurityPanel),
			new PropertyMetadata(true));

		public bool CanEdit
		{
			get { return (bool)GetValue(CanEditProperty); }
			set { SetValue(CanEditProperty, value); }
		}

		public static readonly DependencyProperty IsStartedProperty = DependencyProperty.Register(nameof(IsStarted), typeof(bool), typeof(CompositeSecurityPanel),
			new PropertyMetadata(false, IsStartedPropertyChanged));

		private static void IsStartedPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((CompositeSecurityPanel)sender)._isStarted = (bool)args.NewValue;
		}

		public bool IsStarted
		{
			get { return _isStarted; }
			set { SetValue(IsStartedProperty, value); }
		}

		private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((CompositeSecurityPanel)sender).RaiseChangedCommand();
		}

		public static readonly DependencyProperty DrawSourcesProperty = DependencyProperty.Register(nameof(DrawSources), typeof(bool), typeof(CompositeSecurityPanel),
			new PropertyMetadata(true));

		public bool DrawSources
		{
			get { return (bool)GetValue(DrawSourcesProperty); }
			set { SetValue(DrawSourcesProperty, value); }
		}

		#endregion

		private readonly Color[] _colors =
		{
			Colors.Black, Colors.Blue, Colors.BlueViolet, Colors.CadetBlue, Colors.Chocolate, 
			Colors.DarkBlue, Colors.CornflowerBlue, Colors.ForestGreen, Colors.Indigo, Colors.Turquoise
		};

		private readonly SyncObject _syncRoot = new SyncObject();
		private readonly CandleManager _candleManager = new CandleManager();
		private readonly SecurityIdGenerator _idGenerator = new SecurityIdGenerator();
		private readonly SynchronizedSet<IChartElement> _changedElements = new SynchronizedSet<IChartElement>();
		private readonly SynchronizedSet<IChartElement> _skipElements = new SynchronizedSet<IChartElement>();
		private readonly Dictionary<Security, ChartIndicatorElement> _sourceElements = new Dictionary<Security, ChartIndicatorElement>();
		private readonly Dictionary<ChartIndicatorElement, IIndicator> _indicators = new Dictionary<ChartIndicatorElement, IIndicator>();
		private readonly ResettableTimer _timer;
		private readonly ResettableTimer _drawTimer;
		private readonly BufferedChart _bufferedChart;
		
		//private bool _suspendChangedEvent;
		private bool _isLoaded;
		private bool _canSave;
		private bool _isStarted;
		private int _candlesCount;
		private ChartArea _mainArea;
		private ChartCandleElement _candleElement;

		public bool HasError { get; private set; }

		public virtual Type SecurityType => typeof(Security);

		private Security _security;

		public Security Security
		{
			get { return _security; }
			set
			{
				if (value == null)
					throw new ArgumentNullException();

				_security = value;
				_canSave = true;
				CanEdit = true;

				if (!_security.Id.IsEmpty())
				{
					var id = _idGenerator.Split(_security.Id);

					SecurityCode = id.SecurityCode;
					Board = ExchangeBoard.GetOrCreateBoard(id.BoardCode);

					CanEdit = false;
					Title = _security.Id;
				}
				else
					Title = LocalizedStrings.Str3217;

				if (OnSecurityChanged(_security))
					_canSave = false;
			}
		}

		protected virtual string DefaultSecurityCode => string.Empty;

		public CompositeSecurityPanel()
		{
			InitializeComponent();

			InputBorder.SetBindings(IsEnabledProperty, this, "IsStarted", BindingMode.OneWay, new InverseBooleanConverter());

			MarketDataSettings = ConfigManager.GetService<MarketDataSettingsCache>().Settings.First(s => s.Id != Guid.Empty);
			DateFrom = DateTime.Today.AddDays(-180);
			DateTo = DateTime.Today;

			_timer = new ResettableTimer(TimeSpan.FromSeconds(30), "Composite");
			_timer.Elapsed += canProcess =>
			{
				GuiDispatcher.GlobalDispatcher.AddAction(() => IsStarted = false);
				_bufferedChart.IsAutoRange = false;
			};

			_drawTimer = new ResettableTimer(TimeSpan.FromSeconds(2), "Composite 2");
			_drawTimer.Elapsed += DrawTimerOnElapsed;

			ChartPanel.SubscribeCandleElement += OnChartPanelSubscribeCandleElement;
			ChartPanel.SubscribeIndicatorElement += OnChartPanelSubscribeIndicatorElement;
			ChartPanel.UnSubscribeElement += OnChartPanelUnSubscribeElement;
			
			ChartPanel.IsInteracted = true;
			ChartPanel.MinimumRange = 200;
			ChartPanel.FillIndicators();

			SecurityPicker.SetColumnVisibility("Id", Visibility.Visible);
			SecurityPicker.SetColumnVisibility("Code", Visibility.Collapsed);

			_candleManager.Container.CandlesKeepTime = TimeSpan.FromDays(5000);
			_candleManager.Processing += ProcessCandle;

			WhenLoaded(OnLoaded);

			_bufferedChart = new BufferedChart(ChartPanel);
		}

		protected virtual bool OnSecurityChanged(Security security)
		{
			return false;
		}

		protected virtual void UpdateSecurity(Security security)
		{
			
		}

		protected virtual void InsertSecurity(Security security)
		{
		}

		protected void ShowError(string errorText)
		{
			_canSave = errorText == null;
			HasError = errorText != null;

			InputBorder.BorderBrush = HasError ? Brushes.Red : null;

			var tooltip = (ToolTip)InputBorder.ToolTip;
			((TextBlock)tooltip.Content).Text = errorText;
			tooltip.Placement = PlacementMode.Bottom;
			tooltip.PlacementTarget = InputBorder;
			tooltip.IsOpen = HasError;
		}

		protected string Validate(IEnumerable<Security> innerSecurities, Security parent = null)
		{
			foreach (var inner in innerSecurities)
			{
				if (inner == Security)
					return parent != null
						? LocalizedStrings.Str3218Params.Put(Security.Id, parent.Id)
						: LocalizedStrings.Str3219Params.Put(Security.Id);

				var innerBasket = inner as BasketSecurity;

				if (innerBasket != null)
					return Validate(innerBasket.InnerSecurities, inner);
			}

			return null;
		}

		private void RaiseChangedCommand()
		{
			new ControlChangedCommand(this).Process(this);
		}

		private void OnLoaded()
		{
			SecurityPicker.SecurityProvider = ConfigManager.GetService<ISecurityProvider>();

			_mainArea = ChartPanel.Areas.FirstOrDefault();

			if (_mainArea == null)
				_bufferedChart.AddArea(_mainArea = new ChartArea { Title = LocalizedStrings.Panel + " 1" });

			_candleElement = _mainArea.Elements.OfType<ChartCandleElement>().FirstOrDefault();

			if (_candleElement == null)
				_bufferedChart.AddElement(_mainArea, _candleElement = new ChartCandleElement(), CreateSeries());

			_mainArea
				.Elements
				.OfType<ChartIndicatorElement>()
				.Where(e => _indicators.TryGetValue(e) is CandlePartIndicator)
				.ForEach(e => _sourceElements.Add(((CandleSeries)_bufferedChart.GetSource(e)).Security, e));

			_bufferedChart.AddAction(() =>
			{
				_isLoaded = true;

				if (!HasError)
					StartSeries();
			});
		}

		private void OnChartPanelSubscribeCandleElement(ChartCandleElement element, CandleSeries candleSeries)
		{
			AddElement(element, candleSeries);
		}

		private void OnChartPanelSubscribeIndicatorElement(ChartIndicatorElement element, CandleSeries candleSeries, IIndicator indicator)
		{
			_bufferedChart.SetSource(element, candleSeries);
			_indicators.Add(element, indicator);

			AddElement(element, candleSeries);
		}

		private void OnChartPanelUnSubscribeElement(IChartElement element)
		{
			if (!_isLoaded)
				return;

			var series = (CandleSeries)_bufferedChart.GetSource(element);

			if (series == null)
				return;

			element.DoIf<IChartElement, ChartCandleElement>(e => _candleManager.Stop(series));
		}

		private void AddElement(IChartElement element, CandleSeries candleSeries)
		{
			if (!_isLoaded || candleSeries == null)
				return;

			_changedElements.Add(element);
			_skipElements.Add(element);
			_drawTimer.Activate();
		}

		#region IStudioControl

		//TODO: дописать логику загрузки состояния для DockingManager
		public override void Load(SettingsStorage storage)
		{
			//_suspendChangedEvent = true;

			//Expression = storage.GetValue("Expression", Expression);
			DateFrom = storage.GetValue("DateFrom", DateFrom);
			DateTo = storage.GetValue("DateTo", DateTo);

			var marketDataSettings = storage.GetValue<string>("MarketDataSettings");
			if (marketDataSettings != null)
			{
				var id = marketDataSettings.To<Guid>();
				var settings = ConfigManager.GetService<MarketDataSettingsCache>().Settings.FirstOrDefault(s => s.Id == id);

				if (settings != null)
					MarketDataSettings = settings;
			}

			var chart = storage.GetValue<SettingsStorage>("ChartPanel");
			if (chart != null)
				ChartPanel.Load(chart);

			var securityPicker = storage.GetValue<SettingsStorage>("SecurityPicker");
			if (securityPicker != null)
				SecurityPicker.Load(securityPicker);

			var layout = storage.GetValue<string>("Layout");

			//if (layout != null)
			//	DockingManager.LoadLayout(layout);

			//_suspendChangedEvent = false;
		}

		//TODO: дописать логику сохранения состояния для DockingManager
		public override void Save(SettingsStorage storage)
		{
			//storage.SetValue("Expression", Expression);
			storage.SetValue("DateFrom", DateFrom);
			storage.SetValue("DateTo", DateTo);

			if (MarketDataSettings != null)
				storage.SetValue("MarketDataSettings", MarketDataSettings.Id.To<string>());

			storage.SetValue("ChartPanel", ChartPanel.Save());
			storage.SetValue("SecurityPicker", SecurityPicker.Save());
			//storage.SetValue("Layout", DockingManager.SaveLayout());
		}

		public override void Dispose()
		{
			if (IsStarted)
				StopSeries();

			_drawTimer.Flush();
			_timer.Flush();
		}

		#endregion

		private Security CreateSecurity()
		{
			var security = SecurityType.CreateInstance<Security>();

			var code = SecurityCode.IsEmpty() ? DefaultSecurityCode : SecurityCode;

			security.Id = _idGenerator.GenerateId(code, Board);
			security.Code = code;
			security.Name = code;
			security.Board = Board;

			UpdateSecurity(security);

			return security;
		}

		private CandleSeries CreateSeries()
		{
			return new CandleSeries(typeof(TimeFrameCandle), CreateSecurity(), TimeSpan.FromMinutes(5))
			{
				From = DateFrom,
				To = DateTo
			};
		}

		private void AddIndicator(ChartIndicatorElement element)
		{
			var series = (CandleSeries)_bufferedChart.GetSource(element);

			if (series == null)
				return;

			if (_sourceElements.ContainsKey(series.Security))
				return;

			IEnumerable<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>> allValues;

			lock (_syncRoot)
			{
				allValues = _candleManager
					.GetCandles<TimeFrameCandle>(series)
					.Take(_candlesCount)
					.Select(candle => new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(candle.OpenTime, new Dictionary<IChartElement, object>
					{
						{ element, CreateIndicatorValue(element, candle) }
					}))
					.ToArray();

				_skipElements.Remove(element);
			}

			_bufferedChart.Reset(new[] { element });
			_bufferedChart.Draw(allValues);
		}

		private void ProcessCandle(CandleSeries series, Candle candle)
		{
			// возможно была задержка в получении данных и обработаны еще не все данные
			if (!_isStarted)
				this.GuiAsync(() => IsStarted = true);

			_timer.Activate();

			_candlesCount++;

			// ограничиваем кол-во передаваемых свечек, чтобы не фризился интерфейс
			if (_candlesCount % 100 == 0)
				System.Threading.Thread.Sleep(200);

			var candleSeries = (CandleSeries)_bufferedChart.GetSource(_candleElement);

			if (series == candleSeries)
			{
				var values = new Dictionary<IChartElement, object>();

				lock (_syncRoot)
				{
					foreach (var element in _bufferedChart.Elements.Where(e => _bufferedChart.GetSource(e) == series))
					{
						if (_skipElements.Contains(element))
							continue;

						element.DoIf<IChartElement, ChartCandleElement>(e => values.Add(e, candle));
						element.DoIf<IChartElement, ChartIndicatorElement>(e => values.Add(e, CreateIndicatorValue(e, candle)));
					}
				}

				_bufferedChart.Draw(candle.OpenTime, values);

				if (series.Security is ContinuousSecurity)
				{
					// для непрерывных инструментов всегда приходят данные по одной серии
					// но инструмент у свечки будет равен текущему инструменту
					ProcessContinuousSourceElements(candle);
				}
			}
			else
			{
				// для индексов будут приходить отдельные свечки для каждого инструмента
				ProcessIndexSourceElements(candle);
			}
		}

		private void ProcessIndexSourceElements(Candle candle)
		{
			var element = _sourceElements.TryGetValue(candle.Security);

			if (element == null)
				return;

			_bufferedChart.Draw(candle.OpenTime, new Dictionary<IChartElement, object>
			{
				{ element, CreateIndicatorValue(element, candle) }
			});
		}

		private void ProcessContinuousSourceElements(Candle candle)
		{
			var values = _sourceElements
				.Select(sourceElement => sourceElement.Value)
				.ToDictionary<ChartIndicatorElement, IChartElement, object>(e => e, e => CreateIndicatorValue(e, candle));

			_bufferedChart.Draw(candle.OpenTime, values);
		}

		private IIndicatorValue CreateIndicatorValue(ChartIndicatorElement element, Candle candle)
		{
			var indicator = _indicators.TryGetValue(element);

			if (indicator == null)
				throw new InvalidOperationException(LocalizedStrings.IndicatorNotFound.Put(element));

			return indicator.Process(candle);
		}

		private void DrawTimerOnElapsed(Func<bool> canProcess)
		{
			try
			{
				RaiseChangedCommand();

				var elements = _changedElements.CopyAndClear();

				var candleElement = elements.OfType<ChartCandleElement>().FirstOrDefault();

				if (candleElement == null)
				{
					foreach (var indicatorElement in elements.OfType<ChartIndicatorElement>())
						AddIndicator(indicatorElement);
				}
				else
				{
					_candlesCount = 0;

					_bufferedChart.IsAutoRange = true;
					GuiDispatcher.GlobalDispatcher.AddAction(() => IsStarted = true);

					_skipElements.Clear();
					_candleManager.Start((CandleSeries)_bufferedChart.GetSource(candleElement));
				}
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		private void Save(bool showErrorIfNoCode)
		{
			if (SecurityCode.IsEmpty() || Board == null)
			{
				if (!showErrorIfNoCode)
					return;

				new MessageBoxBuilder()
					.Owner(this)
					.Caption(LocalizedStrings.Str3220)
					.Text(LocalizedStrings.Str3221)
					.Warning()
					.Show();

				return;
			}

			var id = _idGenerator.GenerateId(SecurityCode, Board);
			var registry = ConfigManager.GetService<IEntityRegistry>();
			var security = registry.Securities.ReadById(id);

			if (security != null && security.GetType() != SecurityType)
			{
				new MessageBoxBuilder()
					.Owner(this)
					.Caption(LocalizedStrings.Str3222)
					.Text(LocalizedStrings.Str3223Params.Put(security.Id, security.Type))
					.Error()
					.Show();

				return;
			}

			if (security != null)
			{
				if (security != Security)
				{
					var res = new MessageBoxBuilder()
						.Owner(this)
						.Caption(LocalizedStrings.Str3222)
						.Text(LocalizedStrings.Str3224Params.Put(security.Id))
						.Question()
						.YesNo()
						.Show();

					if (res != MessageBoxResult.Yes)
						return;
				}

				UpdateSecurity(security);
				registry.Securities.Save(security);

				Security = security;
			}
			else
			{
				security = Security;

				security.Id = id;
				security.Code = SecurityCode;
				security.Board = Board;

				UpdateSecurity(security);

				registry.Securities.Add(security);
				SecurityPicker.Securities.Add(security); //TODO индексы не приходят через событие registry.Securities.Added

				_bufferedChart
					.Elements
					.Select(e => _bufferedChart.GetSource(e))
					.OfType<CandleSeries>()
					.ForEach(series =>
					{
						series.Security.Id = id;
						series.Security.Code = SecurityCode;
						series.Security.Board = Board;
					});

				CanEdit = false;
				Title = id;

				RaiseChangedCommand();
			}

			_canSave = false;
		}

		private void StopSeries()
		{
			var element = _bufferedChart.Elements.OfType<ChartCandleElement>().FirstOrDefault();

			if (element != null)
			{
				var series = (CandleSeries)_bufferedChart.GetSource(element);
				_candleManager.Stop(series);
			}

			_timer.Cancel();
		}

		private void ExecutedSaveSecurityCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Save(true);
		}

		private void CanExecuteSaveSecurityCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _canSave && !HasError;
		}

		private void ExecutedDrawSecurityCommand(object sender, ExecutedRoutedEventArgs e)
		{
			if (!IsStarted)
			{
				Save(false);
				StartSeries();
			}
			else
				StopSeries();
		}

		private void StartSeries()
		{
			var mainSeries = (CandleSeries)_bufferedChart.GetSource(_candleElement);

			// останавливаем текущую серию, если он запущена
			if (_candleElement != null && _candleManager.Series.Any(s => s == mainSeries))
				_candleManager.Stop(mainSeries);

			_bufferedChart.Reset(_bufferedChart.Elements);

			RemoveSourceElements();

			mainSeries.Security = CreateSecurity();
			mainSeries.From = DateFrom;
			mainSeries.To = DateTo;

			if (DrawSources)
			{
				mainSeries.Security.DoIf<Security, IndexSecurity>(CreateSourceElements);
				mainSeries.Security.DoIf<Security, ContinuousSecurity>(CreateSourceElements);
			}

			IsStarted = true;

			AddElement(_candleElement, mainSeries);

			_drawTimer.Flush();
		}

		private void RemoveSourceElements()
		{
			var area = _mainArea;

			area.Elements.RemoveWhere(el =>
			{
				var indElement = el as ChartIndicatorElement;

				if (indElement == null)
					return false;

				var series = (CandleSeries)_bufferedChart.GetSource(indElement);

				if (_sourceElements.ContainsKey(series.Security))
				{
					_sourceElements.Remove(series.Security);
					return true;
				}

				return false;
			});

			area.YAxises.RemoveWhere(a => a.Id.StartsWith("SA_"));
		}

		private void CreateSourceElements(IndexSecurity security)
		{
			var area = _mainArea;

			var axisId = 1;

			foreach (var innerSecurity in security.InnerSecurities)
			{
				var axisName = "SA_" + axisId++;

				var series = new CandleSeries(typeof(TimeFrameCandle), innerSecurity, TimeSpan.FromMinutes(5));
				var indicatorElement = new ChartIndicatorElement
				{
					Title = innerSecurity.Id,
					Color = _colors[axisId],
					YAxisId = axisName,
					StrokeThickness = 1
				};

				area.YAxises.Add(new ChartAxis
				{
					Id = axisName,
					AutoRange = true,
					AxisType = ChartAxisType.Numeric,
					AxisAlignment = ChartAxisAlignment.Right
				});

				var indicator = new CandlePartIndicator();

				//_indicators.Add(indicatorElement, indicator);
				_sourceElements.Add(innerSecurity, indicatorElement);
				_bufferedChart.AddElement(area, indicatorElement, series, indicator);
			}
		}

		private void CreateSourceElements(ContinuousSecurity security)
		{
			var area = _mainArea;
			var id = 1;

			foreach (var innerSecurity in security.InnerSecurities)
			{
				var series = new CandleSeries(typeof(TimeFrameCandle), innerSecurity, TimeSpan.FromMinutes(5));
				var indicatorElement = new ChartIndicatorElement
				{
					Title = innerSecurity.Id,
					Color = _colors[id++],
					StrokeThickness = 1
				};

				var indicator = new CandlePartIndicator();

				//_indicators.Add(indicatorElement, indicator);
				_sourceElements.Add(innerSecurity, indicatorElement);
				_bufferedChart.AddElement(area, indicatorElement, series, indicator);
			}
		}

		private void CanExecuteDrawSecurityCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !HasError;
		}

		private void SecurityPicker_OnSecurityDoubleClick(Security security)
		{
			if (IsStarted)
				return;

			InsertSecurity(security);
		}

		private void SecurityPicker_OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (IsStarted)
				return;

			var security = SecurityPicker.SelectedSecurity;

			if (security == null)
				return;

			DragDrop.DoDragDrop(SecurityPicker, security, DragDropEffects.Copy);
		}
	}

	[DisplayNameLoc(LocalizedStrings.Str2691Key)]
	[DescriptionLoc(LocalizedStrings.Str3225Key)]
	[Icon("images/index_32x32.png")]
	public class IndexSecurityPanel : CompositeSecurityPanel
	{
		private readonly Dictionary<string, Security> _securities = new Dictionary<string, Security>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Regex _securityRegex = new Regex(@"[^\[]*\[([^\]]+)]");
		private readonly TextBox _editor = new TextBox { AllowDrop = true };

		public override Type SecurityType => typeof(ExpressionIndexSecurity);

		public string Expression
		{
			get { return _editor.Text; }
			set { _editor.Text = value; }
		}

		protected override string DefaultSecurityCode => "Index";

		public IndexSecurityPanel()
		{
			InputBorder.Child = _editor;

			IndexSecurityWindow.Title = LocalizedStrings.Index;

			_editor.TextChanged += (s, a) => Validate();
			_editor.Drop += InputTextBox_OnDrop;
			_editor.PreviewDragOver += InputTextBox_OnPreviewDragOver;

			//Expression = "RIU4@FORTS / RIM4@FORTS";

			var securities = "RI"
				.GetFortsJumps(DateTime.Today.AddMonths(-4), DateTime.Today.AddMonths(1), code => new Security
				{
					Id = code + "@" + ExchangeBoard.Forts.Code,
					Code = code,
					Board = ExchangeBoard.Forts,
				})
				.Take(2)
				.ToArray();

			Expression = "{0} / {1}".Put(securities[1], securities[0]);
		}

		protected override bool OnSecurityChanged(Security security)
		{
			var expSec = (ExpressionIndexSecurity)security;

			if (expSec.Expression == null)
				return false;

			Expression = expSec.Expression;

			return true;
		}

		protected override void UpdateSecurity(Security security)
		{
			var expSec = (ExpressionIndexSecurity)security;
			expSec.Expression = Expression;
		}

		protected override void InsertSecurity(Security security)
		{
			_editor.Text = _editor.Text.Insert(_editor.CaretIndex, " {0} ".Put(security.Id));
		}

		private void Validate()
		{
			if (!Expression.IsEmpty())
			{
				var expression = new NCalc.Expression(ExpressionHelper.Encode(Expression));

				if (expression.HasErrors())
				{
					ShowError(expression.Error);
				}
				else
				{
					var matches = _securityRegex.Matches(expression.ParsedExpression.ToString());

					var securities = matches
						.Cast<Match>()
						.Select(m => m.Groups[1].ToString())
						.Select(m => new
						{
							Code = m, 
							Security = TryGetSecurity(m)
						})
						.ToArray();

					var item = securities.FirstOrDefault(m => m.Security == null);

					ShowError(item != null 
						? LocalizedStrings.Str1522Params.Put(item.Code) 
						: Validate(securities.Select(s => s.Security)));
				}
			}
			else
				ShowError(null);
		}

		private Security TryGetSecurity(string id)
		{
			var security = _securities.TryGetValue(id);

			if (security != null)
				return security;

			security = ConfigManager.GetService<IEntityRegistry>().Securities.ReadById(id);

			if (security != null)
				_securities.Add(id, security);

			return security;
		}

		private void InputTextBox_OnDrop(object sender, DragEventArgs e)
		{
			var security = (Security)e.Data.GetData(typeof(Security));

			_editor.Text = _editor.Text.Insert(_editor.SelectionStart, " {0} ".Put(security.Id));
		}

		private void InputTextBox_OnPreviewDragOver(object sender, DragEventArgs e)
		{
			var dropPosition = e.GetPosition(_editor);

			_editor.SelectionStart = GetCaretIndexFromPoint(_editor, dropPosition);
			_editor.SelectionLength = 0;

			_editor.Focus();
			e.Handled = true;
		}

		private static int GetCaretIndexFromPoint(TextBox textBox, Point point)
		{
			var index = textBox.GetCharacterIndexFromPoint(point, true);

			if (index != textBox.Text.Length - 1)
				return index;

			var caretRect = textBox.GetRectFromCharacterIndex(index);
			var caretPoint = new Point(caretRect.X, caretRect.Y);

			if (point.X > caretPoint.X)
				index += 1;

			return index;
		}
	}

	[DisplayNameLoc(LocalizedStrings.Str3226Key)]
	[DescriptionLoc(LocalizedStrings.Str3227Key)]
	[Icon("images/continuous_32x32.png")]
	public class ContinuousSecurityPanel : CompositeSecurityPanel
	{
		private readonly SecurityJumpsEditor _editor = new SecurityJumpsEditor();

		public override Type SecurityType => typeof(ContinuousSecurity);

		protected override string DefaultSecurityCode => "Continuous";

		public ContinuousSecurityPanel()
		{
			InputBorder.Child = CreateControl();

			IndexSecurityWindow.Title = LocalizedStrings.ContinuousSecurity;
			_editor.Changed += SecurityChanged;
			_editor.Drop += EditorOnDrop;

			var registry = ConfigManager.GetService<IEntityRegistry>();

			var securities = "RI"
				.GetFortsJumps(DateTime.Today.AddMonths(-4), DateTime.Today.AddMonths(6), code => registry.Securities.ReadById(code + "@" + ExchangeBoard.Forts.Code));

			foreach (var security in securities)
			{
				DateTime expDate;

				if (security.ExpiryDate == null)
				{
					if (security.Code.Length < 4)
						throw new InvalidOperationException(LocalizedStrings.Str3228Params.Put(security.Code));

					var year = (DateTime.Today.Year / 10) * 10 + security.Code.Substring(3, 1).To<int>();
					int month;

					switch (security.Code[2])
					{
						case 'H':
							month = 3;
							break;
						case 'M':
							month = 6;
							break;
						case 'U':
							month = 9;
							break;
						case 'Z':
							month = 12;
							break;
						default:
							throw new ArgumentException();
					}
					
					expDate = new DateTime(year, month, 15);
				}
				else
					expDate = security.ExpiryDate.Value.UtcDateTime;

				_editor.Jumps.Add(new SecurityJump { Security = security, Date = expDate });
			}
		}

		private Grid CreateControl()
		{
			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			Grid.SetColumn(_editor, 0);
			grid.Children.Add(_editor);

			var sp = new StackPanel
			{
				VerticalAlignment = VerticalAlignment.Center,
				Children =
				{
					new Button
					{
						Content = "",
						FontFamily = new FontFamily("Wingdings"),
						FontSize = 15,
						Margin = new Thickness(2),
						Command = new DelegateCommand(o => _editor.Jumps.Add(new SecurityJump { Security = SecurityPicker.SelectedSecurity }), o => SecurityPicker.SelectedSecurity != null),
						ToolTip = LocalizedStrings.Str3229
					},
					new Button
					{
						Content = "",
						FontFamily = new FontFamily("Wingdings"),
						FontSize = 15,
						Margin = new Thickness(2),
						Command = new DelegateCommand(o => _editor.Jumps.RemoveRange(_editor.SelectedJumps), o => _editor.SelectedJump != null),
						ToolTip = LocalizedStrings.Str2060
					}
				}
			};
			
			Grid.SetColumn(sp, 1);
			grid.Children.Add(sp);

			return grid;
		}

		private void SecurityChanged()
		{
			ShowError(_editor.Validate() ?? Validate(_editor.Jumps.Select(j => j.Security)));
		}

		protected override bool OnSecurityChanged(Security security)
		{
			var continuousSecurity = (ContinuousSecurity)security;

			if (continuousSecurity.ExpirationJumps.IsEmpty())
				return false;

			_editor.Jumps.Clear();
			_editor.Jumps.AddRange(continuousSecurity.ExpirationJumps.Select(p => new SecurityJump
			{
				Security = p.Key,
				Date = p.Value.UtcDateTime
			}));

			SecurityChanged();

			return true;
		}

		protected override void UpdateSecurity(Security security)
		{
			var continuousSecurity = (ContinuousSecurity)security;

			continuousSecurity.ExpirationJumps.Clear();
			continuousSecurity.ExpirationJumps.AddRange(_editor.Jumps.Select(j => new KeyValuePair<Security, DateTimeOffset>(j.Security, j.Date)));
		}

		protected override void InsertSecurity(Security security)
		{
			_editor.Jumps.Add(new SecurityJump { Security = security });
		}

		private void EditorOnDrop(object sender, DragEventArgs e)
		{
			var security = (Security)e.Data.GetData(typeof(Security));
			_editor.Jumps.Add(new SecurityJump { Security = security });
		}
	}
}
