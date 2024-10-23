namespace StockSharp.Algo;

/// <summary>
/// Base basket securities processor.
/// </summary>
/// <typeparam name="TBasketSecurity">Basket security type.</typeparam>
public abstract class BasketSecurityBaseProcessor<TBasketSecurity> : IBasketSecurityProcessor
	where TBasketSecurity : BasketSecurity, new()
{
	private readonly CachedSynchronizedSet<SecurityId> _basketLegs = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="BasketSecurityBaseProcessor{TBasketSecurity}"/>.
	/// </summary>
	/// <param name="security">Security.</param>
	protected BasketSecurityBaseProcessor(Security security)
	{
		Security = security ?? throw new ArgumentNullException(nameof(security));
		SecurityId = security.ToSecurityId();
		BasketExpression = security.BasketExpression;

		BasketSecurity = Security.ToBasket<TBasketSecurity>();

		if (BasketSecurity.InnerSecurityIds.IsEmpty())
			throw new ArgumentException(LocalizedStrings.SecurityDoNotContainsLegs.Put(BasketExpression), nameof(security));

		_basketLegs.AddRange(BasketSecurity.InnerSecurityIds);
	}

	/// <summary>
	/// Security.
	/// </summary>
	public Security Security { get; }

	/// <summary>
	/// Instruments basket.
	/// </summary>
	public TBasketSecurity BasketSecurity { get; }

	/// <inheritdoc />
	public SecurityId SecurityId { get; }

	/// <inheritdoc />
	public string BasketExpression { get; }

	/// <inheritdoc />
	public SecurityId[] BasketLegs => _basketLegs.Cache;

	/// <inheritdoc />
	public abstract IEnumerable<Message> Process(Message message);

	/// <summary>
	/// Whether contains the specified leg.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <returns><see langword="true"/> if the leg exist, otherwise <see langword="false"/>.</returns>
	protected bool ContainsLeg(SecurityId securityId) => _basketLegs.Contains(securityId);
}

/// <summary>
/// Base continuous securities processor.
/// </summary>
/// <typeparam name="TBasketSecurity">Basket security type.</typeparam>
public abstract class ContinuousSecurityBaseProcessor<TBasketSecurity> : BasketSecurityBaseProcessor<TBasketSecurity>
	where TBasketSecurity : ContinuousSecurity, new()
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ContinuousSecurityBaseProcessor{TBasketSecurity}"/>.
	/// </summary>
	/// <param name="security">Security.</param>
	protected ContinuousSecurityBaseProcessor(Security security)
		: base(security)
	{
	}

	/// <inheritdoc />
	public override IEnumerable<Message> Process(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.QuoteChange:
			{
				var quoteMsg = (QuoteChangeMessage)message;

				if (quoteMsg.State != null || !ContainsLeg(quoteMsg.SecurityId))
					yield break;

				var bestBid = quoteMsg.GetBestBid();
				var bestAsk = quoteMsg.GetBestAsk();

				var volume = bestBid?.Volume;

				if (bestAsk?.Volume != null)
					volume = volume ?? 0 + bestAsk?.Volume;

				if (!CanProcess(quoteMsg.SecurityId, quoteMsg.ServerTime, (bestBid?.Price).GetSpreadMiddle(bestAsk?.Price, Security.PriceStep), volume, null))
					yield break;

				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (!ContainsLeg(execMsg.SecurityId))
					yield break;

				if (execMsg.DataType == DataType.Ticks)
				{
					if (!CanProcess(execMsg.SecurityId, execMsg.ServerTime, execMsg.TradePrice, execMsg.TradeVolume, execMsg.OpenInterest))
						yield break;
				}
				else if (execMsg.DataType == DataType.OrderLog)
				{
					if (!CanProcess(execMsg.SecurityId, execMsg.ServerTime, execMsg.OrderPrice, execMsg.OrderVolume, execMsg.OpenInterest))
						yield break;
				}

				break;
			}

			default:
			{
				if (message is CandleMessage candleMsg)
				{
					if (!ContainsLeg(candleMsg.SecurityId))
						yield break;

					if (!CanProcess(candleMsg.SecurityId, candleMsg.OpenTime, candleMsg.ClosePrice, candleMsg.TotalVolume, candleMsg.OpenInterest))
						yield break;

					break;
				}

				throw new ArgumentOutOfRangeException(nameof(message), LocalizedStrings.UnknownType.Put(message.Type));
			}
		}

		yield return message.Clone().ReplaceSecurityId(SecurityId);
	}

	/// <summary>
	/// Determines can process message.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="serverTime">Change server time.</param>
	/// <param name="price">Price.</param>
	/// <param name="volume">Volume.</param>
	/// <param name="openInterest">Number of open positions (open interest).</param>
	/// <returns><see langword="true"/> if the specified message can be processed, otherwise, <see langword="false"/>.</returns>
	protected abstract bool CanProcess(SecurityId securityId, DateTimeOffset serverTime, decimal? price, decimal? volume, decimal? openInterest);
}

/// <summary>
/// Continuous securities processor for <see cref="ContinuousSecurity"/>.
/// </summary>
public class ContinuousSecurityExpirationProcessor : ContinuousSecurityBaseProcessor<ExpirationContinuousSecurity>
{
	private SecurityId _currId;
	private DateTimeOffset _expirationDate;

	private bool _finished;

	/// <summary>
	/// Initializes a new instance of the <see cref="ContinuousSecurityExpirationProcessor"/>.
	/// </summary>
	/// <param name="security">Security.</param>
	public ContinuousSecurityExpirationProcessor(Security security)
		: base(security)
	{
		_currId = BasketSecurity.ExpirationJumps.FirstSecurity;
		_expirationDate = BasketSecurity.ExpirationJumps[_currId];
	}

	/// <inheritdoc />
	protected override bool CanProcess(SecurityId securityId, DateTimeOffset serverTime, decimal? price, decimal? volume, decimal? openInterest)
	{
		if (_finished)
			return false;

		if (serverTime > _expirationDate)
		{
			var next = BasketSecurity.ExpirationJumps.GetNextSecurity(_currId);

			if (next == null)
			{
				_finished = true;
				return false;
			}

			_currId = next.Value;
			_expirationDate = BasketSecurity.ExpirationJumps[_currId];
			return true;
		}
		else
			return securityId == _currId;
	}
}

/// <summary>
/// Continuous securities processor for <see cref="VolumeContinuousSecurity"/>.
/// </summary>
public class ContinuousSecurityVolumeProcessor : ContinuousSecurityBaseProcessor<VolumeContinuousSecurity>
{
	private int _idIdx;
	private SecurityId _currId;
	private SecurityId _nextId;
	private decimal? _currVolume;
	private decimal? _nextVolume;
	private bool _finished;

	/// <summary>
	/// Initializes a new instance of the <see cref="ContinuousSecurityVolumeProcessor"/>.
	/// </summary>
	/// <param name="security">Security.</param>
	public ContinuousSecurityVolumeProcessor(Security security)
		: base(security)
	{
		if (!NextId())
			throw new InvalidOperationException();
	}

	private bool NextId()
	{
		if ((_idIdx + 1) >= BasketLegs.Length)
			return false;

		_currId = BasketLegs[_idIdx];
		_nextId = BasketLegs[_idIdx + 1];

		_idIdx++;
		return true;
	}

	/// <inheritdoc />
	protected override bool CanProcess(SecurityId securityId, DateTimeOffset serverTime, decimal? price, decimal? volume, decimal? openInterest)
	{
		if (_finished)
			return _currId == securityId;

		var vol = BasketSecurity.IsOpenInterest ? openInterest : volume;

		if (vol == null)
			return false;

		if (securityId == _currId)
			_currVolume = vol;
		else if (securityId == _nextId)
			_nextVolume = vol;

		if (_currVolume == null || _nextVolume == null)
			return false;

		if ((_currVolume.Value + BasketSecurity.VolumeLevel) >= _nextVolume.Value)
			return securityId == _currId;

		if (!NextId())
		{
			_finished = true;
			return false;
		}

		return _currId == securityId;
	}
}

/// <summary>
/// Base index securities processor.
/// </summary>
/// <typeparam name="TBasketSecurity">Basket security type.</typeparam>
public abstract class IndexSecurityBaseProcessor<TBasketSecurity> : BasketSecurityBaseProcessor<TBasketSecurity>
	where TBasketSecurity : IndexSecurity, new()
{
	private readonly SynchronizedDictionary<MessageTypes, object> _messages = [];

	private readonly Dictionary<SecurityId, ExecutionMessage> _ticks = [];
	private readonly Dictionary<SecurityId, ExecutionMessage> _ol = [];

	private readonly SortedDictionary<DateTimeOffset, Dictionary<SecurityId, CandleMessage>> _candles = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="IndexSecurityBaseProcessor{TBasketSecurity}"/>.
	/// </summary>
	/// <param name="security">Security.</param>
	protected IndexSecurityBaseProcessor(Security security)
		: base(security)
	{
	}

	/// <inheritdoc />
	public override IEnumerable<Message> Process(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.QuoteChange:
				var quotesMsg = (QuoteChangeMessage)message;

				if (quotesMsg.State != null || !ContainsLeg(quotesMsg.SecurityId))
					yield break;

				foreach (var msg in ProcessMessage(GetDict<QuoteChangeMessage>(message.Type), quotesMsg.SecurityId, quotesMsg, quotes => new QuoteChangeMessage
				{
					SecurityId = SecurityId,
					ServerTime = quotesMsg.ServerTime,
				}))
					yield return msg;

				break;

			case MessageTypes.Execution:
				var execMsg = (ExecutionMessage)message;

				if (!ContainsLeg(execMsg.SecurityId))
					yield break;

				if (execMsg.DataType == DataType.OrderLog)
				{
					foreach (var msg in ProcessMessage(_ol, execMsg.SecurityId, execMsg, execMsgs =>
					{
						var prices = new decimal[execMsgs.Length];
						var volumes = new decimal[execMsgs.Length];

						for (var i = 0; i < execMsgs.Length; i++)
						{
							var msg = execMsgs[i];

							prices[i] = msg.OrderPrice;
							volumes[i] = msg.OrderVolume ?? 0;
						}

						return new ExecutionMessage
						{
							SecurityId = SecurityId,
							ServerTime = execMsg.ServerTime,
							DataTypeEx = execMsg.DataTypeEx,
							OrderPrice = Calculate(prices, true),
							OrderVolume = Calculate(volumes, false),
						};
					}))
						yield return msg;
				}
				else if (execMsg.DataType == DataType.Ticks)
				{
					foreach (var msg in ProcessMessage(_ticks, execMsg.SecurityId, execMsg, execMsgs =>
					{
						var prices = new decimal[execMsgs.Length];
						var volumes = new decimal[execMsgs.Length];

						for (var i = 0; i < execMsgs.Length; i++)
						{
							var msg = execMsgs[i];

							prices[i] = msg.TradePrice ?? 0;
							volumes[i] = msg.TradeVolume ?? 0;
						}

						return new ExecutionMessage
						{
							SecurityId = SecurityId,
							ServerTime = execMsg.ServerTime,
							DataTypeEx = execMsg.DataTypeEx,
							TradePrice = Calculate(prices, true),
							TradeVolume = Calculate(volumes, false),
						};
					}))
						yield return msg;
				}

				break;

			case MessageTypes.CandleTimeFrame:
			{
				var candleMsg = (CandleMessage)message;

				if (!ContainsLeg(candleMsg.SecurityId))
					yield break;

				var dict = _candles.SafeAdd(candleMsg.OpenTime);

				dict[candleMsg.SecurityId] = candleMsg.TypedClone();

				if (dict.Count == BasketLegs.Length)
				{
					var keys = _candles.Keys.Where(t => t <= candleMsg.OpenTime).ToArray();

					foreach (var key in keys)
					{
						var d = _candles.GetAndRemove(key);

						if (d.Count < BasketLegs.Length && !BasketSecurity.FillGapsByZeros)
							continue;

						var indexCandle = new TimeFrameCandleMessage();

						FillIndexCandle(indexCandle, candleMsg, [.. d.Values]);

						yield return indexCandle;
					}
				}

				break;
			}

			default:
			{
				if (message is CandleMessage candleMsg)
				{
					if (!ContainsLeg(candleMsg.SecurityId))
						yield break;

					foreach (var msg in ProcessMessage(GetDict<CandleMessage>(candleMsg.Type), candleMsg.SecurityId, candleMsg, candles => CreateBasketCandle(candles, candleMsg)))
						yield return msg;
				}

				break;
			}
		}

		//return Enumerable.Empty<Message>();
	}

	private Dictionary<SecurityId, TMessage> GetDict<TMessage>(MessageTypes type)
	{
		return (Dictionary<SecurityId, TMessage>)_messages.SafeAdd(type, _ => new Dictionary<SecurityId, TMessage>());
	}

	private void FillIndexCandle(CandleMessage indexCandle, CandleMessage candleMsg, CandleMessage[] candles)
	{
		indexCandle.SecurityId = SecurityId;
		indexCandle.DataType = candleMsg.DataType;
		indexCandle.OpenTime = candleMsg.OpenTime;
		indexCandle.CloseTime = candleMsg.CloseTime;

		try
		{
			indexCandle.OpenPrice = Calculate(candles, true, c => c.OpenPrice);
			indexCandle.ClosePrice = Calculate(candles, true, c => c.ClosePrice);
			indexCandle.HighPrice = Calculate(candles, true, c => c.HighPrice);
			indexCandle.LowPrice = Calculate(candles, true, c => c.LowPrice);

			if (BasketSecurity.CalculateExtended)
			{
				indexCandle.TotalVolume = Calculate(candles, false, c => c.TotalVolume);

				indexCandle.TotalPrice = Calculate(candles, true, c => c.TotalPrice);
				indexCandle.OpenVolume = Calculate(candles, false, c => c.OpenVolume ?? 0);
				indexCandle.CloseVolume = Calculate(candles, false, c => c.CloseVolume ?? 0);
				indexCandle.HighVolume = Calculate(candles, false, c => c.HighVolume ?? 0);
				indexCandle.LowVolume = Calculate(candles, false, c => c.LowVolume ?? 0);
			}
		}
		catch (ArithmeticException ex)
		{
			if (!BasketSecurity.IgnoreErrors)
				throw;

			ex.LogError();
			return;
		}

		// ���� ��������� ����� ����� �������� ������, �� � ������ ����� ����� �� ��������
		if (indexCandle.OpenPrice == 0 || indexCandle.HighPrice == 0 || indexCandle.LowPrice == 0 || indexCandle.ClosePrice == 0)
		{
			var nonZeroPrice = indexCandle.OpenPrice;

			if (nonZeroPrice == 0)
				nonZeroPrice = indexCandle.HighPrice;

			if (nonZeroPrice == 0)
				nonZeroPrice = indexCandle.LowPrice;

			if (nonZeroPrice == 0)
				nonZeroPrice = indexCandle.LowPrice;

			if (nonZeroPrice != 0)
			{
				if (indexCandle.OpenPrice == 0)
					indexCandle.OpenPrice = nonZeroPrice;

				if (indexCandle.HighPrice == 0)
					indexCandle.HighPrice = nonZeroPrice;

				if (indexCandle.LowPrice == 0)
					indexCandle.LowPrice = nonZeroPrice;

				if (indexCandle.ClosePrice == 0)
					indexCandle.ClosePrice = nonZeroPrice;
			}
		}

		if (indexCandle.HighPrice < indexCandle.LowPrice)
		{
			(indexCandle.LowPrice, indexCandle.HighPrice) = (indexCandle.HighPrice, indexCandle.LowPrice);
		}

		if (indexCandle.OpenPrice > indexCandle.HighPrice)
			indexCandle.HighPrice = indexCandle.OpenPrice;
		else if (indexCandle.OpenPrice < indexCandle.LowPrice)
			indexCandle.LowPrice = indexCandle.OpenPrice;

		if (indexCandle.ClosePrice > indexCandle.HighPrice)
			indexCandle.HighPrice = indexCandle.ClosePrice;
		else if (indexCandle.ClosePrice < indexCandle.LowPrice)
			indexCandle.LowPrice = indexCandle.ClosePrice;

		indexCandle.State = CandleStates.Finished;
	}

	private CandleMessage CreateBasketCandle(CandleMessage[] candles, CandleMessage last)
	{
		if (last == null)
			throw new ArgumentNullException(nameof(last));

		var indexCandle = last.GetType().CreateCandleMessage();

		FillIndexCandle(indexCandle, last, candles);

		return indexCandle;
	}

	private IEnumerable<Message> ProcessMessage<TMessage>(Dictionary<SecurityId, TMessage> dict, SecurityId securityId, TMessage message, Func<TMessage[], TMessage> convert)
		where TMessage : Message
	{
		dict[securityId] = message.TypedClone();

		if (dict.Count != BasketLegs.Length)
			yield break;

		yield return convert(BasketLegs.Select(leg => dict[leg]).ToArray());
		dict.Clear();
	}

	private decimal Calculate(IEnumerable<CandleMessage> candles, bool isPrice, Func<CandleMessage, decimal> getPart)
	{
		var values = candles.Select(getPart).ToArray();

		try
		{
			return Calculate(values, isPrice);
		}
		catch (ArithmeticException excp)
		{
			throw new ArithmeticException(LocalizedStrings.BuildIndexError.Put(SecurityId, BasketLegs.Zip(values, (s, v) => $"{s}: {v}").JoinCommaSpace()), excp);
		}
	}

	private decimal Calculate(decimal[] values, bool isPrice)
	{
		var value = OnCalculate(values);

		if (isPrice)
		{
			var step = Security.PriceStep;

			if (step != null)
				value = Security.ShrinkPrice(value);
		}
		else
		{
			var step = Security.VolumeStep;

			if (step != null)
				value = value.Round(step.Value, step.Value.GetCachedDecimals());
		}

		return value;
	}

	/// <summary>
	/// To calculate the basket value.
	/// </summary>
	/// <param name="values">Values of basket composite instruments <see cref="BasketSecurity.InnerSecurityIds"/>.</param>
	/// <returns>The basket value.</returns>
	protected abstract decimal OnCalculate(decimal[] values);
}

/// <summary>
/// Index securities processor for <see cref="WeightedIndexSecurity"/>.
/// </summary>
public class WeightedIndexSecurityProcessor : IndexSecurityBaseProcessor<WeightedIndexSecurity>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="WeightedIndexSecurityProcessor"/>.
	/// </summary>
	/// <param name="security">Security.</param>
	public WeightedIndexSecurityProcessor(Security security)
		: base(security)
	{
	}

	/// <inheritdoc />
	protected override decimal OnCalculate(decimal[] values)
	{
		if (values == null)
			throw new ArgumentNullException(nameof(values));

		if (values.Length != BasketLegs.Length)// || !InnerSecurities.All(prices.ContainsKey))
			throw new ArgumentOutOfRangeException(nameof(values));

		var value = 0M;

		for (var i = 0; i < values.Length; i++)
			value += BasketSecurity.Weights.CachedValues[i] * values[i];

		return value;
	}
}