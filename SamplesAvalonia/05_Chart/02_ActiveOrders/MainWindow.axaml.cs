namespace StockSharp.Samples.Chart.ActiveOrders.Avalonia;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Layout;
using global::Avalonia.Threading;

using Ecng.Common;
using Ecng.IO;
using Ecng.Serialization;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Xaml.Charting.Avalonia.Controls;
using StockSharp.Xaml.Charting.Interfaces;

public partial class MainWindow : Window
{
	private const decimal _priceStep = 0.0001m;
	private static readonly TimeSpan _timeFrame = TimeSpan.FromMinutes(1);

	private readonly ObservableCollection<OrderEx> _orders = [];
	private readonly HashSet<DateTime> _loadedCandleTimes = [];
	private readonly HashSet<Task> _transactionTasks = [];
	private readonly Lock _transactionSync = new();
	private readonly IFileSystem _fileSystem = Paths.FileSystem;
	private readonly IncrementalIdGenerator _idGenerator = new();
	private readonly CancellationTokenSource _transactionCancellation = new();
	private readonly Security _security = new()
	{
		Id = Paths.HistoryDefaultSecurity,
		PriceStep = _priceStep,
		Board = ExchangeBoard.Binance,
	};
	private readonly Portfolio _portfolio = new() { Name = "Test portfolio" };
	private CancellationTokenSource _loadCancellation = new();
	private Task _loadTask = Task.CompletedTask;
	private int _loadGeneration;
	private bool _isClosing;
	private bool _closeApproved;
	private IChartArea _area;
	private IChartCandleElement _candleElement;
	private IChartActiveOrdersElement _activeOrdersElement;

	private bool NeedToDelay => DelayTransactions.IsChecked == true;
	private bool NeedToFail => FailTransactions.IsChecked == true;
	private bool NeedToConfirm => ConfirmTransactions.IsChecked == true;
	private bool UseSingleOrderObject => SameOrderObject.IsChecked == true;
	private OrderEx SelectedOrder => OrdersList.SelectedItem as OrderEx;
	private string SettingsPath => Path.Combine(AppContext.BaseDirectory, $"SettingsStorage{Paths.DefaultSettingsExt}");

	public MainWindow()
	{
		InitializeComponent();
		OrdersList.ItemsSource = _orders;

		Chart.RegisterOrder += OnRegisterOrder;
		Chart.MoveOrder += OnMoveOrder;
		Chart.CancelOrder += OnCancelOrder;
		Opened += OnOpened;
		Closing += OnClosing;
	}

	private void OnOpened(object sender, EventArgs e)
	{
		Opened -= OnOpened;
		InitializeChart();
		StartHistoryLoad();
	}

	private void InitializeChart()
	{
		var chart = (IChart)Chart;
		foreach (var area in chart.Areas.ToArray())
			chart.RemoveArea(area);

		Chart.OrderCreationMode = true;
		Chart.IsAutoRange = true;
		Chart.IsAutoScroll = true;
		Chart.OrderSettings.Security = _security;
		Chart.OrderSettings.Portfolio = _portfolio;
		Chart.OrderSettings.Volume = 5m;

		_area = chart.CreateArea();
		_area.Title = "Candles and active orders";
		chart.AddArea(_area);
		Chart.ActiveArea = _area;

		var subscription = new Subscription(_timeFrame.TimeFrame(), _security);
		_candleElement = chart.CreateCandleElement();
		_candleElement.FullTitle = "Candles";
		chart.AddElement(_area, _candleElement, subscription);

		_activeOrdersElement = chart.CreateActiveOrdersElement();
		_activeOrdersElement.FullTitle = "Active orders";
		chart.AddElement(_area, _activeOrdersElement);
		Chart.IsQuickOrderVisible = true;
	}

	private void StartHistoryLoad()
	{
		var previousCancellation = _loadCancellation;
		var previousTask = _loadTask;
		var cancellation = new CancellationTokenSource();
		_loadCancellation = cancellation;
		previousCancellation.Cancel();
		var generation = ++_loadGeneration;
		_loadTask = LoadAfterPreviousAsync(previousTask, previousCancellation, generation, cancellation.Token);
	}

	private async Task LoadAfterPreviousAsync(
		Task previousTask,
		CancellationTokenSource previousCancellation,
		int generation,
		CancellationToken cancellationToken)
	{
		try
		{
			await previousTask.ConfigureAwait(false);
		}
		catch
		{
		}
		finally
		{
			previousCancellation.Dispose();
		}

		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				if (generation != _loadGeneration)
					return;
				_loadedCandleTimes.Clear();
				((IChart)Chart).Reset([_candleElement, _activeOrdersElement]);
				Chart.IsAutoRange = true;
				Log("Loading two days of ticks...");
			}, DispatcherPriority.Background, cancellationToken);

			using var registry = new StorageRegistry();
			using var drive = new LocalMarketDataDrive(_fileSystem, Paths.HistoryDataPath);
			var day = DateTime.MinValue;
			var daysLeft = 2;
			var current = (TimeFrameCandleMessage)null;
			var updates = new Dictionary<DateTime, TimeFrameCandleMessage>();
			var ticks = 0;

			await foreach (var tick in registry.GetTickMessageStorage(_security.ToSecurityId(), drive)
				.LoadAsync(null, null)
				.WithCancellation(cancellationToken)
				.ConfigureAwait(false))
			{
				if (tick.TradePrice is null)
					continue;

				if (day != tick.ServerTime.Date)
				{
					day = tick.ServerTime.Date;
					if (--daysLeft < 0)
						break;
				}

				current = UpdateCandle(current, tick, updates);
				if (++ticks % 512 != 0)
					continue;

				await ApplyCandleBatchAsync(updates.Values.ToArray(), generation, cancellationToken).ConfigureAwait(false);
				updates.Clear();
			}

			if (updates.Count > 0)
				await ApplyCandleBatchAsync(updates.Values.ToArray(), generation, cancellationToken).ConfigureAwait(false);

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				if (generation != _loadGeneration)
					return;
				Chart.IsAutoRange = false;
				foreach (var order in _orders)
					DrawActiveOrder(order);
				Log($"Loaded {_loadedCandleTimes.Count:N0} candles");
			}, DispatcherPriority.Background, cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			await TryLogAsync($"ERROR: {error.Message}").ConfigureAwait(false);
		}
	}

	private TimeFrameCandleMessage UpdateCandle(
		TimeFrameCandleMessage current,
		ExecutionMessage tick,
		IDictionary<DateTime, TimeFrameCandleMessage> updates)
	{
		var time = tick.ServerTime;
		var price = tick.TradePrice.Value;
		if (current is null || time >= current.CloseTime)
		{
			if (current is not null)
			{
				current.State = CandleStates.Finished;
				updates[current.OpenTime] = current.TypedClone();
			}

			var bounds = _timeFrame.GetCandleBounds(time, _security.Board);
			current = new TimeFrameCandleMessage
			{
				TypedArg = _timeFrame,
				OpenTime = bounds.Min,
				CloseTime = bounds.Max,
				SecurityId = tick.SecurityId,
				OpenPrice = price,
				HighPrice = price,
				LowPrice = price,
				ClosePrice = price,
				State = CandleStates.Active,
			};
		}

		current.HighPrice = Math.Max(current.HighPrice, price);
		current.LowPrice = Math.Min(current.LowPrice, price);
		current.ClosePrice = price;
		current.TotalVolume += tick.TradeVolume ?? 0m;
		updates[current.OpenTime] = current.TypedClone();
		return current;
	}

	private async Task ApplyCandleBatchAsync(
		IReadOnlyList<TimeFrameCandleMessage> candles,
		int generation,
		CancellationToken cancellationToken)
		=> await Dispatcher.UIThread.InvokeAsync(() =>
		{
			if (generation != _loadGeneration)
				return;

			var data = new ChartDrawDataImpl();
			foreach (var candle in candles.OrderBy(candle => candle.OpenTime))
			{
				_loadedCandleTimes.Add(candle.OpenTime);
				data.Group(candle.OpenTime).Add(_candleElement, candle);
			}
			Chart.Draw(data);
		}, DispatcherPriority.Background, cancellationToken);

	private void OnOrderSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var enabled = SelectedOrder is not null;
		FillButton.IsEnabled = enabled;
		MoveButton.IsEnabled = enabled;
		CancelButton.IsEnabled = enabled;
	}

	private void OnFillClick(object sender, RoutedEventArgs e)
	{
		var order = SelectedOrder;
		if (order is null)
			return;
		if (IsInFinalState(order))
		{
			Log($"Unable to fill order in state {order.State}");
			return;
		}

		var maximum = Math.Max(1, Math.Min(5, (int)order.Balance));
		var filled = Math.Min(order.Balance, RandomGen.GetInt(1, maximum + 1));
		order.Balance -= filled;
		Log($"Fill {filled} units: {order}");
		if (order.Balance == 0m)
		{
			order.State = OrderStates.Done;
			_orders.Remove(order);
		}
		order.Refresh();
		DrawActiveOrder(order);
	}

	private void OnRegisterOrder(IChartArea area, Order draft)
		=> TrackTransaction(RegisterOrderAsync(draft));

	private async Task RegisterOrderAsync(Order draft)
	{
		if (_isClosing || NeedToConfirm && !await ConfirmAsync("Register order?"))
			return;

		var order = new OrderEx
		{
			TransactionId = _idGenerator.GetNextId(),
			Type = OrderTypes.Limit,
			State = OrderStates.Pending,
			Volume = draft.Volume,
			Balance = draft.Volume,
			Side = draft.Side,
			Security = draft.Security ?? _security,
			Portfolio = draft.Portfolio ?? _portfolio,
			Price = (draft.Price / _priceStep).Round() * _priceStep,
		};
		Log($"RegisterOrder: {order}");
		DrawActiveOrder(order);
		await DelayIfRequestedAsync("register");
		if (_transactionCancellation.IsCancellationRequested)
			return;

		if (NeedToFail)
		{
			order.State = OrderStates.Failed;
			DrawActiveOrder(order);
			Log($"Order failed: {order}");
			return;
		}

		order.State = OrderStates.Active;
		order.Refresh();
		DrawActiveOrder(order);
		_orders.Add(order);
		Log($"Order registered: {order}");
	}

	private void OnMoveOrder(Order order, decimal newPrice)
		=> TrackTransaction(MoveOrderAsync((OrderEx)order, newPrice));

	private async Task MoveOrderAsync(OrderEx order, decimal newPrice)
	{
		if (_isClosing)
			return;
		if (NeedToConfirm && !await ConfirmAsync($"Move order to {newPrice}?"))
		{
			DrawActiveOrder(order);
			return;
		}
		if (IsInFinalState(order))
		{
			Log("Invalid state for re-register");
			return;
		}

		if (UseSingleOrderObject)
			await MoveSingleObjectAsync(order, newPrice);
		else
			await MoveReplacementAsync(order, newPrice);
	}

	private async Task MoveSingleObjectAsync(OrderEx order, decimal newPrice)
	{
		Log($"MoveOrder to {newPrice}: {order}, single order object = true");
		Chart.Draw(new ChartDrawDataImpl().Add(_activeOrdersElement, order, true, price: newPrice, state: OrderStates.Pending));
		await DelayIfRequestedAsync("move");
		if (_transactionCancellation.IsCancellationRequested)
			return;

		if (NeedToFail)
		{
			Log("Move failed");
			Chart.Draw(new ChartDrawDataImpl()
				.Add(_activeOrdersElement, null, isError: true, price: newPrice, balance: order.Balance)
				.Add(_activeOrdersElement, order, isError: true, price: order.Price));
			return;
		}

		order.Price = newPrice;
		order.Refresh();
		DrawActiveOrder(order);
		Log($"Order moved to new price: {order}");
	}

	private async Task MoveReplacementAsync(OrderEx order, decimal newPrice)
	{
		Log($"MoveOrder to {newPrice}: {order}, single order object = false");
		var replacement = new OrderEx
		{
			TransactionId = _idGenerator.GetNextId(),
			Type = OrderTypes.Limit,
			State = OrderStates.Pending,
			Price = newPrice,
			Volume = order.Balance,
			Balance = order.Balance,
			Side = order.Side,
			Security = order.Security,
			Portfolio = order.Portfolio,
		};
		Chart.Draw(new ChartDrawDataImpl()
			.Add(_activeOrdersElement, order, true, state: OrderStates.Pending)
			.Add(_activeOrdersElement, replacement, true));
		await DelayIfRequestedAsync("move");
		if (_transactionCancellation.IsCancellationRequested)
			return;

		if (NeedToFail)
		{
			replacement.State = OrderStates.Failed;
			Chart.Draw(new ChartDrawDataImpl()
				.Add(_activeOrdersElement, order, isError: true)
				.Add(_activeOrdersElement, replacement, isError: true));
			Log("Move failed");
			return;
		}

		order.State = OrderStates.Done;
		replacement.State = OrderStates.Active;
		Chart.Draw(new ChartDrawDataImpl()
			.Add(_activeOrdersElement, order)
			.Add(_activeOrdersElement, replacement));
		_orders.Remove(order);
		_orders.Add(replacement);
		Log($"Order moved to replacement: {replacement}");
	}

	private void OnCancelOrder(Order order)
		=> TrackTransaction(CancelOrderAsync((OrderEx)order));

	private async Task CancelOrderAsync(OrderEx order)
	{
		if (_isClosing || NeedToConfirm && !await ConfirmAsync("Cancel order?"))
			return;

		Log($"CancelOrder: {order}");
		Chart.Draw(new ChartDrawDataImpl().Add(_activeOrdersElement, order, true));
		await DelayIfRequestedAsync("cancel");
		if (_transactionCancellation.IsCancellationRequested)
			return;

		if (NeedToFail)
		{
			Chart.Draw(new ChartDrawDataImpl().Add(_activeOrdersElement, order, isError: true));
			Log("Cancel failed");
			return;
		}

		order.State = OrderStates.Done;
		DrawActiveOrder(order);
		_orders.Remove(order);
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		if (SelectedOrder is { } order)
			OnCancelOrder(order);
	}

	private void OnMoveClick(object sender, RoutedEventArgs e)
	{
		if (SelectedOrder is { } order)
			OnMoveOrder(order, order.Price + RandomGen.GetInt(-3, 4) * _priceStep);
	}

	private void DrawActiveOrder(Order order)
		=> Chart.Draw(new ChartDrawDataImpl().Add(_activeOrdersElement, order));

	private async Task DelayIfRequestedAsync(string actionName)
	{
		if (!NeedToDelay)
			return;
		var delay = RandomGen.GetInt(1, 3);
		Log($"Action '{actionName}' is delayed for {delay} sec");
		await Task.Delay(TimeSpan.FromSeconds(delay), _transactionCancellation.Token);
	}

	private void TrackTransaction(Task task)
	{
		using (_transactionSync.EnterScope())
			_transactionTasks.Add(task);
		_ = ObserveTransactionAsync(task);
	}

	private async Task ObserveTransactionAsync(Task task)
	{
		try
		{
			await task;
		}
		catch (OperationCanceledException) when (_transactionCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			Log($"ERROR: {error.Message}");
		}
		finally
		{
			using (_transactionSync.EnterScope())
				_transactionTasks.Remove(task);
		}
	}

	private async Task<bool> ConfirmAsync(string question)
	{
		var yes = new Button { Content = "Yes", Width = 80 };
		var no = new Button { Content = "No", Width = 80 };
		var dialog = new Window
		{
			Title = "Confirmation",
			Width = 360,
			Height = 150,
			CanResize = false,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			Content = new StackPanel
			{
				Margin = new global::Avalonia.Thickness(16),
				Spacing = 14,
				Children =
				{
					new TextBlock { Text = question, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
					new StackPanel
					{
						Orientation = Orientation.Horizontal,
						HorizontalAlignment = HorizontalAlignment.Center,
						Spacing = 8,
						Children = { yes, no },
					},
				},
			},
		};
		yes.Click += (_, _) => dialog.Close(true);
		no.Click += (_, _) => dialog.Close(false);
		return await dialog.ShowDialog<bool>(this);
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		try
		{
			var settings = new SettingsStorage();
			Chart.Save(settings);
			settings.Serialize(_fileSystem, SettingsPath);
			Log($"Chart settings saved to {SettingsPath}");
		}
		catch (Exception error)
		{
			Log($"ERROR: {error.Message}");
		}
	}

	private void OnLoadClick(object sender, RoutedEventArgs e)
	{
		try
		{
			if (!_fileSystem.FileExists(SettingsPath))
			{
				Log("No saved chart settings found");
				return;
			}

			var settings = SettingsPath.Deserialize<SettingsStorage>(_fileSystem);
			Chart.Load(settings);
			var chart = (IChart)Chart;
			_area = chart.Areas.First();
			_candleElement = chart.Areas.SelectMany(area => area.Elements).OfType<IChartCandleElement>().First();
			_activeOrdersElement = chart.Areas.SelectMany(area => area.Elements).OfType<IChartActiveOrdersElement>().First();
			Chart.ActiveArea = _area;
			StartHistoryLoad();
			Log($"Chart settings loaded from {SettingsPath}");
		}
		catch (Exception error)
		{
			Log($"ERROR: {error.Message}");
		}
	}

	private static bool IsInFinalState(Order order)
		=> order.State is OrderStates.Done or OrderStates.Failed || order.Balance == 0m;

	private void Log(string message)
	{
		var text = $"{DateTime.UtcNow:HH:mm:ss.fff}: {message}{Environment.NewLine}";
		LogBox.Text = (LogBox.Text ?? string.Empty) + text;
		if (LogBox.Text.Length > 60_000)
			LogBox.Text = LogBox.Text[^50_000..];
		LogBox.CaretIndex = LogBox.Text.Length;
	}

	private async Task TryLogAsync(string message)
	{
		try
		{
			await Dispatcher.UIThread.InvokeAsync(() => Log(message), DispatcherPriority.Background);
		}
		catch
		{
		}
	}

	private async void OnClosing(object sender, WindowClosingEventArgs e)
	{
		if (_closeApproved)
			return;
		e.Cancel = true;
		if (_isClosing)
			return;

		_isClosing = true;
		Chart.RegisterOrder -= OnRegisterOrder;
		Chart.MoveOrder -= OnMoveOrder;
		Chart.CancelOrder -= OnCancelOrder;
		_loadCancellation.Cancel();
		_transactionCancellation.Cancel();
		Task[] transactions;
		using (_transactionSync.EnterScope())
			transactions = _transactionTasks.ToArray();

		try
		{
			try
			{
				await _loadTask;
			}
			catch
			{
			}

			try
			{
				await Task.WhenAll(transactions);
			}
			catch
			{
			}
		}
		finally
		{
			Closing -= OnClosing;
			_loadCancellation.Dispose();
			_transactionCancellation.Dispose();
			_closeApproved = true;
			Close();
		}
	}

	private sealed class OrderEx : Order
	{
		public OrderEx()
		{
			PropertyChanged += (_, args) =>
			{
				if (args.PropertyName != nameof(Description))
					NotifyPropertyChanged(nameof(Description));
			};
		}

		public string Description => ToString();

		public void Refresh() => NotifyPropertyChanged(nameof(Description));
	}
}
