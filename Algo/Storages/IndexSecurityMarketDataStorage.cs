#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: IndexSecurityMarketDataStorage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The aggregator-storage, allowing to load data simultaneously market data for <see cref="IndexSecurity"/>.
	/// </summary>
	/// <typeparam name="T">Message type.</typeparam>
	public class IndexSecurityMarketDataStorage<T> : BasketMarketDataStorage<T>
		where T : Message
	{
		//private readonly IndexSecurity _security;
		private readonly IndexBuilder<T> _builder;
		private readonly MessageTypes _messageType;

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexSecurityMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="security">The index, built of instruments. For example, to specify spread at arbitrage or pair trading.</param>
		/// <param name="arg">The additional argument, associated with data. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="ignoreErrors">Ignore errors.</param>
		public IndexSecurityMarketDataStorage(IndexSecurity security, object arg, bool ignoreErrors = true)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (security.InnerSecurityIds.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(security));

			if (typeof(T) == typeof(ExecutionMessage))
			{
				_builder = new TradeIndexBuilder(security, ignoreErrors).To<IndexBuilder<T>>();
				_messageType = MessageTypes.Execution;
			}
			else if (typeof(T) == typeof(CandleMessage))
			{
				_builder = new TimeFrameCandleIndexBuilder(security, ignoreErrors).To<IndexBuilder<T>>();
				_messageType = MessageTypes.CandleTimeFrame;
			}
			else if (typeof(T) == typeof(QuoteChangeMessage))
			{
				_builder = new QuoteChangeIndexBuilder(security, ignoreErrors).To<IndexBuilder<T>>();
				_messageType = MessageTypes.QuoteChange;
			}
			else
				throw new InvalidOperationException(LocalizedStrings.Str2142Params.Put(typeof(T)));

			Security = security;
			Arg = arg;
		}

		/// <summary>
		/// The type of market-data, operated by given storage.
		/// </summary>
		public override Type DataType => typeof(T);

		/// <summary>
		/// The instrument, operated by the external storage.
		/// </summary>
		public override Security Security { get; }

		/// <summary>
		/// The additional argument, associated with data. For example, <see cref="CandleMessage.Arg"/>.
		/// </summary>
		public override object Arg { get; }

		/// <summary>
		/// To load messages from embedded storages for specified date.
		/// </summary>
		/// <param name="date">Date.</param>
		/// <returns>The messages.</returns>
		protected override IEnumerable<T> OnLoad(DateTime date)
		{
			var messages = base.OnLoad(date);

			_builder.Reset();

			return messages
				.Where(msg => msg.Type == _messageType)
				.SelectMany(msg => _builder.Process(msg));
		}
	}

	abstract class IndexBuilder<T>
		where T : Message
	{
		protected sealed class MessageBuffer<TBuffer>
			where TBuffer : Message
		{
			public MessageBuffer(DateTimeOffset time, int maxMessageCount)
			{
				Time = time;
				Messages = new TBuffer[maxMessageCount];
			}

			public DateTimeOffset Time { get; }

			public TBuffer[] Messages { get; }

			public bool IsFilled => Messages.All(m => m != null);

			public void AddMessage(int securityIndex, TBuffer msg)
			{
				//if (Messages[securityIndex] != null)
				//	throw new ArgumentException(LocalizedStrings.Str654Params.Put(msg.LocalTime), nameof(msg));

				Messages[securityIndex] = msg ?? throw new ArgumentNullException(nameof(msg));
			}

			public void Fill(MessageBuffer<TBuffer> prevBuffer)
			{
				if (prevBuffer == null)
					throw new ArgumentNullException(nameof(prevBuffer));

				for (var i = 0; i < Messages.Length; i++)
				{
					if (Messages[i] != null || prevBuffer.Messages[i] == null)
						continue;

					Messages[i] = (TBuffer)prevBuffer.Messages[i].Clone();
				}
			}
		}

		private readonly CachedSynchronizedOrderedDictionary<DateTimeOffset, MessageBuffer<T>> _buffers = new CachedSynchronizedOrderedDictionary<DateTimeOffset, MessageBuffer<T>>();
		private readonly SynchronizedDictionary<SecurityId, int> _securityIndecies = new SynchronizedDictionary<SecurityId, int>();

		private readonly bool _ignoreErrors;
		private readonly int _bufferSize;

		private MessageBuffer<T> _lastProcessBuffer;

		public IndexSecurity Security { get; }
		public SecurityId SecurityId { get; }

		protected IndexBuilder(IndexSecurity security, bool ignoreErrors)
		{
			_ignoreErrors = ignoreErrors;
			Security = security ?? throw new ArgumentNullException(nameof(security));
			SecurityId = security.ToSecurityId();
			SecurityId.EnsureHashCode();

			var index = 0;

			foreach (var innerSec in security.InnerSecurityIds)
			{
				_securityIndecies[innerSec] = index;

				index++;
			}

			_bufferSize = index;
		}

		public abstract IEnumerable<T> Process(T msg);

		protected abstract T Process(MessageBuffer<T> buffer);

		public void Reset()
		{
			_lastProcessBuffer = null;
			_buffers.Clear();
		}

		private IEnumerable<MessageBuffer<T>> GetFormedBuffers(T msg, DateTimeOffset openTime, SecurityId secId)
		{
			var buffers = new List<MessageBuffer<T>>();

			lock (_buffers.SyncRoot)
			{
				var buffer = _buffers.SafeAdd(openTime, key => new MessageBuffer<T>(openTime, _bufferSize));
				buffer.AddMessage(_securityIndecies[secId], msg);

				if (!buffer.IsFilled)
					return Enumerable.Empty<MessageBuffer<T>>();

				// mika
				// далее идет обработка 4-рех ситуаций
				//
				// 1. первая свеча оказалась заполненой полностью, и мы просто удаляем начальные буфера
				// так как они уже никогда не заполняться.
				//
				// 2. первая свеча оказалось размазанной, тогда так же удаляем хвосты, но при этом формируем
				// из одной размазанной свечи N (= равное кол-ву инструментов) дозаполненных.
				//
				// 3. появилась заполненная полностью свеча, и тогда дозаполняем промежутки с _lastProcessBuffer
				//
				// 4. появилась размазанная свеча, и тогда дозаполняем промежутки с _lastProcessBuffer + формируем
				// из одной размазанной свечи N (= равное кол-ву инструментов) дозаполненных.

				var firstTimeBuffer = _lastProcessBuffer == null;

				// последней буфер, до которого (включительно) можно сформировать спред по текущим данным
				var lastBuffer = buffer;

				var deleteKeys = new List<DateTimeOffset>();

				foreach (var time in _buffers.CachedKeys)
				{
					if (time >= lastBuffer.Time)
						break;

					var curr = _buffers[time];

					if (firstTimeBuffer)
					{
						if (!buffer.IsFilled)
						{
							if (_lastProcessBuffer == null)
								_lastProcessBuffer = curr;
							else
							{
								_lastProcessBuffer.Fill(curr);
								_lastProcessBuffer = curr;
							}
						}
					}
					else
					{
						curr.Fill(_lastProcessBuffer);

						_lastProcessBuffer = curr;

						if (curr.IsFilled)
							buffers.Add(curr);
					}

					deleteKeys.Add(time);
				}

				if (!buffer.IsFilled)
					lastBuffer.Fill(_lastProcessBuffer);

				if (lastBuffer.IsFilled)
				{
					deleteKeys.Add(lastBuffer.Time);

					_lastProcessBuffer = lastBuffer;
					buffers.Add(lastBuffer);
				}

				deleteKeys.ForEach(k => _buffers.Remove(k));
			}

			return buffers;
		}

		protected IEnumerable<T> OnProcess(T msg, DateTimeOffset time, SecurityId securityId)
		{
			return GetFormedBuffers(msg, time, securityId)
				.Select(buffer =>
				{
					try
					{
						if (buffer.Messages.Any(m => m == null))
							return null;

						return Process(buffer);
					}
					catch (ArithmeticException ex)
					{
						if (!_ignoreErrors)
							throw;

						ex.LogError();
						return null;
					}
				})
				.Where(c => c != null);
		}

		protected decimal Calculate(MessageBuffer<T> buffer, bool isPrice, Func<T, decimal> getPart)
		{
			return Calculate(buffer.Messages, isPrice, getPart);
		}

		protected decimal Calculate<TItem>(IEnumerable<TItem> items, bool isPrice, Func<TItem, decimal> getPart)
		{
			var values = items.Select(getPart).ToArray();

			try
			{
				return Security.Calculate(values, isPrice);
			}
			catch (ArithmeticException excp)
			{
				throw new ArithmeticException(LocalizedStrings.BuildIndexError.Put(SecurityId, Security.InnerSecurityIds.Zip(values, (s, v) => "{0}: {1}".Put(s, v)).Join(", ")), excp);
			}
		}

		protected int GetIndex(SecurityId securityId)
		{
			return _securityIndecies.TryGetValue(securityId);
		}
	}

	sealed class TradeIndexBuilder : IndexBuilder<ExecutionMessage>
	{
		public TradeIndexBuilder(IndexSecurity security, bool ignoreErrors)
			: base(security, ignoreErrors)
		{
		}

		public override IEnumerable<ExecutionMessage> Process(ExecutionMessage msg)
		{
			return OnProcess(msg, msg.ServerTime, msg.SecurityId);
		}

		protected override ExecutionMessage Process(MessageBuffer<ExecutionMessage> buffer)
		{
			var res = new ExecutionMessage
			{
				SecurityId = SecurityId,
				ServerTime = buffer.Time,
				LocalTime = buffer.Time,
				ExecutionType = ExecutionTypes.Tick,
				TradePrice = Calculate(buffer, true, m => m.TradePrice ?? 0),
				TradeVolume = Calculate(buffer, false, m => m.TradeVolume ?? 0)
			};

			return res;
		}
	}

	sealed class TimeFrameCandleIndexBuilder : IndexBuilder<CandleMessage>
	{
		public TimeFrameCandleIndexBuilder(IndexSecurity security, bool ignoreErrors)
			: base(security, ignoreErrors)
		{
		}

		public override IEnumerable<CandleMessage> Process(CandleMessage msg)
		{
			return OnProcess(msg, msg.OpenTime, msg.SecurityId);
		}

		protected override CandleMessage Process(MessageBuffer<CandleMessage> buffer)
		{
			var res = new TimeFrameCandleMessage
			{
				SecurityId = SecurityId,
				OpenTime = buffer.Time,
				LocalTime = buffer.Time,
				TotalVolume = Calculate(buffer, false, c => c.TotalVolume),
				OpenPrice = Calculate(buffer, true, m => m.OpenPrice),
				HighPrice = Calculate(buffer, true, m => m.HighPrice),
				LowPrice = Calculate(buffer, true, m => m.LowPrice),
				ClosePrice = Calculate(buffer, true, m => m.ClosePrice),
				State = CandleStates.Finished
			};

			//res.Arg = IndexCandleBuilder.CloneArg(candle.Arg, Security);

			if (Security.CalculateExtended)
			{
				//res.TotalPrice = Calculate(buffer, c => c.TotalPrice);
				res.OpenVolume = Calculate(buffer, false, c => c.OpenVolume ?? 0);
				res.CloseVolume = Calculate(buffer, false, c => c.CloseVolume ?? 0);
				res.HighVolume = Calculate(buffer, false, c => c.HighVolume ?? 0);
				res.LowVolume = Calculate(buffer, false, c => c.LowVolume ?? 0);
			}

			return res;
		}
	}

	sealed class QuoteChangeIndexBuilder : IndexBuilder<QuoteChangeMessage>
	{
		public QuoteChangeIndexBuilder(IndexSecurity security, bool ignoreErrors)
			: base(security, ignoreErrors)
		{
		}

		public override IEnumerable<QuoteChangeMessage> Process(QuoteChangeMessage msg)
		{
			return OnProcess(msg, msg.ServerTime, msg.SecurityId);
		}

		protected override QuoteChangeMessage Process(MessageBuffer<QuoteChangeMessage> buffer)
		{
			var res = new QuoteChangeMessage
			{
				SecurityId = SecurityId,
				ServerTime = buffer.Time,
				LocalTime = buffer.Time,
			};

			var bids = new List<QuoteChange[]>();
			var asks = new List<QuoteChange[]>();

			foreach (var msg in buffer.Messages)
			{
				var index = GetIndex(msg.SecurityId);

				AddChanges(msg.Bids, bids, index, buffer.Messages.Length);
				AddChanges(msg.Asks, asks, index, buffer.Messages.Length);
			}

			res.Bids = CreateQuoteChanges(bids, Sides.Buy);
			res.Asks = CreateQuoteChanges(asks, Sides.Sell);

			return res;
		}

		private IEnumerable<QuoteChange> CreateQuoteChanges(IEnumerable<QuoteChange[]> pairs, Sides side)
		{
			return pairs
				.Where(p => p.All(q => q != null))
				.Select(p => new QuoteChange
				{
					Side = side,
					Price = Calculate(p, true, item => item.Price),
					Volume = Calculate(p, false, item => item.Volume)
				})
				.ToArray();
		}

		private static void AddChanges(IEnumerable<QuoteChange> changes, IList<QuoteChange[]> bids, int index, int count)
		{
			var i = 0;

			foreach (var change in changes)
			{
				if (bids.Count <= i)
					bids.Add(new QuoteChange[count]);

				bids[i][index] = change;
				i++;
			}
		}
	}
}