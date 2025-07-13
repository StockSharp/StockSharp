namespace StockSharp.Diagram.Elements;

using System.Collections.Specialized;

using Ecng.Configuration;

/// <summary>
/// </summary>
public interface IChartIndicatorElementWrapper
{
	/// <summary>
	/// </summary>
	IChartIndicatorElement Element { get; }
}

/// <summary>
/// Chart panel element (candles display area, indicators, orders and trades).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ChartPanelKey,
	Description = LocalizedStrings.ChartPanelElementKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/chart.html")]
[Browsable(false)]
public abstract class ChartDiagramElement<TChartIndicatorElementWrapper> : DiagramElement
	where TChartIndicatorElementWrapper : class, IChartIndicatorElementWrapper, new()
{
	private class ObsCollection<T> : ObservableCollection<T>
	{
		public event Action CollectionChanging;

		protected override void InsertItem(int index, T item)
		{
			CollectionChanging?.Invoke();
			base.InsertItem(index, item);
		}

		protected override void RemoveItem(int index)
		{
			CollectionChanging?.Invoke();
			base.RemoveItem(index);
		}

		protected override void ClearItems()
		{
			CollectionChanging?.Invoke();
			base.ClearItems();
		}

		public override string ToString()
		{
			return string.Empty;
		}
	}

	private class ChartElementSocket(ChartDiagramElement<TChartIndicatorElementWrapper> parent, DiagramSocketDirection dir, string socketId = null) : DiagramSocket(dir, socketId)
	{
		public const string EmptySuffix = "empty";

		private static readonly Type _invalidPainterType = typeof(object);
		private Type _cachedPainterType = _invalidPainterType;

		public IChartElement ChartElement { get; private set; }
		public TChartIndicatorElementWrapper IndicatorWrapper { get; private set; }

		private readonly ChartDiagramElement<TChartIndicatorElementWrapper> _parent = parent ?? throw new ArgumentNullException(nameof(parent));

		public static string GenerateSocketId(Guid? chartElementId)
			=> DiagramElement.GenerateSocketId(chartElementId == null ? EmptySuffix : chartElementId.Value.ToN());

		public static DiagramSocketType ChartElementToSocketType(IChartElement el, bool required)
			=> _typesList.FirstOrDefault(p => p.elementType.IsInstanceOfType(el)).socketType ?? (required ? throw new ArgumentOutOfRangeException(el.To<string>()) : null);

		private static readonly List<(Type elementType, DiagramSocketType socketType)> _typesList =
		[
			(typeof(IChartCandleElement),            DiagramSocketType.Candle),
			(typeof(IChartIndicatorElement),         DiagramSocketType.IndicatorValue),
			(typeof(IChartTradeElement),             DiagramSocketType.MyTrade),
			(typeof(IChartOrderElement),             DiagramSocketType.Order),
			(typeof(IChartIndicatorElement),         DiagramSocketType.Unit),
		];

		public override bool CanConnectFrom(DiagramSocket from)
			=> _typesList.Any(p => p.socketType == from.Type);

		public void Reset(TChartIndicatorElementWrapper wrapper) => Reset(wrapper!, null, false);

		public void Reset(IChartElement element = null) => Reset(null, element, false);

		public void Reset(DiagramSocket sourceSocket)
		{
			var elType = _typesList.FirstOrDefault(p => p.socketType == sourceSocket.Type).elementType
				?? throw new InvalidOperationException(LocalizedStrings.UnsupportedType.Put(sourceSocket.Type.Name));

			if (ChartElement != null && elType != null)
				throw new InvalidOperationException("unexpected state: chart element already exists");

			var builder = _parent._chartBuilder;

			if (elType.Is<IChartIndicatorElement>())
				Reset(new TChartIndicatorElementWrapper());
			else if (elType == typeof(IChartCandleElement))
				Reset(builder.CreateCandleElement());
			else if (elType == typeof(IChartTradeElement))
				Reset(builder.CreateTradeElement());
			else if (elType == typeof(IChartOrderElement))
				Reset(builder.CreateOrderElement());
			else
				throw new ArgumentOutOfRangeException(elType.To<string>());
		}

		public void TrySetName(string name)
		{
			var element = ChartElement;

			if (name == null || name == element.FullTitle)
				return;

			_parent.Dispatcher.Invoke(() => element.FullTitle = name);
		}

		public void TrySetPainter(Type painterType)
		{
			var element = (IChartIndicatorElement)ChartElement;

			if (_cachedPainterType == painterType)
				return;

			_parent.Dispatcher.Invoke(() =>
			{
				_cachedPainterType = painterType;

				if (painterType != element.IndicatorPainter?.GetType())
					element.IndicatorPainter = painterType?.CreateInstance<IChartIndicatorPainter>();
			});
		}

		private void Reset(TChartIndicatorElementWrapper wrapper, IChartElement element, bool force)
		{
			if (wrapper != null && !ReferenceEquals(wrapper.Element, element) && element != null)
				throw new ArgumentException(LocalizedStrings.InvalidValue, nameof(element));

			element ??= wrapper?.Element;

			if (ChartElement == element && !force)
				return;

			if (ChartElement != null)
			{
				ChartElement.PropertyChanging -= OnElementPropertyChanging;
				ChartElement.PropertyChanged -= OnElementPropertyChanged;
			}

			ChartElement = element;
			IndicatorWrapper = wrapper;

			if (ChartElement is IChartIndicatorElement indElement)
			{
				if (IndicatorWrapper == null)
				{
					IndicatorWrapper = _parent.CreateWrapper(indElement);
					TrySetName(indElement.FullTitle);
					TrySetPainter(indElement.IndicatorPainter?.GetType());
				}
			}

			AvailableTypes.Clear();

			if (ChartElement == null)
			{
				//Id = GenerateSocketId(Parent, null);
				Type = DiagramSocketType.Any;
				AvailableTypes.AddRange(_typesList.Select(t => t.socketType));
			}
			else
			{
				Id = GenerateSocketId(ChartElement.Id);
				Type = ChartElementToSocketType(ChartElement, true);

				AvailableTypes.Add(Type);

				if (Type == DiagramSocketType.IndicatorValue)
					AvailableTypes.Add(DiagramSocketType.Unit);

				ChartElement.PropertyChanging += OnElementPropertyChanging;
				ChartElement.PropertyChanged += OnElementPropertyChanged;
			}
		}

		protected override void DisposeManaged()
		{
			Reset();
			base.DisposeManaged();
		}

		private void OnElementPropertyChanging(object _, PropertyChangingEventArgs e) => (Parent as ChartDiagramElement<TChartIndicatorElementWrapper>)?.RaisePropertyChanging(e.PropertyName);
		private void OnElementPropertyChanged(object _, PropertyChangedEventArgs e) => (Parent as ChartDiagramElement<TChartIndicatorElementWrapper>)?.RaisePropertyChanged(e.PropertyName);
	}

	// https://stocksharp.myjetbrains.com/youtrack/issue/DESIGNER-166/Strannosti-s-grafikom
	private class IndicatorTimes
	{
        public DateTimeOffset Last { get; set; }
		public List<DateTimeOffset> Pending { get; } = [];
    }

	/// <summary>
	/// </summary>
	protected abstract TChartIndicatorElementWrapper CreateWrapper(IChartIndicatorElement element);

	/// <inheritdoc />
	protected override DiagramSocket CreateSocketInstance(DiagramSocketDirection dir, string socketId = null) => new ChartElementSocket(this, dir, socketId);

	private readonly IChartBuilder _chartBuilder;
	private readonly IChartArea _area;
	private readonly SortedDictionary<DateTimeOffset, Dictionary<IChartElement, object>> _chartValues = [];
	private readonly ObsCollection<IChartAxis> _xAxes;
	private readonly ObsCollection<IChartAxis> _yAxes;
	private readonly Dictionary<DiagramSocket, PassThroughIndicator> _unitIndicators = [];
	private bool _hasChart;

	private bool _needToAddArea;
	private bool _processCollectionChanged = true;
	private ChartElementSocket _emptySocket;

	private readonly Dictionary<SecurityId, Security> _foundSecIds = [];

	private readonly Dictionary<IChartIndicatorElement, IndicatorTimes> _indicatorTimes = [];

	/// <inheritdoc />
	public override Guid TypeId { get; } = "1926C40E-AAA3-4948-98E6-FBA4B38B580E".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Chart1";

	private readonly DiagramElementParam<bool> _showNonFormedIndicators;

	/// <summary>
	/// Show non formed indicators values.
	/// </summary>
	public bool ShowNonFormedIndicators
	{
		get => _showNonFormedIndicators.Value;
		set => _showNonFormedIndicators.Value = value;
	}

	private readonly DiagramElementParam<string> _chartGroupId;

	/// <summary>
	/// Chart group id is used to sync panels on mouse events.
	/// </summary>
	public string ChartGroupId
	{
		get => _chartGroupId.Value;
		set => _chartGroupId.Value = value;
	}

	private readonly ObsCollection<IChartCandleElement> _candleElements;
	private readonly ObsCollection<TChartIndicatorElementWrapper> _indicatorElements;
	private readonly ObsCollection<IChartOrderElement> _orderElements;
	private readonly ObsCollection<IChartTradeElement> _tradeElements;

	/// <summary>
	/// Candles.
	/// </summary>
	public ICollection<IChartCandleElement> CandleElements => _candleElements;

	/// <summary>
	/// Indicators.
	/// </summary>
	public ICollection<TChartIndicatorElementWrapper> IndicatorElements => _indicatorElements;

	/// <summary>
	/// Orders.
	/// </summary>
	public ICollection<IChartOrderElement> OrderElements => _orderElements;

	/// <summary>
	/// Trades.
	/// </summary>
	public ICollection<IChartTradeElement> TradeElements => _tradeElements;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChartDiagramElement{TChartIndicatorElementWrapper}"/>.
	/// </summary>
	protected ChartDiagramElement(IChartBuilder chartBuilder)
	{
		_chartBuilder = chartBuilder ?? throw new ArgumentNullException(nameof(chartBuilder));

		_area = _chartBuilder.CreateArea();
		_needToAddArea = true;

		_showNonFormedIndicators = AddParam(nameof(ShowNonFormedIndicators), false)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Chart, LocalizedStrings.NonFormed, LocalizedStrings.ShowNonFormedIndicators, 90);

		_chartGroupId = AddParam(nameof(ChartGroupId), string.Empty)
			.SetDisplay(LocalizedStrings.Chart, LocalizedStrings.GroupId, LocalizedStrings.ChartPaneGroupDescription, 91)
			.SetOnValueChangedHandler(newGroupId => _area.GroupId = newGroupId);

		AddParam("AreaName", Name)
			.SetDisplay(LocalizedStrings.Chart, LocalizedStrings.Panel, LocalizedStrings.PanelName, 10)
			.SetOnValueChangedHandler(name => _area.Title = name);

		_candleElements = AddElementsCollectionParam<IChartCandleElement>("Candles");
		_indicatorElements = AddElementsCollectionParam<TChartIndicatorElementWrapper>("Indicators");
		_orderElements = AddElementsCollectionParam<IChartOrderElement>("Orders");
		_tradeElements = AddElementsCollectionParam<IChartTradeElement>("Trades");

		EnsureEmptySocket();

		_xAxes = AddAxesCollectionParam(LocalizedStrings.XAxis, AddXAxis, RemoveXAxis);
		_yAxes = AddAxesCollectionParam(LocalizedStrings.YAxis, AddYAxis, RemoveYAxis);

		RemoveAxes();
		TryAddDefaultAxes();
	}

	private ObsCollection<IChartAxis> AddAxesCollectionParam(string name, Action<IChartAxis> addAxis, Action<IChartAxis> removeAxis)
	{
		var paramName = name + "CollectionParam";

		var axesCollection = new ObsCollection<IChartAxis>();
		axesCollection.CollectionChanging += () => RaisePropertyChanging(paramName);
		axesCollection.CollectionChanged += (_, a) =>
		{
			if (a.NewItems != null)
			{
				foreach (IChartAxis axis in a.NewItems)
				{
					axis.PropertyChanging += RaiseElementPropertyChanging;
					axis.PropertyChanged += RaiseElementPropertyChanged;

					addAxis(axis);
				}
			}

			if (a.OldItems != null)
			{
				foreach (IChartAxis axis in a.OldItems)
				{
					axis.PropertyChanging -= RaiseElementPropertyChanging;
					axis.PropertyChanged -= RaiseElementPropertyChanged;

					removeAxis(axis);
				}
			}

			RaisePropertyChanged(paramName);
		};

		var collectionParam = AddParam(paramName, axesCollection)
			.SetDisplay("Chart axes", name, string.Empty, 20)
			.SetSaveLoadHandlers(_ => [], _ => null);

		var editor = CreateEditor("AxesEditorTemplate");

		if (editor is not null)
			collectionParam.SetEditor(editor);

		collectionParam.IgnoreOnSave = true;

		return axesCollection;
	}

	/// <summary>
	/// </summary>
	protected abstract Attribute CreateEditor(string templateKey);

	/// <summary>
	/// The list of horizontal axes.
	/// </summary>
	public ICollection<IChartAxis> XAxes => _xAxes;

	/// <summary>
	/// The list of vertical axes.
	/// </summary>
	public ICollection<IChartAxis> YAxes => _yAxes;

	private IEnumerable<IChartElement> ChartElements => InputSockets.OfType<ChartElementSocket>().Where(s => s.ChartElement != null).Select(s => s.ChartElement);

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		base.OnStart(time);

		if (!_needToAddArea)
			return;

		_hasChart = false;

		GetChart(chart =>
		{
			_hasChart = true;

			foreach (var indicator in IndicatorElements)
				_indicatorTimes.Add(indicator.Element, new());

			Dispatcher.Invoke(() =>
			{
				chart.AddArea(_area);
				chart.ShowNonFormedIndicators = ShowNonFormedIndicators;
			});
		});

		_needToAddArea = false;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_chartValues.Clear();
		_indicatorTimes.Clear();
		_unitIndicators.Clear();
		_foundSecIds.Clear();

		GetChart(chart => Dispatcher.Invoke(() =>
		{
			chart.Reset(ChartElements);

			if (chart.Areas.Contains(_area))
				chart.RemoveArea(_area);
		}));

		_needToAddArea = true;
		_hasChart = false;
	}

	private void GetChart(Action<IChart> action)
	{
		var chart = Strategy?.GetChart();

		if (chart is null)
			return;

		action(chart);
	}

	/// <inheritdoc />
	public override void Flush(DateTimeOffset _)
	{
		GetChart(chart =>
		{
			var data = chart.CreateData();

			foreach (var (time, valuesAtTime) in _chartValues.CopyAndClear())
			{
				var group = data.Group(time);

				foreach (var (element, value) in valuesAtTime)
				{
					if (element is IChartCandleElement candleElement)
					{
						if (candleElement.PriceStep is null)
						{
							var security = _foundSecIds.SafeAdd(((ICandleMessage)value).SecurityId, Strategy.LookupById);
							candleElement.PriceStep = security?.PriceStep;
						}

						foreach (var (_, it) in _indicatorTimes)
						{
							if (it.Last < time && it.Pending.LastOrDefault() < time)
								it.Pending.Add(time);
						}
					}
					else if (element is IChartIndicatorElement indElement)
					{
						var it = _indicatorTimes[indElement];

						var emptyValue = ((IIndicatorValue)value).Indicator.CreateEmptyValue(time);

						while (it.Pending.Count > 0)
						{
							var pendingTime = it.Pending[0];

							if (pendingTime < time)
							{
								var g = data.Group(pendingTime);
								g.Add(indElement, emptyValue);
							}
							else if (pendingTime > time)
								break;

							it.Pending.RemoveAt(0);
						}

						it.Last = time;
					}

					group.Add(element, value);
				}
			}

			chart.Draw(data);

			ResetFlushPriority();
		});
	}

	private void AddXAxis(IChartAxis axis)
	{
		axis.AxisType = ChartAxisType.CategoryDateTime;

		if (_area.XAxises.FirstOrDefault(a => a.Id == axis.Id) == null)
			_area.XAxises.Add(axis);
	}

	private void AddYAxis(IChartAxis axis)
	{
		axis.AxisType = ChartAxisType.Numeric;

		if (_area.YAxises.FirstOrDefault(a => a.Id == axis.Id) == null)
			_area.YAxises.Add(axis);
	}

	private void RemoveXAxis(IChartAxis axis)
	{
		if (!axis.IsDefault())
			_area.XAxises.Remove(axis);
	}

	private void RemoveYAxis(IChartAxis axis)
	{
		if (!axis.IsDefault())
			_area.YAxises.Remove(axis);
	}

	private void RemoveAxes()
	{
		_xAxes.Clear();
		_yAxes.Clear();

		_area.XAxises.RemoveWhere(a => !a.IsDefault());
		_area.YAxises.RemoveWhere(a => !a.IsDefault());
	}

	private void TryAddDefaultAxes()
	{
		if (_xAxes.Count == 0)
		{
			var x = _chartBuilder.CreateAxis();

			x.Id = "X";
			x.Title = "X";
			x.AxisType = ChartAxisType.CategoryDateTime;
			x.TextFormatting = "G";
			x.SubDayTextFormatting = "T";

			_xAxes.Add(x);
		}

		if (_yAxes.Count == 0)
		{
			var y = _chartBuilder.CreateAxis();

			y.Id = "Y";
			y.Title = "Y";
			y.AxisType = ChartAxisType.Numeric;

			_yAxes.Add(y);
		}
	}

	private void OnProcess(DiagramSocketValue value)
	{
		if (!_hasChart || value.Socket.Type == DiagramSocketType.Any)
			return;

		var socket = (ChartElementSocket)value.Socket;

		void addChartValue(DateTimeOffset time, object v)
		{
			if (time == default)
				time = value.Time;

			_chartValues.SafeAdd(time, _ => [])[socket.ChartElement] = v;
			FlushPriority = int.MaxValue;
		}

		var inputValue = value.Value;

		socket.TrySetName(value.Source?.Socket.Parent.Name);

		void addDecimal(decimal d)
		{
			var indicator = _unitIndicators.SafeAdd(socket);

			var indInputValue = value.Value == null
				? new DecimalIndicatorValue(indicator, value.Time)
				: new DecimalIndicatorValue(indicator, d, value.Time);

			socket.TrySetPainter(null);
			addChartValue(value.Time, indicator.Process(indInputValue));
		}

		switch (inputValue)
		{
			case ICandleMessage c:
				addChartValue(c.OpenTime, c);
				break;
			case IIndicatorValue v:
				socket.TrySetPainter(IChartExtensions.TryIndicatorPainterProvider?.TryGetPainter(v.Indicator.GetType()));
				addChartValue(value.Time, v);
				break;
			case Order o:
				addChartValue(o.Time, o);
				break;
			case MyTrade t:
				addChartValue(t.Trade.ServerTime, t);
				break;
			case Unit u:
				addDecimal((decimal)u);
				break;
			case decimal d:
				addDecimal(d);
				break;
		}
	}

	private ObsCollection<T> AddElementsCollectionParam<T>(string name)
	{
		var paramName = name + "CollectionParam";

		void OnCollectionChanged(object _, NotifyCollectionChangedEventArgs a)
		{
			if (_processCollectionChanged)
			{
				if (a.NewItems != null)
					foreach (var obj in a.NewItems)
						AddElementImpl(obj);

				if (a.OldItems != null)
					foreach (var obj in a.OldItems)
						RemoveElementImpl(obj);
			}

			RaisePropertyChanged(paramName);
		}

		var elementsColl = new ObsCollection<T>();
		elementsColl.CollectionChanging += () => RaisePropertyChanging(paramName);
		elementsColl.CollectionChanged += OnCollectionChanged;

		var collectionParam = AddParam(paramName, elementsColl)
			.SetDisplay(LocalizedStrings.ChartElements, name, string.Empty, 20)
			.SetSaveLoadHandlers(_ => [], _ => null);

		collectionParam.IgnoreOnSave = true;

		return elementsColl;
	}

	private void OnInputSocketConnected(DiagramSocket s, DiagramSocket source)
	{
		if (IsUndoRedoing)
			return;

		var socket = (ChartElementSocket)s;

		if (socket.ChartElement != null)
			return; // can be when the model is loading

		if (socket != _emptySocket)
			throw new InvalidOperationException("unexpected socket connected");

		using var _ = SaveUndoState();

		try
		{
			socket.Reset(source);
			socket.IsDynamic = true;
			_processCollectionChanged = false;
			_emptySocket = null;
			AddElement(socket);
		}
		finally
		{
			_processCollectionChanged = true;
		}

		EnsureEmptySocket();
	}

	private void OnInputSocketDisconnected(DiagramSocket s, DiagramSocket source)
	{
		if (IsUndoRedoing)
			return;

		using var _ = SaveUndoState();

		try
		{
			_processCollectionChanged = false;
			RemoveElement((ChartElementSocket)s);
		}
		finally
		{
			_processCollectionChanged = true;
		}
	}

	private void AddElement(ChartElementSocket socket) => AddElementImpl(socket);
	private void AddElement(IChartElement element) => AddElementImpl(element);
	private void EnsureEmptySocket() => AddElementImpl(null);

	private int _socketNameIndex;

	/// <summary>
	/// the parameter can be ChartElementSocket, ChartIndicatorElementWrapper, IChartElement or null (for empty socket)
	/// </summary>
	private void AddElementImpl(object o)
	{
		var socket = o as ChartElementSocket;
		var wrapper = socket?.IndicatorWrapper ?? o as TChartIndicatorElementWrapper;
		var element = socket?.ChartElement ?? wrapper?.Element ?? o as IChartElement;
		var isEmptySocket = element == null;

		if (isEmptySocket && _emptySocket != null)
			return; // empty socket already exists

		if (element != null)
			socket ??= InputSockets.OfType<ChartElementSocket>().FirstOrDefault(s => s.ChartElement == element);

		if (socket == null)
		{
			var socketId = ChartElementSocket.GenerateSocketId(element?.Id);
			var sockType = ChartElementSocket.ChartElementToSocketType(element, false) ?? DiagramSocketType.Any;
			var fullName = $"{LocalizedStrings.Input} {++_socketNameIndex}";

			socket = (ChartElementSocket)AddInput(socketId, fullName, sockType, OnProcess, index: isEmptySocket ? int.MaxValue : 0, isDynamic: !isEmptySocket);

			if (wrapper == null)
				socket.Reset(element);
			else
				socket.Reset(wrapper);

			socket.Connected += OnInputSocketConnected;
			socket.Disconnected += OnInputSocketDisconnected;

			if (isEmptySocket)
				_emptySocket = socket;
		}

		switch (element)
		{
			case IChartCandleElement cel:
				if (!_candleElements.Contains(cel))
					_candleElements.Add(cel);
				break;
			case IChartIndicatorElement:
			{
				var indWrapper = socket.IndicatorWrapper;

				if (indWrapper is not null)
					_indicatorElements.TryAdd(indWrapper);

				break;
			}
			case IChartOrderElement oel:
				if (!_orderElements.Contains(oel))
					_orderElements.Add(oel);
				break;
			case IChartTradeElement tel:
				if (!_tradeElements.Contains(tel))
					_tradeElements.Add(tel);
				break;
		}

		if (element != null && !_area.Elements.Contains(element))
			_area.Elements.Add(element);
	}

	private void RemoveElement(ChartElementSocket socket) => RemoveElementImpl(socket);

	private void RemoveElementImpl(object o)
	{
		var socket = o as ChartElementSocket;
		var wrapper = socket?.IndicatorWrapper ?? o as TChartIndicatorElementWrapper;
		var element = socket?.ChartElement ?? wrapper?.Element ?? o as IChartElement;

		var socketId = ChartElementSocket.GenerateSocketId(element?.Id);

		socket ??= (ChartElementSocket)InputSockets.FindById(socketId);

		if (element != null)
		{
			if (_area.Elements.Contains(element))
				_area.Elements.Remove(element);

			switch (element)
			{
				case IChartCandleElement cel:
					_candleElements.Remove(cel);
					break;
				case IChartIndicatorElement _:
					_indicatorElements.Remove(socket?.IndicatorWrapper);
					break;
				case IChartOrderElement oel:
					_orderElements.Remove(oel);
					break;
				case IChartTradeElement tel:
					_tradeElements.Remove(tel);
					break;
			}
		}

		if (socket != null)
		{
			socket.Reset();
			socket.Connected -= OnInputSocketConnected;
			socket.Disconnected -= OnInputSocketDisconnected;
			RemoveSocket(socket);
		}
	}

	/// <inheritdoc/>
	protected override DiagramElement CreateCopy()
		=> Scope<StrategyContext>.Current?.Value.ExcludeUI == true
		? new DummyChartDiagramElement()
		: base.CreateCopy();

	private void RaiseElementPropertyChanging(object sender, PropertyChangingEventArgs e) => RaisePropertyChanging(this, e);
	private void RaiseElementPropertyChanged(object sender, PropertyChangedEventArgs e) => RaisePropertyChanged(this, e);

	private static void Load<T>(SettingsStorage storage, string name, Func<T> creator, Action<T> action)
		where T : IPersistable
		=> storage.GetValue<SettingsStorage[]>(name)?.Select(e =>
		{
			var obj = creator();
			obj.Load(e);
			return obj;
		}).ForEach(action);

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		var newElements = new Dictionary<Guid, IChartElement>();

		Load(storage, nameof(CandleElements), _chartBuilder.CreateCandleElement, e => newElements[e.Id] = e);
		Load(storage, nameof(IndicatorElements), _chartBuilder.CreateIndicatorElement, e => newElements[e.Id] = e);
		Load(storage, nameof(OrderElements), _chartBuilder.CreateOrderElement, e => newElements[e.Id] = e);
		Load(storage, nameof(TradeElements), _chartBuilder.CreateTradeElement, e => newElements[e.Id] = e);

		var newIds = newElements.Select(e => e.Value.Id).ToSet();

		_candleElements.RemoveWhere(e => !newIds.Contains(e.Id));
		_indicatorElements.RemoveWhere(e => !newIds.Contains(e.Element.Id));
		_orderElements.RemoveWhere(e => !newIds.Contains(e.Id));
		_tradeElements.RemoveWhere(e => !newIds.Contains(e.Id));

		RemoveAxes();

		base.Load(storage);

		Load(storage, nameof(XAxes), _chartBuilder.CreateAxis, _xAxes.Add);
		Load(storage, nameof(YAxes), _chartBuilder.CreateAxis, _yAxes.Add);

		TryAddDefaultAxes();

		foreach (var e in newElements.Values.ToArray())
		{
			var socket = InputSockets.OfType<ChartElementSocket>().FirstOrDefault(s => s.ChartElement?.Id == e.Id);
			if (socket != null)
			{
				socket.ChartElement.Load(e.Save());
				newElements.Remove(e.Id);
			}
		}

		newElements.Values.ForEach(AddElement);

		EnsureEmptySocket();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(CandleElements), _candleElements.Select(e => e.Save()).ToArray());
		storage.SetValue(nameof(IndicatorElements), _indicatorElements.Select(e => e.Element.Save()).ToArray());
		storage.SetValue(nameof(OrderElements), _orderElements.Select(e => e.Save()).ToArray());
		storage.SetValue(nameof(TradeElements), _tradeElements.Select(e => e.Save()).ToArray());
		storage.SetValue(nameof(XAxes), _xAxes.Select(a => a.Save()).ToArray());
		storage.SetValue(nameof(YAxes), _yAxes.Select(a => a.Save()).ToArray());
	}

	/// <inheritdoc/>
	public override void InitializeCopy(DiagramElement copiedFrom)
	{
		base.InitializeCopy(copiedFrom);
		InputSockets.Where(s => s.IsDynamic).ToArray().ForEach(s => RemoveElement(s as ChartElementSocket));
	}
}

/// <summary>
/// <see cref="ChartDiagramElement{TChartIndicatorElementWrapper}"/> dummy implementation.
/// </summary>
[Browsable(false)]
public class DummyChartDiagramElement : ChartDiagramElement<DummyChartDiagramElement.DummyChartIndicatorElementWrapper>
{
	/// <summary>
	/// <see cref="IChartIndicatorElementWrapper"/> dummy implementation.
	/// </summary>
	public class DummyChartIndicatorElementWrapper : IChartIndicatorElementWrapper
	{
		private readonly IChartIndicatorElement _element;

		/// <summary>
		/// Initializes a new instance of the <see cref="DummyChartIndicatorElementWrapper"/>.
		/// </summary>
		public DummyChartIndicatorElementWrapper()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DummyChartIndicatorElementWrapper"/>.
		/// </summary>
		/// <param name="element"><see cref="IChartIndicatorElement"/></param>
		public DummyChartIndicatorElementWrapper(IChartIndicatorElement element)
		{
			_element = element ?? throw new ArgumentNullException(nameof(element));
		}

		IChartIndicatorElement IChartIndicatorElementWrapper.Element => _element;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DummyChartDiagramElement"/>.
	/// </summary>
	public DummyChartDiagramElement()
		: base(new DummyChartBuilder())
	{
	}

	/// <inheritdoc/>
	protected override Attribute CreateEditor(string templateKey) => null;

	/// <inheritdoc/>
	protected override DummyChartIndicatorElementWrapper CreateWrapper(IChartIndicatorElement element) => new(element);

	private static readonly IDispatcher _dispatcher = ConfigManager.TryGetService<IDispatcher>() as DummyDispatcher ?? new DummyDispatcher();

	/// <inheritdoc/>
	protected override IDispatcher Dispatcher => _dispatcher;
}