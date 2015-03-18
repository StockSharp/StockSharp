namespace StockSharp.Studio
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3183Key)]
	[DescriptionLoc(LocalizedStrings.Str3612Key)]
	[AutoStart]
	[NoEmulation]
	[InteractedStrategy]
	public class TerminalStrategy : Strategy
	{
		private readonly object _syncRoot = new object();

		private readonly Dictionary<IChartElement, RefPair<DateTimeOffset, CandleSeries>> _elementsInfo = new Dictionary<IChartElement, RefPair<DateTimeOffset, CandleSeries>>();
		private readonly Dictionary<CandleSeries, List<IChartElement>> _elementsBySeries = new Dictionary<CandleSeries, List<IChartElement>>();

		private readonly SynchronizedDictionary<MarketDataTypes, SynchronizedDictionary<Security, int>> _subscriptions = new SynchronizedDictionary<MarketDataTypes, SynchronizedDictionary<Security, int>>();

		private readonly Dictionary<Security, List<IChartElement>> _tradeElements = new Dictionary<Security, List<IChartElement>>();
		private readonly Dictionary<Security, List<IChartElement>> _orderElements = new Dictionary<Security, List<IChartElement>>();

		private readonly Dictionary<ChartIndicatorElement, IIndicator> _indicators = new Dictionary<ChartIndicatorElement, IIndicator>();

		public override Security Security
		{
			get { return base.Security; }
			set
			{
				if (base.Security == value)
					return;

				if (base.Security != null)
					TryUnSubscribe(_subscriptions.SafeAdd(MarketDataTypes.Level1), base.Security);

				base.Security = value;

				if (base.Security != null)
					TrySubscribe(_subscriptions.SafeAdd(MarketDataTypes.Level1), base.Security);

				Reset();
			}
		}

		public override Portfolio Portfolio
		{
			get { return base.Portfolio; }
			set
			{
				if (base.Portfolio == value)
					return;

				base.Portfolio = value;

				if (base.Portfolio != null)
					new PortfolioCommand(value, true).Process(this);
			}
		}

		public TerminalStrategy()
		{
			var cmdSvc = ConfigManager.TryGetService<IStudioCommandService>();

			if (cmdSvc == null)
				return;

			cmdSvc.Register<RegisterOrderCommand>(this, false, cmd =>
			{
				var order = cmd.Order;

				if (order.Security == null)
					order.Security = Security;

				if (order.Portfolio == null)
					order.Portfolio = Portfolio;

				if (order.Volume == 0)
					order.Volume = Volume; //при выставлении заявки с графика объем равен 0

				if (order.Type == OrderTypes.Market && !order.Security.Board.IsSupportMarketOrders)
				{
					order.Type = OrderTypes.Limit;
					order.Price = order.Security.GetMarketPrice(SafeGetConnector(), order.Direction) ?? 0;
				}

				order.ShrinkPrice();

				RegisterOrder(order);
			});
			cmdSvc.Register<ReRegisterOrderCommand>(this, false, cmd => ReRegisterOrder(cmd.OldOrder, cmd.NewOrder));
			cmdSvc.Register<CancelOrderCommand>(this, false, cmd =>
			{
				if (cmd.Mask != null)
				{
					Orders
						.Where(o => o.Security == cmd.Mask.Security && o.Portfolio == cmd.Mask.Portfolio && o.Price == cmd.Mask.Price && o.State == OrderStates.Active)
						.ForEach(CancelOrder);
				}
				else
				{
					cmd.Orders.ForEach(o =>
					{
						if (o.Security == null)
							o.Security = Security;

						if (o.Portfolio == null)
							o.Portfolio = Portfolio;

						CancelOrder(o);	
					});
				}
			});
			cmdSvc.Register<RevertPositionCommand>(this, false, cmd =>
			{
				var pos = PositionManager.Position;

				if (pos == 0)
				{
					this.AddWarningLog(LocalizedStrings.Str3631);
					return;
				}

				if (cmd.Position != null)
				{
					ClosePosition(cmd.Position, cmd.Position.CurrentValue.Abs() * 2);
				}
				else
				{
					if (pos < 0)
						this.BuyAtMarket(pos * 2);
					else
						this.SellAtMarket(pos * 2);
				}
			});
			cmdSvc.Register<ClosePositionCommand>(this, false, cmd =>
			{
				if (PositionManager.Position == 0)
				{
					this.AddWarningLog(LocalizedStrings.Str3632);
					return;
				}

				if (cmd.Position != null)
				{
					ClosePosition(cmd.Position, cmd.Position.CurrentValue.Abs());
				}
				else
					this.ClosePosition();
			});
			cmdSvc.Register<CancelAllOrdersCommand>(this, false, cmd => CancelActiveOrders());

			cmdSvc.Register<RequestMarketDataCommand>(this, false, cmd =>
			{
				var security = cmd.Security ?? Security;

				this.AddDebugLog("RequestMarketDataCommand {0} {1}", cmd.Type, security);

				if (TrySubscribe(_subscriptions.SafeAdd(cmd.Type), security) && CanProcess)
					Subscribe(cmd.Type, security);
			});
			cmdSvc.Register<RefuseMarketDataCommand>(this, false, cmd =>
			{
				var security = cmd.Security ?? Security;

				this.AddDebugLog("RefuseMarketDataCommand {0} {1}", cmd.Type, security);

				if (TryUnSubscribe(_subscriptions.SafeAdd(cmd.Type), security) && CanProcess)
					UnSubscribe(cmd.Type, security);
			});

			cmdSvc.Register<SubscribeCandleElementCommand>(this, false, cmd => SetSource(cmd.Element, cmd.CandleSeries));
			cmdSvc.Register<UnSubscribeCandleElementCommand>(this, false, cmd => RemoveSource(cmd.Element));

			cmdSvc.Register<SubscribeIndicatorElementCommand>(this, false, cmd => SetSource(cmd.Element, cmd.CandleSeries, cmd.Indicator));
			cmdSvc.Register<UnSubscribeIndicatorElementCommand>(this, false, cmd => RemoveSource(cmd.Element));

			cmdSvc.Register<SubscribeTradeElementCommand>(this, false, cmd => Subscribe(_tradeElements, cmd.Security, cmd.Element));
			cmdSvc.Register<UnSubscribeTradeElementCommand>(this, false, cmd => UnSubscribe(_tradeElements, cmd.Element));

			cmdSvc.Register<SubscribeOrderElementCommand>(this, false, cmd => Subscribe(_orderElements, cmd.Security, cmd.Element));
			cmdSvc.Register<UnSubscribeOrderElementCommand>(this, false, cmd => UnSubscribe(_orderElements, cmd.Element));

			cmdSvc.Register<RequestTradesCommand>(this, false, cmd => new NewTradesCommand(SafeGetConnector().Trades).Process(this));

			cmdSvc.Register<RequestPortfoliosCommand>(this, false, cmd => new PortfolioCommand(Portfolio, true).Process(this));
			cmdSvc.Register<RequestPositionsCommand>(this, false, cmd => PositionManager.Positions.ForEach(p => new PositionCommand(CurrentTime, p, true).Process(this)));
		}

		private bool CanProcess
		{
			get { return ProcessState == ProcessStates.Started; }
		}

		protected override void OnStarted()
		{
			_subscriptions.ForEach(s => s.Value.Where(p => p.Value > 0).ForEach(v => Subscribe(s.Key, v.Key)));

			lock (_syncRoot)
			{
				_elementsBySeries
					.Keys
					.Distinct()
					.ForEach(StartSeries);
			}
			
			this
				.WhenOrderRegistered()
				.Do(ProcessOrder)
				.Apply(this);

			this
				.WhenNewMyTrades()
				.Do(ProcessTrades)
				.Apply(this);

			base.OnStarted();
		}

		protected override void OnStopping()
		{
			_subscriptions.ForEach(s => s.Value.Where(p => p.Value > 0).ForEach(v => UnSubscribe(s.Key, v.Key)));

			lock (_syncRoot)
			{
				_elementsBySeries
					.Keys
					.Distinct()
					.ForEach(StopSeries);	
			}

			base.OnStopping();
		}

		#region Market data subscription

		private void Subscribe(IDictionary<Security, List<IChartElement>> elements, Security security, IChartElement element)
		{
			lock (_syncRoot)
				elements.SafeAdd(security).Add(element);
		}

		private void UnSubscribe(IDictionary<Security, List<IChartElement>> elements, IChartElement element)
		{
			lock (_syncRoot)
			{
				foreach (var pair in elements.ToArray())
				{
					if (!pair.Value.Remove(element))
						continue;

					if (pair.Value.Count == 0)
						elements.Remove(pair.Key);
				}
			}
		}

		private static bool TrySubscribe(SynchronizedDictionary<Security, int> subscribers, Security subscriber)
		{
			return ChangeSubscribers(subscribers, subscriber, 1) == 1;
		}

		private static bool TryUnSubscribe(SynchronizedDictionary<Security, int> subscribers, Security subscriber)
		{
			return ChangeSubscribers(subscribers, subscriber, -1) == 0;
		}

		private static int ChangeSubscribers(SynchronizedDictionary<Security, int> subscribers, Security subscriber, int delta)
		{
			if (subscribers == null)
				throw new ArgumentNullException("subscribers");

			lock (subscribers.SyncRoot)
			{
				var value = subscribers.TryGetValue2(subscriber) ?? 0;

				value += delta;

				if (value > 0)
					subscribers[subscriber] = value;
				else
					subscribers.Remove(subscriber);

				return value;
			}
		}

		private void Subscribe(MarketDataTypes type, Security security)
		{
			switch (type)
			{
				case MarketDataTypes.Level1:
					SafeGetConnector().RegisterSecurity(security);
					break;

				case MarketDataTypes.OrderLog:
					break;

				case MarketDataTypes.Trades:
				{
					security
						.WhenNewTrades(SafeGetConnector())
						.Do(trades => new NewTradesCommand(trades).Process(this))
						.Until(() => !(CanProcess && IsSubscribed(MarketDataTypes.Trades, security)))
						.Apply(this);

					SafeGetConnector().RegisterTrades(security);
					break;
				}

				case MarketDataTypes.MarketDepth:
				{
					security
						.WhenMarketDepthChanged(SafeGetConnector())
						.Do(md => new UpdateMarketDepthCommand(md).Process(this))
						.Until(() => !(CanProcess && IsSubscribed(MarketDataTypes.MarketDepth, security)))
						.Apply(this);

					SafeGetConnector().RegisterMarketDepth(security);
					break;
				}
			}
		}

		private void UnSubscribe(MarketDataTypes type, Security security)
		{
			switch (type)
			{
				case MarketDataTypes.Level1:
				case MarketDataTypes.OrderLog:
					break;

				case MarketDataTypes.Trades:
					SafeGetConnector().UnRegisterTrades(security);
					break;

				case MarketDataTypes.MarketDepth:
					SafeGetConnector().UnRegisterMarketDepth(security);
					break;
			}
		}

		private bool IsSubscribed(MarketDataTypes type, Security security)
		{
			var dic = _subscriptions.TryGetValue(type);

			if (dic == null)
				return false;

			return dic.TryGetValue(security) != 0;
		}

		#endregion

		#region Chart

		private void RemoveSource(IChartElement element)
		{
			lock (_syncRoot)
			{
				var info = _elementsInfo.TryGetValue(element);

				if (info == null)
					return;

				_elementsInfo.Remove(element);

				var elements = _elementsBySeries.TryGetValue(info.Second);
				if (elements == null)
					return;

				elements.Remove(element);

				if (elements.Count != 0)
					return;

				UnSubscribeSeries(info.Second);
				_elementsBySeries.Remove(info.Second);
			}
		}

		private void SetSource(ChartCandleElement element, CandleSeries candleSeries)
		{
			lock (_syncRoot)
			{
				var isNew = !_elementsBySeries.ContainsKey(candleSeries);

				if ((isNew || !CanProcess) && isNew)
					SubscribeSeries(candleSeries);

				_elementsInfo.SafeAdd(element, e => RefTuple.Create(DateTimeOffset.MinValue, candleSeries));
				_elementsBySeries.SafeAdd(candleSeries).Add(element);
			}
		}

		private void SetSource(ChartIndicatorElement element, CandleSeries candleSeries, IIndicator indicator)
		{
			List<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>> values = null;

			lock (_syncRoot)
			{
				_indicators[element] = indicator;

				var isNew = !_elementsBySeries.ContainsKey(candleSeries);

				if (!isNew && CanProcess)
				{
					values = ProcessHistoryCandles(element, candleSeries);
				}
				else if (isNew)
					SubscribeSeries(candleSeries);

				var lastDate = values == null || values.IsEmpty() ? DateTimeOffset.MinValue : values.Last().First;

				_elementsInfo.SafeAdd(element, e => RefTuple.Create(lastDate, candleSeries));
				_elementsBySeries.SafeAdd(candleSeries).Add(element);
			}

			if (values != null && values.Count > 0)
				new ChartDrawCommand(values).Process(this);
		}

		private List<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>> ProcessHistoryCandles(ChartIndicatorElement element, CandleSeries series)
		{
			var candles = series.GetCandles<Candle>().Where(c => c.State == CandleStates.Finished).ToArray();

			return candles
				.Select(candle => new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(candle.OpenTime, new Dictionary<IChartElement, object>
				{
					{ element, CreateIndicatorValue(element, candle) }
				}))
				.ToList();
		}

		private IIndicatorValue CreateIndicatorValue(ChartIndicatorElement element, Candle candle)
		{
			var indicator = _indicators.TryGetValue(element);

			if (indicator == null)
				throw new InvalidOperationException(LocalizedStrings.IndicatorNotFound.Put(element));

			return indicator.Process(candle);
		}

		private void SubscribeSeries(CandleSeries series)
		{
			series.ProcessCandle += Process;
			StartSeries(series);
		}

		private void UnSubscribeSeries(CandleSeries series)
		{
			series.ProcessCandle -= Process;
			StopSeries(series);
		}

		private void StartSeries(CandleSeries series)
		{
			if (!CanProcess)
				return;

			var candleManager = this.GetCandleManager();

			if (candleManager == null)
				throw new InvalidOperationException(LocalizedStrings.Str3633);

			candleManager.Start(series);
		}

		private void StopSeries(CandleSeries series)
		{
			var candleManager = this.GetCandleManager();

			if (candleManager != null)
				candleManager.Stop(series);
		}

		private void Process(Candle candle)
		{
			var values = new Dictionary<IChartElement, object>();

			lock (_syncRoot)
			{
				this.AddInfoLog("{0}: {1}", candle.OpenTime, candle.State);

				var elements = _elementsBySeries[candle.Series]
					.OfType<ChartIndicatorElement>();

				var candleElement = _elementsBySeries[candle.Series]
					.OfType<ChartCandleElement>()
					.FirstOrDefault();

				if (candle.State == CandleStates.Finished)
					this.AddInfoLog(LocalizedStrings.Str3634Params, candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume, candle.Security.Id);

				if (candleElement != null)
					values.Add(candleElement, candle);

				foreach (var element in elements)
				{
					var info = _elementsInfo[element];

					if (candle.OpenTime < info.First)
						continue;

					info.First = candle.OpenTime;
					values.Add(element, CreateIndicatorValue(element, candle));
				}
			}

			if (values.Count > 0)
				new ChartDrawCommand(candle.OpenTime, values).Process(this);
		}

		private void ProcessOrder(Order order)
		{
			Dictionary<IChartElement, object> values;

			lock (_syncRoot)
			{
				var elements = _orderElements.TryGetValue(order.Security);

				if (elements == null)
					return;

				values = elements.ToDictionary(e => e, e => (object)order);
			}

			if (values.Count > 0)
				new ChartDrawCommand(order.Time, values).Process(this);
		}

		private void ProcessTrades(IEnumerable<MyTrade> trades)
		{
			var values = new List<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>>();

			lock (_syncRoot)
			{
				foreach (var myTrade in trades)
				{
					var elements = _tradeElements.TryGetValue(myTrade.Order.Security);

					if (elements == null)
						continue;

					values.Add(new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(myTrade.Trade.Time, elements.ToDictionary(e => e, e => (object)myTrade)));
				}
			}

			if (values.Count > 0)
				new ChartDrawCommand(values).Process(this);
		}

		#endregion

		private void ClosePosition(Position position, decimal volume)
		{
			var side = position.CurrentValue > 0 ? Sides.Sell : Sides.Buy;
			var security = position.Security;

			var order = new Order
			{
				Security = security,
				Portfolio = position.Portfolio,
				Direction = side,
				Volume = volume,
				Type = position.Portfolio.Board.IsSupportMarketOrders ? OrderTypes.Market : OrderTypes.Limit,
			};

			if (order.Type == OrderTypes.Limit)
				order.Price = security.GetMarketPrice(SafeGetConnector(), side) ?? 0;

			RegisterOrder(order);
		}
	}
}
