#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: BufferMessageAdapter.cs
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

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Buffered message adapter.
	/// </summary>
	public class BufferMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// The market data buffer.
		/// </summary>
		/// <typeparam name="TKey">The key type.</typeparam>
		/// <typeparam name="TMarketData">Market data type.</typeparam>
		class DataBuffer<TKey, TMarketData>
		{
			private readonly SynchronizedDictionary<TKey, List<TMarketData>> _data = new SynchronizedDictionary<TKey, List<TMarketData>>();

			///// <summary>
			///// The buffer size.
			///// </summary>
			//public int Size { get; set; }

			/// <summary>
			/// To add new information to the buffer.
			/// </summary>
			/// <param name="key">The key possessing new information.</param>
			/// <param name="data">New information.</param>
			public void Add(TKey key, TMarketData data)
			{
				Add(key, new[] { data });
			}

			/// <summary>
			/// To add new information to the buffer.
			/// </summary>
			/// <param name="key">The key possessing new information.</param>
			/// <param name="data">New information.</param>
			public void Add(TKey key, IEnumerable<TMarketData> data)
			{
				_data.SyncDo(d => d.SafeAdd(key).AddRange(data));
			}

			/// <summary>
			/// To get accumulated data from the buffer and delete them.
			/// </summary>
			/// <returns>Gotten data.</returns>
			public IDictionary<TKey, IEnumerable<TMarketData>> Get()
			{
				return _data.SyncGet(d =>
				{
					var retVal = d.ToDictionary(p => p.Key, p => (IEnumerable<TMarketData>)p.Value);
					d.Clear();
					return retVal;
				});
			}

			///// <summary>
			///// To get accumulated data from the buffer and delete them.
			///// </summary>
			///// <param name="key">The key possessing market data.</param>
			///// <returns>Gotten data.</returns>
			//public IEnumerable<TMarketData> Get(TKey key)
			//{
			//	if (key.IsDefault())
			//		throw new ArgumentNullException("key");

			//	return _data.SyncGet(d =>
			//	{
			//		var data = d.TryGetValue(key);

			//		if (data != null)
			//		{
			//			var retVal = data.CopyAndClear();
			//			d.Remove(key);
			//			return retVal;
			//		}

			//		return Enumerable.Empty<TMarketData>();
			//	});
			//}
		}

		private readonly DataBuffer<SecurityId, ExecutionMessage> _ticksBuffer = new DataBuffer<SecurityId, ExecutionMessage>();
		private readonly DataBuffer<SecurityId, QuoteChangeMessage> _orderBooksBuffer = new DataBuffer<SecurityId, QuoteChangeMessage>();
		private readonly DataBuffer<SecurityId, ExecutionMessage> _orderLogBuffer = new DataBuffer<SecurityId, ExecutionMessage>();
		private readonly DataBuffer<SecurityId, Level1ChangeMessage> _level1Buffer = new DataBuffer<SecurityId, Level1ChangeMessage>();
		private readonly DataBuffer<Tuple<SecurityId, Type, object>, CandleMessage> _candleBuffer = new DataBuffer<Tuple<SecurityId, Type, object>, CandleMessage>();
		private readonly DataBuffer<SecurityId, ExecutionMessage> _transactionsBuffer = new DataBuffer<SecurityId, ExecutionMessage>();
		private readonly SynchronizedSet<NewsMessage> _newsBuffer = new SynchronizedSet<NewsMessage>(); 

		/// <summary>
		/// Initializes a new instance of the <see cref="BufferMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public BufferMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <summary>
		/// Get accumulated ticks.
		/// </summary>
		/// <returns>Ticks.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTicks()
		{
			return _ticksBuffer.Get();
		}

		/// <summary>
		/// Get accumulated order log.
		/// </summary>
		/// <returns>Order log.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetOrderLog()
		{
			return _orderLogBuffer.Get();
		}

		/// <summary>
		/// Get accumulated transactions.
		/// </summary>
		/// <returns>Transactions.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTransactions()
		{
			return _transactionsBuffer.Get();
		}

		/// <summary>
		/// Get accumulated candles.
		/// </summary>
		/// <returns>Candles.</returns>
		public IDictionary<Tuple<SecurityId, Type, object>, IEnumerable<CandleMessage>> GetCandles()
		{
			return _candleBuffer.Get();
		}

		/// <summary>
		/// Get accumulated level1.
		/// </summary>
		/// <returns>Level1.</returns>
		public IDictionary<SecurityId, IEnumerable<Level1ChangeMessage>> GetLevel1()
		{
			return _level1Buffer.Get();
		}

		/// <summary>
		/// Get accumulated order books.
		/// </summary>
		/// <returns>Order books.</returns>
		public IDictionary<SecurityId, IEnumerable<QuoteChangeMessage>> GetOrderBooks()
		{
			return _orderBooksBuffer.Get();
		}

		/// <summary>
		/// Get accumulated news.
		/// </summary>
		/// <returns>News.</returns>
		public IEnumerable<NewsMessage> GetNews()
		{
			return _newsBuffer.SyncGet(c => c.CopyAndClear());
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Level1Change:
				{
					var level1Msg = (Level1ChangeMessage)message.Clone();
					_level1Buffer.Add(level1Msg.SecurityId, level1Msg);
					break;
				}
				case MessageTypes.QuoteChange:
				{
					var quotesMsg = (QuoteChangeMessage)message.Clone();
					_orderBooksBuffer.Add(quotesMsg.SecurityId, quotesMsg);
					break;
				}
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message.Clone();

					DataBuffer<SecurityId, ExecutionMessage> buffer;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
							buffer = _ticksBuffer;
							break;
						case ExecutionTypes.Transaction:
							buffer = _transactionsBuffer;
							break;
						case ExecutionTypes.OrderLog:
							buffer = _orderLogBuffer;
							break;
						default:
							throw new ArgumentOutOfRangeException(LocalizedStrings.Str1695Params.Put(execMsg.ExecutionType));
					}

					buffer.Add(execMsg.SecurityId, execMsg);
					break;
				}
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRange:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandleVolume:
				{
					var candleMsg = (CandleMessage)message.Clone();
					_candleBuffer.Add(Tuple.Create(candleMsg.SecurityId, candleMsg.GetType(), candleMsg.Arg), candleMsg);
					break;
				}
				case MessageTypes.News:
				{
					_newsBuffer.Add((NewsMessage)message.Clone());
					break;
				}
				//case MessageTypes.Position:
				//	break;
				//case MessageTypes.Portfolio:
				//	break;
				//case MessageTypes.PositionChange:
				//	break;
				//case MessageTypes.PortfolioChange:
				//	break;
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="StorageMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new BufferMessageAdapter(InnerAdapter);
		}
	}
}