namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Генератор лога заявок случайным методом.
	/// </summary>
	public class OrderLogGenerator : MarketDataGenerator
	{
		private decimal _lastOrderPrice;
		private readonly SynchronizedQueue<ExecutionMessage> _activeOrders = new SynchronizedQueue<ExecutionMessage>(); 

		/// <summary>
		/// Создать <see cref="OrderLogGenerator"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо генерировать данные.</param>
		public OrderLogGenerator(SecurityId securityId)
			: this(securityId, new RandomWalkTradeGenerator(securityId))
		{
		}

		/// <summary>
		/// Создать <see cref="OrderLogGenerator"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо генерировать данные.</param>
		/// <param name="tradeGenerator">Генератор тиковых сделок случайным методом.</param>
		public OrderLogGenerator(SecurityId securityId, TradeGenerator tradeGenerator)
			: base(securityId)
		{
			if (tradeGenerator == null)
				throw new ArgumentNullException("tradeGenerator");

			//_lastOrderPrice = startPrice;

			TradeGenerator = tradeGenerator;
			IdGenerator = new IncrementalIdGenerator();
		}

		/// <summary>
		/// Генератор тиковых сделок случайным методом.
		/// </summary>
		public TradeGenerator TradeGenerator { get; private set; }

		private IdGenerator _idGenerator;

		/// <summary>
		/// Генератор номера заявки <see cref="Order.Id"/>.
		/// </summary>
		public IdGenerator IdGenerator
		{
			get { return _idGenerator; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_idGenerator = value;
			}
		}

		/// <summary>
		/// Инициализировать состояние генератора.
		/// </summary>
		public override void Init()
		{
			TradeGenerator.Init();
			base.Init();
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <returns>Результат обработки. Если будет возрвщено <see langword="null"/>,
		/// то генератору пока недостаточно данных для генерации нового сообщения.</returns>
		public override Message Process(Message message)
		{
			if (message.Type == MessageTypes.Security)
				TradeGenerator.Process(message);

			return base.Process(message);
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <returns>Результат обработки. Если будет возрвщено <see langword="null"/>,
		/// то генератору пока недостаточно данных для генерации нового сообщения.</returns>
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
							_lastOrderPrice = execMsg.TradePrice;
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
				_lastOrderPrice += RandomGen.GetInt(-MaxPriceStepCount, MaxPriceStepCount) * SecurityDefinition.PriceStep;

				if (_lastOrderPrice <= 0)
					_lastOrderPrice = SecurityDefinition.PriceStep;

				item = new ExecutionMessage
				{
					OrderId = IdGenerator.GetNextId(),
					SecurityId = SecurityId,
					ServerTime = time,
					OrderState = OrderStates.Active,
					Volume = Volumes.Next(),
					Side = RandomGen.GetEnum<Sides>(),
					Price = _lastOrderPrice,
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
					item.Volume = RandomGen.GetInt(1, (int)activeOrder.Volume);

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

					activeOrder.Volume -= item.Volume;

					if (activeOrder.Volume == 0)
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
		/// Создать копию генератора.
		/// </summary>
		/// <returns>Копия.</returns>
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