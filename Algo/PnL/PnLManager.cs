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
	private readonly Dictionary<SecurityId, Level1ChangeMessage> _secLevel1 = [];

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
	/// Use <see cref="DataType.MarketDepth"/> for <see cref="UnrealizedPnL"/> calculation.
	/// </summary>
	public bool UseOrderBook { get; set; }

	/// <summary>
	/// Use <see cref="DataType.Level1"/> for <see cref="UnrealizedPnL"/> calculation.
	/// </summary>
	public bool UseLevel1 { get; set; }

	/// <summary>
	/// Use <see cref="DataType.IsCandles"/> for <see cref="UnrealizedPnL"/> calculation.
	/// </summary>
	public bool UseCandles { get; set; } = true;

	/// <inheritdoc />
	public decimal RealizedPnL { get; private set; }

	/// <inheritdoc />
	public decimal UnrealizedPnL => _managersByPf.CachedValues.Sum(m => m.UnrealizedPnL);

	/// <inheritdoc />
	public void Reset()
	{
		lock (_managersByPf.SyncRoot)
		{
			RealizedPnL = default;

			_managersByPf.Clear();
			_managersByTransId.Clear();
			_managersByOrderId.Clear();
			_managersByOrderStringId.Clear();

			_secLevel1.Clear();
		}
	}

	void IPnLManager.UpdateSecurity(Level1ChangeMessage l1Msg)
	{
		if (l1Msg is null)
			throw new ArgumentNullException(nameof(l1Msg));

		if (l1Msg.SecurityId.IsAllSecurity())
			throw new ArgumentException(l1Msg.SecurityId.ToString(), nameof(l1Msg));

		l1Msg = l1Msg.TypedClone();

		_secLevel1[l1Msg.SecurityId] = l1Msg;

		foreach (var manager in _managersByPf.CachedValues)
			manager.UpdateSecurity(l1Msg);
	}

	PnLInfo IPnLManager.ProcessMessage(Message message, ICollection<PortfolioPnLManager> changedPortfolios)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		PortfolioPnLManager createManager(SecurityId secId, string pfName)
			=> new(pfName, secId =>
			{
				lock (_managersByPf.SyncRoot)
					return _secLevel1.TryGetValue(secId);
			});

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
					var manager = _managersByPf.SafeAdd(regMsg.PortfolioName, pf => createManager(regMsg.SecurityId, pf));
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
									manager = _managersByPf.SafeAdd(execMsg.PortfolioName, key => createManager(execMsg.SecurityId, key));
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

							RealizedPnL += info.PnL;
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
		UseCandles = storage.GetValue(nameof(UseCandles), UseCandles);
		UseLevel1 = storage.GetValue(nameof(UseLevel1), UseLevel1);
		UseOrderBook = storage.GetValue(nameof(UseOrderBook), UseOrderBook);
		UseOrderLog = storage.GetValue(nameof(UseOrderLog), UseOrderLog);
		UseTick = storage.GetValue(nameof(UseTick), UseTick);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.Set(nameof(UseCandles), UseCandles);
		storage.Set(nameof(UseLevel1), UseLevel1);
		storage.Set(nameof(UseOrderBook), UseOrderBook);
		storage.Set(nameof(UseOrderLog), UseOrderLog);
		storage.Set(nameof(UseTick), UseTick);
	}
}