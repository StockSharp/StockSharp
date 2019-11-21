namespace StockSharp.Algo
{
	using System;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Message adapter that initialize missed <see cref="ISubscriptionIdMessage.OriginalTransactionId"/>.
	/// </summary>
	public class OriginalTransactionMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedPairSet<Tuple<MessageTypes, SecurityId, object>, long> _transactionIds = new SynchronizedPairSet<Tuple<MessageTypes, SecurityId, object>, long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OriginalTransactionMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OriginalTransactionMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					_transactionIds.Clear();
					break;

				case MessageTypes.MarketData:
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						if (mdMsg.To == null && mdMsg.Count == null)
							_transactionIds[Tuple.Create(mdMsg.DataType.ToMessageType2(), mdMsg.SecurityId, mdMsg.Arg)] = mdMsg.TransactionId;
					}
					else
					{
						_transactionIds.RemoveByValue(mdMsg.OriginalTransactionId);
					}

					break;
			}

			base.SendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			void TryInitId<TMessage>(TMessage msg, SecurityId secId, object arg = null)
				where TMessage : Message, ISubscriptionIdMessage
			{
				if (msg.OriginalTransactionId == 0)
					msg.OriginalTransactionId = _transactionIds.TryGetValue(Tuple.Create(msg.Type, secId, arg));
			}

			switch (message)
			{
				case CandleMessage candleMsg:
				{
					TryInitId(candleMsg, candleMsg.SecurityId, candleMsg.Arg);
					break;
				}

				case ExecutionMessage execMsg:
				{
					if (execMsg.IsMarketData())
						TryInitId(execMsg, execMsg.SecurityId);

					break;
				}

				case Level1ChangeMessage l1Msg:
				{
					TryInitId(l1Msg, l1Msg.SecurityId);
					break;
				}

				case QuoteChangeMessage quotesMsg:
				{
					TryInitId(quotesMsg, quotesMsg.SecurityId);
					break;
				}

				case NewsMessage newsMsg:
				{
					TryInitId(newsMsg, default);
					break;
				}

				case BoardStateMessage boardMsg:
				{
					TryInitId(boardMsg, default);
					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="OriginalTransactionMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OriginalTransactionMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}