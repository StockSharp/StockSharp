namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.PnL;

/// <summary>
/// State machine + message processing. Handles <see cref="Strategy.ProcessState"/> transitions and market data routing to <see cref="Strategy.PnLManager"/>.
/// </summary>
public class StrategyEngine
{
	private const MessageTypes _strategyChangeState = (MessageTypes)(-11);

	private static readonly Func<ProcessStates, CancellationToken, ValueTask> _emptyStateChangedAsync
		= static (_, _) => default;

	private readonly IStrategyHost _host;
	private readonly Func<ProcessStates, CancellationToken, ValueTask> _stateChangedAsync;
	private IPnLManager _pnlManager;
	private ProcessStates _processState;
	private DateTime _lastPnlRefreshTime;
	// Set once a stop is requested and kept until the final Stopped is emitted. Used (instead of the
	// round-trip-updated ProcessState) to gate TryFinalStopAsync, so the Stopped message is emitted in the
	// same request even before the Stopping message has been processed back, while the rule-completion
	// re-drive stays a no-op during normal running.
	private bool _stopRequested;

	/// <summary>
	/// Initializes a new instance of the <see cref="StrategyEngine"/>.
	/// </summary>
	/// <param name="host">Strategy host.</param>
	/// <param name="pnlManager">PnL manager.</param>
	public StrategyEngine(IStrategyHost host, IPnLManager pnlManager)
		: this(host, pnlManager, _emptyStateChangedAsync)
	{
	}

	internal StrategyEngine(
		IStrategyHost host,
		IPnLManager pnlManager,
		Func<ProcessStates, CancellationToken, ValueTask> stateChangedAsync)
	{
		_host = host ?? throw new ArgumentNullException(nameof(host));
		_pnlManager = pnlManager ?? throw new ArgumentNullException(nameof(pnlManager));
		_stateChangedAsync = stateChangedAsync ?? throw new ArgumentNullException(nameof(stateChangedAsync));
	}

	/// <summary>
	/// Swap the PnL manager used for market-data routing. Used when <see cref="Strategy.PnLManager"/>
	/// is reassigned so the engine routes quotes/ticks into the new manager.
	/// </summary>
	/// <param name="pnlManager">The new PnL manager.</param>
	public void SetPnLManager(IPnLManager pnlManager)
		=> _pnlManager = pnlManager ?? throw new ArgumentNullException(nameof(pnlManager));

	/// <summary>
	/// Current process state.
	/// </summary>
	public ProcessStates ProcessState => _processState;

	private async ValueTask SetProcessStateAsync(ProcessStates value, CancellationToken cancellationToken)
	{
		if (_processState == value)
			return;

		if (_processState == ProcessStates.Stopped && value == ProcessStates.Stopping)
			throw new InvalidOperationException($"Cannot transition from Stopped to Stopping.");

		_processState = value;
		await _stateChangedAsync(value, cancellationToken).NoWait();
		StateChanged?.Invoke(value);
	}

	/// <summary>
	/// Interval for unrealized PnL refresh. The default value is 1 minute, matching the monolith
	/// strategy, so the out-of-the-box refresh cadence is identical even before the public
	/// <see cref="Strategy.UnrealizedPnLInterval"/> setter runs.
	/// </summary>
	public TimeSpan UnrealizedPnLInterval { get; set; } = TimeSpan.FromMinutes(1);

	/// <summary>
	/// Market time of the last unrealized-PnL refresh; used for statistic attribution to match the monolith.
	/// </summary>
	public DateTime LastPnLRefreshTime => _lastPnlRefreshTime;

	/// <summary>
	/// Fires when ProcessState changes.
	/// </summary>
	public event Action<ProcessStates> StateChanged;

	/// <summary>
	/// Gate for the final <see cref="ProcessStates.Stopping"/> -&gt; <see cref="ProcessStates.Stopped"/> transition.
	/// Mirrors the monolith TryFinalStop: while it returns <see langword="false"/> the strategy stays in
	/// <see cref="ProcessStates.Stopping"/> (e.g. outstanding rules under WaitRulesOnStop). When unset the
	/// transition is never gated, preserving the previous immediate-stop behaviour.
	/// </summary>
	public Func<bool> CanFinalStop { get; set; }

	/// <summary>
	/// Fires when PnL should be recalculated.
	/// </summary>
	public event Action<DateTime> PnLRefreshRequired;

	/// <summary>
	/// Fires when current price updates from market data.
	/// </summary>
	public event Action<SecurityId, decimal, DateTime, DateTime> CurrentPriceUpdated;

	/// <summary>
	/// Request strategy start. Sends state change message through host.
	/// </summary>
	public ValueTask RequestStartAsync(CancellationToken cancellationToken)
	{
		_processState = ProcessStates.Stopped; // ensure clean state
		_stopRequested = false;
		return _host.SendOutMessageAsync(new StrategyStateMessage(_host.StrategyId, ProcessStates.Started), cancellationToken);
	}

	/// <summary>
	/// Request strategy stop. Sends state change message through host.
	/// </summary>
	public async ValueTask RequestStopAsync(CancellationToken cancellationToken)
	{
		if (ProcessState == ProcessStates.Stopped)
			return;

		_stopRequested = true;
		await _host.SendOutMessageAsync(new StrategyStateMessage(_host.StrategyId, ProcessStates.Stopping), cancellationToken);
		await TryFinalStopAsync(cancellationToken);
	}

	/// <summary>
	/// Attempt the final <see cref="ProcessStates.Stopping"/> -&gt; <see cref="ProcessStates.Stopped"/> transition,
	/// honouring <see cref="CanFinalStop"/>. While the gate denies it the strategy stays in
	/// <see cref="ProcessStates.Stopping"/>; the method is re-driven once the gating condition clears
	/// (mirroring the monolith TryFinalStop re-entry from rule completion).
	/// </summary>
	public ValueTask TryFinalStopAsync(CancellationToken cancellationToken)
	{
		if (!_stopRequested)
			return default;

		if (CanFinalStop is { } gate && !gate())
			return default;

		_stopRequested = false;
		return _host.SendOutMessageAsync(new StrategyStateMessage(_host.StrategyId, ProcessStates.Stopped), cancellationToken);
	}

	/// <summary>
	/// Process incoming message — state transitions + market data routing.
	/// </summary>
	public void OnMessage(Message message)
		=> AsyncHelper.Run(() => OnMessageAsync(message, default));

	/// <summary>
	/// Process incoming message asynchronously.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing message processing.</returns>
	public ValueTask OnMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		if (message is StrategyStateMessage stateMessage)
		{
			if (!string.IsNullOrEmpty(stateMessage.StrategyId) &&
				!string.Equals(stateMessage.StrategyId, _host.StrategyId, StringComparison.Ordinal))
				return default;

			return OnStateMessageAsync(stateMessage, cancellationToken);
		}

		OnMessageCore(message);
		return default;
	}

	private ValueTask OnStateMessageAsync(StrategyStateMessage message, CancellationToken cancellationToken)
	{
		switch (message.RequestedState)
		{
			case ProcessStates.Stopping:
				return ProcessState == ProcessStates.Started
					? SetProcessStateAsync(ProcessStates.Stopping, cancellationToken)
					: default;

			case ProcessStates.Started:
				return ProcessState == ProcessStates.Stopped
					? SetProcessStateAsync(ProcessStates.Started, cancellationToken)
					: default;

			case ProcessStates.Stopped:
				return ProcessState != ProcessStates.Stopped
					? SetProcessStateAsync(ProcessStates.Stopped, cancellationToken)
					: default;

			default:
				return default;
		}
	}

	private void OnMessageCore(Message message)
	{
		DateTime? msgTime = null;

		switch (message.Type)
		{
			case MessageTypes.QuoteChange:
			{
				var quoteMsg = (QuoteChangeMessage)message;

				if (quoteMsg.State != null)
					return;

				if (quoteMsg.Asks.IsEmpty() || quoteMsg.Bids.IsEmpty())
					return;

				_pnlManager.ProcessMessage(message);
				msgTime = quoteMsg.ServerTime;

				var price = quoteMsg.GetSpreadMiddle(null);
				if (price != null)
					CurrentPriceUpdated?.Invoke(quoteMsg.SecurityId, price.Value, quoteMsg.ServerTime, quoteMsg.LocalTime);

				break;
			}

			case MessageTypes.Level1Change:
			{
				var level1Msg = (Level1ChangeMessage)message;
				_pnlManager.ProcessMessage(message);
				msgTime = level1Msg.ServerTime;

				var price = level1Msg.TryGet(Level1Fields.LastTradePrice) ??
						   level1Msg.TryGet(Level1Fields.ClosePrice) ??
						   level1Msg.TryGet(Level1Fields.SpreadMiddle);

				if (price is decimal priceDec)
					CurrentPriceUpdated?.Invoke(level1Msg.SecurityId, priceDec, level1Msg.ServerTime, level1Msg.LocalTime);

				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.IsMarketData())
				{
					_pnlManager.ProcessMessage(execMsg);

					if (execMsg.TradePrice is decimal tickPrice)
						CurrentPriceUpdated?.Invoke(execMsg.SecurityId, tickPrice, execMsg.ServerTime, execMsg.LocalTime);
				}

				msgTime = execMsg.ServerTime;
				break;
			}

			case MessageTypes.Time:
			{
				var timeMsg = (TimeMessage)message;

				if (timeMsg.IsBack())
					return;

				msgTime = _host.CurrentTime;
				break;
			}

			default:
			{
				if (message is CandleMessage candleMsg)
				{
					_pnlManager.ProcessMessage(message);
					CurrentPriceUpdated?.Invoke(candleMsg.SecurityId, candleMsg.ClosePrice, candleMsg.OpenTime, candleMsg.LocalTime);
				}

				return;
			}
		}

		var unrealInterval = UnrealizedPnLInterval;
		if (msgTime == null || unrealInterval == default || (msgTime.Value - _lastPnlRefreshTime) < unrealInterval)
			return;

		_lastPnlRefreshTime = msgTime.Value;

		if (!_host.CanRefreshPnL(_lastPnlRefreshTime))
			return;

		PnLRefreshRequired?.Invoke(_lastPnlRefreshTime);
	}

	/// <summary>
	/// Force state to Stopped (for reset).
	/// </summary>
	public void ForceStop()
	{
		_processState = ProcessStates.Stopped;
		_lastPnlRefreshTime = default;
		_stopRequested = false;
	}

	/// <summary>
	/// Internal message for strategy state change requests.
	/// </summary>
	public class StrategyStateMessage : Message, IStrategyIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyStateMessage"/> class.
		/// </summary>
		/// <param name="state">The requested state.</param>
		public StrategyStateMessage(ProcessStates state)
			: this(null, state)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyStateMessage"/> class.
		/// </summary>
		/// <param name="strategyId">Strategy identifier.</param>
		/// <param name="state">The requested state.</param>
		public StrategyStateMessage(string strategyId, ProcessStates state)
			: base(_strategyChangeState)
		{
			StrategyId = strategyId;
			RequestedState = state;
		}

		/// <inheritdoc />
		public string StrategyId { get; set; }

		/// <summary>
		/// The requested state.
		/// </summary>
		public ProcessStates RequestedState { get; }

		/// <inheritdoc />
		public override Message Clone()
		{
			var clone = new StrategyStateMessage(StrategyId, RequestedState);
			CopyTo(clone);
			return clone;
		}
	}
}
