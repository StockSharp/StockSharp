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
/// <remarks>
/// Initializes a new instance of the <see cref="ContinuousSecurityBaseProcessor{TBasketSecurity}"/>.
/// </remarks>
/// <param name="security">Security.</param>
public abstract class ContinuousSecurityBaseProcessor<TBasketSecurity>(Security security) : BasketSecurityBaseProcessor<TBasketSecurity>(security)
	where TBasketSecurity : ContinuousSecurity, new()
{
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
					volume = (volume ?? 0) + bestAsk?.Volume;

				if (!CanProcess(quoteMsg.SecurityId, quoteMsg.ServerTime, (bestBid?.Price).GetSpreadMiddle(bestAsk?.Price, Security.PriceStep), volume, null))
					yield break;

				break;
			}

			case MessageTypes.Level1Change:
			{
				var l1Msg = (Level1ChangeMessage)message;

				if (!ContainsLeg(l1Msg.SecurityId))
					yield break;

				if (!CanProcess(l1Msg.SecurityId, l1Msg.ServerTime,
					l1Msg.TryGetDecimal(Level1Fields.LastTradePrice),
					l1Msg.TryGetDecimal(Level1Fields.Volume),
					l1Msg.TryGetDecimal(Level1Fields.OpenInterest)))
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

				// Every message of a leg subscription is fed in, and what the series is not made
				// of - news and the like - is not its data.
				yield break;
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
	protected abstract bool CanProcess(SecurityId securityId, DateTime serverTime, decimal? price, decimal? volume, decimal? openInterest);
}

/// <summary>
/// Continuous securities processor for <see cref="ContinuousSecurity"/>.
/// </summary>
public class ContinuousSecurityExpirationProcessor : ContinuousSecurityBaseProcessor<ExpirationContinuousSecurity>
{
	private SecurityId _currId;
	private DateTime _expirationDate;

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
	protected override bool CanProcess(SecurityId securityId, DateTime serverTime, decimal? price, decimal? volume, decimal? openInterest)
	{
		if (_finished)
			return false;

		// Roll forward until the contract that is alive at this time is the current one: more
		// than one expiration can lie behind a gap in the data.
		while (serverTime > _expirationDate)
		{
			var next = BasketSecurity.ExpirationJumps.GetNextSecurity(_currId);

			if (next == null)
			{
				_finished = true;
				return false;
			}

			_currId = next.Value;
			_expirationDate = BasketSecurity.ExpirationJumps[_currId];
		}

		// Only the contract that carries the series is the series: a contract further out is
		// another instrument.
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
	private bool _isCurrentActive;
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
	protected override bool CanProcess(SecurityId securityId, DateTime serverTime, decimal? price, decimal? volume, decimal? openInterest)
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

		if (_currVolume == null)
			return false;

		if (_nextVolume == null)
			return _isCurrentActive && securityId == _currId;

		if ((_currVolume.Value + BasketSecurity.VolumeLevel) >= _nextVolume.Value)
		{
			_isCurrentActive = true;
			return securityId == _currId;
		}

		// Volume switched - next security is now active
		var currVolume = _nextVolume;

		if (!NextId())
		{
			// No more legs after _nextId, switch to _nextId as final active leg
			_currId = _nextId;
			_finished = true;
		}

		_currVolume = currVolume;
		_nextVolume = null;
		_isCurrentActive = true;

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

	private readonly SortedDictionary<DateTime, Dictionary<SecurityId, CandleMessage>> _candles = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="IndexSecurityBaseProcessor{TBasketSecurity}"/>.
	/// </summary>
	/// <param name="security">Security.</param>
	protected IndexSecurityBaseProcessor(Security security)
		: base(security)
	{
		// The basket the processor calculates on is a copy made of the fields every security
		// has, so the options that drive the calculation are taken over separately.
		if (security is IndexSecurity index)
		{
			BasketSecurity.IgnoreErrors = index.IgnoreErrors;
			BasketSecurity.CalculateExtended = index.CalculateExtended;
			BasketSecurity.FillGapsByZeros = index.FillGapsByZeros;
		}
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

				foreach (var msg in ProcessMessage(GetDict<QuoteChangeMessage>(message.Type), quotesMsg.SecurityId, quotesMsg, legQuotes =>
				{
					var allBids = new List<QuoteChange>();
					var allAsks = new List<QuoteChange>();

					DateTime? time = null;

					var minBidDepth = int.MaxValue;
					var minAskDepth = int.MaxValue;

					var legsCount = legQuotes.Length;

					for (var i = 0; i < legsCount; i++)
					{
						var legQuote = legQuotes[i];

						time = time is null
							? legQuote.ServerTime
							: time.Value.Max(legQuote.ServerTime);

						var bids = GetQuotes(legQuote, i, true);
						var asks = GetQuotes(legQuote, i, false);

						if (bids.Length < minBidDepth)
							minBidDepth = bids.Length;

						if (asks.Length < minAskDepth)
							minAskDepth = asks.Length;
					}

					if (minBidDepth < int.MaxValue)
					{
						for (var level = 0; level < minBidDepth; level++)
						{
							var prices = new decimal[legsCount];
							var volumes = new decimal[legsCount];

							for (var j = 0; j < legsCount; j++)
							{
								var b = GetQuotes(legQuotes[j], j, true)[level];
								prices[j] = b.Price;
								volumes[j] = b.Volume;
							}

							var aggregatedPrice = Calculate(prices, isPrice: true);
							var aggregatedVolume = Calculate(volumes, isPrice: false);

							allBids.Add(new(aggregatedPrice, aggregatedVolume));
						}
					}

					if (minAskDepth < int.MaxValue)
					{
						for (var level = 0; level < minAskDepth; level++)
						{
							var prices = new decimal[legsCount];
							var volumes = new decimal[legsCount];

							for (var j = 0; j < legsCount; j++)
							{
								var a = GetQuotes(legQuotes[j], j, false)[level];
								prices[j] = a.Price;
								volumes[j] = a.Volume;
							}

							var aggregatedPrice = Calculate(prices, true);
							var aggregatedVolume = Calculate(volumes, false);

							allAsks.Add(new(aggregatedPrice, aggregatedVolume));
						}
					}

					var bidsArr = allBids
						.GroupBy(q => q.Price)
						.Select(g => new QuoteChange(g.Key, g.Sum(x => x.Volume)))
						.OrderByDescending(q => q.Price)
						.ToArray();

					var asksArr = allAsks
						.GroupBy(q => q.Price)
						.Select(g => new QuoteChange(g.Key, g.Sum(x => x.Volume)))
						.OrderBy(q => q.Price)
						.ToArray();

					// Legs quoting different spreads can lift the index bid above its own ask. The
					// two sides are then exchanged and each re-sorted, so what is published is
					// still a book: bids best price first and downwards, asks upwards.
					if (bidsArr.FirstOr()?.Price > asksArr.FirstOr()?.Price)
					{
						var crossed = bidsArr;

						bidsArr = [.. asksArr.OrderByDescending(q => q.Price)];
						asksArr = [.. crossed.OrderBy(q => q.Price)];
					}

					return new QuoteChangeMessage
					{
						SecurityId = SecurityId,
						ServerTime = time ?? quotesMsg.ServerTime,

						Bids = bidsArr,
						Asks = asksArr,
					};
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

						var legCandles = GetLegCandles(d);

						var indexCandle = new TimeFrameCandleMessage();

						// Every candle of the bucket, reported or stood in for, carries the
						// bucket's own time frame and boundaries.
						FillIndexCandle(indexCandle, legCandles[0], legCandles);

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

	/// <summary>
	/// Get a leg side used to build one side of the basket book.
	/// </summary>
	/// <param name="message">Leg order book.</param>
	/// <param name="legIndex">Leg index.</param>
	/// <param name="isBid">Whether the basket bid is being built.</param>
	/// <returns>Quotes used for the basket side.</returns>
	protected virtual QuoteChange[] GetQuotes(QuoteChangeMessage message, int legIndex, bool isBid)
		=> isBid ? message.Bids : message.Asks;

	/// <summary>
	/// Get a leg price used to build the basket candle high.
	/// </summary>
	protected virtual decimal GetCandleHigh(CandleMessage candle, int legIndex) => candle.HighPrice;

	/// <summary>
	/// Get a leg price used to build the basket candle low.
	/// </summary>
	protected virtual decimal GetCandleLow(CandleMessage candle, int legIndex) => candle.LowPrice;

	private Dictionary<SecurityId, TMessage> GetDict<TMessage>(MessageTypes type)
	{
		return (Dictionary<SecurityId, TMessage>)_messages.SafeAdd(type, _ => new Dictionary<SecurityId, TMessage>());
	}

	// Lays a time bucket out in BasketLegs order, which is the order OnCalculate reads its values
	// in. A leg that did not report for the bucket stands in as a candle of zero prices.
	private CandleMessage[] GetLegCandles(Dictionary<SecurityId, CandleMessage> bucket)
	{
		var legs = BasketLegs;
		var candles = new CandleMessage[legs.Length];

		CandleMessage reported = null;

		for (var i = 0; i < legs.Length; i++)
		{
			if (!bucket.TryGetValue(legs[i], out var legCandle))
				continue;

			candles[i] = legCandle;
			reported ??= legCandle;
		}

		for (var i = 0; i < legs.Length; i++)
		{
			candles[i] ??= new TimeFrameCandleMessage
			{
				SecurityId = legs[i],
				DataType = reported.DataType,
				OpenTime = reported.OpenTime,
				CloseTime = reported.CloseTime,
				State = CandleStates.Finished,
			};
		}

		return candles;
	}

	private void FillIndexCandle(CandleMessage indexCandle, CandleMessage template, CandleMessage[] candles)
	{
		indexCandle.SecurityId = SecurityId;
		indexCandle.DataType = template.DataType;
		indexCandle.OpenTime = template.OpenTime;
		indexCandle.CloseTime = template.CloseTime;

		try
		{
			indexCandle.OpenPrice = Calculate(candles, true, (c, _) => c.OpenPrice);
			indexCandle.ClosePrice = Calculate(candles, true, (c, _) => c.ClosePrice);
			indexCandle.HighPrice = Calculate(candles, true, GetCandleHigh);
			indexCandle.LowPrice = Calculate(candles, true, GetCandleLow);

			if (BasketSecurity.CalculateExtended)
			{
				indexCandle.TotalVolume = Calculate(candles, false, (c, _) => c.TotalVolume);

				indexCandle.TotalPrice = Calculate(candles, true, (c, _) => c.TotalPrice);
				indexCandle.OpenVolume = Calculate(candles, false, (c, _) => c.OpenVolume ?? 0);
				indexCandle.CloseVolume = Calculate(candles, false, (c, _) => c.CloseVolume ?? 0);
				indexCandle.HighVolume = Calculate(candles, false, (c, _) => c.HighVolume ?? 0);
				indexCandle.LowVolume = Calculate(candles, false, (c, _) => c.LowVolume ?? 0);
			}
		}
		catch (ArithmeticException ex)
		{
			if (!BasketSecurity.IgnoreErrors)
				throw;

			ex.LogError();
			return;
		}

		// Legs that reported only part of their candle leave the index as incomplete: the prices
		// that did arrive stand in for the ones that did not.
		if (indexCandle.OpenPrice == 0 || indexCandle.HighPrice == 0 || indexCandle.LowPrice == 0 || indexCandle.ClosePrice == 0)
		{
			var nonZeroPrice = indexCandle.OpenPrice;

			if (nonZeroPrice == 0)
				nonZeroPrice = indexCandle.HighPrice;

			if (nonZeroPrice == 0)
				nonZeroPrice = indexCandle.LowPrice;

			if (nonZeroPrice == 0)
				nonZeroPrice = indexCandle.ClosePrice;

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

		yield return convert([.. BasketLegs.Select(leg => dict[leg])]);
		dict.Clear();
	}

	private decimal Calculate(CandleMessage[] candles, bool isPrice, Func<CandleMessage, int, decimal> getPart)
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
/// <remarks>
/// Initializes a new instance of the <see cref="WeightedIndexSecurityProcessor"/>.
/// </remarks>
/// <param name="security">Security.</param>
public class WeightedIndexSecurityProcessor(Security security) : IndexSecurityBaseProcessor<WeightedIndexSecurity>(security)
{
	/// <inheritdoc />
	protected override QuoteChange[] GetQuotes(QuoteChangeMessage message, int legIndex, bool isBid)
	{
		var isPositive = BasketSecurity.Weights[BasketLegs[legIndex]] >= 0;
		return isBid == isPositive ? message.Bids : message.Asks;
	}

	/// <inheritdoc />
	protected override decimal GetCandleHigh(CandleMessage candle, int legIndex)
		=> BasketSecurity.Weights[BasketLegs[legIndex]] >= 0 ? candle.HighPrice : candle.LowPrice;

	/// <inheritdoc />
	protected override decimal GetCandleLow(CandleMessage candle, int legIndex)
		=> BasketSecurity.Weights[BasketLegs[legIndex]] >= 0 ? candle.LowPrice : candle.HighPrice;

	/// <inheritdoc />
	protected override decimal OnCalculate(decimal[] values)
	{
		if (values == null)
			throw new ArgumentNullException(nameof(values));

		var legs = BasketLegs;

		if (values.Length != legs.Length)
			throw new ArgumentOutOfRangeException(nameof(values));

		var weights = BasketSecurity.Weights;

		var value = 0M;

		// Each value belongs to the leg standing at the same position, so its weight is looked
		// up by that security rather than taken from the same position among the weights.
		for (var i = 0; i < values.Length; i++)
			value += weights[legs[i]] * values[i];

		return value;
	}
}
