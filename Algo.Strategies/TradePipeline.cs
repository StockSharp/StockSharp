namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.PnL;
using StockSharp.Algo.Statistics;

/// <summary>
/// Trade processing. Handles deduplication, PnL calculation, commission/slippage accumulation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TradePipeline"/>.
/// </remarks>
/// <param name="pnlManager">PnL manager.</param>
/// <param name="stats">Statistic manager.</param>
/// <param name="feedSecurity">Pushes the traded security's valuation snapshot into the PnL manager.</param>
public class TradePipeline(IPnLManager pnlManager, IStatisticManager stats, Action<MyTrade> feedSecurity)
{
	private sealed class ProcessingState
	{
		public bool BeforeProcessingCompleted { get; set; }
		public bool CommissionCompleted { get; set; }
		public bool SecurityFeedCompleted { get; set; }
		public bool PnLCompleted { get; set; }
		public bool StatisticsCompleted { get; set; }
		public bool SlippageCompleted { get; set; }
		public bool CommissionChanged { get; set; }
		public bool SlippageChanged { get; set; }
		public PnLInfo TradeInfo { get; set; }
		public DateTime? PnLChangeTime { get; set; }
	}

	private readonly CachedSynchronizedSet<MyTrade> _myTrades = [];
	private readonly HashSet<MyTrade> _processingTrades = [];
	private readonly Dictionary<MyTrade, ProcessingState> _processingStates = [];
	private readonly Action<MyTrade> _feedSecurity = feedSecurity ?? throw new ArgumentNullException(nameof(feedSecurity));
	private IPnLManager _pnlManager = pnlManager ?? throw new ArgumentNullException(nameof(pnlManager));
	private IStatisticManager _stats = stats ?? throw new ArgumentNullException(nameof(stats));

	/// <summary>
	/// Swap the PnL manager (used when <see cref="Strategy.PnLManager"/> is reassigned).
	/// </summary>
	/// <param name="pnlManager">The new PnL manager.</param>
	public void SetPnLManager(IPnLManager pnlManager)
		=> _pnlManager = pnlManager ?? throw new ArgumentNullException(nameof(pnlManager));

	/// <summary>
	/// Swap the statistic manager (used when <see cref="Strategy.StatisticManager"/> is reassigned).
	/// </summary>
	/// <param name="stats">The new statistic manager.</param>
	public void SetStatisticManager(IStatisticManager stats)
		=> _stats = stats ?? throw new ArgumentNullException(nameof(stats));

	/// <summary>
	/// Total accumulated commission.
	/// </summary>
	public decimal? Commission { get; private set; }

	/// <summary>
	/// Total accumulated slippage.
	/// </summary>
	public decimal? Slippage { get; private set; }

	/// <summary>
	/// Fires when a new trade is successfully added.
	/// </summary>
	public event Action<MyTrade> TradeAdded;

	/// <summary>
	/// Fires when PnL changes due to a trade.
	/// </summary>
	public event Action<DateTime> PnLChanged;

	/// <summary>
	/// Fires when commission changes.
	/// </summary>
	public event Action CommissionChanged;

	/// <summary>
	/// Fires when slippage changes.
	/// </summary>
	public event Action SlippageChanged;

	/// <summary>
	/// Try to add a trade. Returns false if duplicate.
	/// Processes PnL, commission, slippage.
	/// </summary>
	/// <remarks>
	/// If an internal processing stage throws, the same trade instance can be submitted again. Stages
	/// that returned successfully are remembered and are not invoked again by that retry.
	/// </remarks>
	public bool TryAdd(MyTrade trade)
		=> TryAdd(trade, null);

	internal bool TryAdd(MyTrade trade, Action<MyTrade> beforeProcessing)
	{
		if (trade is null)
			throw new ArgumentNullException(nameof(trade));

		ProcessingState state;

		using (_myTrades.EnterScope())
		{
			if (_myTrades.Contains(trade) || !_processingTrades.Add(trade))
				return false;

			if (!_processingStates.TryGetValue(trade, out state))
				_processingStates.Add(trade, state = new());
		}

		try
		{
			if (!state.BeforeProcessingCompleted)
			{
				beforeProcessing?.Invoke(trade);
				state.BeforeProcessingCompleted = true;
			}

			if (!state.CommissionCompleted)
			{
				if (trade.Commission != null)
				{
					Commission ??= 0;
					Commission += trade.Commission.Value;
					state.CommissionChanged = true;
				}

				state.CommissionCompleted = true;
			}

			// The PnL queue values this trade with the security's price step, step price and multiplier,
			// so the snapshot has to reach the manager before the trade does. A successful stage is
			// remembered so a retry after a later failure does not apply it twice.
			if (!state.SecurityFeedCompleted)
			{
				_feedSecurity(trade);
				state.SecurityFeedCompleted = true;
			}

			if (!state.PnLCompleted)
			{
				var execMsg = trade.ToMessage();
				state.TradeInfo = _pnlManager.ProcessMessage(execMsg);

				if (state.TradeInfo is { } tradeInfo && tradeInfo.PnL != 0)
				{
					state.PnLChangeTime = execMsg.LocalTime;
					trade.PnL ??= tradeInfo.PnL;
				}

				state.PnLCompleted = true;
			}

			if (!state.StatisticsCompleted)
			{
				if (state.TradeInfo is not null)
					_stats.AddMyTrade(state.TradeInfo);

				state.StatisticsCompleted = true;
			}

			if (!state.SlippageCompleted)
			{
				if (trade.Slippage is decimal slippage)
				{
					Slippage = (Slippage ?? 0) + slippage;
					state.SlippageChanged = true;
				}

				state.SlippageCompleted = true;
			}
		}
		catch
		{
			using (_myTrades.EnterScope())
				_processingTrades.Remove(trade);

			throw;
		}

		using (_myTrades.EnterScope())
		{
			if (!_processingTrades.Remove(trade))
				return false;

			_processingStates.Remove(trade);
			_myTrades.Add(trade);
		}

		TradeAdded?.Invoke(trade);

		if (state.CommissionChanged)
			CommissionChanged?.Invoke();

		if (state.PnLChangeTime is not null)
			PnLChanged?.Invoke(state.PnLChangeTime.Value);

		if (state.SlippageChanged)
			SlippageChanged?.Invoke();

		return true;
	}

	/// <summary>
	/// Check whether the trade is already tracked.
	/// </summary>
	public bool Contains(MyTrade trade) => _myTrades.Contains(trade);

	/// <summary>
	/// All tracked trades.
	/// </summary>
	public IEnumerable<MyTrade> MyTrades => _myTrades.Cache;

	/// <summary>
	/// Clear all data.
	/// </summary>
	public void Reset()
	{
		using (_myTrades.EnterScope())
		{
			_myTrades.Clear();
			_processingTrades.Clear();
			_processingStates.Clear();
		}

		Commission = default;
		Slippage = default;
	}
}
