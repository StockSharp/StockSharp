namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Xml.Serialization;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Messages;

	using StockSharp.Localization;

	/// <summary>
	/// Стакан с котировками.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	[Ignore(FieldName = "IsDisposed")]
	//[EntityFactory(typeof(UnitializedEntityFactory<MarketDepth>))]
	public class MarketDepth : Cloneable<MarketDepth>, IEnumerable<Quote>, ISynchronizedCollection
	{
		private readonly SyncObject _syncRoot = new SyncObject();

		/// <summary>
		/// Создать стакан.
		/// </summary>
		/// <param name="security">Инструмент стакана.</param>
		public MarketDepth(Security security)
		{
			if (ReferenceEquals(security, null))
				throw new ArgumentNullException("security");

			Security = security;
			_bids = _asks = ArrayHelper<Quote>.EmptyArray;
		}

		private int _maxDepth = 100;

		/// <summary>
		/// Максимальная глубина стакана.
		/// </summary>
		/// <remarks>
		/// По умолчанию значение равно 100. Если новая котировка превысила максимальную глубину, то будет вызвано событие <see cref="QuoteOutOfDepth"/>.
		/// </remarks>
		public int MaxDepth
		{
			get { return _maxDepth; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str480);

				_maxDepth = value;

				Truncate(Bids, Asks, default(DateTimeOffset));
			}
		}

		/// <summary>
		/// Инструмент стакана.
		/// </summary>
		public Security Security { get; private set; }

		[field: NonSerialized]
		private IConnector _connector;

		/// <summary>
		/// Подключение к торговой системе.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Obsolete("Security.Connector устарел и всегда равен null.")]
		public IConnector Connector
		{
			get { return _connector; }
			set { _connector = value; }
		}

		/// <summary>
		/// Автоматически проверять котировки через метод <see cref="Verify()"/>.
		/// </summary>
		/// <remarks>
		/// По-умолчанию выключено в целях производительности.
		/// </remarks>
		public bool AutoVerify { get; set; }

		/// <summary>
		/// Использовать ли аггрегированные котировки <see cref="AggregatedQuote"/> при слиянии объемом с одинаковой ценой.
		/// </summary>
		/// <remarks>
		/// По-умолчанию выключено в целях производительности.
		/// </remarks>
		public bool UseAggregatedQuotes { get; set; }

		/// <summary>
		/// Время последнего изменения стакана.
		/// </summary>
		public DateTimeOffset LastChangeTime { get; set; }

		/// <summary>
		/// Локальное время последнего изменения стакана.
		/// </summary>
		public DateTime LocalTime { get; set; }

		// TODO
		//private Quote[] _bidsCache;
		private Quote[] _bids;

		/// <summary>
		/// Возвращает массив бидов упорядоченных по убыванию цены. Первый бид будет иметь максимальную цену, и будет являться лучшим.
		/// </summary>
		public Quote[] Bids 
		{
			get
			{
				return _bids;
				//lock (_syncRoot)
				//{
				//    return _bidsCache ?? (_bidsCache = _bids.Select(q => q.Clone()).ToArray());
				//}
			}
			private set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_bids = value;
				//_bidsCache = null;
			}
		}

		//private Quote[] _asksCache;
		private Quote[] _asks;

		/// <summary>
		/// Возвращает массив офферов упорядоченных по возрастанию цены. Первый оффер будет иметь минимальную цену, и будет являться лучшим.
		/// </summary>
		public Quote[] Asks 
		{ 
			get
			{
				return _asks;
				//lock (_syncRoot)
				//{
				//    return _asksCache ?? (_asksCache = _asks.Select(q => q.Clone()).ToArray());
				//}
			}
			private set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_asks = value;
				//_asksCache = null;
			}
		}

		/// <summary>
		/// Лучший бид. Если стакан не содержит бидов, то будет возвращено <see langword="null"/>.
		/// </summary>
		public Quote BestBid { get; private set; }

		/// <summary>
		/// Лучший оффер. Если стакан не содержит офферов, то будет возвращено <see langword="null"/>.
		/// </summary>
		public Quote BestAsk { get; private set; }

		/// <summary>
		/// Лучшая пара котировок. Если стакан пустой, то будет возвращено <see langword="null"/>.
		/// </summary>
		public MarketDepthPair BestPair
		{
			get { return GetPair(0); }
		}

		/// <summary>
		/// Получить общий ценовой размер по бидам.
		/// </summary>
		public decimal TotalBidsPrice
		{
			get
			{
				lock (_syncRoot)
					return _bids.Length > 0 ? Security.ShrinkPrice(_bids.Sum(b => b.Price)) : 0;
			}
		}

		/// <summary>
		/// Получить общий ценовой размер по офферам.
		/// </summary>
		public decimal TotalAsksPrice
		{
			get
			{
				lock (_syncRoot)
					return _asks.Length > 0 ? Security.ShrinkPrice(_asks.Sum(a => a.Price)) : 0;
			}
		}

		/// <summary>
		/// Получить общий объем по бидам.
		/// </summary>
		public decimal TotalBidsVolume
		{
			get
			{
				lock (_syncRoot)
					return _bids.Sum(b => b.Volume);
			}
		}

		/// <summary>
		/// Получить общий объем по офферам.
		/// </summary>
		public decimal TotalAsksVolume
		{
			get
			{
				lock (_syncRoot)
					return _asks.Sum(a => a.Volume);
			}
		}

		/// <summary>
		/// Получить общий объем.
		/// </summary>
		public decimal TotalVolume
		{
			get
			{
				lock (_syncRoot)
					return TotalBidsVolume + TotalAsksVolume;
			}
		}

		/// <summary>
		/// Получить общий ценовой размер.
		/// </summary>
		public decimal TotalPrice
		{
			get
			{
				lock (_syncRoot)
					return TotalBidsPrice + TotalAsksPrice;
			}
		}

		/// <summary>
		/// Общее количество котировок (бидов + оферов) в стакане.
		/// </summary>
		public int Count
		{
			get
			{
				lock (_syncRoot)
					return _bids.Length + _asks.Length;
			}
		}

		private int _depth;

		/// <summary>
		/// Глубина стакана.
		/// </summary>
		public int Depth
		{
			get { return _depth; }
			private set
			{
				if (_depth == value)
					return;

				_depth = value;
				DepthChanged.SafeInvoke();
			}
		}

		/// <summary>
		/// Событие о превышении котировки максимально допустимой глубины в стакане.
		/// </summary>
		public event Action<Quote> QuoteOutOfDepth;

		/// <summary>
		/// Событие об изменении глубины стакана <see cref="Depth"/>.
		/// </summary>
		public event Action DepthChanged;

		/// <summary>
		/// Событие изменения котировок в стакане.
		/// </summary>
		public event Action QuotesChanged;

		/// <summary>
		/// Уменьшить стакан до необходимой глубины.
		/// </summary>
		/// <param name="newDepth">Новая глубина стакана.</param>
		public void Decrease(int newDepth)
		{
			var currentDepth = Depth;

			if (newDepth < 0)
				throw new ArgumentOutOfRangeException("newDepth", newDepth, LocalizedStrings.Str481);
			else if (newDepth > currentDepth)
				throw new ArgumentOutOfRangeException("newDepth", newDepth, LocalizedStrings.Str482Params.Put(currentDepth));

			lock (_syncRoot)
			{
				Bids = Decrease(_bids, newDepth);
				Asks = Decrease(_asks, newDepth);

				UpdateDepthAndTime();
			}

			RaiseQuotesChanged();
		}

		private static Quote[] Decrease(Quote[] quotes, int newDepth)
		{
			if (quotes == null)
				throw new ArgumentNullException("quotes");

			if (newDepth <= quotes.Length)
				Array.Resize(ref quotes, newDepth);

			return quotes;
		}

		/// <summary>
		/// Получить котировку по направлению <see cref="Sides"/> и индексу глубины.
		/// </summary>
		/// <param name="orderDirection">Направление заявок.</param>
		/// <param name="depthIndex">Индекс глубины. Нулевой индекс означает лучшую котировку.</param>
		/// <returns>Котировка. Если для заданной глубины не существует котировки, то будет возвращено null.</returns>
		public Quote GetQuote(Sides orderDirection, int depthIndex)
		{
			lock (_syncRoot)
				return GetQuotesInternal(orderDirection).ElementAtOrDefault(depthIndex);
		}

		/// <summary>
		/// Получить котировку по цене.
		/// </summary>
		/// <param name="price">Цена котировки.</param>
		/// <returns>Найденная котировка. Если для переданной цены в стакане не существует котировки, то будет возвращено null.</returns>
		public Quote GetQuote(decimal price)
		{
			var quotes = GetQuotes(price);
			var i = GetQuoteIndex(quotes, price);
			return i < 0 ? null : quotes[i];
		}

		/// <summary>
		/// Получить котировки по направлению <see cref="Sides"/>.
		/// </summary>
		/// <param name="orderDirection">Направление заявок.</param>
		/// <returns>Котировки.</returns>
		public Quote[] GetQuotes(Sides orderDirection)
		{
			return orderDirection == Sides.Buy ? Bids : Asks;
		}

		/// <summary>
		/// Получить лучшую котировку по направлению <see cref="Sides"/>.
		/// </summary>
		/// <param name="orderDirection">Направление заявки.</param>
		/// <returns>Лучшая котировка. Если стакан пустой, то будет возвращено null.</returns>
		public Quote GetBestQuote(Sides orderDirection)
		{
			return orderDirection == Sides.Buy ? BestBid : BestAsk;
		}

		/// <summary>
		/// Получить пару котировок (бид + оффер) по индексу глубины.
		/// </summary>
		/// <param name="depthIndex">Индекс глубины. Нулевой индекс означает лучшую пару котировок.</param>
		/// <returns>Пара котировок. Если индекс больше глубины стакана <see cref="Depth"/>, то возвращается <see langword="null"/>.</returns>
		public MarketDepthPair GetPair(int depthIndex)
		{
			if (depthIndex < 0)
				throw new ArgumentOutOfRangeException("depthIndex", depthIndex, LocalizedStrings.Str483);

			lock (_syncRoot)
			{
				var bid = GetQuote(Sides.Buy, depthIndex);
				var ask = GetQuote(Sides.Sell, depthIndex);

				if (bid == null && ask == null)
					return null;
				
				return new MarketDepthPair(Security, bid, ask);
			}
		}

		/// <summary>
		/// Получить край стакана на заданную глубину в виде пар котировок.
		/// </summary>
		/// <param name="depth">Глубина края стакана. Отсчет идет от лучших котировок.</param>
		/// <returns>Край стакана.</returns>
		public IEnumerable<MarketDepthPair> GetTopPairs(int depth)
		{
			if (depth < 0)
				throw new ArgumentOutOfRangeException("depth", depth, LocalizedStrings.Str484);

			var retVal = new List<MarketDepthPair>();

			lock (_syncRoot)
			{
				for (var i = 0; i < depth; i++)
				{
					var single = GetPair(i);

					if (single != null)
						retVal.Add(single);
					else
						break;
				}
			}

			return retVal;
		}

		/// <summary>
		/// Получить край стакана на заданную глубину в виде котировок.
		/// </summary>
		/// <param name="depth">Глубина края стакана. Котировки идут в порядке увеличения цены от бидов до офферов.</param>
		/// <returns>Край стакана.</returns>
		public IEnumerable<Quote> GetTopQuotes(int depth)
		{
			if (depth < 0)
				throw new ArgumentOutOfRangeException("depth", depth, LocalizedStrings.Str484);

			var retVal = new List<Quote>();

			lock (_syncRoot)
			{
				for (var i = depth - 1; i >= 0; i--)
				{
					var single = GetQuote(Sides.Buy, i);

					if (single != null)
						retVal.Add(single);
				}

				for (var i = 0; i < depth; i++)
				{
					var single = GetQuote(Sides.Sell, i);

					if (single != null)
						retVal.Add(single);
					else
						break;
				}
			}

			return retVal;
		}

		/// <summary>
		/// Обновить стакан новыми котировками.
		/// </summary>
		/// <remarks>
		/// Старые котировки удаляются из стакана.
		/// </remarks>
		/// <param name="quotes">Новые котировки.</param>
		/// <param name="lastChangeTime">Время последнего изменения стакана.</param>
		/// <returns>Стакан.</returns>
		public MarketDepth Update(IEnumerable<Quote> quotes, DateTimeOffset lastChangeTime = default(DateTimeOffset))
		{
			if (quotes == null)
				throw new ArgumentNullException("quotes");

			var bids = Enumerable.Empty<Quote>();
			var asks = Enumerable.Empty<Quote>();

			foreach (var group in quotes.GroupBy(q => q.OrderDirection))
			{
				if (group.Key == Sides.Buy)
					bids = group;
				else
					asks = group;
			}

			return Update(bids, asks, false, lastChangeTime);
		}

		/// <summary>
		/// Обновить стакан новыми бидами и офферами.
		/// </summary>
		/// <remarks>
		/// Старые котировки удаляются из стакана.
		/// </remarks>
		/// <param name="bids">Новые биды.</param>
		/// <param name="asks">Новые оффера.</param>
		/// <param name="isSorted">Отсортированы ли котировки. Параметр используется в целях оптимизации для предотвращения повторной сортировки.</param>
		/// <param name="lastChangeTime">Время последнего изменения стакана.</param>
		/// <returns>Стакан.</returns>
		public MarketDepth Update(IEnumerable<Quote> bids, IEnumerable<Quote> asks, bool isSorted = false, DateTimeOffset lastChangeTime = default(DateTimeOffset))
		{
			if (bids == null)
				throw new ArgumentNullException("bids");

			if (asks == null)
				throw new ArgumentNullException("asks");

			if (!isSorted)
			{
				bids = bids.OrderBy(q => 0 - q.Price);
				asks = asks.OrderBy(q => q.Price);
			}

			bids = bids.ToArray();
			asks = asks.ToArray();

			if (AutoVerify)
			{
				if (!Verify(bids, asks))
					throw new ArgumentException(LocalizedStrings.Str485);
			}

			Truncate((Quote[])bids, (Quote[])asks, lastChangeTime);
			return this;
		}

		private void Truncate(Quote[] bids, Quote[] asks, DateTimeOffset lastChangeTime)
		{
			Quote[] outOfRangeBids;
			Quote[] outOfRangeAsks;

			lock (_syncRoot)
			{
				Update(Truncate(bids, out outOfRangeBids), Truncate(asks, out outOfRangeAsks), lastChangeTime);
			}

			var evt = QuoteOutOfDepth;

			if (evt != null)
			{
				if (outOfRangeBids != null)
					outOfRangeBids.ForEach(evt);

				if (outOfRangeAsks != null)
					outOfRangeAsks.ForEach(evt);
			}
		}

		private Quote[] Truncate(Quote[] quotes, out Quote[] outOfRangeQuotes)
		{
			if (quotes.Length > MaxDepth)
			{
				outOfRangeQuotes = new Quote[quotes.Length - MaxDepth];
				Array.Copy(quotes, MaxDepth, outOfRangeQuotes, 0, outOfRangeQuotes.Length);

				Array.Resize(ref quotes, MaxDepth);
			}
			else
			{
				outOfRangeQuotes = null;
			}

			return quotes;
		}

		/// <summary>
		/// Обновить стакан. Версия без проверок и блокировок.
		/// </summary>
		/// <param name="bids">Отсортированные биды.</param>
		/// <param name="asks">Отсортированные офера.</param>
		/// <param name="lastChangeTime">Время обновления.</param>
		public void Update(Quote[] bids, Quote[] asks, DateTimeOffset lastChangeTime)
		{
			//_bidsCache = null;
			//_asksCache = null;

			_bids = bids;
			_asks = asks;

			UpdateDepthAndTime(lastChangeTime, false);

			if (null != QuotesChanged)
				QuotesChanged();
			//RaiseQuotesChanged();
		}

		/// <summary>
		/// Обновить котировку. Если котировка с такое ценой уже присутствует в стакане, то она обновляется переданной.
		/// Иначе, она автоматически перестраивает стакан.
		/// </summary>
		/// <param name="quote">Новая котировка.</param>
		public void UpdateQuote(Quote quote)
		{
			SetQuote(quote, false);
		}

		/// <summary>
		/// Добавить котировку на покупку.
		/// </summary>
		/// <param name="price">Цена покупки.</param>
		/// <param name="volume">Объем покупки.</param>
		public void AddBid(decimal price, decimal volume)
		{
			AddQuote(new Quote
			{
				Security = Security,
				Price = price,
				Volume = volume,
				OrderDirection = Sides.Buy,
			});
		}

		/// <summary>
		/// Добавить котировку на продажу.
		/// </summary>
		/// <param name="price">Цена продажи.</param>
		/// <param name="volume">Объем продажи.</param>
		public void AddAsk(decimal price, decimal volume)
		{
			AddQuote(new Quote
			{
				Security = Security,
				Price = price,
				Volume = volume,
				OrderDirection = Sides.Sell,
			});
		}

		/// <summary>
		/// Добавить котировку. Если котировка с такое ценой уже присутствует в стакане, то они объединяются в <see cref="AggregatedQuote"/>.
		/// </summary>
		/// <param name="quote">Новая котировка.</param>
		public void AddQuote(Quote quote)
		{
			SetQuote(quote, true);
		}

		private void SetQuote(Quote quote, bool isAggregate)
		{
			CheckQuote(quote);

			Quote outOfDepthQuote = null;

			lock (_syncRoot)
			{
				var quotes = GetQuotes(quote.OrderDirection);

				var index = GetQuoteIndex(quotes, quote.Price);

				if (index != -1)
				{
					if (isAggregate)
					{
						var existedQuote = quotes[index];

						if (UseAggregatedQuotes)
						{
							var aggQuote = existedQuote as AggregatedQuote;

							if (aggQuote == null)
							{
								aggQuote = new AggregatedQuote
								{
									Price = quote.Price,
									Security = quote.Security,
									OrderDirection = quote.OrderDirection
								};

								aggQuote.InnerQuotes.Add(existedQuote);

								quotes[index] = aggQuote;
							}

							aggQuote.InnerQuotes.Add(quote);
						}
						else
							existedQuote.Volume += quote.Volume;
					}
					else
					{
						quotes[index] = quote;
					}
				}
				else
				{
					for (index = 0; index < quotes.Length; index++)
					{
						var currentPrice = quotes[index].Price;

						if (quote.OrderDirection == Sides.Buy)
						{
							if (quote.Price > currentPrice)
								break;
						}
						else
						{
							if (quote.Price < currentPrice)
								break;
						}
					}

					Array.Resize(ref quotes, quotes.Length + 1);

					if (index < (quotes.Length - 1))
						Array.Copy(quotes, index, quotes, index + 1, quotes.Length - 1 - index);

					quotes[index] = quote;

					if (quotes.Length > MaxDepth)
					{
						outOfDepthQuote = quotes[quotes.Length - 1];
						quotes = RemoveAt(quotes, quotes.Length - 1);
					}

					if (quote.OrderDirection == Sides.Buy)
						Bids = quotes;
					else
						Asks = quotes;
				}

				UpdateDepthAndTime();

				if (quotes.Length > MaxDepth)
					throw new InvalidOperationException(LocalizedStrings.Str486Params.Put(MaxDepth, quotes.Length));
			}

			RaiseQuotesChanged();

			if (outOfDepthQuote != null)
				QuoteOutOfDepth.SafeInvoke(outOfDepthQuote);
		}

		#region IEnumerable<Quote>

		/// <summary>
		/// Получить объект-перечислитель.
		/// </summary>
		/// <returns>Объект-перечислитель.</returns>
		public IEnumerator<Quote> GetEnumerator()
		{
			return this.SyncGet(c => Bids.Reverse().Concat(Asks)).Cast<Quote>().GetEnumerator();
		}

		/// <summary>
		/// Получить объект-перечислитель.
		/// </summary>
		/// <returns>Объект-перечислитель.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		/// <summary>
		/// Получить все пары из стакана.
		/// </summary>
		/// <returns>Пары, из которых составлен стакан.</returns>
		public IEnumerable<MarketDepthPair> ToPairs()
		{
			return GetTopPairs(Depth);
		}

		/// <summary>
		/// Удалить котировку из стакана.
		/// </summary>
		/// <param name="quote">Котировка, которую необходимо удалить.</param>
		/// <param name="lastChangeTime">Время изменения стакана.</param>
		public void Remove(Quote quote, DateTimeOffset lastChangeTime = default(DateTimeOffset))
		{
			if (quote == null)
				throw new ArgumentNullException("quote");

			Remove(quote.OrderDirection, quote.Price, quote.Volume, lastChangeTime);
		}

		/// <summary>
		/// Удалить объем для заданной цены.
		/// </summary>
		/// <param name="price">Цена, для которой необходимо удалить котировку.</param>
		/// <param name="volume">Объем, который нужно удалить. Если он не указан, значит удаляется вся котировка.</param>
		/// <param name="lastChangeTime">Время изменения стакана.</param>
		public void Remove(decimal price, decimal volume = 0, DateTimeOffset lastChangeTime = default(DateTimeOffset))
		{
			lock (_syncRoot)
			{
				var dir = GetDirection(price);

				if (dir == null)
					throw new ArgumentOutOfRangeException("price", price, LocalizedStrings.Str487);

				Remove((Sides)dir, price, volume, lastChangeTime);
			}
		}

		/// <summary>
		/// Удалить объем для заданной цены.
		/// </summary>
		/// <param name="direction">Направление заявки.</param>
		/// <param name="price">Цена, для которой необходимо удалить котировку.</param>
		/// <param name="volume">Объем, который нужно удалить. Если он не указан, значит удаляется вся котировка.</param>
		/// <param name="lastChangeTime">Время изменения стакана.</param>
		public void Remove(Sides direction, decimal price, decimal volume = 0, DateTimeOffset lastChangeTime = default(DateTimeOffset))
		{
			if (price <= 0)
				throw new ArgumentOutOfRangeException("price", price, LocalizedStrings.Str488);

			if (volume < 0)
				throw new ArgumentOutOfRangeException("volume", volume, LocalizedStrings.Str489);

			lock (_syncRoot)
			{
				var quotes = GetQuotesInternal(direction);
				var index = GetQuoteIndex(quotes, price);

				if (index == -1)
					throw new ArgumentOutOfRangeException("price", price, LocalizedStrings.Str487);

				var quote = quotes[index];

				decimal leftVolume;

				if (volume > 0)
				{
					if (quote.Volume < volume)
						throw new ArgumentOutOfRangeException("volume", volume, LocalizedStrings.Str490Params.Put(quote));

					leftVolume = quote.Volume - volume;

					if (UseAggregatedQuotes)
					{
						var aggQuote = quote as AggregatedQuote;

						if (aggQuote != null)
						{
							while (volume > 0)
							{
								var innerQuote = aggQuote.InnerQuotes.First();

								if (innerQuote.Volume > volume)
								{
									innerQuote.Volume -= volume;
									break;
								}
								else
								{
									aggQuote.InnerQuotes.Remove(innerQuote);
									volume -= innerQuote.Volume;
								}
							}
						}
					}
				}
				else
					leftVolume = 0;

				if (leftVolume == 0)
				{
					quotes = RemoveAt(quotes, index);

					if (quote.OrderDirection == Sides.Buy)
						Bids = quotes;
					else
						Asks = quotes;

					UpdateDepthAndTime(lastChangeTime);
				}
				else
				{
					quote.Volume = leftVolume;
					UpdateTime(lastChangeTime);
				}
			}

			RaiseQuotesChanged();
		}

		private static Quote[] RemoveAt(Quote[] quotes, int index)
		{
			var newQuotes = new Quote[quotes.Length - 1];

			if (index > 0)
				Array.Copy(quotes, 0, newQuotes, 0, index);

			if (index < (quotes.Length - 1))
				Array.Copy(quotes, index + 1, newQuotes, index, quotes.Length - index - 1);

			return newQuotes;
		}

		private static int GetQuoteIndex(Quote[] quotes, decimal price)
		{
			var stop = quotes.Length - 1;
			if (stop < 0)
				return -1;

			var first = quotes[0];

			var cmp = decimal.Compare(price, first.Price);
			if (cmp == 0)
				return 0;

			var last = quotes[stop];
			var desc = first.Price - last.Price > 0m;

			if (desc)
				cmp = -cmp;

			if (cmp < 0)
				return -1;

			cmp = decimal.Compare(price, last.Price);

			if (desc)
				cmp = -cmp;

			if (cmp > 0)
				return -1;

			if (cmp == 0)
				return stop;

			var start = 0;

			while (stop - start >= 0)
			{
				var mid = (start + stop) >> 1;

				cmp = decimal.Compare(price, quotes[mid].Price);

				if (desc)
					cmp = -cmp;
				if (cmp > 0)
					start = mid + 1;
				else if (cmp < 0)
					stop = mid - 1;
				else
					return mid;
			}

			return -1;
		}

		private Quote[] GetQuotesInternal(Sides direction)
		{
			return direction == Sides.Buy ? _bids : _asks;
		}

		private Quote[] GetQuotes(decimal price)
		{
			var dir = GetDirection(price);

			if (dir == null)
				return ArrayHelper<Quote>.EmptyArray;
			else
				return dir == Sides.Buy ? _bids : _asks;
		}

		private Sides? GetDirection(decimal price)
		{
			if (!ReferenceEquals(BestBid, null) && BestBid.Price >= price)
				return Sides.Buy;
			else if (!ReferenceEquals(BestAsk, null) && BestAsk.Price <= price)
				return Sides.Sell;
			else
				return null;
		}

		private void CheckQuote(Quote quote)
		{
			if (quote == null)
				throw new ArgumentNullException("quote");

			if (quote.Security != null && quote.Security != Security)
				throw new ArgumentException(LocalizedStrings.Str491Params.Put(quote.Security.Id, Security.Id), "quote");

			if (quote.Security == null)
				quote.Security = Security;

			if (quote.Price <= 0)
				throw new ArgumentOutOfRangeException("quote", quote.Price, LocalizedStrings.Str488);

			if (quote.Volume < 0)
				throw new ArgumentOutOfRangeException("quote", quote.Volume, LocalizedStrings.Str489);
		}

		private void UpdateDepthAndTime(DateTimeOffset lastChangeTime = default(DateTimeOffset), bool depthChangedEventNeeded = true)
		{
			if (depthChangedEventNeeded)
			{
				Depth = _bids.Length > _asks.Length ? _bids.Length : _asks.Length;
			}
			else
			{
				_depth = _bids.Length > _asks.Length ? _bids.Length : _asks.Length;
			}

			BestBid = _bids.Length > 0 ? _bids[0] : null;
			BestAsk = _asks.Length > 0 ? _asks[0] : null;

			UpdateTime(lastChangeTime);
		}

		private void UpdateTime(DateTimeOffset lastChangeTime)
		{
			if (lastChangeTime != default(DateTimeOffset))
			{
				LastChangeTime = lastChangeTime;
			}
		}

		private void RaiseQuotesChanged()
		{
			QuotesChanged.SafeInvoke();
		}

		/// <summary>
		/// Создать копию объекта <see cref="MarketDepth"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override MarketDepth Clone()
		{
			var clone = new MarketDepth(Security)
			{
				MaxDepth = MaxDepth,
				UseAggregatedQuotes = UseAggregatedQuotes,
				AutoVerify = AutoVerify,
			};

			lock (_syncRoot)
			{
				clone.Update(_bids.Select(q => q.Clone()), _asks.Select(q => q.Clone()), true, LastChangeTime);
				clone.LocalTime = LocalTime;
			}

			return clone;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return this.Select(q => q.ToString()).Join(Environment.NewLine);
		}

		/// <summary>
		/// Определить, правильное ли состояние содержит стакан.
		/// </summary>
		/// <remarks>
		/// Используется в случаях, когда торговая система в результате ошибки присылает неправильные котировки.
		/// </remarks>
		/// <returns><see langword="true"/>, если стакан содержит корректные данные, иначе, <see langword="false"/>.</returns>
		public bool Verify()
		{
			lock (_syncRoot)
				return Verify(_bids, _asks);
		}

		private bool Verify(IEnumerable<Quote> bids, IEnumerable<Quote> asks)
		{
			var bestBid = bids.FirstOrDefault();
			var bestAsk = asks.FirstOrDefault();

			if (bestBid != null && bestAsk != null)
			{
				return bids.All(b => b.Price < bestAsk.Price) && asks.All(a => a.Price > bestBid.Price) && Verify(bids, true) && Verify(asks, false);
			}
			else
			{
				return Verify(bids, true) && Verify(asks, false);
			}
		}

		private bool Verify(IEnumerable<Quote> quotes, bool isBids)
		{
			if (quotes.IsEmpty())
				return true;

			if (quotes.Any(q => !Verify(q, isBids)))
				return false;

			if (quotes.GroupBy(q => q.Price).Any(g => g.Count() > 1))
				return false;

			var prev = quotes.First();

			foreach (var current in quotes.Skip(1))
			{
				if (isBids)
				{
					if (current.Price > prev.Price)
						return false;
				}
				else
				{
					if (current.Price < prev.Price)
						return false;
				}

				prev = current;
			}

			return true;
		}

		private bool Verify(Quote quote, bool isBids)
		{
			if (quote == null)
				throw new ArgumentNullException("quote");

			return
				quote.Price > 0 &&
				quote.Volume > 0 &&
				quote.OrderDirection == (isBids ? Sides.Buy : Sides.Sell) &&
				quote.Security == Security;
		}

		SyncObject ISynchronizedCollection.SyncRoot
		{
			get { return _syncRoot; }
		}
	}
}