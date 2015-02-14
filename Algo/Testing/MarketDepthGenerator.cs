namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Генератор стакана случайным методом.
	/// </summary>
	public abstract class MarketDepthGenerator : MarketDataGenerator
	{
		/// <summary>
		/// Инициализировать <see cref="MarketDepthGenerator"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо генерировать данные.</param>
		protected MarketDepthGenerator(SecurityId securityId)
			: base(securityId)
		{
			UseTradeVolume = true;
			MinSpreadStepCount = 1;
			MaxSpreadStepCount = int.MaxValue;
			MaxBidsDepth = 10;
			MaxAsksDepth = 10;
			MaxGenerations = 20;
		}

		/// <summary>
		/// Использовать для генерации лучших котировок в стакане объем исторических сделок.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию true.
		/// </remarks>
		public bool UseTradeVolume { get; set; } // TODO

		private int _minSpreadStepCount;

		/// <summary>
		/// Минимальная величина спреда между лучшими котировками в единицах числа шагов цены.
		/// Величина спреда будет выбрана случайно между <see cref="MinSpreadStepCount"/> и <see cref="MaxSpreadStepCount"/>.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию 1.
		/// </remarks>
		public int MinSpreadStepCount
		{
			get { return _minSpreadStepCount; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1137);

				_minSpreadStepCount = value;
			}
		}

		private int _maxSpreadStepCount;

		/// <summary>
		/// Максимальная величина спреда между лучшими котировками в единицах числа шагов цены.
		/// Величина спреда будет выбрана случайно между <see cref="MinSpreadStepCount"/> и <see cref="MaxSpreadStepCount"/>.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию <see cref="int.MaxValue"/>.
		/// </remarks>
		public int MaxSpreadStepCount
		{
			get { return _maxSpreadStepCount; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1138);

				_maxSpreadStepCount = value;
			}
		}

		private int _maxBidsDepth;

		/// <summary>
		/// Максимальная глубина бидов.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию равно 1.
		/// </remarks>
		public int MaxBidsDepth
		{
			get { return _maxBidsDepth; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1139);

				_maxBidsDepth = value;
			}
		}

		private int _maxAsksDepth;

		/// <summary>
		/// Максимальная глубина офферов.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию равно 1.
		/// </remarks>
		public int MaxAsksDepth
		{
			get { return _maxAsksDepth; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1140);

				_maxAsksDepth = value;
			}
		}

		/// <summary>
		/// Генерировать ли стаканы после каждой сделки. По умолчанию false.
		/// </summary>
		public bool GenerateDepthOnEachTrade { get; set; }

		private int _maxGenerations;

		/// <summary>
		/// Максимальное количество генераций после последнего поступления исходных данных для стакана.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию равно 20.
		/// </remarks>
		public int MaxGenerations
		{
			get { return _maxGenerations; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1141);

				_maxGenerations = value;
			}
		}

		/// <summary>
		/// Создать случайным методом котировку.
		/// </summary>
		/// <param name="startPrice">Начальная цена, от которой случайным методом необходимо получить цену котировки.</param>
		/// <param name="side">Направление котировки.</param>
		/// <returns>Случайная котировка.</returns>
		protected QuoteChange CreateQuote(decimal startPrice, Sides side)
		{
			var price = startPrice + (side == Sides.Sell ? 1 : -1) * Steps.Next() * SecurityDefinition.PriceStep;

			if (price <= 0)
				price = SecurityDefinition.PriceStep;

			return new QuoteChange(side, price, Volumes.Next());
		}
	}

	/// <summary>
	/// Генератор стаканов, учитывающий последовательность сделок.
	/// </summary>
	public class TrendMarketDepthGenerator : MarketDepthGenerator
	{
		private bool _newTrades;

		// не генерировать стаканы, если у нас давно не было сделок
		private int _currGenerations;

		private decimal? _lastTradePrice;
		private decimal? _prevTradePrice;
		private decimal? _bestAskPrice;
		private decimal? _bestBidPrice;

		private BoardMessage _boardDefinition;

		/// <summary>
		/// Создать <see cref="TrendMarketDepthGenerator"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо генерировать данные.</param>
		public TrendMarketDepthGenerator(SecurityId securityId)
			: base(securityId)
		{
			Interval = TimeSpan.FromMilliseconds(50);
		}

		/// <summary>
		/// Инициализировать состояние генератора.
		/// </summary>
		public override void Init()
		{
			base.Init();

			_lastTradePrice = null;
			_prevTradePrice = null;
			_bestAskPrice = null;
			_bestBidPrice = null;

			_newTrades = false;
			_currGenerations = MaxGenerations;

			_boardDefinition = null;
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <returns>Результат обработки. Если будет возрвщено <see langword="null"/>,
		/// то генератору пока недостаточно данных для генерации нового сообщения.</returns>
		protected override Message OnProcess(Message message)
		{
			if (_boardDefinition == null)
			{
				if (message.Type == MessageTypes.Board)
					_boardDefinition = (BoardMessage)message.Clone();
				
				return null;
			}

			DateTimeOffset time;

			switch (message.Type)
			{
				case MessageTypes.Level1Change:
				{
					var l1Msg = (Level1ChangeMessage)message;

					var value = l1Msg.Changes.TryGetValue(Level1Fields.LastTradePrice);

					if (value != null)
						_lastTradePrice = (decimal)value;

					value = l1Msg.Changes.TryGetValue(Level1Fields.BestBidPrice);

					if (value != null)
						_bestBidPrice = (decimal)value;

					value = l1Msg.Changes.TryGetValue(Level1Fields.BestAskPrice);

					if (value != null)
						_bestAskPrice = (decimal)value;

					time = l1Msg.ServerTime;

					break;
				}
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
						{
							var tradePrice = execMsg.TradePrice;

							if (null == _prevTradePrice)
							{
								_prevTradePrice = tradePrice;
								_bestAskPrice = tradePrice;
								_bestBidPrice = tradePrice;
							}

							switch (execMsg.OriginSide)
							{
								case null:
								{
									if (tradePrice > _prevTradePrice)
									{
										_bestAskPrice = tradePrice;
										//BestBid = PrevTrade;
										_prevTradePrice = tradePrice;
									}
									else if (tradePrice < _prevTradePrice)
									{
										_bestBidPrice = tradePrice;
										//BestAsk = PrevTrade;
										_prevTradePrice = tradePrice;
									}

									break;
								}
								case Sides.Buy:
									_bestAskPrice = tradePrice;
									break;
								default:
									_bestBidPrice = tradePrice;
									break;
							}

							_lastTradePrice = tradePrice;
							_newTrades = true;

							break;
						}
						default:
							return null;
					}

					time = execMsg.ServerTime;

					break;
				}
				case MessageTypes.Time:
				{
					var timeMsg = (TimeMessage)message;

					time = timeMsg.ServerTime;

					break;
				}
				default:
					return null;
			}

			if (_currGenerations == 0 || _bestBidPrice == null || _bestAskPrice == null)
				return null;

			var isTradeTime = _boardDefinition.WorkingTime.IsTradeTime(message.LocalTime);

			var canProcess = GenerateDepthOnEachTrade && _newTrades
				? isTradeTime
				: (IsTimeToGenerate(time) && isTradeTime);

			if (!canProcess)
				return null;

			_currGenerations = MaxGenerations;

			var depth = new QuoteChangeMessage
			{
				SecurityId = SecurityId,
				ServerTime = time,
				LocalTime = time.LocalDateTime,
			};

			if (_bestBidPrice == null || _bestAskPrice == null)
			{
				if (_lastTradePrice == null)
					throw new InvalidOperationException(LocalizedStrings.Str1142);

				_bestBidPrice = _bestAskPrice = _lastTradePrice;
			}

			if (_currGenerations == 0)
				throw new InvalidOperationException(LocalizedStrings.Str1143);

			var bidPrice = _bestBidPrice;
			var askPrice = _bestAskPrice;

			var minSpred = MinSpreadStepCount * SecurityDefinition.PriceStep;
			var maxStread = MaxSpreadStepCount * SecurityDefinition.PriceStep;

			if ((askPrice - bidPrice) < minSpred)
			{
				if (_bestBidPrice == _lastTradePrice) // up trend
					askPrice = bidPrice + minSpred;
				else
					bidPrice = askPrice - minSpred;
			}
			else if ((askPrice - bidPrice) > maxStread)
			{
				if (_bestBidPrice == _lastTradePrice) // down trend
					askPrice = bidPrice + maxStread;
				else
					bidPrice = askPrice - maxStread;
			}

			var bids = new List<QuoteChange>
			{
				new QuoteChange(Sides.Buy, bidPrice.Value, Volumes.Next())
			};

			var count = MaxBidsDepth - bids.Count;

			for (var i = 0; i < count; i++)
			{
				var quote = CreateQuote(bidPrice.Value, Sides.Buy);

				if (quote.Price <= 0)
					break;

				bids.Add(quote);
				bidPrice = quote.Price;
			}

			var asks = new List<QuoteChange>
			{
				new QuoteChange(Sides.Sell, askPrice.Value, Volumes.Next())
			};

			count = MaxAsksDepth - asks.Count;

			for (var i = 0; i < count; i++)
			{
				var quote = CreateQuote(askPrice.Value, Sides.Sell);

				if (quote.Price <= 0)
					break;

				asks.Add(quote);
				askPrice = quote.Price;
			}

			depth.Bids = bids;
			depth.Asks = asks;

			_newTrades = false;

			_currGenerations--;

			return depth;
		}

		//private static bool IsTickMessage(Message message)
		//{
		//	if (message.Type != MessageTypes.Execution)
		//		return false;

		//	var tradeMessage = (ExecutionMessage)message;

		//	return tradeMessage.ExecutionType == ExecutionTypes.Tick;
		//}
		
		/// <summary>
		/// Создать копию генератора.
		/// </summary>
		/// <returns>Копия.</returns>
		public override MarketDataGenerator Clone()
		{
			return new TrendMarketDepthGenerator(SecurityId)
			{
				MaxVolume = MaxVolume,
				MinVolume = MinVolume,
				MaxPriceStepCount = MaxPriceStepCount,
				Interval = Interval,
				Volumes = Volumes,
				Steps = Steps,

				UseTradeVolume = UseTradeVolume,
				MinSpreadStepCount = MinSpreadStepCount,
				MaxSpreadStepCount = MaxSpreadStepCount,
				MaxBidsDepth = MaxBidsDepth,
				MaxAsksDepth = MaxAsksDepth,
				MaxGenerations = MaxGenerations,
			};
		}
	}
}