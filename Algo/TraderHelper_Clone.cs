namespace StockSharp.Algo;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.Latency;
using StockSharp.Algo.PnL;
using StockSharp.Algo.Positions;
using StockSharp.Algo.Risk;
using StockSharp.Algo.Slippage;

partial class TraderHelper
{
	private const string _blockingClone = "Blocking sync-over-async wrapper. Use CloneAsync instead.";

	/// <summary>
	/// Create a copy of the manager.
	/// </summary>
	/// <param name="manager"><see cref="IRiskManager"/></param>
	/// <returns>Copy.</returns>
	[Obsolete(_blockingClone)]
	public static IRiskManager Clone(this IRiskManager manager)
	{
		if (manager is null)
			throw new ArgumentNullException(nameof(manager));

		return AsyncHelper.Run(() => manager.CloneAsync(default));
	}

	/// <summary>
	/// Create a copy of the manager.
	/// </summary>
	/// <param name="manager"><see cref="ICommissionManager"/></param>
	/// <returns>Copy.</returns>
	[Obsolete(_blockingClone)]
	public static ICommissionManager Clone(this ICommissionManager manager)
	{
		if (manager is null)
			throw new ArgumentNullException(nameof(manager));

		return AsyncHelper.Run(() => manager.CloneAsync(default));
	}

	/// <summary>
	/// Create a copy of the manager.
	/// </summary>
	/// <param name="manager"><see cref="ILatencyManager"/></param>
	/// <returns>Copy.</returns>
	[Obsolete(_blockingClone)]
	public static ILatencyManager Clone(this ILatencyManager manager)
	{
		if (manager is null)
			throw new ArgumentNullException(nameof(manager));

		return AsyncHelper.Run(() => manager.CloneAsync(default));
	}

	/// <summary>
	/// Create a copy of the manager.
	/// </summary>
	/// <param name="manager"><see cref="ISlippageManager"/></param>
	/// <returns>Copy.</returns>
	[Obsolete(_blockingClone)]
	public static ISlippageManager Clone(this ISlippageManager manager)
	{
		if (manager is null)
			throw new ArgumentNullException(nameof(manager));

		return AsyncHelper.Run(() => manager.CloneAsync(default));
	}

	/// <summary>
	/// Create a copy of the manager.
	/// </summary>
	/// <param name="manager"><see cref="IPnLManager"/></param>
	/// <returns>Copy.</returns>
	[Obsolete(_blockingClone)]
	public static IPnLManager Clone(this IPnLManager manager)
	{
		if (manager is null)
			throw new ArgumentNullException(nameof(manager));

		return AsyncHelper.Run(() => manager.CloneAsync(default));
	}

	/// <summary>
	/// Create a copy of the manager.
	/// </summary>
	/// <param name="manager"><see cref="IPositionManager"/></param>
	/// <returns>Copy.</returns>
	[Obsolete(_blockingClone)]
	public static IPositionManager Clone(this IPositionManager manager)
	{
		if (manager is null)
			throw new ArgumentNullException(nameof(manager));

		return AsyncHelper.Run(() => manager.CloneAsync(default));
	}

	/// <summary>
	/// Create a copy of the buffer.
	/// </summary>
	/// <param name="buffer"><see cref="IStorageBuffer"/></param>
	/// <returns>Copy.</returns>
	[Obsolete(_blockingClone)]
	public static IStorageBuffer Clone(this IStorageBuffer buffer)
	{
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		return AsyncHelper.Run(() => buffer.CloneAsync(default));
	}
}
