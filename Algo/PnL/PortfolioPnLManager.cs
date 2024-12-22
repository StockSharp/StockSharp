namespace StockSharp.Algo.PnL;

/// <summary>
/// The profit-loss manager, related for specified <see cref="PortfolioName"/>.
/// </summary>
public class PortfolioPnLManager : IPnLManager
{
	private readonly Dictionary<string, PnLInfo> _tradeByStringIdInfos = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly Dictionary<long, PnLInfo> _tradeByIdInfos = [];
	private readonly CachedSynchronizedDictionary<SecurityId, PnLQueue> _securityPnLs = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="PortfolioPnLManager"/>.
	/// </summary>
	/// <param name="portfolioName">Portfolio name.</param>
	public PortfolioPnLManager(string portfolioName)
	{
		if (portfolioName.IsEmpty())
			throw new ArgumentNullException(nameof(portfolioName));

		PortfolioName = portfolioName;
	}

	/// <summary>
	/// Portfolio name.
	/// </summary>
	public string PortfolioName { get; }

	/// <inheritdoc />
	public decimal PnL => RealizedPnL + UnrealizedPnL;

	private decimal _realizedPnL;

	/// <inheritdoc />
	public virtual decimal RealizedPnL => _realizedPnL;

	/// <inheritdoc />
	public void Reset()
	{
		_realizedPnL = 0;
		_securityPnLs.Clear();

		_tradeByStringIdInfos.Clear();
		_tradeByIdInfos.Clear();
	}

	/// <inheritdoc />
	public void UpdateSecurity(Level1ChangeMessage l1Msg)
	{
		if (TryGetQueue(l1Msg, out var queue))
			queue.UpdateSecurity(l1Msg);
	}

	PnLInfo IPnLManager.ProcessMessage(Message message, ICollection<PortfolioPnLManager> changedPortfolios)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	public decimal UnrealizedPnL => _securityPnLs.CachedValues.Sum(q => q.UnrealizedPnL);

	/// <summary>
	/// To calculate trade profitability. If the trade was already processed earlier, previous information returns.
	/// </summary>
	/// <param name="trade">Trade.</param>
	/// <param name="info">Information on new trade.</param>
	/// <returns><see langword="true" />, if new trade received, otherwise, <see langword="false" />.</returns>
	public bool ProcessMyTrade(ExecutionMessage trade, out PnLInfo info)
	{
		if (trade == null)
			throw new ArgumentNullException(nameof(trade));

		info = null;

		var tradeId = trade.TradeId;
		var tradeStringId = trade.TradeStringId;

		if (tradeId != null)
		{
			if (_tradeByIdInfos.TryGetValue(tradeId.Value, out info))
				return false;

			var queue = _securityPnLs.SafeAdd(trade.SecurityId, security => new PnLQueue(security));

			info = queue.Process(trade);

			_tradeByIdInfos.Add(tradeId.Value, info);
			_realizedPnL += info.PnL;
			return true;
		}
		else if (!tradeStringId.IsEmpty())
		{
			if (_tradeByStringIdInfos.TryGetValue(tradeStringId, out info))
				return false;

			var queue = _securityPnLs.SafeAdd(trade.SecurityId, security => new PnLQueue(security));

			info = queue.Process(trade);

			_tradeByStringIdInfos.Add(tradeStringId, info);
			_realizedPnL += info.PnL;
			return true;
		}

		return false;
	}

	private bool TryGetQueue<TMsg>(TMsg msg, out PnLQueue queue)
		where TMsg : ISecurityIdMessage
		=> _securityPnLs.TryGetValue(msg.SecurityId, out queue);

	/// <summary>
	/// To process the message, containing market data.
	/// </summary>
	/// <param name="message">The message, containing market data.</param>
	/// <returns><see cref="PnL"/> was changed.</returns>
	public bool ProcessMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.DataType != DataType.Ticks)
					break;

				if (!TryGetQueue(execMsg, out var queue))
					break;

				queue.ProcessExecution(execMsg);
				return true;
			}

			case MessageTypes.QuoteChange:
			{
				var quoteMsg = (QuoteChangeMessage)message;

				if (quoteMsg.State != null)
					break;

				if (!TryGetQueue(quoteMsg, out var queue))
					break;

				queue.ProcessQuotes(quoteMsg);
				return true;
			}

			case MessageTypes.PositionChange:
			{
				var posMsg = (PositionChangeMessage)message;

				var leverage = posMsg.TryGetDecimal(PositionChangeTypes.Leverage);
				if (leverage != null)
				{
					if (posMsg.IsMoney())
						_securityPnLs.CachedValues.ForEach(q => q.Leverage = leverage.Value);
					else
						_securityPnLs.SafeAdd(posMsg.SecurityId, security => new PnLQueue(security)).Leverage = leverage.Value;
				}

				break;
			}

			default:
			{
				if (message is not CandleMessage candleMsg)
					break;

				if (!TryGetQueue(candleMsg, out var queue))
					break;

				queue.ProcessCandle(candleMsg);
				return true;
			}
		}

		return false;
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		
	}
}