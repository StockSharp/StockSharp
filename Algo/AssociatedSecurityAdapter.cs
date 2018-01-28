namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Associated security adapter.
	/// </summary>
	public class AssociatedSecurityAdapter : MessageAdapterWrapper
	{
		private sealed class QuoteChangeDepthBuilder
		{
			private readonly Dictionary<SecurityId, QuoteChangeMessage> _feeds = new Dictionary<SecurityId, QuoteChangeMessage>();

			private readonly string _securityCode;
			private readonly string _boardCode;

			public QuoteChangeDepthBuilder(string securityCode, string boardCode)
			{
				_securityCode = securityCode;
				_boardCode = boardCode;
			}

			public QuoteChangeMessage Process(QuoteChangeMessage message)
			{
				_feeds[message.SecurityId] = message;

				var bids = _feeds.SelectMany(f => f.Value.Bids).ToArray();
				var asks = _feeds.SelectMany(f => f.Value.Asks).ToArray();

				return new QuoteChangeMessage
				{
					SecurityId = new SecurityId
					{
						SecurityCode = _securityCode,
						BoardCode = _boardCode
					},
					ServerTime = message.ServerTime,
					LocalTime = message.LocalTime,
					Bids = bids,
					Asks = asks
				};
			}
		}

		private readonly SynchronizedDictionary<string, QuoteChangeDepthBuilder> _quoteChangeDepthBuilders = new SynchronizedDictionary<string, QuoteChangeDepthBuilder>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="AssociatedSecurityAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public AssociatedSecurityAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message.IsBack)
			{
				base.OnInnerAdapterNewOutMessage(message);
				return;
			}

			switch (message.Type)
			{
				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;
					if (!IsAssociated(secMsg.SecurityId.BoardCode))
					{
						var clone = (SecurityMessage)secMsg.Clone();
						clone.SecurityId = CreateAssociatedId(clone.SecurityId);
						RaiseNewOutMessage(clone);
					}
					break;
				}

				case MessageTypes.Level1Change:
				{
					var level1Msg = (Level1ChangeMessage)message;

					if (!IsAssociated(level1Msg.SecurityId.BoardCode))
					{
						// обновление BestXXX для ALL из конкретных тикеров
						var clone = (Level1ChangeMessage)level1Msg.Clone();
						clone.SecurityId = CreateAssociatedId(clone.SecurityId);
						RaiseNewOutMessage(clone);
					}

					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					if (quoteMsg.SecurityId.IsDefault())
						return;

					//if (IsAssociated(quoteMsg.SecurityId.BoardCode))
					//	return;

					var builder = _quoteChangeDepthBuilders
						.SafeAdd(quoteMsg.SecurityId.SecurityCode, c => new QuoteChangeDepthBuilder(c, AssociatedBoardCode));

					quoteMsg = builder.Process(quoteMsg);

					RaiseNewOutMessage(quoteMsg);

					break;
				}

				case MessageTypes.Execution:
				{
					var executionMsg = (ExecutionMessage)message;

					switch (executionMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
						case ExecutionTypes.OrderLog:
						{
							if (!IsAssociated(executionMsg.SecurityId.BoardCode))
							{
								var clone = (ExecutionMessage)executionMsg.Clone();
								clone.SecurityId = CreateAssociatedId(clone.SecurityId);
								RaiseNewOutMessage(clone);
							}

							break;
						}
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private bool IsAssociated(string boardCode)
		{
			return /*boardCode.IsEmpty() || */boardCode.CompareIgnoreCase(AssociatedBoardCode);
		}

		private SecurityId CreateAssociatedId(SecurityId securityId)
		{
			return new SecurityId
			{
				SecurityCode = securityId.SecurityCode,
				BoardCode = AssociatedBoardCode,
				SecurityType = securityId.SecurityType,
				Bloomberg = securityId.Bloomberg,
				Cusip = securityId.Cusip,
				IQFeed = securityId.IQFeed,
				InteractiveBrokers = securityId.InteractiveBrokers,
				Isin = securityId.Isin,
				Native = securityId.Native,
				Plaza = securityId.Plaza,
				Ric = securityId.Ric,
				Sedol = securityId.Sedol,
			};
		}

		/// <summary>
		/// Create a copy of <see cref="AssociatedSecurityAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new AssociatedSecurityAdapter(InnerAdapter);
		}
	}
}