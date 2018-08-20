namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Base basket securities processor.
	/// </summary>
	/// <typeparam name="TSecurity">Basket security type.</typeparam>
	public abstract class BasketSecurityBaseProcessor<TSecurity> : IBasketSecurityProcessor
		where TSecurity : BasketSecurity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BasketSecurityBaseProcessor{TSecurity}"/>.
		/// </summary>
		/// <param name="basketSecurity">Instruments basket.</param>
		protected BasketSecurityBaseProcessor(TSecurity basketSecurity)
		{
			BasketSecurity = basketSecurity ?? throw new ArgumentNullException(nameof(basketSecurity));

			if (BasketSecurity.InnerSecurityIds.IsEmpty())
				throw new ArgumentException(LocalizedStrings.SecurityDoNotContainsLegs.Put(basketSecurity.Id), nameof(basketSecurity));

			SecurityId = BasketSecurity.ToSecurityId();
		}

		/// <summary>
		/// Instruments basket.
		/// </summary>
		public TSecurity BasketSecurity { get; set; }

		/// <inheritdoc />
		public SecurityId SecurityId { get; }

		/// <inheritdoc />
		public abstract IEnumerable<Message> Process(Message message);
	}

	/// <summary>
	/// Base continuous securities processor.
	/// </summary>
	/// <typeparam name="TSecurity">Basket security type.</typeparam>
	public abstract class ContinuousSecurityBaseProcessor<TSecurity> : BasketSecurityBaseProcessor<TSecurity>
		where TSecurity : ContinuousSecurity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ContinuousSecurityBaseProcessor{TSecurity}"/>.
		/// </summary>
		/// <param name="basketSecurity">Continuous security (generally, a futures contract), containing expirable securities.</param>
		protected ContinuousSecurityBaseProcessor(TSecurity basketSecurity)
			: base(basketSecurity)
		{
		}

		/// <inheritdoc />
		public override IEnumerable<Message> Process(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.QuoteChange:
					var quoteMsg = (QuoteChangeMessage)message;

					if (!CanProcess(quoteMsg.SecurityId, quoteMsg.ServerTime, null, null, null))
						yield break;

					break;

				case MessageTypes.Execution:
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType != ExecutionTypes.OrderLog && execMsg.ExecutionType != ExecutionTypes.Tick)
						yield break;

					if (!CanProcess(execMsg.SecurityId, execMsg.ServerTime, execMsg.TradePrice, execMsg.TradeVolume, execMsg.OpenInterest))
						yield break;

					break;

				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRange:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
					var candleMsg = (CandleMessage)message;

					if (!CanProcess(candleMsg.SecurityId, candleMsg.OpenTime, candleMsg.ClosePrice, candleMsg.TotalVolume, candleMsg.OpenInterest))
						yield break;

					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(message), LocalizedStrings.Str2142Params.Put(message.Type));
			}

			yield return message;
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
	public class ContinuousSecurityExpirationProcessor : ContinuousSecurityBaseProcessor<ContinuousSecurity>
	{
		private SecurityId _currId;
		private DateTimeOffset _expirationDate;

		private bool _finished;

		/// <summary>
		/// Initializes a new instance of the <see cref="ContinuousSecurityExpirationProcessor"/>.
		/// </summary>
		/// <param name="basketSecurity">Continuous security (generally, a futures contract), containing expirable securities.</param>
		public ContinuousSecurityExpirationProcessor(ContinuousSecurity basketSecurity)
			: base(basketSecurity)
		{
			_currId = basketSecurity.ExpirationJumps.FirstSecurity;
			_expirationDate = basketSecurity.ExpirationJumps[_currId];
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
		private readonly SecurityId[] _ids;
		private int _idIdx;
		private SecurityId _currId;
		private SecurityId _nextId;
		private decimal? _currVolume;
		private decimal? _nextVolume;
		private bool _finished;

		/// <summary>
		/// Initializes a new instance of the <see cref="ContinuousSecurityVolumeProcessor"/>.
		/// </summary>
		/// <param name="basketSecurity">Continuous security (generally, a futures contract), containing expirable securities.</param>
		public ContinuousSecurityVolumeProcessor(VolumeContinuousSecurity basketSecurity)
			: base(basketSecurity)
		{
			_ids = basketSecurity.InnerSecurityIds.ToArray();

			if (!NextId())
				throw new InvalidOperationException();
		}

		private bool NextId()
		{
			if ((_idIdx + 1) >= _ids.Length)
				return false;

			_currId = _ids[_idIdx];
			_nextId = _ids[_idIdx + 1];

			_idIdx++;
			return true;
		}

		/// <inheritdoc />
		protected override bool CanProcess(SecurityId securityId, DateTimeOffset serverTime, decimal? price, decimal? volume, decimal? openInterest)
		{
			if (_finished)
				return false;

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
				return false;

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
	/// <typeparam name="TSecurity">Basket security type.</typeparam>
	public abstract class IndexSecurityBaseProcessor<TSecurity> : BasketSecurityBaseProcessor<TSecurity>
		where TSecurity : IndexSecurity
	{
		private readonly SortedDictionary<DateTimeOffset, Dictionary<SecurityId, CandleMessage>> _candles = new SortedDictionary<DateTimeOffset, Dictionary<SecurityId, CandleMessage>>();
		private readonly int _secCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexSecurityBaseProcessor{TSecurity}"/>.
		/// </summary>
		/// <param name="basketSecurity">The index, built of instruments. For example, to specify spread at arbitrage or pair trading.</param>
		protected IndexSecurityBaseProcessor(TSecurity basketSecurity)
			: base(basketSecurity)
		{
			_secCount = basketSecurity.InnerSecurityIds.Count();
		}

		/// <inheritdoc />
		public override IEnumerable<Message> Process(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.QuoteChange:
					break;

				case MessageTypes.Execution:
					break;

				case MessageTypes.CandleTimeFrame:
					var candleMsg = (CandleMessage)message;
					var dict = _candles.SafeAdd(candleMsg.OpenTime);
					
					dict[candleMsg.SecurityId] = (CandleMessage)candleMsg.Clone();

					if (dict.Count == _secCount)
					{
						var keys = _candles.Keys.Where(t => t <= candleMsg.OpenTime).ToArray();

						foreach (var key in keys)
						{
							var d = _candles.GetAndRemove(key);

							if (d.Count < _secCount && BasketSecurity.FillGapsByZeros)
								continue;

							var indexCandle = candleMsg.GetType().CreateInstance<CandleMessage>();

							indexCandle.SecurityId = SecurityId;
							indexCandle.Arg = candleMsg.CloneArg();
							indexCandle.OpenTime = candleMsg.OpenTime;
							indexCandle.CloseTime = candleMsg.CloseTime;

							try
							{
								indexCandle.OpenPrice = Calculate(d, true, c => c.OpenPrice);
								indexCandle.ClosePrice = Calculate(d, true, c => c.ClosePrice);
								indexCandle.HighPrice = Calculate(d, true, c => c.HighPrice);
								indexCandle.LowPrice = Calculate(d, true, c => c.LowPrice);

								if (BasketSecurity.CalculateExtended)
								{
									indexCandle.TotalVolume = Calculate(dict, false, c => c.TotalVolume);

									indexCandle.TotalPrice = Calculate(d, true, c => c.TotalPrice);
									indexCandle.OpenVolume = Calculate(d, false, c => c.OpenVolume ?? 0);
									indexCandle.CloseVolume = Calculate(d, false, c => c.CloseVolume ?? 0);
									indexCandle.HighVolume = Calculate(d, false, c => c.HighVolume ?? 0);
									indexCandle.LowVolume = Calculate(d, false, c => c.LowVolume ?? 0);
								}
							}
							catch (ArithmeticException ex)
							{
								if (!BasketSecurity.IgnoreErrors)
									throw;

								ex.LogError();
								continue;
							}

							// если некоторые свечи имеют неполные данные, то и индекс будет таким же неполным
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
								var high = indexCandle.HighPrice;

								indexCandle.HighPrice = indexCandle.LowPrice;
								indexCandle.LowPrice = high;
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

							yield return indexCandle;
						}
					}

					break;

				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRange:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
					break;
			}

			//return Enumerable.Empty<Message>();
		}

		private decimal Calculate(Dictionary<SecurityId, CandleMessage> buffer, bool isPrice, Func<CandleMessage, decimal> getPart)
		{
			var values = buffer.Values.Select(getPart).ToArray();

			try
			{
				return Calculate(values, isPrice);
			}
			catch (ArithmeticException excp)
			{
				throw new ArithmeticException(LocalizedStrings.BuildIndexError.Put(BasketSecurity, BasketSecurity.InnerSecurityIds.Zip(values, (s, v) => "{0}: {1}".Put(s, v)).Join(", ")), excp);
			}
		}

		/// <summary>
		/// To calculate the basket value.
		/// </summary>
		/// <param name="values">Values of basket composite instruments <see cref="BasketSecurity.InnerSecurityIds"/>.</param>
		/// <param name="isPrice">Is price based value calculation.</param>
		/// <returns>The basket value.</returns>
		public decimal Calculate(decimal[] values, bool isPrice)
		{
			var value = OnCalculate(values);

			if (isPrice)
			{
				var step = BasketSecurity.PriceStep;

				if (step != null)
					value = BasketSecurity.ShrinkPrice(value);
			}
			else
			{
				var step = BasketSecurity.VolumeStep;

				if (step != null)
					value = MathHelper.Round(value, step.Value, step.Value.GetCachedDecimals());
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
		/// <param name="basketSecurity">The instruments basket, based on weigh-scales <see cref="WeightedIndexSecurity.Weights"/>.</param>
		public WeightedIndexSecurityProcessor(WeightedIndexSecurity basketSecurity)
			: base(basketSecurity)
		{
		}

		/// <inheritdoc />
		protected override decimal OnCalculate(decimal[] values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			if (values.Length != BasketSecurity.Weights.Count)// || !InnerSecurities.All(prices.ContainsKey))
				throw new ArgumentOutOfRangeException(nameof(values));

			var value = 0M;

			for (var i = 0; i < values.Length; i++)
				value += BasketSecurity.Weights.CachedValues[i] * values[i];

			return value;
		}
	}
}