#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: OrderLogGenerator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The orders log generator using random method.
	/// </summary>
	public class OrderLogGenerator : MarketDataGenerator
	{
		private decimal _lastOrderPrice;
		private readonly SynchronizedQueue<ExecutionMessage> _activeOrders = new SynchronizedQueue<ExecutionMessage>(); 

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderLogGenerator"/>.
		/// </summary>
		/// <param name="securityId">The identifier of the instrument, for which data shall be generated.</param>
		public OrderLogGenerator(SecurityId securityId)
			: this(securityId, new RandomWalkTradeGenerator(securityId))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderLogGenerator"/>.
		/// </summary>
		/// <param name="securityId">The identifier of the instrument, for which data shall be generated.</param>
		/// <param name="tradeGenerator">Tick trades generator using random method.</param>
		public OrderLogGenerator(SecurityId securityId, TradeGenerator tradeGenerator)
			: base(securityId)
		{
			//_lastOrderPrice = startPrice;

			TradeGenerator = tradeGenerator ?? throw new ArgumentNullException(nameof(tradeGenerator));
			IdGenerator = new IncrementalIdGenerator();
		}

		/// <summary>
		/// Market data type.
		/// </summary>
		public override MarketDataTypes DataType => MarketDataTypes.OrderLog;

		/// <summary>
		/// Tick trades generator using random method.
		/// </summary>
		public TradeGenerator TradeGenerator { get; }

		private IdGenerator _idGenerator;

		/// <summary>
		/// The order identifier generator <see cref="Order.Id"/>.
		/// </summary>
		public IdGenerator IdGenerator
		{
			get => _idGenerator;
			set => _idGenerator = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// To initialize the generator state.
		/// </summary>
		public override void Init()
		{
			TradeGenerator.Init();
			base.Init();
		}

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>The result of processing. If <see langword="null" /> is returned, then generator has no sufficient data to generate new message.</returns>
		public override Message Process(Message message)
		{
			if (message.Type == MessageTypes.Security)
				TradeGenerator.Process(message);

			return base.Process(message);
		}

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>The result of processing. If <see langword="null" /> is returned, then generator has no sufficient data to generate new message.</returns>
		protected override Message OnProcess(Message message)
		{
			DateTimeOffset time;

			switch (message.Type)
			{
				case MessageTypes.Level1Change:
				{
					var l1Msg = (Level1ChangeMessage)message;

					var value = l1Msg.Changes.TryGetValue(Level1Fields.LastTradePrice);

					if (value != null)
						_lastOrderPrice = (decimal)value;

					TradeGenerator.Process(message);

					time = l1Msg.ServerTime;
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
							_lastOrderPrice = execMsg.GetTradePrice();
							break;
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

			if (!IsTimeToGenerate(time))
				return null;

			// TODO более реалистичную генерацию, так как сейчас объемы, цены и сделки c потолка

			var action = RandomGen.GetInt(0, 5);

			var isNew = action < 3 || _activeOrders.IsEmpty();

			ExecutionMessage item;

			if (isNew)
			{
				var priceStep = SecurityDefinition.PriceStep ?? 0.01m;

				_lastOrderPrice += RandomGen.GetInt(-MaxPriceStepCount, MaxPriceStepCount) * priceStep;

				if (_lastOrderPrice <= 0)
					_lastOrderPrice = priceStep;

				item = new ExecutionMessage
				{
					OrderId = IdGenerator.GetNextId(),
					SecurityId = SecurityId,
					ServerTime = time,
					OrderState = OrderStates.Active,
					OrderVolume = Volumes.Next(),
					Side = RandomGen.GetEnum<Sides>(),
					OrderPrice = _lastOrderPrice,
					ExecutionType = ExecutionTypes.OrderLog,
				};

				_activeOrders.Enqueue((ExecutionMessage)item.Clone());
			}
			else
			{
				var activeOrder = _activeOrders.Peek();

				item = (ExecutionMessage)activeOrder.Clone();
				item.ServerTime = time;

				var isMatched = action == 5;

				ExecutionMessage trade = null;

				if (isMatched)
					trade = (ExecutionMessage)TradeGenerator.Process(message);

				if (isMatched && trade != null)
				{
					item.OrderVolume = RandomGen.GetInt(1, (int)activeOrder.SafeGetVolume());

					item.TradeId = trade.TradeId;
					item.TradePrice = trade.TradePrice;
					item.TradeStatus = trade.TradeStatus;

					// TODO
					//quote.Trade = TradeGenerator.Generate(time);
					//item.Volume = activeOrder.Volume;

					//if (item.Side == Sides.Buy && quote.Trade.Price > quote.Order.Price)
					//	item.TradePrice = item.Price;
					//else if (item.Side == Sides.Sell && quote.Trade.Price < quote.Order.Price)
					//	item.TradePrice = item.Price;

					activeOrder.OrderVolume -= item.OrderVolume;

					if (activeOrder.OrderVolume == 0)
					{
						item.OrderState = OrderStates.Done;
						_activeOrders.Dequeue();
					}
					else
						item.OrderState = OrderStates.Active;
				}
				else
				{
					item.OrderState = OrderStates.Done;
					item.IsCancelled = true;
					_activeOrders.Dequeue();
				}
			}

			LastGenerationTime = time;

			return item;
		}

		/// <summary>
		/// Create a copy of <see cref="MarketDataGenerator"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override MarketDataGenerator Clone()
		{
			return new OrderLogGenerator(SecurityId, (TradeGenerator)TradeGenerator.Clone())
			{
				_lastOrderPrice = _lastOrderPrice,
				IdGenerator = IdGenerator
			};
		}
	}
}