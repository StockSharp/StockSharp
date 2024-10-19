namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// The statistics manager.
/// </summary>
public interface IStatisticManager : IPersistable
{
	/// <summary>
	/// Calculated parameters.
	/// </summary>
	public IStatisticParameter[] Parameters { get; }

	/// <summary>
	/// To add the new profit-loss value.
	/// </summary>
	/// <param name="time">The change time <paramref name="pnl" />.</param>
	/// <param name="pnl">New profit-loss value.</param>
	/// <param name="commission">Commission.</param>
	void AddPnL(DateTimeOffset time, decimal pnl, decimal? commission);

	/// <summary>
	/// To add the new position value.
	/// </summary>
	/// <param name="time">The change time <paramref name="position" />.</param>
	/// <param name="position">The new position value.</param>
	void AddPosition(DateTimeOffset time, decimal position);

	/// <summary>
	/// To add information about new trade.
	/// </summary>
	/// <param name="info">Information on new trade.</param>
	void AddMyTrade(PnLInfo info);

	/// <summary>
	/// To add new order.
	/// </summary>
	/// <param name="order">New order.</param>
	void AddNewOrder(Order order);

	/// <summary>
	/// To add the changed order.
	/// </summary>
	/// <param name="order">The changed order.</param>
	void AddChangedOrder(Order order);

	/// <summary>
	/// To add the order registration error.
	/// </summary>
	/// <param name="fail">Error registering order.</param>
	void AddRegisterFailedOrder(OrderFail fail);

	/// <summary>
	/// To add the order cancelling error.
	/// </summary>
	/// <param name="fail">The order error.</param>
	void AddFailedOrderCancel(OrderFail fail);

	/// <summary>
	/// To clear data on equity.
	/// </summary>
	void Reset();
}