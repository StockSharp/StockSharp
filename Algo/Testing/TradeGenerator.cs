namespace StockSharp.Algo.Testing;

/// <summary>
/// Tick trades generator using random method.
/// </summary>
public abstract class TradeGenerator : MarketDataGenerator
{
	/// <summary>
	/// Initialize <see cref="TradeGenerator"/>.
	/// </summary>
	/// <param name="securityId">The identifier of the instrument, for which data shall be generated.</param>
	protected TradeGenerator(SecurityId securityId)
		: base(securityId)
	{
		IdGenerator = new IncrementalIdGenerator();
	}

	/// <inheritdoc />
	public override DataType DataType => DataType.Ticks;

	private IdGenerator _idGenerator;

	/// <summary>
	/// The trade identifier generator <see cref="Trade.Id"/>.
	/// </summary>
	public IdGenerator IdGenerator
	{
		get => _idGenerator;
		set => _idGenerator = value ?? throw new ArgumentNullException(nameof(value));
	}
}

/// <summary>
/// The trade generator based on normal distribution.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RandomWalkTradeGenerator"/>.
/// </remarks>
/// <param name="securityId">The identifier of the instrument, for which data shall be generated.</param>
public class RandomWalkTradeGenerator(SecurityId securityId) : TradeGenerator(securityId)
{
	private decimal _lastTradePrice;

	/// <inheritdoc />
	public override void Init()
	{
		base.Init();

		_lastTradePrice = default;
	}

	/// <summary>
	/// To generate the value for <see cref="ExecutionMessage.OriginSide"/>. By default is disabled.
	/// </summary>
	public bool GenerateOriginSide { get; set; }

	/// <inheritdoc />
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

				var value = l1Msg.TryGetDecimal(Level1Fields.LastTradePrice);

				if (value != null)
					_lastTradePrice = value.Value;

				time = l1Msg.ServerTime;

				break;
			}
			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				var price = execMsg.TradePrice;

				if (price != null)
					_lastTradePrice = price.Value;
				else if (execMsg.DataType != DataType.OrderLog)
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

		var v = Volumes.Next();
		if(v == 0)
			v = 1;

		var trade = new ExecutionMessage
		{
			SecurityId = SecurityId,
			TradeId = IdGenerator.GetNextId(),
			ServerTime = time,
			LocalTime = time,
			OriginSide = GenerateOriginSide ? RandomGen.GetEnum<Sides>() : null,
			TradeVolume = v * (SecurityDefinition?.VolumeStep ?? 1m),
			DataTypeEx = DataType.Ticks
		};

		var priceStep = SecurityDefinition.PriceStep ?? 0.01m;

		_lastTradePrice += RandomGen.GetInt(-MaxPriceStepCount, MaxPriceStepCount) * priceStep;

		if (_lastTradePrice <= 0)
			_lastTradePrice = priceStep;

		trade.TradePrice = _lastTradePrice;

		LastGenerationTime = time;

		return trade;
	}

	/// <summary>
	/// Create a copy of <see cref="RandomWalkTradeGenerator"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override MarketDataGenerator Clone()
	{
		var clone = new RandomWalkTradeGenerator(SecurityId)
		{
			GenerateOriginSide = GenerateOriginSide,
			IdGenerator = IdGenerator
		};

		CopyTo(clone);

		return clone;
	}
}