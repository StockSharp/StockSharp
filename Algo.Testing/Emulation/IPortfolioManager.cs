namespace StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Position information for a security.
/// </summary>
public class PositionInfo(SecurityId securityId)
{
	/// <summary>
	/// Security ID.
	/// </summary>
	public SecurityId SecurityId { get; } = securityId;

	/// <summary>
	/// Begin value (initial position).
	/// </summary>
	public decimal BeginValue { get; set; }

	/// <summary>
	/// Position change since begin.
	/// </summary>
	public decimal Diff { get; set; }

	/// <summary>
	/// Current position value.
	/// </summary>
	public decimal CurrentValue => BeginValue + Diff;

	/// <summary>
	/// Average entry price.
	/// </summary>
	public decimal AveragePrice { get; set; }

	/// <summary>
	/// Total volume of active buy orders.
	/// </summary>
	public decimal TotalBidsVolume { get; set; }

	/// <summary>
	/// Total volume of active sell orders.
	/// </summary>
	public decimal TotalAsksVolume { get; set; }

	/// <summary>
	/// Total value of active buy orders (volume * price).
	/// </summary>
	public decimal TotalBidsValue { get; set; }

	/// <summary>
	/// Total value of active sell orders (volume * price).
	/// </summary>
	public decimal TotalAsksValue { get; set; }

	/// <summary>
	/// Margin leverage for this position. Null means default (1x).
	/// </summary>
	public decimal? Leverage { get; set; }
}

/// <summary>
/// Result of trade processing.
/// </summary>
/// <param name="RealizedPnL">Realized PnL from the trade.</param>
/// <param name="PositionChange">Position change amount.</param>
/// <param name="Position">Updated position info.</param>
public record TradeProcessingResult(decimal RealizedPnL, decimal PositionChange, PositionInfo Position);

/// <summary>
/// Interface for managing a single portfolio's state.
/// </summary>
public interface IPortfolio
{
	/// <summary>
	/// Portfolio name.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Initial money amount.
	/// </summary>
	decimal BeginMoney { get; }

	/// <summary>
	/// Current money (begin + PnL).
	/// </summary>
	decimal CurrentMoney { get; }

	/// <summary>
	/// Available money (current - blocked).
	/// </summary>
	decimal AvailableMoney { get; }

	/// <summary>
	/// Total realized PnL.
	/// </summary>
	decimal RealizedPnL { get; }

	/// <summary>
	/// Total PnL (realized - commission).
	/// </summary>
	decimal TotalPnL { get; }

	/// <summary>
	/// Blocked money for pending orders.
	/// </summary>
	decimal BlockedMoney { get; }

	/// <summary>
	/// Total commission paid.
	/// </summary>
	decimal Commission { get; }

	/// <summary>
	/// Margin call level threshold. When margin level falls to this value, a warning is triggered.
	/// </summary>
	decimal MarginCallLevel { get; set; }

	/// <summary>
	/// Stop-out level threshold. When margin level falls to this value, positions are liquidated.
	/// </summary>
	decimal StopOutLevel { get; set; }

	/// <summary>
	/// Enable automatic position liquidation on stop-out.
	/// </summary>
	bool EnableStopOut { get; set; }

	/// <summary>
	/// Set initial money.
	/// </summary>
	/// <param name="money">Money amount.</param>
	void SetMoney(decimal money);

	/// <summary>
	/// Set initial position.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="volume">Position volume.</param>
	/// <param name="avgPrice">Average entry price.</param>
	void SetPosition(SecurityId securityId, decimal volume, decimal avgPrice = 0);

	/// <summary>
	/// Get position for security.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <returns>Position info or null.</returns>
	PositionInfo GetPosition(SecurityId securityId);

	/// <summary>
	/// Process a trade execution.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="side">Trade side.</param>
	/// <param name="price">Trade price.</param>
	/// <param name="volume">Trade volume.</param>
	/// <param name="commission">Commission amount.</param>
	/// <returns>Trade processing result.</returns>
	TradeProcessingResult ProcessTrade(SecurityId securityId, Sides side, decimal price, decimal volume, decimal? commission = null);

	/// <summary>
	/// Process order registration (block funds).
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="side">Order side.</param>
	/// <param name="volume">Order volume.</param>
	/// <param name="price">Order price for margin calculation.</param>
	void ProcessOrderRegistration(SecurityId securityId, Sides side, decimal volume, decimal price);

	/// <summary>
	/// Process order cancellation (unblock funds).
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="side">Order side.</param>
	/// <param name="volume">Cancelled volume.</param>
	/// <param name="price">Price used for margin calculation.</param>
	void ProcessOrderCancellation(SecurityId securityId, Sides side, decimal volume, decimal price = 0);

	/// <summary>
	/// Get all positions.
	/// </summary>
	/// <returns>Enumeration of positions.</returns>
	IEnumerable<(SecurityId securityId, decimal volume, decimal avgPrice)> GetPositions();

	/// <summary>
	/// Get all position info objects.
	/// </summary>
	IEnumerable<PositionInfo> GetAllPositions();

	/// <summary>
	/// Calculate unrealized PnL across all positions.
	/// </summary>
	/// <param name="getCurrentPrice">Function to get current market price for a security. Returns null if price unavailable.</param>
	/// <returns>Total unrealized PnL.</returns>
	decimal CalculateUnrealizedPnL(Func<SecurityId, decimal?> getCurrentPrice);
}

/// <summary>
/// Interface for managing multiple portfolios.
/// Abstracts portfolio state management to support both emulated and real portfolios.
/// </summary>
public interface IPortfolioManager
{
	/// <summary>
	/// Get or create a portfolio by name.
	/// </summary>
	/// <param name="name">Portfolio name.</param>
	/// <returns>Portfolio instance.</returns>
	IPortfolio GetPortfolio(string name);

	/// <summary>
	/// Check if portfolio exists.
	/// </summary>
	/// <param name="name">Portfolio name.</param>
	/// <returns>True if exists.</returns>
	bool HasPortfolio(string name);

	/// <summary>
	/// Get all portfolios.
	/// </summary>
	IEnumerable<IPortfolio> GetAllPortfolios();

	/// <summary>
	/// Validate that portfolio has sufficient funds for order registration.
	/// </summary>
	/// <param name="portfolioName">Portfolio name.</param>
	/// <param name="securityId">Security ID (for per-position leverage).</param>
	/// <param name="price">Order price.</param>
	/// <param name="volume">Order volume.</param>
	/// <returns>Error if insufficient funds, null otherwise.</returns>
	InvalidOperationException ValidateFunds(string portfolioName, SecurityId securityId, decimal price, decimal volume);

	/// <summary>
	/// Clear all portfolio state.
	/// </summary>
	void Clear();
}
