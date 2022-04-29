namespace SampleChartActiveOrders
{
	using System;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Collections.ObjectModel;
	using System.IO;
	using System.Windows.Controls;
	using System.Windows.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;
	using StockSharp.Configuration;
	using StockSharp.Charting;

	public partial class MainWindow
	{
		private readonly ObservableCollection<Order> _orders = new();

		private IChartArea _area;
		private IChartCandleElement _candleElement;
		private IChartActiveOrdersElement _activeOrdersElement;
		private TimeFrameCandle _candle;

		private readonly DispatcherTimer _chartUpdateTimer = new();
		private readonly SynchronizedDictionary<DateTimeOffset, TimeFrameCandle> _updatedCandles = new();
		private readonly CachedSynchronizedList<TimeFrameCandle> _allCandles = new();

		private const decimal _priceStep = 0.01m;
		private const int _timeframe = 1;

		private bool NeedToDelay => DelayCtrl.IsChecked == true;
		private bool NeedToFail => FailCtrl.IsChecked == true;
		private bool NeedToConfirm => ConfirmCtrl.IsChecked == true;
		private bool UseSingleOrderObject => SameOrderCtrl.IsChecked == true;

		private readonly Security _security = new()
		{
			Id = "SBER@TQBR",
			PriceStep = _priceStep,
			Board = ExchangeBoard.Micex
		};

		private readonly PortfolioDataSource _portfolios = new();

		private readonly IdGenerator _idGenerator = new IncrementalIdGenerator();

		public MainWindow()
		{
			ConfigManager.RegisterService(_portfolios);

			InitializeComponent();

			OrdersList.ItemsSource = _orders;

			Loaded += OnLoaded;

			var pf = new Portfolio { Name = "Test portfolio" };
			_portfolios.Add(pf);

			Chart.OrderSettings.Security = _security;
			Chart.OrderSettings.Portfolio = pf;
			Chart.OrderSettings.Volume = 5;

			_chartUpdateTimer.Interval = TimeSpan.FromMilliseconds(100);
			_chartUpdateTimer.Tick += ChartUpdateTimerOnTick;
			_chartUpdateTimer.Start();
		}

		private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
		{
			InitCharts();
			LoadData(Paths.HistoryDataPath);
		}

		private void InitCharts()
		{
			Chart.FillIndicators();
			Chart.ClearAreas();
			Chart.OrderCreationMode = true;

			_area = Chart.CreateArea();

			var yAxis = _area.YAxises.First();

			yAxis.AutoRange = true;
			Chart.IsAutoRange = true;
			Chart.IsAutoScroll = true;

			Chart.AddArea(_area);

			var series = new CandleSeries(
				typeof(TimeFrameCandle),
				_security,
				TimeSpan.FromMinutes(_timeframe));

			_candleElement = Chart.CreateCandleElement();
			_candleElement.FullTitle = "Candles";
			Chart.AddElement(_area, _candleElement, series);

			_activeOrdersElement = Chart.CreateActiveOrdersElement();
			_activeOrdersElement.FullTitle = "Active orders";
			Chart.AddElement(_area, _activeOrdersElement);
		}

		private void LoadData(string path)
		{
			_candle = null;
			_allCandles.Clear();

			Chart.Reset(new IChartElement[] { _candleElement, _activeOrdersElement });

			var storage = new StorageRegistry();

			var maxDays = 2;

			//BusyIndicator.IsBusy = true;

			Chart.IsAutoRange = true;

			Task.Factory.StartNew(() =>
			{
				var date = DateTime.MinValue;

				foreach (var tick in storage.GetTickMessageStorage(_security.ToSecurityId(), new LocalMarketDataDrive(path)).Load())
				{
					AppendTick(_security, tick);

					if (date == tick.ServerTime.Date)
						continue;

					date = tick.ServerTime.Date;

					//var str = date.To<string>();
					//this.GuiAsync(() => BusyIndicator.BusyContent = str);

					maxDays--;

					if (maxDays == 0)
						break;
				}
			})
			.ContinueWith(t =>
			{
				if (t.Exception != null)
					Error(t.Exception.Message);

				this.GuiAsync(() =>
				{
					//BusyIndicator.IsBusy = false;
					Chart.IsAutoRange = false;

					Log($"Loaded {_allCandles.Count} candles");
				});

			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private void ChartUpdateTimerOnTick(object sender, EventArgs eventArgs)
		{
			TimeFrameCandle[] candlesToUpdate;

			lock (_updatedCandles.SyncRoot)
			{
				candlesToUpdate = _updatedCandles.OrderBy(p => p.Key).Select(p => p.Value).ToArray();
				_updatedCandles.Clear();
			}

			var lastCandle = _allCandles.LastOrDefault();
			_allCandles.AddRange(candlesToUpdate.Where(c => lastCandle == null || c.OpenTime != lastCandle.OpenTime));

			var data = Chart.CreateData();

			foreach (var candle in candlesToUpdate)
			{
				data.Group(candle.OpenTime).Add(_candleElement, candle);
			}

			Chart.Draw(data);
		}

		private void AppendTick(Security security, ExecutionMessage tick)
		{
			var time = tick.ServerTime;
			var price = tick.TradePrice.Value;

			if (_candle == null || time >= _candle.CloseTime)
			{
				if (_candle != null)
				{
					_candle.State = CandleStates.Finished;
					lock (_updatedCandles.SyncRoot)
						_updatedCandles[_candle.OpenTime] = _candle;
				}

				var tf = TimeSpan.FromMinutes(_timeframe);
				var bounds = tf.GetCandleBounds(time, _security.Board);
				_candle = new TimeFrameCandle
				{
					TimeFrame = tf,
					OpenTime = bounds.Min,
					CloseTime = bounds.Max,
					Security = security,
				};

				_candle.OpenPrice = _candle.HighPrice = _candle.LowPrice = _candle.ClosePrice = price;
			}

			if (price > _candle.HighPrice)
				_candle.HighPrice = price;

			if (price < _candle.LowPrice)
				_candle.LowPrice = price;

			_candle.ClosePrice = price;

			_candle.TotalVolume += tick.TradeVolume.Value;

			lock (_updatedCandles.SyncRoot)
				_updatedCandles[_candle.OpenTime] = _candle;
		}

		private void Error(string msg)
		{
			new MessageBoxBuilder()
				.Owner(this)
				.Error()
				.Text(msg)
				.Show();

			Log($"ERROR: {msg}");
		}

		private Order SelectedOrder => (Order)OrdersList.SelectedItem;

		private void OrdersList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			FillBtn.IsEnabled = MoveBtn.IsEnabled = CancelBtn.IsEnabled = SelectedOrder != null;
		}

		private void Fill_Click(object sender, RoutedEventArgs e)
		{
			var order = SelectedOrder;

			if (IsInFinalState(order))
			{
				Log($"Unable to fill order in state {order.State}");
				return;
			}

			Log($"Fill order: {order}");

			order.Balance -= RandomGen.GetInt(1, Math.Min((int)order.Balance, 5));

			if (order.Balance == 0)
			{
				order.State = OrderStates.Done;
				_orders.Remove(order);
			}

			Chart.Draw(Chart.CreateData().Add(_activeOrdersElement, order));
		}

		private void Load_Click(object sender, RoutedEventArgs e)
		{
			if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + $"/SettingsStorage{Paths.DefaultSettingsExt}"))
			{
				var settingsStorage = (AppDomain.CurrentDomain.BaseDirectory + $"/SettingsStorage{Paths.DefaultSettingsExt}")
					.Deserialize<SettingsStorage>();

				Chart.LoadIfNotNull(settingsStorage);

				_area = Chart.Areas.First();
				_candleElement = Chart.Elements.OfType<IChartCandleElement>().First();
				_activeOrdersElement = Chart.Elements.OfType<IChartActiveOrdersElement>().First();
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			var settingsStorage = new SettingsStorage();
			Chart.Save(settingsStorage);
			settingsStorage.Serialize(AppDomain.CurrentDomain.BaseDirectory + $"/SettingsStorage{Paths.DefaultSettingsExt}");
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Chart_OnCancelOrder(SelectedOrder);
		}

		private void Move_Click(object sender, RoutedEventArgs e)
		{
			Chart_OnMoveOrder(SelectedOrder, SelectedOrder.Price + RandomGen.GetInt(-3, 3) * _priceStep);
		}

		private void Chart_OnRegisterOrder(IChartArea area, Order orderDraft)
		{
			if (NeedToConfirm && !Confirm("Register order?"))
				return;

			var order = new OrderEx
			{
				TransactionId = _idGenerator.GetNextId(),
				Type = OrderTypes.Limit,
				State = OrderStates.Pending,
				Volume = orderDraft.Volume,
				Balance = orderDraft.Volume,
				Direction = orderDraft.Direction,
				Security = orderDraft.Security,
				Portfolio = orderDraft.Portfolio,
				Price = Math.Round(orderDraft.Price / _priceStep) * _priceStep,
			};

			Log($"RegisterOrder: {order}");

			Chart.Draw(Chart.CreateData().Add(_activeOrdersElement, order));

			void RegAction()
			{
				if (NeedToFail)
				{
					order.State = OrderStates.Failed;
					Chart.Draw(Chart.CreateData().Add(_activeOrdersElement, order));

					Log($"Order failed: {order}");
				}
				else
				{
					order.State = OrderStates.Active;
					Chart.Draw(Chart.CreateData().Add(_activeOrdersElement, order));

					Log($"Order registered: {order}");

					_orders.Add(order);
				}
			}

			if (NeedToDelay)
				DelayedAction(RegAction, "register");
			else
				RegAction();
		}

		private void Chart_OnMoveOrder(Order order, decimal newPrice)
		{
			if (NeedToConfirm && !Confirm($"Move order to price={newPrice}?"))
			{
				var d = new ChartDrawData();
				// redraw order with old price
				d.Add(_activeOrdersElement, order);
				Chart.Draw(d);

				return;
			}

			var singleObject = UseSingleOrderObject;
			Log($"MoveOrder to {newPrice}: {order}, single order object = {singleObject}");

			if (IsInFinalState(order))
			{
				Log("invalid state for re-register");
				return;
			}

			if(singleObject)
				Chart_OnMoveOrder1(order, newPrice);
			else
				Chart_OnMoveOrder2(order, newPrice);
		}

		private void Chart_OnMoveOrder1(Order order, decimal newPrice)
		{
			// freeze order on chart
			Chart.Draw(Chart.CreateData()
				.Add(_activeOrdersElement, order, true, price: newPrice, state: OrderStates.Pending));

			void MoveAction()
			{
				if (NeedToFail)
				{
					Log("Move failed");
					Chart.Draw(Chart.CreateData()
						.Add(_activeOrdersElement, null, isError: true, price: newPrice, balance: order.Balance) // draw error animation on newprice
						.Add(_activeOrdersElement, order, isError: true, price: order.Price)); // redraw order with old price
				}
				else
				{
					order.Price = newPrice;
					Log($"Order moved to new price: {order}");

					Chart.Draw(Chart.CreateData()
						.Add(_activeOrdersElement, order)); // redraw/unfreeze

					((OrderEx)order).Refresh();
				}
			}

			if (NeedToDelay)
				DelayedAction(MoveAction, "move");
			else
				MoveAction();
		}

		private void Chart_OnMoveOrder2(Order order, decimal newPrice)
		{
			var newOrder = new OrderEx
			{
				TransactionId = _idGenerator.GetNextId(),
				Type = OrderTypes.Limit,
				State = OrderStates.Pending,
				Price = newPrice,
				Volume = order.Balance,
				Direction = order.Direction,
				Balance = order.Balance,
				Security = order.Security,
				Portfolio = order.Portfolio,
			};

			Chart.Draw(Chart.CreateData().Add(_activeOrdersElement, order, true, state: OrderStates.Pending)); // freeze old order on chart
			Chart.Draw(Chart.CreateData().Add(_activeOrdersElement, newOrder, true)); // freeze new order on chart

			void MoveAction()
			{
				if (NeedToFail)
				{
					Log("Move failed");

					newOrder.State = OrderStates.Failed;

					Chart.Draw(Chart.CreateData().Add(_activeOrdersElement, order, isError: true)); // show error on old order
					Chart.Draw(Chart.CreateData().Add(_activeOrdersElement, newOrder, isError: true)); // show error on new order
				}
				else
				{
					order.State = OrderStates.Done;
					newOrder.State = OrderStates.Active;

					Log($"Order moved to new: {newOrder}");

					Chart.Draw(Chart.CreateData()
							.Add(_activeOrdersElement, order)
							.Add(_activeOrdersElement, newOrder));

					_orders.Remove(order);
					_orders.Add(newOrder);
				}
			}

			if (NeedToDelay)
				DelayedAction(MoveAction, "move");
			else
				MoveAction();
		}

		private void Chart_OnCancelOrder(Order order)
		{
			if (NeedToConfirm && !Confirm("Cancel order?"))
				return;

			Log($"CancelOrder: {order}");

			Chart.Draw(Chart.CreateData().Add(_activeOrdersElement, order, true));

			void CancelAction()
			{
				if (NeedToFail)
				{
					Log("Cancel failed");

					Chart.Draw(Chart.CreateData().Add(_activeOrdersElement, order, isError: true));
				}
				else
				{
					order.State = OrderStates.Done;
					Chart.Draw(new ChartDrawData().Add(_activeOrdersElement, order));
					_orders.Remove(order);
				}
			}

			if (NeedToDelay)
				DelayedAction(CancelAction, "cancel");
			else
				CancelAction();
		}

		private void Log(string msg)
		{
			LogBox.AppendText($"{DateTime.Now:HH:mm:ss.fff}: {msg}\n");
			LogBox.ScrollToEnd();
		}

		private static bool IsInFinalState(Order o)
		{
			return o.State == OrderStates.Done || o.State == OrderStates.Failed || o.Balance == 0;
		}

		private static bool Confirm(string question)
		{
			return MessageBox.Show(question, "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
		}

		private void DelayedAction(Action action, string actionName)
		{
			var delay = RandomGen.GetInt(1, 2);
			Log($"Action '{actionName}' is delayed for {delay:0.##}sec");
			Task.Delay(delay * 1000).ContinueWith(t => Dispatcher.GuiAsync(action));
		}

		class OrderEx : Order
		{
			public OrderEx()
			{
				PropertyChanged += (sender, args) =>
				{
					if (args.PropertyName != nameof(Description))
						NotifyPropertyChanged(nameof(Description));
				};
			}

			public void Refresh() => NotifyPropertyChanged(nameof(Description));

			public string Description => ToString();
		}
	}
}