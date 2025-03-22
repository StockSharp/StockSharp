namespace StockSharp.Samples.Advanced.MultiConnect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Xaml;
using Ecng.Serialization;
using Ecng.ComponentModel;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Xaml;
using StockSharp.Localization;
using StockSharp.Messages;

using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

public partial class SecuritiesWindow
{
	private class DatesSettings : NotifiableObject, IPersistable
	{
		private DateTimeOffset? _from;

		public DateTimeOffset? From
		{
			get => _from;
			set
			{
				_from = value;
				NotifyChanged(nameof(From));
			}
		}

		private DateTimeOffset? _to;

		public DateTimeOffset? To
		{
			get => _to;
			set
			{
				_to = value;
				NotifyChanged(nameof(To));
			}
		}

		private long? _skip;

		public long? Skip
		{
			get => _skip;
			set
			{
				_skip = value;
				NotifyChanged(nameof(Skip));
			}
		}

		private long? _count;

		public long? Count
		{
			get => _count;
			set
			{
				_count = value;
				NotifyChanged(nameof(Count));
			}
		}

		public MarketDataBuildModes BuildMode { get; set; } = MarketDataBuildModes.LoadAndBuild;

            public bool IsFinishedOnly { get; set; }

            void IPersistable.Load(SettingsStorage storage)
		{
			throw new NotSupportedException();
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			throw new NotSupportedException();
		}
	}

	private class Level1DatesSettings : DatesSettings
	{
		private Level1Fields? _field;

		public Level1Fields? Field
		{
			get => _field;
			set
			{
				_field = value;
				NotifyChanged(nameof(Field));
			}
		}
	}

	private class DepthSettings : IPersistable
	{
		public DateTimeOffset? From { get; set; }

		public DateTimeOffset? To { get; set; }

		public int? MaxDepth { get; set; }

		public DataType BuildFrom { get; set; } = DataType.OrderLog;

		public MarketDataBuildModes BuildMode { get; set; } = MarketDataBuildModes.Build;

		void IPersistable.Load(SettingsStorage storage)
		{
			throw new NotSupportedException();
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			throw new NotSupportedException();
		}
	}

	private readonly SynchronizedDictionary<SecurityId, CachedSynchronizedList<QuotesWindow>> _quotesWindows = new();
	private readonly SynchronizedDictionary<Subscription, QuotesWindow> _quotesWindowsBySubscription = new();
	private readonly SynchronizedList<ChartWindow> _chartWindows = new();
	private bool _initialized;
	private bool _appClosing;

	public SecuritiesWindow()
	{
		InitializeComponent();
	}

	private static Connector Connector => MainWindow.Instance.MainPanel.Connector;

	private void SecuritiesWindow_OnLoaded(object sender, RoutedEventArgs e)
	{
		UpdateTimeFrames(new[]
		{
			TimeSpan.FromMinutes(1),
			TimeSpan.FromMinutes(5),
			TimeSpan.FromMinutes(15),
			TimeSpan.FromMinutes(30),
			TimeSpan.FromHours(1),
			TimeSpan.FromDays(1),
		}.Concat(Connector.Adapter.GetTimeFrames()).OrderBy().Distinct());
	}

	public void UpdateTimeFrames(IEnumerable<TimeSpan> timeFrames)
	{
		if (timeFrames == null)
			throw new ArgumentNullException(nameof(timeFrames));

		timeFrames = timeFrames.ToArray();

		if (!timeFrames.Any())
			return;

		CandlesPeriods.ItemsSource = timeFrames;
		CandlesPeriods.SelectedIndex = 0;
	}

	protected override void OnClosed(EventArgs e)
	{
		_appClosing = true;
		_quotesWindows.SyncDo(d => d.Values.ForEach(w => w.Cache.ForEach(w1 => w1.Close())));
		_quotesWindowsBySubscription.SyncDo(d => d.Values.ForEach(w => w.Close()));

		_chartWindows.SyncDo(c => c.ToArray().ForEach(w =>
		{
			w.SeriesInactive = true;
			w.Close();
		}));

		var connector = Connector;

		if (connector != null)
		{
			if (_initialized)
				connector.OrderBookReceived -= TraderOnMarketDepthReceived;
		}

		base.OnClosed(e);
	}

	public void ProcessOrder(Order order)
	{
		lock (_quotesWindows.SyncRoot)
		{
			foreach (var pair in _quotesWindows)
			{
				if (pair.Key != order.Security.ToSecurityId())
					continue;

				pair.Value.Cache.ForEach(wnd => wnd.ProcessOrder(order));
			}
		}
	}

	public void ProcessOrderFail(OrderFail fail)
	{
		lock (_quotesWindows.SyncRoot)
		{
			foreach (var pair in _quotesWindows)
			{
				if (pair.Key != fail.Order.Security.ToSecurityId())
					continue;

				pair.Value.Cache.ForEach(wnd => wnd.ProcessOrderFail(fail));
			}
		}
	}

	private void NewOrderClick(object sender, RoutedEventArgs e)
	{
		var connector = Connector;

		var newOrder = new OrderWindow
		{
			Order = new Order
			{
				Security = SecurityPicker.SelectedSecurity,
				Portfolio = connector.Portfolios.FirstOrDefault(),
			},
		}.Init(connector);

		if (newOrder.ShowModal(this))
			connector.RegisterOrder(newOrder.Order);
	}

	private void SecurityPicker_OnSecuritySelected(Security security)
	{
		Level1.IsEnabled = Level1Hist.IsEnabled = Ticks.IsEnabled = TicksHist.IsEnabled =
			OrderLog.IsEnabled = NewOrder.IsEnabled = Depth.IsEnabled =
			DepthAdvanced.IsEnabled = DepthFiltered.IsEnabled = security != null;

		TryEnableCandles();
	}

	private void DepthAdvancedClick(object sender, RoutedEventArgs e)
	{
		var settings = new DepthSettings();

		var settingsWnd = new SettingsWindow
		{
			Settings = settings,
		};

		if (!settingsWnd.ShowModal(this))
			return;

		SubscribeDepths(settings);
	}

	private void DepthClick(object sender, RoutedEventArgs e)
	{
		SubscribeDepths(null);
	}

	private void TraderOnMarketDepthReceived(Subscription subscription, IOrderBookMessage depth)
	{
		if (subscription.DataType == DataType.FilteredMarketDepth)
		{
			if (_quotesWindowsBySubscription.TryGetValue(subscription, out var wnd))
				wnd.DepthCtrl.UpdateDepth(depth);
		}
		else
		{
			if (_quotesWindows.TryGetValue(depth.SecurityId, out var list))
				list.Cache.ForEach(wnd => wnd.DepthCtrl.UpdateDepth(depth));
		}
	}

	private void DepthFilteredClick(object sender, RoutedEventArgs e)
	{
		var connector = Connector;

		if (!_initialized)
		{
			connector.OrderBookReceived += TraderOnMarketDepthReceived;
			_initialized = true;
		}

		foreach (var security in SecurityPicker.SelectedSecurities)
		{
			// create order book window
			var window = new QuotesWindow
			{
				Title = security.Id + " " + LocalizedStrings.MarketDepth,
				Security = security,
			};

			//window.DepthCtrl.UpdateDepth(connector.GetMarketDepth(security));
			window.Show();

			// subscribe on order book flow
			var subscription = new Subscription(DataType.FilteredMarketDepth, security);

			_quotesWindowsBySubscription.Add(subscription, window);

			window.Closed += (s, e) =>
			{
				if (_appClosing)
					return;

				if (subscription.State.IsActive())
					connector.UnSubscribe(subscription);
			};

			connector.Subscribe(subscription);
		}
	}

	private void SubscribeDepths(DepthSettings settings)
	{
		var connector = Connector;

		if (!_initialized)
		{
			connector.OrderBookReceived += TraderOnMarketDepthReceived;
			_initialized = true;
		}

		foreach (var security in SecurityPicker.SelectedSecurities)
		{
			// create order book window
			var window = new QuotesWindow
			{
				Title = security.Id + " " + LocalizedStrings.MarketDepth,
				Security = security,
				MaxDepth = settings?.MaxDepth
			};

			window.Show();
			
			// subscribe on order book flow
			var subscription = new Subscription(DataType.MarketDepth, security)
			{
				From = settings?.From,
				To = settings?.To,
				MarketData =
				{
					BuildMode = settings?.BuildMode ?? MarketDataBuildModes.LoadAndBuild,
					MaxDepth = settings?.MaxDepth,
					BuildFrom = settings?.BuildFrom,
				}
			};
			connector.Subscribe(subscription);

			_quotesWindows.SafeAdd(security.ToSecurityId()).Add(window);

			window.Closed += (s, e) =>
			{
				if (_appClosing)
					return;

				if (subscription.State.IsActive())
					connector.UnSubscribe(subscription);
			};
		}
	}

	private static Subscription FindSubscription(Security security, DataType dataType)
	{
		return Connector.FindSubscriptions(security, dataType).Where(s => s.To == null && s.State.IsActive()).FirstOrDefault();
	}

	private void Level1Click(object sender, RoutedEventArgs e)
	{
		var connector = Connector;

		foreach (var security in SecurityPicker.SelectedSecurities)
		{
			var subscription = FindSubscription(security, DataType.Level1);

			if (subscription != null)
				connector.UnSubscribe(subscription);
			else
				connector.Subscribe(new(DataType.Level1, security));
		}
	}

	private void Level1HistClick(object sender, RoutedEventArgs e)
	{
		var connector = Connector;

		var settings = new Level1DatesSettings { From = DateTime.Today.AddDays(-1) };

		var wnd = new SettingsWindow { Settings = settings };

		if (!wnd.ShowModal(this))
			return;

		foreach (var security in SecurityPicker.SelectedSecurities)
		{
			connector.Subscribe(new(new MarketDataMessage
			{
				DataType2 = DataType.Level1,
				From = settings.From,
				To = settings.To,
				Skip = settings.Skip,
				Count = settings.Count,
				BuildMode = settings.BuildMode,
				IsFinishedOnly = settings.IsFinishedOnly,
				Fields = settings.Field is null ? null : new[] { settings.Field.Value },
			}, security));
		}
	}

	private void TicksClick(object sender, RoutedEventArgs e)
	{
		var connector = Connector;

		foreach (var security in SecurityPicker.SelectedSecurities)
		{
			var subscription = FindSubscription(security, DataType.Ticks);

			if (subscription != null)
				connector.UnSubscribe(subscription);
			else
				connector.Subscribe(new(DataType.Ticks, security));
		}
	}

	private void TicksHistClick(object sender, RoutedEventArgs e)
	{
		var connector = Connector;

		var settings = new DatesSettings { From = DateTime.Today.AddDays(-1) };

		var wnd = new SettingsWindow { Settings = settings };

		if (!wnd.ShowModal(this))
			return;

		foreach (var security in SecurityPicker.SelectedSecurities)
		{
			connector.Subscribe(new(DataType.Ticks, security)
			{
				From = settings.From,
				To = settings.To,
				Skip = settings.Skip,
				Count = settings.Count,
			});
		}
	}

	private void OrderLogClick(object sender, RoutedEventArgs e)
	{
		var connector = Connector;

		foreach (var security in SecurityPicker.SelectedSecurities)
		{
			var subscription = FindSubscription(security, DataType.OrderLog);

			if (subscription != null)
				connector.UnSubscribe(subscription);
			else
				connector.Subscribe(new(DataType.OrderLog, security));
		}
	}

	private void FindClick(object sender, RoutedEventArgs e)
	{
		var wnd = new SecurityLookupWindow
		{
			ShowAllOption = Connector.Adapter.IsSupportSecuritiesLookupAll(),
			CriteriaMessage = new()
			{
				SecurityId = new() { SecurityCode = "EUR" },
				//Currency = CurrencyTypes.USD,
				//SecurityType = SecurityTypes.Currency,
			}
		};

		if (!wnd.ShowModal(this))
			return;

		Connector.Subscribe(new(wnd.CriteriaMessage));
	}

	private void CandlesClick(object sender, RoutedEventArgs e)
	{
		var tf = (TimeSpan)CandlesPeriods.SelectedItem;

		var range = TimeSpan.FromTicks(tf.Ticks * 10000);

		if (range.TotalYears() > 5)
			range = TimeSpan.FromTicks(TimeHelper.TicksPerYear * 5);

		var settings = new DatesSettings { From = DateTime.Today - range };

		var wnd = new SettingsWindow { Settings = settings };

		if (!wnd.ShowModal(this))
			return;

		foreach (var security in SecurityPicker.SelectedSecurities)
		{
			var mdMsg = new MarketDataMessage
			{
				IsSubscribe = true,
				DataType2 = tf.TimeFrame(),
				From = settings.From,
				To = settings.To,
				BuildMode = settings.BuildMode,
				Skip = settings.Skip,
				Count = settings.Count,
				IsFinishedOnly = settings.IsFinishedOnly,
			};
			security.ToMessage().CopyTo(mdMsg);
			var chartWnd = new ChartWindow(mdMsg);

			_chartWindows.Add(chartWnd);
			chartWnd.Closed += (s, e1) => _chartWindows.Remove(chartWnd);

			chartWnd.Show();
		}
	}

	private void CandlesPeriods_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		TryEnableCandles();
	}

	private void TryEnableCandles()
	{
		Candles.IsEnabled = CandlesPeriods.SelectedItem != null && SecurityPicker.SelectedSecurity != null;
	}
}