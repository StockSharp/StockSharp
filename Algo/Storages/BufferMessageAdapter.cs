namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Buffered message adapter.
	/// </summary>
	public class BufferMessageAdapter : MessageAdapterWrapper
	{
		private readonly MarketDataBuffer<SecurityId, ExecutionMessage> _ticksBuffer = new MarketDataBuffer<SecurityId, ExecutionMessage>();
		private readonly MarketDataBuffer<SecurityId, QuoteChangeMessage> _orderBooksBuffer = new MarketDataBuffer<SecurityId, QuoteChangeMessage>();
		private readonly MarketDataBuffer<SecurityId, ExecutionMessage> _orderLogBuffer = new MarketDataBuffer<SecurityId, ExecutionMessage>();
		private readonly MarketDataBuffer<SecurityId, Level1ChangeMessage> _level1Buffer = new MarketDataBuffer<SecurityId, Level1ChangeMessage>();
		private readonly MarketDataBuffer<Tuple<SecurityId, Type, object>, CandleMessage> _candleBuffer = new MarketDataBuffer<Tuple<SecurityId, Type, object>, CandleMessage>();
		private readonly MarketDataBuffer<SecurityId, ExecutionMessage> _transactionsBuffer = new MarketDataBuffer<SecurityId, ExecutionMessage>();
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

					MarketDataBuffer<SecurityId, ExecutionMessage> buffer;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
							buffer = _ticksBuffer;
							break;
						case ExecutionTypes.Order:
						case ExecutionTypes.Trade:
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