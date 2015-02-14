namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Xaml;
	using Ecng.Xaml.Grids;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Визуальный контрол, отображающий стакан с котировками (<see cref="MarketDepth"/>).
	/// </summary>
	public partial class MarketDepthControl
	{
		private readonly MarketDepthQuote.OrderRegistry _ordersRegistry = new MarketDepthQuote.OrderRegistry();
		private readonly MarketDepthQuote.OrderRegistry _stopOrdersRegistry = new MarketDepthQuote.OrderRegistry();
		private readonly ObservableCollection<MarketDepthQuote> _quotes = new ObservableCollection<MarketDepthQuote>();
		private readonly PairSet<MarketDepthColumns, DataGridColumn> _columnIndecies = new PairSet<MarketDepthColumns, DataGridColumn>();
		private readonly SyncObject _lastDepthSync = new SyncObject();
		private MarketDepth _lastDepth;
		private QuoteChangeMessage _lastQuoteMsg;
		private bool _needToClear;
		private Security _prevSecurity;

		/// <summary>
		/// Создать <see cref="MarketDepthControl"/>.
		/// </summary>
		public MarketDepthControl()
		{
			InitializeComponent();

			_columnIndecies.Add(MarketDepthColumns.OwnBuy, Columns[0]);
			_columnIndecies.Add(MarketDepthColumns.Buy, Columns[1]);
			_columnIndecies.Add(MarketDepthColumns.Price, Columns[2]);
			_columnIndecies.Add(MarketDepthColumns.Sell, Columns[3]);
			_columnIndecies.Add(MarketDepthColumns.OwnSell, Columns[4]);

			var rules = FormatRules;

			rules.Add(_columnIndecies[MarketDepthColumns.OwnBuy], new FormatRule
			{
				//Type = FormatRuleTypes.CellValue,
				Value = string.Empty,
				Condition = ComparisonOperator.NotEqual,
				Font = { Weight = FontWeights.Bold }
			});

			rules.Add(_columnIndecies[MarketDepthColumns.OwnSell], new FormatRule
			{
				//Type = FormatRuleTypes.CellValue,
				Value = string.Empty,
				Condition = ComparisonOperator.NotEqual,
				Font = { Weight = FontWeights.Bold }
			});

			rules.Add(_columnIndecies[MarketDepthColumns.Buy], new FormatRule
			{
				//Type = FormatRuleTypes.CellValue,
				Condition = ComparisonOperator.Any,
				Font = { Weight = FontWeights.Bold },
				Background = Brushes.LightBlue,
				Foreground = Brushes.DarkBlue,
			});

			rules.Add(_columnIndecies[MarketDepthColumns.Sell], new FormatRule
			{
				//Type = FormatRuleTypes.CellValue,
				Condition = ComparisonOperator.Any,
				Font = { Weight = FontWeights.Bold },
				Background = Brushes.LightPink,
				Foreground = Brushes.DarkRed,
			});

			rules.Add(_columnIndecies[MarketDepthColumns.Price], new FormatRule
			{
				//Type = FormatRuleTypes.CellValue,
				Condition = ComparisonOperator.Any,
				Background = Brushes.Beige,
				Foreground = Brushes.Black,
			});

			//rules.Add(_columnIndecies[MarketDepthColumns.Price], new FormatRule
			//{
			//    Type = FormatRuleTypes.PropertyValue,
			//    PropertyName = "IsBest",
			//    Value = true,
			//    Condition = ComparisonOperator.Equal,
			//    Font = new FontInfo { Weight = FontWeights.Bold },
			//});

			MaxDepth = DefaultDepth;

			ItemsSource = _quotes;

			GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(UpdateIfDepthDirty);
		}

		/// <summary>
		/// Максимальная глубина по-умолчанию, равная 20.
		/// </summary>
		public const int DefaultDepth = 20;

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="MaxDepth"/>.
		/// </summary>
		public static readonly DependencyProperty MaxDepthProperty =
			DependencyProperty.Register("MaxDepth", typeof(int), typeof(MarketDepthControl), new PropertyMetadata(
				(o, args) =>
				{
					var md = (MarketDepthControl)o;
					var newValue = (int)args.NewValue;
					var prevValue = md._maxDepth;

					if (newValue < 1 || newValue > 100)
						return;

					if (newValue == prevValue)
						return;

					var quotes = md._quotes;

					var delta = newValue - prevValue;

					if (delta > 0)
					{
						for (var y = 0; y < delta * 2; y++)
							quotes.Add(new MarketDepthQuote(md) { Quote = new Quote() });

						md._maxDepth = newValue;
					}
					else
					{
						md._maxDepth = newValue;

						delta = delta.Abs();

						for (var y = 0; y < delta * 2; y++)
							quotes.RemoveAt(quotes.Count - 1);
					}
				}));

		private int _maxDepth;

		/// <summary>
		/// Максимальная глубина отображения стакана. По-умолчанию равно <see cref="DefaultDepth"/>.
		/// </summary>
		public int MaxDepth
		{
			get { return _maxDepth; }
			set { SetValue(MaxDepthProperty, value); }
		}

		/// <summary>
		/// Показывать ли биды сверху. По-умолчанию выключено.
		/// </summary>
		public bool IsBidsOnTop { get; set; }

		/// <summary>
		/// Выбранная котировка.
		/// </summary>
		public Quote SelectedQuote
		{
			get
			{
				var cells = SelectedCells;
				return cells.IsEmpty() ? null : ((MarketDepthQuote)cells[0].Item).Quote;
			}
		}

		private string _priceTextFormat = "0.00";

		/// <summary>
		/// Формат цены. По умолчанию равен "0.00".
		/// </summary>
		public string PriceTextFormat
		{
			get { return _priceTextFormat; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_priceTextFormat = value;
			}
		}

		private string _volumeTextFormat = "0";

		/// <summary>
		/// Формат объема. По умолчанию равен "0".
		/// </summary>
		public string VolumeTextFormat
		{
			get { return _volumeTextFormat; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_volumeTextFormat = value;
			}
		}

		/// <summary>
		/// Обновить <see cref="PriceTextFormat"/> и <see cref="VolumeTextFormat"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		public void UpdateFormat(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			var priceDecFormat = new string('0', security.Decimals);
			PriceTextFormat = "0" + (priceDecFormat.Length == 0 ? string.Empty : "." + priceDecFormat);

			var volDecFormat = new string('0', security.VolumeStep.GetCachedDecimals());
			VolumeTextFormat = "0" + (volDecFormat.Length == 0 ? string.Empty : "." + volDecFormat);

			_prevSecurity = security;
		}

		/// <summary>
		/// Обработать новую заявку.
		/// </summary>
		/// <param name="order">Заявка.</param>
		public void ProcessNewOrder(Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			var registry = order.Type == OrderTypes.Conditional ? _stopOrdersRegistry : _ordersRegistry;
			registry.ProcessNewOrder(order);
		}

		/// <summary>
		/// Обработать изменившуюсю заявку.
		/// </summary>
		/// <param name="order">Заявка.</param>
		public void ProcessChangedOrder(Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			var registry = order.Type == OrderTypes.Conditional ? _stopOrdersRegistry : _ordersRegistry;
			registry.ProcessChangedOrder(order);
		}

		/// <summary>
		/// Обновить стакан.
		/// </summary>
		/// <param name="depth">Стакан.</param>
		public void UpdateDepth(MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			lock (_lastDepthSync)
				_lastDepth = depth;

			if (_prevSecurity == depth.Security)
				return;

			_prevSecurity = depth.Security;
			UpdateFormat(depth.Security);
		}

		/// <summary>
		/// Обновить стакан.
		/// </summary>
		/// <param name="message">Стакан.</param>
		public void UpdateDepth(QuoteChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var clone = new QuoteChangeMessage
			{
				Bids = message.Bids,
				Asks = message.Asks,
				IsSorted = message.IsSorted,
			};

			lock (_lastDepthSync)
				_lastQuoteMsg = clone;
		}

		/// <summary>
		/// Очистить стакан.
		/// </summary>
		public void Clear()
		{
			lock (_lastDepthSync)
				_needToClear = true;

			_prevSecurity = null;
		}

		private int GetQuoteIndex(Sides direction, int depthLevel)
		{
			var isBidsOnTop = IsBidsOnTop;

			if (direction == Sides.Buy)
				return isBidsOnTop ? MaxDepth - 1 - depthLevel : MaxDepth + depthLevel;
			else
				return isBidsOnTop ? MaxDepth + depthLevel : MaxDepth - 1 - depthLevel;
		}

		private void UpdateIfDepthDirty()
		{
			IEnumerable<MarketDepthPair> top = null;

			lock (_lastDepthSync)
			{
				if (_needToClear)
				{
					_needToClear = false;
					top = Enumerable.Empty<MarketDepthPair>();
				}
				else if (_lastDepth != null)
				{
					top = _lastDepth.GetTopPairs(MaxDepth);
				}
				else if (_lastQuoteMsg != null)
				{
					top = _lastQuoteMsg.ToMarketDepth(_prevSecurity).GetTopPairs(MaxDepth);
				}

				_lastDepth = null;
			}

			if (top == null)
				return;
			
			var index = 0;

			foreach (var pair in top)
			{
				var bid = _quotes[GetQuoteIndex(Sides.Buy, index)];
				if (pair.Bid != null)
				{
					bid.Init(pair.Bid, _ordersRegistry.GetContainer(pair.Bid.Price), _stopOrdersRegistry.GetContainer(pair.Bid.Price)/*, trades.TryGetValue(pair.Bid.Price), myTrades.TryGetValue(pair.Bid.Price)*/);
					bid.IsBest = index == 0;
				}
				else
					bid.Init();

				var ask = _quotes[GetQuoteIndex(Sides.Sell, index)];
				if (pair.Ask != null)
				{
					ask.Init(pair.Ask, _ordersRegistry.GetContainer(pair.Ask.Price), _stopOrdersRegistry.GetContainer(pair.Ask.Price)/*, trades.TryGetValue(pair.Ask.Price), myTrades.TryGetValue(pair.Ask.Price)*/);
					ask.IsBest = index == 0;
				}
				else
					ask.Init();

				index++;
			}

			for (var i = 0; i < (MaxDepth - index); i++)
			{
				_quotes[GetQuoteIndex(Sides.Buy, index + i)].Init();
				_quotes[GetQuoteIndex(Sides.Sell, index + i)].Init();
			}
		}

		/// <summary>
		/// Получить тип колонки по ячейке.
		/// </summary>
		/// <param name="cell">Ячейка.</param>
		/// <returns>Тип колонки.</returns>
		public MarketDepthColumns GetColumnIndex(DataGridCell cell)
		{
			if (cell == null)
				throw new ArgumentNullException("cell");

			return _columnIndecies[cell.Column];
		}

		///// <summary>
		///// Событие захвата ячейки.
		///// </summary>
		//public event Func<DataGridCell, bool> CanDrag
		//{
		//	add { Quotes.CanDrag += value; }
		//	remove { Quotes.CanDrag -= value; }
		//}

		///// <summary>
		///// Событие освобождения ячейки.
		///// </summary>
		//public event Func<DataGridCell, DataGridCell, bool> Dropping
		//{
		//	add { Quotes.Dropping += value; }
		//	remove { Quotes.Dropping -= value; }
		//}

		///// <summary>
		///// Событие нажатия левой кнопкой мышки.
		///// </summary>
		//public event Action<DataGridCell, MouseButtonEventArgs> CellMouseLeftButtonUp
		//{
		//	add { Quotes.CellMouseLeftButtonUp += value; }
		//	remove { Quotes.CellMouseLeftButtonUp -= value; }
		//}

		//public event Action<DataGridCell, MouseButtonEventArgs> CellMouseRightButtonUp
		//{
		//	add { Quotes.CellMouseRightButtonUp += value; }
		//	remove { Quotes.CellMouseRightButtonUp -= value; }
		//}
	}
}