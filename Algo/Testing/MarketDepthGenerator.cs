#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: MarketDepthGenerator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The order book generator using random method.
	/// </summary>
	public abstract class MarketDepthGenerator : MarketDataGenerator
	{
		/// <summary>
		/// Initialize <see cref="MarketDepthGenerator"/>.
		/// </summary>
		/// <param name="securityId">The identifier of the instrument, for which data shall be generated.</param>
		protected MarketDepthGenerator(SecurityId securityId)
			: base(securityId)
		{
		}

		/// <summary>
		/// Market data type.
		/// </summary>
		public override MarketDataTypes DataType => MarketDataTypes.MarketDepth;

		/// <summary>
		/// To use to generate best quotes in the order book volume of history trades.
		/// </summary>
		/// <remarks>
		/// The default value is <see langword="true" />.
		/// </remarks>
		public bool UseTradeVolume { get; set; } = true; // TODO

		private int _minSpreadStepCount = 1;

		/// <summary>
		/// The minimal value of spread between the best quotes in units of price increments number. The spread value will be selected randomly between <see cref="MarketDepthGenerator.MinSpreadStepCount"/> and <see cref="MarketDepthGenerator.MaxSpreadStepCount"/>.
		/// </summary>
		/// <remarks>
		/// The default value is 1.
		/// </remarks>
		public int MinSpreadStepCount
		{
			get => _minSpreadStepCount;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1137);

				_minSpreadStepCount = value;
			}
		}

		private int _maxSpreadStepCount = int.MaxValue;

		/// <summary>
		/// The maximal value of spread between the best quotes in units of price increments number. The spread value will be selected randomly between <see cref="MarketDepthGenerator.MinSpreadStepCount"/> and <see cref="MarketDepthGenerator.MaxSpreadStepCount"/>.
		/// </summary>
		/// <remarks>
		/// The default value is <see cref="Int32.MaxValue"/>.
		/// </remarks>
		public int MaxSpreadStepCount
		{
			get => _maxSpreadStepCount;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1138);

				_maxSpreadStepCount = value;
			}
		}

		private int _maxBidsDepth = 10;

		/// <summary>
		/// The maximal depth of bids.
		/// </summary>
		/// <remarks>
		/// The default value is 10.
		/// </remarks>
		public int MaxBidsDepth
		{
			get => _maxBidsDepth;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1139);

				_maxBidsDepth = value;
			}
		}

		private int _maxAsksDepth = 10;

		/// <summary>
		/// The maximal depth of offers.
		/// </summary>
		/// <remarks>
		/// The default value is 10.
		/// </remarks>
		public int MaxAsksDepth
		{
			get => _maxAsksDepth;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1140);

				_maxAsksDepth = value;
			}
		}

		/// <summary>
		/// Shall order books be generated after each trade. The default is <see langword="false" />.
		/// </summary>
		public bool GenerateDepthOnEachTrade { get; set; }

		private int _maxGenerations = 20;

		/// <summary>
		/// The maximal number of generations after last occurrence of source data for the order book.
		/// </summary>
		/// <remarks>
		/// The default value equals 20.
		/// </remarks>
		public int MaxGenerations
		{
			get => _maxGenerations;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1141);

				_maxGenerations = value;
			}
		}

		/// <summary>
		/// To create the quote using random method.
		/// </summary>
		/// <param name="startPrice">The initial price, based on which a quote price shall be got using random method.</param>
		/// <param name="side">The quote direction.</param>
		/// <returns>The random quote.</returns>
		protected QuoteChange CreateQuote(decimal startPrice, Sides side)
		{
			var priceStep = SecurityDefinition.PriceStep ?? 0.01m;

			var price = startPrice + (side == Sides.Sell ? 1 : -1) * Steps.Next() * priceStep;

			if (price <= 0)
				price = priceStep;

			return new QuoteChange(side, price, Volumes.Next());
		}
	}

	/// <summary>
	/// The order book generator, accounting for trades sequence.
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
		/// Initializes a new instance of the <see cref="TrendMarketDepthGenerator"/>.
		/// </summary>
		/// <param name="securityId">The identifier of the instrument, for which data shall be generated.</param>
		public TrendMarketDepthGenerator(SecurityId securityId)
			: base(securityId)
		{
		}

		/// <summary>
		/// To initialize the generator state.
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
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>The result of processing. If <see langword="null" /> is returned, then generator has no sufficient data to generate new message.</returns>
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

			var isTradeTime = _boardDefinition.IsTradeTime(time);

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
				LocalTime = time,
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

			var bids = new List<QuoteChange>();
			//{
			//	new QuoteChange(Sides.Buy, bidPrice.Value, Volumes.Next())
			//};

			var count = MaxBidsDepth/* - bids.Count*/;

			for (var i = 0; i < count; i++)
			{
				var quote = CreateQuote(bidPrice.Value, Sides.Buy);

				if (quote.Price <= 0)
					break;

				bids.Add(quote);
				bidPrice = quote.Price;
			}

			var asks = new List<QuoteChange>();
			//{
			//	new QuoteChange(Sides.Sell, askPrice.Value, Volumes.Next())
			//};

			count = MaxAsksDepth/* - asks.Count*/;

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
		/// Create a copy of <see cref="TrendMarketDepthGenerator"/>.
		/// </summary>
		/// <returns>Copy.</returns>
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