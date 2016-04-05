namespace SampleChartActiveOrders
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Collections.ObjectModel;
	using System.Windows.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.Configuration;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;

	public partial class MainWindow
	{
		public ObservableCollection<Order> Orders { get; }

		private ChartArea _area;
		private ChartCandleElement _candleElement;
		private ChartActiveOrdersElement _activeOrdersElement;
		private TimeFrameCandle _candle;

		private readonly DispatcherTimer _chartUpdateTimer = new DispatcherTimer();
		private readonly SynchronizedDictionary<DateTimeOffset, TimeFrameCandle> _updatedCandles = new SynchronizedDictionary<DateTimeOffset, TimeFrameCandle>();
		private readonly CachedSynchronizedList<TimeFrameCandle> _allCandles = new CachedSynchronizedList<TimeFrameCandle>();
		private readonly CachedSynchronizedSet<Order> _chartOrders = new CachedSynchronizedSet<Order>();

		private const decimal _priceStep = 10m;
		private const int _timeframe = 1;

		private bool NeedToDelay => _chkDelay.IsChecked == true;
		private bool NeedToFail => _chkFail.IsChecked == true;
		private bool NeedToConfirm => _chkConfirm.IsChecked == true;

		private static readonly TimeSpan _delay = TimeSpan.FromSeconds(2);

		private readonly Security _security = new Security
		{
			Id = "RIZ2@FORTS",
			PriceStep = _priceStep,
			Board = ExchangeBoard.Forts
		};

		private readonly ThreadSafeObservableCollection<Portfolio> _portfolios = new ThreadSafeObservableCollection<Portfolio>(new ObservableCollectionEx<Portfolio>
		{
			new Portfolio
			{
				Name = "Test portfolio"
			}
		});

		public MainWindow()
		{
			ConfigManager.RegisterService(_portfolios);

			Orders = new ObservableCollection<Order>();
			InitializeComponent();
			Loaded += OnLoaded;

			Chart.OrderSettings.Security = _security;
			Chart.OrderSettings.Portfolio = _portfolios.First();
			Chart.OrderSettings.Volume = 5;

			_chartUpdateTimer.Interval = TimeSpan.FromMilliseconds(100);
			_chartUpdateTimer.Tick += ChartUpdateTimerOnTick;
			_chartUpdateTimer.Start();
		}

		private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
		{
			InitCharts();
			LoadData(@"..\..\..\..\Testing\HistoryData\".ToFullPath());
		}

		private void InitCharts()
		{
			Chart.ClearAreas();

			_area = new ChartArea();

			var yAxis = _area.YAxises.First();

			yAxis.AutoRange = true;
			Chart.IsAutoRange = true;
			Chart.IsAutoScroll = true;

			Chart.AddArea(_area);

			var series = new CandleSeries(
				typeof(TimeFrameCandle),
				_security,
				TimeSpan.FromMinutes(_timeframe));

			_candleElement = new ChartCandleElement
			{
				FullTitle = "Candles"
			};
			Chart.AddElement(_area, _candleElement, series);

			_activeOrdersElement = new ChartActiveOrdersElement
			{
				FullTitle = "Active orders"
			};
			Chart.AddElement(_area, _activeOrdersElement);
		}

		private void LoadData(string path)
		{
			_candle = null;
			_allCandles.Clear();

			Chart.Reset(new IChartElement[] { _candleElement, _activeOrdersElement });

			var storage = new StorageRegistry();

			var maxDays = 2;

			BusyIndicator.IsBusy = true;

			Task.Factory.StartNew(() =>
			{
				var date = DateTime.MinValue;

				foreach (var tick in storage.GetTickMessageStorage(_security, new LocalMarketDataDrive(path)).Load())
				{
					AppendTick(_security, tick);

					if (date != tick.ServerTime.Date)
					{
						date = tick.ServerTime.Date;

						this.GuiAsync(() =>
						{
							BusyIndicator.BusyContent = date.ToString();
						});

						maxDays--;

						if (maxDays == 0)
							break;
					}
				}
			})
			.ContinueWith(t =>
			{
				if (t.Exception != null)
					Error(t.Exception.Message);

				this.GuiAsync(() =>
				{
					BusyIndicator.IsBusy = false;
					Chart.IsAutoRange = false;
					_area.YAxises.First().AutoRange = false;

					Chart.Draw(DateTimeOffset.MinValue, _activeOrdersElement, _chartOrders);

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

			candlesToUpdate.ForEach(c =>
			{
				Chart.Draw(c.OpenTime, new Dictionary<IChartElement, object>
				{
					{ _candleElement, c },
				});
			});
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

			if (time < _candle.OpenTime)
				throw new InvalidOperationException("invalid time");

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

		private void Fill_Click(object sender, RoutedEventArgs e)
		{
			var order = _ordersListBox.SelectedItem as Order;
			if (order == null)
				return;

			Log($"Fill order: {order}");

			if (order.Balance == 0)
				RemoveOrder(order);

			order.Balance -= 1;

			if (order.Balance == 0)
				order.State = OrderStates.Done;
		}

		private void Remove_Click(object sender, RoutedEventArgs e)
		{
			var order = _ordersListBox.SelectedItem as Order;
			if (order == null)
				return;

			Log($"Remove order: {order}");
			RemoveOrder(order);
		}

		private long _transId;

		private void Chart_OnRegisterOrder(Order order)
		{
			if (NeedToConfirm && !Confirm("Register order?"))
				return;

			order.Price = Math.Round(order.Price / _priceStep) * _priceStep;
			order.TransactionId = ++_transId;
			order.Balance = order.Volume;
			order.State = OrderStates.Pending;
			InitExtensionInfo(order);

			Log($"RegisterOrder: {order}");
			AddOrder(order);

			Action regAction = () =>
			{
				if (NeedToFail)
				{
					order.State = OrderStates.Failed;
					Log($"Order failed: {order}");
					RemoveOrder(order);
				}
				else
				{
					order.State = OrderStates.Active;
					Log($"Order registered: {order}");
				}
			};

			if (NeedToDelay)
				DelayedAction(regAction, _delay, "register");
			else
				regAction();
		}

		private void Chart_OnMoveOrder(Order order, decimal newPrice)
		{
			if (!RemoveOrder(order))
			{
				Log($"error: order not found: {order}");
				return;
			}

			if (NeedToConfirm && !Confirm($"Move order to price={newPrice}?"))
			{
				AddOrder(order);
				return;
			}

			Log($"MoveOrder to {newPrice}: {order}");
			if (IsInFinalState(order))
			{
				Log("invalid state for re-register");
				return;
			}

			var newOrder = new Order
			{
				TransactionId = ++_transId,
				Type = OrderTypes.Limit,
				State = OrderStates.Pending,
				Price = newPrice,
				Volume = order.Balance,
				Direction = order.Direction,
				Balance = order.Balance,
				Security = order.Security,
				Portfolio = order.Portfolio,
			};
			InitExtensionInfo(newOrder);
			newOrder.ExtensionInfo[ChartActiveOrdersElement.OldOrderKey] = order;

			AddOrder(newOrder);

			Action moveAction = () =>
			{
				if (NeedToFail)
				{
					Log("Move failed");
					RemoveOrder(newOrder);
					AddOrder(order);
				}
				else
				{
					newOrder.State = OrderStates.Active;
					Log($"Order moved to new: {newOrder}");
				}
			};

			if(NeedToDelay)
				DelayedAction(moveAction, _delay, "move");
			else
				moveAction();
		}

		private void Chart_OnCancelOrder(Order order)
		{
			if (NeedToConfirm && !Confirm("Cancel order?"))
				return;

			Log($"CancelOrder: {order}");

			Action cancelAction = () =>
			{
				if (NeedToFail)
					Log("Cancel failed");
				else
					RemoveOrder(order);
			};

			if(NeedToDelay)
				DelayedAction(cancelAction, _delay, "cancel");
			else
				cancelAction();
		}

		private bool AddOrder(Order o)
		{
			if (_chartOrders.Contains(o))
				return false;

			_chartOrders.Add(o);
			Orders.Add(o);

			return true;
		}

		private bool RemoveOrder(Order o)
		{
			var res = _chartOrders.Remove(o);
			Orders.Remove(o);

			return res;
		}

		private void Log(string msg)
		{
			_logBox.AppendText($"{DateTime.Now:HH:mm:ss.fff}: {msg}\n");
			_logBox.ScrollToEnd();
		}

		private static bool IsInFinalState(Order o)
		{
			return o.State == OrderStates.Done || o.State == OrderStates.Failed || o.Balance == 0;
		}

		private static bool Confirm(string question)
		{
			return MessageBox.Show(question, "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
		}

		private static void InitExtensionInfo(Order order)
		{
			if (order.ExtensionInfo == null)
				order.ExtensionInfo = new SynchronizedDictionary<object, object>();
		}

		private void DelayedAction(Action action, TimeSpan delay, string actionName)
		{
			Log($"Action '{actionName}' is delayed for {delay.TotalSeconds:0.##}sec");
			Task.Delay(delay).ContinueWith(t => Dispatcher.GuiAsync(action));
		}
	}
}