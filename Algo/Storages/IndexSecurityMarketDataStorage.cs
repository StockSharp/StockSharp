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
		private readonly IndexSecurity _security;
		private readonly IndexBuilder<T> _builder;
		private readonly MessageTypes _messageType;

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexSecurityMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="security">The index, built of instruments. For example, to specify spread at arbitrage or pair trading.</param>
		/// <param name="arg">The additional argument, associated with data. For example, <see cref="CandleMessage.Arg"/>.</param>
		public IndexSecurityMarketDataStorage(IndexSecurity security, object arg)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (security.InnerSecurityIds.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(security));

			if (typeof(T) == typeof(ExecutionMessage))
			{
				_builder = new TradeIndexBuilder(security) as IndexBuilder<T>;
				_messageType = MessageTypes.Execution;
			}
			else if (typeof(T) == typeof(CandleMessage))
			{
				_builder = new TimeFrameCandleIndexBuilder(security) as IndexBuilder<T>;
				_messageType = MessageTypes.CandleTimeFrame;
			}
			else
				throw new ArgumentException();

			Security = _security = security;
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
			private int _counter;

			public MessageBuffer(DateTimeOffset time, int maxMessageCount)
			{
				_counter = maxMessageCount;

				Time = time;
				Messages = new TBuffer[maxMessageCount];
			}

			public DateTimeOffset Time { get; }

			public TBuffer[] Messages { get; }

			public bool IsFilled => _counter <= 0;

			public void AddMessage(int securityIndex, TBuffer msg)
			{
				if (msg == null)
					throw new ArgumentNullException(nameof(msg));

				//if (Messages[securityIndex] != null)
				//	throw new ArgumentException(LocalizedStrings.Str654Params.Put(msg.LocalTime), nameof(msg));

				Messages[securityIndex] = msg;

				_counter--;
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
					_counter--;
				}
			}
		}

		private readonly CachedSynchronizedOrderedDictionary<DateTimeOffset, MessageBuffer<T>> _buffers = new CachedSynchronizedOrderedDictionary<DateTimeOffset, MessageBuffer<T>>();
		private readonly SynchronizedDictionary<SecurityId, int> _securityIndecies = new SynchronizedDictionary<SecurityId, int>();

		private readonly int _bufferSize;

		private MessageBuffer<T> _lastProcessBuffer;

		public IndexSecurity Security { get; }

		protected IndexBuilder(IndexSecurity security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			Security = security;
			FillSecurityIndecies(Security);
			_bufferSize = _securityIndecies.Values.Distinct().Count();
		}

		public abstract IEnumerable<T> Process(T msg);

		protected abstract T Process(MessageBuffer<T> buffer);

		public void Reset()
		{
			_lastProcessBuffer = null;
			_buffers.Clear();
		}

		private void FillSecurityIndecies(BasketSecurity basketSecurity)
		{
			var index = 0;

			foreach (var security in basketSecurity.InnerSecurityIds)
			{
				_securityIndecies[security] = index;

				index++;
			}
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

						if (!curr.IsFilled)
							throw new InvalidOperationException(LocalizedStrings.Str655);

						_lastProcessBuffer = curr;
						buffers.Add(curr);
					}

					deleteKeys.Add(time);
				}

				if (!buffer.IsFilled)
					lastBuffer.Fill(_lastProcessBuffer);

				if (!lastBuffer.IsFilled)
					throw new InvalidOperationException(LocalizedStrings.Str656);

				deleteKeys.Add(lastBuffer.Time);

				_lastProcessBuffer = lastBuffer;
				buffers.Add(lastBuffer);

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
						if (!Security.IgnoreErrors)
							throw;

						ex.LogError();
						return null;
					}
				})
				.Where(c => c != null);
		}

		protected decimal Calculate(MessageBuffer<T> buffer, Func<T, decimal> getPart)
		{
			var values = buffer.Messages.Select(getPart).ToArray();

			try
			{
				return Security.Calculate(values);
			}
			catch (ArithmeticException excp)
			{
				throw new ArithmeticException("Build index candle {0} for {1} error.".Put(Security, Security.InnerSecurityIds.Zip(values, (s, v) => "{0}: {1}".Put(s, v)).Join(", ")), excp);
			}
		}
	}

	sealed class TradeIndexBuilder : IndexBuilder<ExecutionMessage>
	{
		public TradeIndexBuilder(IndexSecurity security)
			: base(security)
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
				SecurityId = Security.ToSecurityId(),
				ServerTime = buffer.Time,
				LocalTime = buffer.Time,
				ExecutionType = ExecutionTypes.Tick,
				TradePrice = Calculate(buffer, m => m.TradePrice ?? 0),
				TradeVolume = Calculate(buffer, m => m.TradeVolume ?? 0)
			};

			return res;
		}
	}

	sealed class TimeFrameCandleIndexBuilder : IndexBuilder<CandleMessage>
	{
		public TimeFrameCandleIndexBuilder(IndexSecurity security)
			: base(security)
		{
		}

		public override IEnumerable<CandleMessage> Process(CandleMessage msg)
		{
			return OnProcess(msg, msg.OpenTime, msg.SecurityId);
		}

		protected override CandleMessage Process(MessageBuffer<CandleMessage> buffer)
		{
			var res = new TimeFrameCandleMessage();

			res.SecurityId = Security.ToSecurityId();
			//res.Arg = IndexCandleBuilder.CloneArg(candle.Arg, Security);
			res.OpenTime = buffer.Time;
			res.LocalTime = buffer.Time;

			res.TotalVolume = Calculate(buffer, c => c.TotalVolume);
			res.OpenPrice = Calculate(buffer, m => m.OpenPrice);
			res.HighPrice = Calculate(buffer, m => m.HighPrice);
			res.LowPrice = Calculate(buffer, m => m.LowPrice);
			res.ClosePrice = Calculate(buffer, m => m.ClosePrice);

			if (Security.CalculateExtended)
			{
				//res.TotalPrice = Calculate(buffer, c => c.TotalPrice);
				res.OpenVolume = Calculate(buffer, c => c.OpenVolume ?? 0);
				res.CloseVolume = Calculate(buffer, c => c.CloseVolume ?? 0);
				res.HighVolume = Calculate(buffer, c => c.HighVolume ?? 0);
				res.LowVolume = Calculate(buffer, c => c.LowVolume ?? 0);
			}

			res.State = CandleStates.Finished;

			return res;
		}
	}
}