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

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml;
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
		private readonly Dictionary<Order, ChartActiveOrderInfo> _chartOrderInfos = new Dictionary<Order, ChartActiveOrderInfo>();

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

		private readonly PortfolioDataSource _portfolios = new PortfolioDataSource();

		public MainWindow()
		{
			ConfigManager.RegisterService(_portfolios);

			Orders = new ObservableCollection<Order>();
			InitializeComponent();
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
			LoadData(@"..\..\..\..\Testing\HistoryData\".ToFullPath());
		}

		private void InitCharts()
		{
			Chart.ClearAreas();
			Chart.OrderCreationMode = true;

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

			var data = new ChartDrawData();

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
			if (!(_ordersListBox.SelectedItem is Order order))
				return;

			if (IsInFinalState(order))
			{
				Log($"Unable to fill order in state {order.State}");
				return;
			}

			var oi = GetOrderInfo(order);

			Log($"Fill order: {order}");

			order.Balance -= 1;

			if (order.Balance == 0)
				order.State = OrderStates.Done;

			oi.UpdateOrderState(order);
		}

		private void Remove_Click(object sender, RoutedEventArgs e)
		{
			if (!(_ordersListBox.SelectedItem is Order order))
				return;

			Log($"Remove order: {order}");
			RemoveOrder(order);
		}

		private long _transId;

		private void Chart_OnRegisterOrder(ChartArea area, Order orderDraft)
		{
			if (NeedToConfirm && !Confirm("Register order?"))
				return;

			var order = new OrderEx
			{
				TransactionId = ++_transId,
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
			var oi = GetOrderInfo(order);

			oi.IsFrozen = true;

			void RegAction()
			{
				if (NeedToFail)
				{
					order.State = OrderStates.Failed;
					oi.UpdateOrderState(order, true);
					Log($"Order failed: {order}");
				}
				else
				{
					order.State = OrderStates.Active;
					oi.UpdateOrderState(order);
					Log($"Order registered: {order}");
				}
			}

			if (NeedToDelay)
				DelayedAction(RegAction, _delay, "register");
			else
				RegAction();
		}

		private void Chart_OnMoveOrder(Order oldOrder, decimal newPrice)
		{
			var oiOld = GetOrderInfo(oldOrder);

			if (NeedToConfirm && !Confirm($"Move order to price={newPrice}?"))
			{
				oiOld.UpdateOrderState(oldOrder);
				return;
			}

			Log($"MoveOrder to {newPrice}: {oldOrder}");
			if (IsInFinalState(oldOrder))
			{
				Log("invalid state for re-register");
				return;
			}

			var newOrder = new OrderEx
			{
				TransactionId = ++_transId,
				Type = OrderTypes.Limit,
				State = OrderStates.Pending,
				Price = newPrice,
				Volume = oldOrder.Balance,
				Direction = oldOrder.Direction,
				Balance = oldOrder.Balance,
				Security = oldOrder.Security,
				Portfolio = oldOrder.Portfolio,
			};

			var oiNew = GetOrderInfo(newOrder, oiOld);

			oiOld.UpdateOrderState(oldOrder);

			oiOld.IsFrozen = oiNew.IsFrozen = true;

			void MoveAction()
			{
				if (NeedToFail)
				{
					Log("Move failed");
					oiOld.UpdateOrderState(oldOrder, true);

					newOrder.State = OrderStates.Failed;
					oiNew.UpdateOrderState(newOrder, true);
				}
				else
				{
					oldOrder.State = OrderStates.Done;
					oiOld.UpdateOrderState(oldOrder);

					newOrder.State = OrderStates.Active;
					oiNew.UpdateOrderState(newOrder);
					Log($"Order moved to new: {newOrder}");
				}
			}

			if(NeedToDelay)
				DelayedAction(MoveAction, _delay, "move");
			else
				MoveAction();
		}

		private void Chart_OnCancelOrder(Order order)
		{
			if (NeedToConfirm && !Confirm("Cancel order?"))
				return;

			Log($"CancelOrder: {order}");

			var oi = GetOrderInfo(order);

			oi.IsFrozen = true;

			void CancelAction()
			{
				if (NeedToFail)
				{
					Log("Cancel failed");
					oi.UpdateOrderState(order, true);
				}
				else
				{
					order.State = OrderStates.Done;
					oi.UpdateOrderState(order);
				}
			}

			if (NeedToDelay)
				DelayedAction(CancelAction, _delay, "cancel");
			else
				CancelAction();
		}

		private ChartActiveOrderInfo GetOrderInfo(Order order, ChartActiveOrderInfo initFrom = null)
		{
			var oi = _chartOrderInfos.SafeAdd(order, o =>
			{
				var info = new ChartActiveOrderInfo();

				if (initFrom != null)
				{
					info.AutoRemoveFromChart = initFrom.AutoRemoveFromChart;
					info.ChartX = initFrom.ChartX;
				}

				return info;
			}, out var isNew);

			if (isNew)
			{
				oi.UpdateOrderState(order);
				_activeOrdersElement.Orders.Add(oi);
				Orders.Add(order);
			}

			return oi;
		}

		private bool RemoveOrder(Order o)
		{
			if (!_chartOrderInfos.TryGetValue(o, out var oi))
				return false;

			_activeOrdersElement.Orders.Remove(oi);
			_chartOrderInfos.Remove(o);
			Orders.Remove(o);

			return true;
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

		private void DelayedAction(Action action, TimeSpan delay, string actionName)
		{
			Log($"Action '{actionName}' is delayed for {delay.TotalSeconds:0.##}sec");
			Task.Delay(delay).ContinueWith(t => Dispatcher.GuiAsync(action));
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

			public string Description => ToString();
		}
	}
}