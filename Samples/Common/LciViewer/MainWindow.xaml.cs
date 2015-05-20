namespace LciViewer
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.History.Russian.Finam;
	using StockSharp.Algo.History.Russian.Rts;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.PnL;
	using StockSharp.Algo.Statistics;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Xaml.Charting.IndicatorPainters;

	public partial class MainWindow
	{
		class SecurityStorage : ISecurityStorage
		{
			private readonly Dictionary<string, Security> _securitiesByCode = new Dictionary<string, Security>(StringComparer.InvariantCultureIgnoreCase);
			private readonly Dictionary<string, Security> _securitiesById = new Dictionary<string, Security>(StringComparer.InvariantCultureIgnoreCase);

			IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
			{
				if (criteria.Code == "*")
					return _securitiesById.Values;

				var security = _securitiesById.TryGetValue(criteria.Id) ?? _securitiesByCode.TryGetValue(criteria.Code);
				return security == null ? Enumerable.Empty<Security>() : new[] { security };
			}

			object ISecurityProvider.GetNativeId(Security security)
			{
				throw new NotSupportedException();
			}

			void ISecurityStorage.Save(Security security)
			{
				_securitiesByCode[security.Code] = security;
				_securitiesById[security.Id] = security;
			}

			IEnumerable<string> ISecurityStorage.GetSecurityIds()
			{
				return _securitiesById.Keys;
			}
		}

		class DatesCache
		{
			private readonly SynchronizedOrderedList<DateTime> _dates = new SynchronizedOrderedList<DateTime>();

			private readonly string _filePath;

			private bool _isChanged;

			public DateTime MinValue { get { return _dates.FirstOrDefault(); } }

			public DateTime MaxValue { get { return _dates.LastOrDefault(); } }

			public DatesCache(string filePath)
			{
				_filePath = filePath;

				if (File.Exists(_filePath))
					CultureInfo.InvariantCulture.DoInCulture(() => _dates.AddRange(new XmlSerializer<DateTime[]>().Deserialize(_filePath)));
			}

			public void Add(DateTime date)
			{
				if (date >= DateTime.Today)
					return;

				_dates.Add(date);
				_isChanged = true;
			}

			public bool Contains(DateTime date)
			{
				return _dates.Contains(date);
			}

			public void Save()
			{
				if (!_isChanged)
					return;

				_isChanged = true;

				_filePath.CreateDirIfNotExists();
				CultureInfo.InvariantCulture.DoInCulture(() => new XmlSerializer<DateTime[]>().Serialize(_dates.ToArray(), _filePath));
			}
		}

		class PnlPainter : BaseChartIndicatorPainter
		{
			private ChartIndicatorElement _pnl;

			/// <summary>
			/// Инициализировать рендерер.
			/// </summary>
			/// <returns>Графические данные.</returns>
			public override IEnumerable<ChartIndicatorElement> Init()
			{
				InnerElements.Clear();

				InnerElements.Add(_pnl = new ChartIndicatorElement
				{
					YAxisId = BaseElement.YAxisId,
					DrawStyle = ChartIndicatorDrawStyles.BandOneValue,
					Color = Colors.Green,
					AdditionalColor = Colors.Red,
					StrokeThickness = BaseElement.StrokeThickness,
					Title = "PnL"
				});

				return InnerElements;
			}

			/// <summary>
			/// Обработать новые значения.
			/// </summary>
			/// <param name="time">Временная отметка формирования новых данных.</param>
			/// <param name="value">Значения индикатора.</param>
			/// <param name="draw">Метод отрисовки значения на графике.</param>
			/// <returns>Новые значения для отображения на графике.</returns>
			public override IEnumerable<decimal> ProcessValues(DateTimeOffset time, IIndicatorValue value, DrawHandler draw)
			{
				var newYValues = new List<decimal>();

				if (!value.IsFormed)
				{
					draw(_pnl, 0, double.NaN, double.NaN);
				}
				else
				{
					var pnl = value.GetValue<decimal>();

					draw(_pnl, 0, (double)pnl, (double)0);
					newYValues.Add(pnl);
				}

				return newYValues;
			}
		}

		private readonly FinamHistorySource _finamHistorySource = new FinamHistorySource();
		private readonly ISecurityStorage _securityStorage = new SecurityStorage();
		private readonly StorageRegistry _dataRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive("Data") };
		private readonly Dictionary<string, StorageRegistry> _traderStorages = new Dictionary<string, StorageRegistry>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Competition _competition = new Competition();
		private readonly StatisticManager _statisticManager = new StatisticManager();
		private readonly ObservableCollection<Security> _securities = new ObservableCollection<Security>();

		private readonly Dictionary<string, DatesCache> _tradesDates = new Dictionary<string, DatesCache>();
		private readonly Dictionary<Tuple<Security, TimeSpan>, DatesCache> _candlesDates = new Dictionary<Tuple<Security, TimeSpan>, DatesCache>();

		private IEnumerable<Candle> _candles;

		public MainWindow()
		{
			InitializeComponent();

			Chart.IsInteracted = true;
			//Chart.IsAutoRange = true;

			Chart.SubscribeIndicatorElement += Chart_SubscribeIndicatorElement;

			Security.ItemsSource = _securities;
			Security.SelectedIndex = 0;

			TimeFrame.ItemsSource = new[] { TimeSpan.FromTicks(1) }.Concat(FinamHistorySource.TimeFrames);
			TimeFrame.SelectedItem = TimeSpan.FromMinutes(5);

			Statistics.StatisticManager = _statisticManager;
		}

		private void Chart_SubscribeIndicatorElement(ChartIndicatorElement element, CandleSeries series, IIndicator indicator)
		{
			if (_candles == null)
				throw new InvalidOperationException("_candles == null");

			var values = _candles
				.Select(candle =>
				{
					if (candle.State != CandleStates.Finished)
						candle.State = CandleStates.Finished;

					return new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(candle.OpenTime, new Dictionary<IChartElement, object>
					{
						{ element, indicator.Process(candle) }
					});
				})
				.ToArray();

			Chart.Draw(values);
		}

		private Competition.CompetitionYear SelectedYear
		{
			get { return _competition.Get((DateTime)Year.SelectedItem); }
		}

		private string SelectedTrader
		{
			get { return (string)Trader.SelectedItem; }
		}

		private Security SelectedSecurity
		{
			get { return (Security)Security.SelectedItem; }
		}

		private TimeSpan SelectedTimeFrame
		{
			get { return (TimeSpan)TimeFrame.SelectedItem; }
		}

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			Year.ItemsSource = Competition.AllYears;
			Year.SelectedItem = Competition.AllYears.Last();

			var ns = typeof(IIndicator).Namespace;

			var rendererTypes = typeof(Chart).Assembly
				.GetTypes()
				.Where(t => !t.IsAbstract && typeof(BaseChartIndicatorPainter).IsAssignableFrom(t))
				.ToDictionary(t => t.Name);

			var indicators = typeof(IIndicator).Assembly
				.GetTypes()
				.Where(t => t.Namespace == ns && !t.IsAbstract && typeof(IIndicator).IsAssignableFrom(t))
				.Select(t => new IndicatorType(t, rendererTypes.TryGetValue(t.Name + "Painter")));

			Chart.IndicatorTypes.AddRange(indicators);

			const string finamSecurities = "finam.csv";

			if (File.Exists(finamSecurities))
			{
				var idGen = new SecurityIdGenerator();

				var securities = File.ReadAllLines(finamSecurities).Select(line =>
				{
					var cells = line.SplitByComma();
					var idParts = idGen.Split(cells[0]);

					return new Security
					{
						Id = cells[0],
						Code = idParts.Item1,
						Board = ExchangeBoard.GetOrCreateBoard(idParts.Item2),
						ExtensionInfo = new Dictionary<object, object>
						{
							{ FinamHistorySource.MarketIdField, cells[1].To<long>() },
							{ FinamHistorySource.SecurityIdField, cells[2].To<long>() },
						}
					};
				});

				foreach (var security in securities)
				{
					_securities.Add(security);
					_securityStorage.Save(security);
				}
			}
			else
			{
				_finamHistorySource.Refresh(_securityStorage, new Security(), s => { }, () => false);

				var securities = _securityStorage.LookupAll().ToArray();

				foreach (var security in securities)
					_securities.Add(security);

				File.WriteAllLines(finamSecurities, securities.Where(s => !s.Id.Contains(',')).Select(s => "{0},{1},{2}"
					.Put(s.Id, s.ExtensionInfo[FinamHistorySource.MarketIdField], s.ExtensionInfo[FinamHistorySource.SecurityIdField])));
			}

			Trader.Text = "Bull";
			Security.Text = "SIZ4@FORTS";
			From.Value = new DateTime(2014, 09, 16);
		}

		private void Year_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			From.Value = To.Value = null;

			From.Minimum = To.Minimum = SelectedYear.Days.First();
			From.Maximum = To.Maximum = SelectedYear.Days.Last();

			if (SelectedYear.Year.Year == DateTime.Today.Year)
			{
				From.Value = DateTime.Today.Min(From.Maximum.Value).Subtract(TimeSpan.FromDays(7));
			}

			Trader.ItemsSource = SelectedYear.Members;
			Trader.SelectedIndex = 0;
		}

		private void Trader_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TryEnableDownload();
		}

		private void Security_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TryEnableDownload();
		}

		private void TryEnableDownload()
		{
			Download.IsEnabled = SelectedTrader != null && SelectedSecurity != null;
		}
		
		private void Download_OnClick(object sender, RoutedEventArgs e)
		{
			var year = SelectedYear;
			var from = From.Value ?? year.Days.First();
			var to = (To.Value ?? year.Days.Last()).EndOfDay();
			var trader = SelectedTrader;
			var security = SelectedSecurity;
			var tf = SelectedTimeFrame;
			var series = new CandleSeries(typeof(TimeFrameCandle), security, tf);

			BusyIndicator.BusyContent = "Подготовка данных...";
			BusyIndicator.IsBusy = true;

			Dictionary<DateTimeOffset, Tuple<MyTrade[], MyTrade>> trades = null;

			var worker = new BackgroundWorker { WorkerReportsProgress = true };

			worker.DoWork += (o, ea) =>
			{
				var candleStorage = _dataRegistry.GetCandleStorage(series, format: StorageFormats.Csv);

				_candles = candleStorage.Load(from, to);

				var candlesDatesCache = _candlesDates.SafeAdd(Tuple.Create(security, tf), k => new DatesCache(Path.Combine(((LocalMarketDataDrive)candleStorage.Drive.Drive).GetSecurityPath(security.ToSecurityId()), "{0}min_date.bin".Put((int)tf.TotalMinutes))));

				var minCandleDate = candlesDatesCache.MinValue;
				var maxCandleDate = candlesDatesCache.MaxValue;

				if (from < minCandleDate || to > maxCandleDate)
				{
					var finamFrom = from;
					var finamTo = to;

					if (maxCandleDate != default(DateTime) && finamFrom >= minCandleDate && finamFrom <= maxCandleDate)
						finamFrom = maxCandleDate + TimeSpan.FromDays(1);

					if (minCandleDate != default(DateTime) && finamTo >= minCandleDate && finamTo <= maxCandleDate)
						finamTo = minCandleDate - TimeSpan.FromDays(1);

					if (finamTo > finamFrom)
					{
						worker.ReportProgress(1);

						var newCandles = (tf.Ticks == 1
							? finamFrom.Range(finamTo, TimeSpan.FromDays(1)).SelectMany(day => _finamHistorySource.GetTrades(security, day, day)).ToEx().ToCandles<TimeFrameCandle>(tf)
							: _finamHistorySource.GetCandles(security, tf, finamFrom, finamTo)
						).ToArray();

						candleStorage.Save(newCandles);

						foreach (var date in newCandles.Select(c => c.OpenTime.Date).Distinct())
							candlesDatesCache.Add(date);

						candlesDatesCache.Save();

						_candles = _candles.Concat(newCandles);	
					}
				}
				
				var traderDrive = new LocalMarketDataDrive(trader);
				var traderStorage = _traderStorages.SafeAdd(trader, key => new StorageRegistry { DefaultDrive = traderDrive });

				var olStorage = traderStorage.GetOrderLogStorage(security, format: StorageFormats.Csv);
				var tradeDatesCache = _tradesDates.SafeAdd(trader, k => new DatesCache(Path.Combine(traderDrive.Path, "dates.bin")));

				trades = from
					.Range(to, TimeSpan.FromDays(1))
					.Intersect(year.Days)
					.SelectMany(date =>
					{
						if (olStorage.Dates.Contains(date))
							return olStorage.Load(date);

						if (tradeDatesCache.Contains(date))
							return Enumerable.Empty<OrderLogItem>();

						worker.ReportProgress(2, date);

						var loadedTrades = year.GetTrades(_securityStorage, trader, date);

						var secTrades = Enumerable.Empty<OrderLogItem>();

						foreach (var group in loadedTrades.GroupBy(t => t.Order.Security))
						{
							var sec = group.Key;

							traderStorage
								.GetOrderLogStorage(sec, format: StorageFormats.Csv)
								.Save(group.OrderBy(i => i.Order.Time));

							if (group.Key == security)
								secTrades = group;
						}

						tradeDatesCache.Add(date);
						tradeDatesCache.Save();

						return secTrades;
					})
					.GroupBy(ol =>
					{
						var time = ol.Order.Time;

						var period = security.Board.WorkingTime.GetPeriod(time.DateTime);
						if (period != null && period.Times.Length > 0)
						{
							var last = period.Times.Last().Max;

							if (time.TimeOfDay >= last)
								time = time.AddTicks(-1);
						}

						return time.Truncate(tf);
					})
					.ToDictionary(g => g.Key, g =>
					{
						var candleTrades = g
							.Select(order => new MyTrade
							{
								Order = order.Order,
								Trade = order.Trade
							})
							.ToArray();

						if (candleTrades.Length > 0)
						{
							var order = candleTrades[0].Order;
							var volume = candleTrades.Sum(t1 => t1.Trade.Volume * (t1.Order.Direction == Sides.Buy ? 1 : -1));

							if (volume == 0)
								return Tuple.Create(candleTrades, (MyTrade)null);

							var side = volume > 0 ? Sides.Buy : Sides.Sell;

							volume = volume.Abs();

							var availableVolume = volume;
							var avgPrice = 0m;

							foreach (var trade in candleTrades.Where(t1 => t1.Order.Direction == side))
							{
								var tradeVol = trade.Trade.Volume.Min(availableVolume);
								avgPrice += trade.Trade.Price * tradeVol;

								availableVolume -= tradeVol;

								if (availableVolume <= 0)
									break;
							}

							avgPrice = avgPrice / volume;

							return Tuple.Create(candleTrades, new MyTrade
							{
								Order = new Order
								{
									Security = order.Security,
									Direction = side,
									Time = g.Key,
									Portfolio = order.Portfolio,
									Price = avgPrice,
									Volume = volume,
								},
								Trade = new Trade
								{
									Security = order.Security,
									Time = g.Key,
									Volume = volume,
									Price = avgPrice
								}
							});
						}

						return null;
					});
			};

			worker.ProgressChanged += (o, ea) =>
			{
				switch (ea.ProgressPercentage)
				{
					case 1:
						BusyIndicator.BusyContent = "Скачивание свечей...";
						break;

					default:
						BusyIndicator.BusyContent = "Скачивание сделок за {0:yyyy-MM-dd}...".Put(ea.UserState);
						break;
				}
			};

			worker.RunWorkerCompleted += (o, ea) =>
			{
				BusyIndicator.IsBusy = false;

				if (ea.Error == null)
				{
					Chart.ClearAreas();
					_statisticManager.Reset();

					var area = new ChartArea();
					area.YAxises.Add(new ChartAxis
					{
						Id = "equity",
						AutoRange = true,
						AxisType = ChartAxisType.Numeric,
						AxisAlignment = ChartAxisAlignment.Left,
					});
					Chart.AddArea(area);

					var candlesElem = new ChartCandleElement { ShowAxisMarker = false };
					Chart.AddElement(area, candlesElem, series);

					var tradesElem = new ChartTradeElement
					{
						BuyStrokeColor = Colors.Black,
						SellStrokeColor = Colors.Black,
						FullTitle = "trades",
					};
					Chart.AddElement(area, tradesElem);
					
					var equityElem = new ChartIndicatorElement { YAxisId = "equity", FullTitle = "equity", IndicatorPainter = new PnlPainter() };
					var equityInd = new SimpleMovingAverage { Length = 1 };
					Chart.AddElement(area, equityElem);

					var positionArea = new ChartArea { Height = 200 };
					Chart.AddArea(positionArea);

					var positionElem = new ChartIndicatorElement { FullTitle = "position" };
					var positionInd = new SimpleMovingAverage { Length = 1 };
					Chart.AddElement(positionArea, positionElem);

					Chart.IsAutoRange = true;

					var pnlQueue = new PnLQueue(security.ToSecurityId());
					//var level1Info = new Level1ChangeMessage
					//{
					//	SecurityId = pnlQueue.SecurityId,
					//}
					//.TryAdd(Level1Fields.PriceStep, security.PriceStep)
					//.TryAdd(Level1Fields.StepPrice, security.StepPrice);

					//pnlQueue.ProcessLevel1(level1Info);

					var pos = 0m;

					var chartValues = _candles
						.Select(c =>
						{
							if (c.State != CandleStates.Finished)
								c.State = CandleStates.Finished;

							pnlQueue.ProcessLevel1(new Level1ChangeMessage
							{
								SecurityId = security.ToSecurityId(),
							}.TryAdd(Level1Fields.LastTradePrice, c.ClosePrice));

							var values = new Dictionary<IChartElement, object>
							{
								{ candlesElem, c },
							};

							var candleTrade = trades.TryGetValue(c.OpenTime);

							if (candleTrade != null)
							{
								if (candleTrade.Item2!=null)
									values.Add(tradesElem, candleTrade.Item2);

								foreach (var myTrade in candleTrade.Item1)
								{
									pos += myTrade.Order.Direction == Sides.Buy ? myTrade.Trade.Volume : -myTrade.Trade.Volume;
									var pnl = pnlQueue.Process(myTrade.ToMessage());

									_statisticManager.AddMyTrade(pnl);
								}

								_statisticManager.AddPosition(c.OpenTime, pos);
								_statisticManager.AddPnL(c.OpenTime, pnlQueue.RealizedPnL + pnlQueue.UnrealizedPnL);
							}

							values.Add(equityElem, equityInd.Process(pnlQueue.RealizedPnL + pnlQueue.UnrealizedPnL));
							values.Add(positionElem, positionInd.Process(pos));

							return new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>
							{
								First = c.OpenTime,
								Second = values
							};
						})
						.ToArray();

					Chart.Draw(chartValues);

					Chart.IsAutoRange = false;
				}
				else
				{
					new MessageBoxBuilder()
						.Error()
						.Owner(this)
						.Text(ea.Error.ToString())
						.Show();
				}
			};

			worker.RunWorkerAsync();
		}
	}
}