namespace StockSharp.Algo.PnL;

/// <summary>
/// The profit-loss manager.
/// </summary>
public class PnLManager : IPnLManager
{
	private readonly CachedSynchronizedDictionary<string, PortfolioPnLManager> _managersByPf = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly Dictionary<long, PortfolioPnLManager> _managersByTransId = [];
	private readonly Dictionary<long, PortfolioPnLManager> _managersByOrderId = [];
	private readonly Dictionary<string, PortfolioPnLManager> _managersByOrderStringId = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the <see cref="PnLManager"/>.
	/// </summary>
	public PnLManager()
	{
	}

	/// <summary>
	/// Use <see cref="DataType.Ticks"/> for <see cref="UnrealizedPnL"/> calculation.
	/// </summary>
	public bool UseTick { get; set; } = true;

	/// <summary>
	/// Use <see cref="DataType.OrderLog"/> for <see cref="UnrealizedPnL"/> calculation.
	/// </summary>
	public bool UseOrderLog { get; set; }

	/// <summary>
	/// Use <see cref="QuoteChangeMessage"/> for <see cref="UnrealizedPnL"/> calculation.
	/// </summary>
	public bool UseOrderBook { get; set; }

	/// <summary>
	/// Use <see cref="Level1ChangeMessage"/> for <see cref="UnrealizedPnL"/> calculation.
	/// </summary>
	public bool UseLevel1 { get; set; }

	/// <summary>
	/// Use <see cref="CandleMessage"/> for <see cref="UnrealizedPnL"/> calculation.
	/// </summary>
	public bool UseCandles { get; set; } = true;

	/// <inheritdoc />
	public decimal PnL => RealizedPnL + UnrealizedPnL ?? 0;

	private decimal _realizedPnL;

	/// <inheritdoc />
	public decimal RealizedPnL => _realizedPnL;

	/// <inheritdoc />
	public decimal? UnrealizedPnL
	{
		get
		{
			decimal? retVal = null;

			foreach (var manager in _managersByPf.CachedValues)
			{
				var pnl = manager.UnrealizedPnL;

				if (pnl != null)
				{
					retVal ??= 0;

					retVal += pnl.Value;
				}
			}

			return retVal;
		}
	}

	/// <inheritdoc />
	public void Reset()
	{
		lock (_managersByPf.SyncRoot)
		{
			_realizedPnL = 0;
			_managersByPf.Clear();
			_managersByTransId.Clear();
			_managersByOrderId.Clear();
			_managersByOrderStringId.Clear();
		}
	}

	/// <inheritdoc />
	public PnLInfo ProcessMessage(Message message, ICollection<PortfolioPnLManager> changedPortfolios)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				Reset();
				return null;
			}

			case MessageTypes.OrderRegister:
			{
				var regMsg = (OrderRegisterMessage)message;

				lock (_managersByPf.SyncRoot)
				{
					var manager = _managersByPf.SafeAdd(regMsg.PortfolioName, pf => new PortfolioPnLManager(pf));
					_managersByTransId.Add(regMsg.TransactionId, manager);
				}

				return null;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.DataType == DataType.Transactions)
				{
					var transId = execMsg.TransactionId == 0
						? execMsg.OriginalTransactionId
						: execMsg.TransactionId;

					PortfolioPnLManager manager = null;

					if (execMsg.HasOrderInfo())
					{
						lock (_managersByPf.SyncRoot)
						{
							if (!_managersByTransId.TryGetValue(transId, out manager))
							{
								if (!execMsg.PortfolioName.IsEmpty())
									manager = _managersByPf.SafeAdd(execMsg.PortfolioName, key => new PortfolioPnLManager(key));
								else if (execMsg.OrderId != null)
									manager = _managersByOrderId.TryGetValue(execMsg.OrderId.Value);
								else if (!execMsg.OrderStringId.IsEmpty())
									manager = _managersByOrderStringId.TryGetValue(execMsg.OrderStringId);
							}

							if (manager == null)
								return null;

							if (execMsg.OrderId != null)
								_managersByOrderId.TryAdd2(execMsg.OrderId.Value, manager);
							else if (!execMsg.OrderStringId.IsEmpty())
								_managersByOrderStringId.TryAdd2(execMsg.OrderStringId, manager);
						}
					}

					if (execMsg.HasTradeInfo())
					{
						lock (_managersByPf.SyncRoot)
						{
							if (manager == null && !_managersByTransId.TryGetValue(transId, out manager))
								return null;

							if (!manager.ProcessMyTrade(execMsg, out var info))
								return null;

							_realizedPnL += info.PnL;
							changedPortfolios?.Add(manager);
							return info;
						}
					}
				}
				else if (execMsg.DataType == DataType.Ticks)
				{
					if (!UseTick)
						return null;
				}
				else if (execMsg.DataType == DataType.OrderLog)
				{
					if (!UseOrderLog)
						return null;
				}
				else
					return null;

				break;
			}

			case MessageTypes.Level1Change:
			{
				if (!UseLevel1)
					return null;

				break;
			}
			case MessageTypes.QuoteChange:
			{
				if (!UseOrderBook || ((QuoteChangeMessage)message).State != null)
					return null;

				break;
			}

			//case MessageTypes.PortfolioChange:
			case MessageTypes.PositionChange:
			{
				break;
			}

			default:
			{
				if (message is CandleMessage)
				{
					if (UseCandles)
						break;
				}

				return null;
			}
		}

		foreach (var manager in _managersByPf.CachedValues)
		{
			if (manager.ProcessMessage(message))
				changedPortfolios?.Add(manager);
		}

		return null;
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public void Load(SettingsStorage storage)
	{
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public void Save(SettingsStorage storage)
	{
	}
}