namespace StockSharp.Algo;

/// <summary>
/// Associated security adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AssociatedSecurityAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Inner message adapter.</param>
public class AssociatedSecurityAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	private sealed class QuoteChangeDepthBuilder(string securityCode, string boardCode)
	{
		private readonly Dictionary<SecurityId, QuoteChangeMessage> _feeds = [];

		private static void SetBoardCode(QuoteChange[] quotes, string boardCode)
		{
			for (var i = 0; i < quotes.Length; i++)
			{
				if (!quotes[i].BoardCode.IsEmpty())
					continue;

				quotes[i].BoardCode = boardCode;
			}
		}

		private static QuoteChange Aggregate(decimal price, IEnumerable<QuoteChange> quotes)
		{
			var result = new QuoteChange { Price = price };
			result.InnerQuotes = [.. quotes.Select(q => q.Clone())];
			return result;
		}

		public QuoteChangeMessage Process(QuoteChangeMessage message)
		{
			var feed = message.TypedClone();
			SetBoardCode(feed.Bids, message.SecurityId.BoardCode);
			SetBoardCode(feed.Asks, message.SecurityId.BoardCode);
			_feeds[message.SecurityId] = feed;

			var bids = _feeds
				.SelectMany(f => f.Value.Bids)
				.GroupBy(q => q.Price)
				.Select(g => Aggregate(g.Key, g))
				.OrderByDescending(q => q.Price)
				.ToArray();
			var asks = _feeds
				.SelectMany(f => f.Value.Asks)
				.GroupBy(q => q.Price)
				.Select(g => Aggregate(g.Key, g))
				.OrderBy(q => q.Price)
				.ToArray();

			return new QuoteChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = securityCode,
					BoardCode = boardCode
				},
				ServerTime = message.ServerTime,
				LocalTime = message.LocalTime,
				Bids = bids,
				Asks = asks
			};
		}
	}

	private readonly SynchronizedDictionary<string, QuoteChangeDepthBuilder> _quoteChangeDepthBuilders = new(StringComparer.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message.Type == MessageTypes.Reset)
			_quoteChangeDepthBuilders.Clear();

		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
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
					await base.OnInnerAdapterNewOutMessageAsync(clone, cancellationToken);
				}
				break;
			}

			case MessageTypes.Level1Change:
			{
				var level1Msg = (Level1ChangeMessage)message;

				if (!IsAssociated(level1Msg.SecurityId.BoardCode))
				{
					// обновление BestXXX для ALL из конкретных тикеров
					var clone = level1Msg.TypedClone();
					clone.SecurityId = CreateAssociatedId(clone.SecurityId);
					await base.OnInnerAdapterNewOutMessageAsync(clone, cancellationToken);
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

				// An associated snapshot already contains the venue books. Feeding it back into the
				// builder would count those levels once through their venues and once through ALL.
				if (IsAssociated(quoteMsg.SecurityId.BoardCode))
					break;

				var builder = _quoteChangeDepthBuilders
					.SafeAdd(quoteMsg.SecurityId.SecurityCode, c => new QuoteChangeDepthBuilder(c, SecurityId.AssociatedBoardCode));

				quoteMsg = builder.Process(quoteMsg);

				await base.OnInnerAdapterNewOutMessageAsync(quoteMsg, cancellationToken);

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
						await base.OnInnerAdapterNewOutMessageAsync(clone, cancellationToken);
					}
				}

				break;
			}
		}

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
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
	public override IMessageAdapter Clone()
	{
		return new AssociatedSecurityAdapter(InnerAdapter.TypedClone());
	}
}
