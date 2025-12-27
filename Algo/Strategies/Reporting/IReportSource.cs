namespace StockSharp.Algo.Strategies.Reporting;

/// <summary>
/// Order data for reports.
/// </summary>
/// <param name="Id">Order id.</param>
/// <param name="TransactionId">Transaction id.</param>
/// <param name="Side">Side (buy/sell).</param>
/// <param name="Time">Order time.</param>
/// <param name="Price">Order price.</param>
/// <param name="State">Order state.</param>
/// <param name="Balance">Balance.</param>
/// <param name="Volume">Volume.</param>
/// <param name="Type">Order type.</param>
public record ReportOrder(
	long? Id,
	long TransactionId,
	Sides Side,
	DateTimeOffset Time,
	decimal Price,
	OrderStates? State,
	decimal? Balance,
	decimal? Volume,
	OrderTypes? Type
);

/// <summary>
/// Trade data for reports.
/// </summary>
/// <param name="TradeId">Trade id.</param>
/// <param name="OrderTransactionId">Order transaction id.</param>
/// <param name="Time">Trade time.</param>
/// <param name="TradePrice">Trade price.</param>
/// <param name="OrderPrice">Order price.</param>
/// <param name="Volume">Volume.</param>
/// <param name="Side">Side.</param>
/// <param name="OrderId">Order id.</param>
/// <param name="Slippage">Slippage.</param>
/// <param name="PnL">Profit-loss.</param>
/// <param name="Position">Position change.</param>
public record ReportTrade(
	long? TradeId,
	long OrderTransactionId,
	DateTimeOffset Time,
	decimal TradePrice,
	decimal OrderPrice,
	decimal Volume,
	Sides Side,
	long? OrderId,
	decimal? Slippage,
	decimal? PnL,
	decimal? Position
);

/// <summary>
/// The interface for providing data to report generators.
/// Decouples reports from concrete <see cref="Strategy"/> implementation.
/// </summary>
public interface IReportSource
{
	/// <summary>
	/// Strategy name.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// The total time of strategy operation.
	/// </summary>
	TimeSpan TotalWorkingTime { get; }

	/// <summary>
	/// Total commission.
	/// </summary>
	decimal? Commission { get; }

	/// <summary>
	/// Current position.
	/// </summary>
	decimal Position { get; }

	/// <summary>
	/// Total profit-loss.
	/// </summary>
	decimal PnL { get; }

	/// <summary>
	/// Total slippage.
	/// </summary>
	decimal? Slippage { get; }

	/// <summary>
	/// Total latency.
	/// </summary>
	TimeSpan? Latency { get; }

	/// <summary>
	/// Get strategy parameters as name-value pairs.
	/// </summary>
	IEnumerable<(string Name, object Value)> Parameters { get; }

	/// <summary>
	/// Statistic parameters as name-value pairs.
	/// </summary>
	IEnumerable<(string Name, object Value)> StatisticParameters { get; }

	/// <summary>
	/// Orders collection.
	/// </summary>
	IEnumerable<ReportOrder> Orders { get; }

	/// <summary>
	/// Own trades collection.
	/// </summary>
	IEnumerable<ReportTrade> MyTrades { get; }
}
