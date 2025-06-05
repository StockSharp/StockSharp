namespace StockSharp.Algo.Testing;

/// <summary>
/// The orders log generator using random method.
/// </summary>
public class OrderLogGenerator : MarketDataGenerator
{
	private decimal _lastOrderPrice;
	private readonly SynchronizedQueue<ExecutionMessage> _activeOrders = [];

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

	/// <inheritdoc />
	public override DataType DataType => DataType.OrderLog;

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
		base.Init();

		_lastOrderPrice = default;
		TradeGenerator.Init();
	}

	/// <inheritdoc />
	public override Message Process(Message message)
	{
		if (message.Type == MessageTypes.Security)
			TradeGenerator.Process(message);

		return base.Process(message);
	}

	/// <inheritdoc />
	protected override Message OnProcess(Message message)
	{
		DateTimeOffset time;

		switch (message.Type)
		{
			case MessageTypes.Level1Change:
			{
				var l1Msg = (Level1ChangeMessage)message;

				var value = l1Msg.TryGetDecimal(Level1Fields.LastTradePrice);

				if (value != null)
					_lastOrderPrice = value.Value;

				TradeGenerator.Process(message);

				time = l1Msg.ServerTime;
				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.DataType == DataType.Ticks)
					_lastOrderPrice = execMsg.GetTradePrice();
				else
					return null;

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

			var v = Volumes.Next();
			if(v == 0)
				v = 1;

			item = new ExecutionMessage
			{
				OrderId = IdGenerator.GetNextId(),
				SecurityId = SecurityId,
				ServerTime = time,
				OrderState = OrderStates.Active,
				OrderVolume = v * (SecurityDefinition.VolumeStep ?? 1m),
				Side = RandomGen.GetEnum<Sides>(),
				OrderPrice = _lastOrderPrice,
				DataTypeEx = DataType.OrderLog,
			};

			_activeOrders.Enqueue(item.TypedClone());
		}
		else
		{
			var activeOrder = _activeOrders.Peek();

			ExecutionMessage trade = null;

			var isMatched = action == 5;
			if (isMatched)
				trade = (ExecutionMessage)TradeGenerator.Process(message);

			if (trade != null)
			{
				item = activeOrder.TypedClone();
				item.ServerTime = time;

				item.TradeVolume = RandomGen.GetInt(1, (int)activeOrder.SafeGetVolume());

				item.TradeId = trade.TradeId;
				item.TradePrice = trade.TradePrice;
				item.TradeStatus = trade.TradeStatus;

				activeOrder.OrderVolume -= item.TradeVolume;

				if (activeOrder.OrderVolume <= 0)
				{
					if (activeOrder.OrderVolume < 0)
						item.TradeVolume += activeOrder.OrderVolume;

					item.OrderState = OrderStates.Done;
					_activeOrders.Dequeue();
				}
				else
					item.OrderState = OrderStates.Active;
			}
			else
				item = null;
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
		var clone = new OrderLogGenerator(SecurityId, TradeGenerator.TypedClone())
		{
			_lastOrderPrice = _lastOrderPrice,
			IdGenerator = IdGenerator
		};

		CopyTo(clone);

		return clone;
	}
}