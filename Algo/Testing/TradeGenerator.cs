namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Генератор тиковых сделок случайным методом.
	/// </summary>
	public abstract class TradeGenerator : MarketDataGenerator
	{
		/// <summary>
		/// Инициализировать <see cref="TradeGenerator"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо генерировать данные.</param>
		protected TradeGenerator(SecurityId securityId)
			: base(securityId)
		{
			IdGenerator = new IncrementalIdGenerator();
		}

		/// <summary>
		/// Генерировать значение для <see cref="Trade.OrderDirection"/>. По-умолчанию отключено.
		/// </summary>
		public bool GenerateDirection { get; set; }

		private IdGenerator _idGenerator;

		/// <summary>
		/// Генератор номера сделки <see cref="Trade.Id"/>.
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
	}

	/// <summary>
	/// Генератор сделок на основе нормального распределения.
	/// </summary>
	public class RandomWalkTradeGenerator : TradeGenerator
	{
		private decimal _lastTradePrice;

		/// <summary>
		/// Создать <see cref="RandomWalkTradeGenerator"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо генерировать данные.</param>
		public RandomWalkTradeGenerator(SecurityId securityId)
			: base(securityId)
		{
			Interval = TimeSpan.FromMilliseconds(50);
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
				case MessageTypes.Board:
					return null;
				case MessageTypes.Level1Change:
				{
					var l1Msg = (Level1ChangeMessage)message;

					var value = l1Msg.Changes.TryGetValue(Level1Fields.LastTradePrice);

					if (value != null)
						_lastTradePrice = (decimal)value;

					time = l1Msg.ServerTime;

					break;
				}
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
						case ExecutionTypes.Trade:
							_lastTradePrice = execMsg.TradePrice;
							break;
						case ExecutionTypes.OrderLog:
							if (execMsg.TradePrice != 0)
								_lastTradePrice = execMsg.TradePrice;
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

			var trade = new ExecutionMessage
			{
				SecurityId = SecurityId,
				TradeId = IdGenerator.GetNextId(),
				ServerTime = time,
				LocalTime = time.LocalDateTime,
				OriginSide = GenerateDirection ? RandomGen.GetEnum<Sides>() : (Sides?)null,
				Volume = Volumes.Next(),
				ExecutionType = ExecutionTypes.Tick
			};

			_lastTradePrice += RandomGen.GetInt(-MaxPriceStepCount, MaxPriceStepCount) * SecurityDefinition.PriceStep;

			if (_lastTradePrice <= 0)
				_lastTradePrice = SecurityDefinition.PriceStep;

			trade.TradePrice = _lastTradePrice;

			LastGenerationTime = time;

			return trade;
		}

		/// <summary>
		/// Создать копию генератора.
		/// </summary>
		/// <returns>Копия.</returns>
		public override MarketDataGenerator Clone()
		{
			return new RandomWalkTradeGenerator(SecurityId)
			{
				_lastTradePrice = _lastTradePrice,

				MaxVolume = MaxVolume,
				MinVolume = MinVolume,
				MaxPriceStepCount = MaxPriceStepCount,
				Interval = Interval,
				Volumes = Volumes,
				Steps = Steps,

				GenerateDirection = GenerateDirection,
				IdGenerator = IdGenerator
			};
		}
	}
}