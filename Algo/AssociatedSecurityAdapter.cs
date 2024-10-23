namespace StockSharp.Algo;

/// <summary>
/// Associated security adapter.
/// </summary>
public class AssociatedSecurityAdapter : MessageAdapterWrapper
{
	private sealed class QuoteChangeDepthBuilder
	{
		private readonly Dictionary<SecurityId, QuoteChangeMessage> _feeds = [];

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

	private readonly SynchronizedDictionary<string, QuoteChangeDepthBuilder> _quoteChangeDepthBuilders = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the <see cref="AssociatedSecurityAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	public AssociatedSecurityAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Security:
			{
				var secMsg = (SecurityMessage)message;
				if (!IsAssociated(secMsg.SecurityId.BoardCode))
				{
					var clone = secMsg.TypedClone();
					clone.SecurityId = CreateAssociatedId(clone.SecurityId);
					base.OnInnerAdapterNewOutMessage(clone);
				}
				break;
			}

			case MessageTypes.Level1Change:
			{
				var level1Msg = (Level1ChangeMessage)message;

				if (!IsAssociated(level1Msg.SecurityId.BoardCode))
				{
					// ���������� BestXXX ��� ALL �� ���������� �������
					var clone = level1Msg.TypedClone();
					clone.SecurityId = CreateAssociatedId(clone.SecurityId);
					base.OnInnerAdapterNewOutMessage(clone);
				}

				break;
			}

			case MessageTypes.QuoteChange:
			{
				var quoteMsg = (QuoteChangeMessage)message;

				if (quoteMsg.State != null)
					break;

				if (quoteMsg.SecurityId == default)
					break;

				//if (IsAssociated(quoteMsg.SecurityId.BoardCode))
				//	return;

				var builder = _quoteChangeDepthBuilders
					.SafeAdd(quoteMsg.SecurityId.SecurityCode, c => new QuoteChangeDepthBuilder(c, SecurityId.AssociatedBoardCode));

				quoteMsg = builder.Process(quoteMsg);

				base.OnInnerAdapterNewOutMessage(quoteMsg);

				break;
			}

			case MessageTypes.Execution:
			{
				var executionMsg = (ExecutionMessage)message;

				if (executionMsg.DataType == DataType.Ticks ||
					executionMsg.DataType == DataType.OrderLog)
				{
					if (!IsAssociated(executionMsg.SecurityId.BoardCode))
					{
						var clone = executionMsg.TypedClone();
						clone.SecurityId = CreateAssociatedId(clone.SecurityId);
						base.OnInnerAdapterNewOutMessage(clone);
					}
				}

				break;
			}
		}

		base.OnInnerAdapterNewOutMessage(message);
	}

	private static bool IsAssociated(string boardCode)
	{
		return /*boardCode.IsEmpty() || */boardCode.EqualsIgnoreCase(SecurityId.AssociatedBoardCode);
	}

	private static SecurityId CreateAssociatedId(SecurityId securityId)
	{
		return new()
		{
			SecurityCode = securityId.SecurityCode,
			BoardCode = SecurityId.AssociatedBoardCode,
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