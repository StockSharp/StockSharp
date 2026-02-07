namespace StockSharp.Algo.Strategies.Decomposed;

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
public class TradePipeline(IPnLManager pnlManager, IStatisticManager stats)
{
	private readonly CachedSynchronizedSet<MyTrade> _myTrades = [];
	private readonly IPnLManager _pnlManager = pnlManager ?? throw new ArgumentNullException(nameof(pnlManager));
	private readonly IStatisticManager _stats = stats ?? throw new ArgumentNullException(nameof(stats));

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
	public bool TryAdd(MyTrade trade)
	{
		if (trade is null)
			throw new ArgumentNullException(nameof(trade));

		if (!_myTrades.TryAdd(trade))
			return false;

		var isComChanged = false;
		var isSlipChanged = false;

		if (trade.Commission != null)
		{
			Commission ??= 0;
			Commission += trade.Commission.Value;
			isComChanged = true;
		}

		var execMsg = trade.ToMessage();
		DateTime? pnLChangeTime = null;

		var tradeInfo = _pnlManager.ProcessMessage(execMsg);

		if (tradeInfo != null)
		{
			if (tradeInfo.PnL != 0)
			{
				pnLChangeTime = execMsg.LocalTime;
				trade.PnL ??= tradeInfo.PnL;
			}

			_stats.AddMyTrade(tradeInfo);
		}

		if (trade.Slippage is decimal slippage)
		{
			Slippage = (Slippage ?? 0) + slippage;
			isSlipChanged = true;
		}

		TradeAdded?.Invoke(trade);

		if (isComChanged)
			CommissionChanged?.Invoke();

		if (pnLChangeTime is not null)
			PnLChanged?.Invoke(pnLChangeTime.Value);

		if (isSlipChanged)
			SlippageChanged?.Invoke();

		return true;
	}

	/// <summary>
	/// All tracked trades.
	/// </summary>
	public IEnumerable<MyTrade> MyTrades => _myTrades.Cache;

	/// <summary>
	/// Clear all data.
	/// </summary>
	public void Reset()
	{
		_myTrades.Clear();
		Commission = default;
		Slippage = default;
	}
}
