namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The interface describing a client for access to <see cref="IStrategyService"/>.
	/// </summary>
	public interface IStrategyClient
	{
		/// <summary>
		/// All strategies.
		/// </summary>
		IEnumerable<StrategyData> Strategies { get; }

		/// <summary>
		/// Strategy subscriptions signed by <see cref="Subscribe"/>.
		/// </summary>
		IEnumerable<StrategySubscription> Subscriptions { get; }

		/// <summary>
		/// All strategy backtests.
		/// </summary>
		IEnumerable<StrategyBacktest> StrategyBacktests { get; }

		/// <summary>
		/// A new strategy was created in the store.
		/// </summary>
		event Action<StrategyData> StrategyCreated;

		/// <summary>
		/// Existing strategy was updated in the store.
		/// </summary>
		event Action<StrategyData> StrategyUpdated;

		/// <summary>
		/// Existing strategy was deleted from the store.
		/// </summary>
		event Action<StrategyData> StrategyDeleted;

		/// <summary>
		/// Strategy was subscribed.
		/// </summary>
		event Action<StrategySubscription> StrategySubscribed;

		/// <summary>
		/// Strategy was unsubscribed.
		/// </summary>
		event Action<StrategySubscription> StrategyUnSubscribed;

		/// <summary>
		/// Backtesting process has changed.
		/// </summary>
		event Action<StrategyBacktest, int> BacktestProgressChanged;

		/// <summary>
		/// Backtesting process has stopped.
		/// </summary>
		event Action<StrategyBacktest> BacktestStopped;

		/// <summary>
		/// To add the strategy to the store.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		void CreateStrategy(StrategyData strategy);

		/// <summary>
		/// To update the strategy in the store.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		void UpdateStrategy(StrategyData strategy);

		/// <summary>
		/// To remove the strategy from the store.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		void DeleteStrategy(StrategyData strategy);

		/// <summary>
		/// To subscribe for the strategy.
		/// </summary>
		/// <param name="isAutoRenew">Is auto renewable subscription.</param>
		/// <param name="strategy">The strategy data.</param>
		/// <returns>The strategy subscription.</returns>
		StrategySubscription Subscribe(StrategyData strategy, bool isAutoRenew);

		/// <summary>
		/// To unsubscribe from the strategy.
		/// </summary>
		/// <param name="subscription">The strategy subscription.</param>
		void UnSubscribe(StrategySubscription subscription);

		/// <summary>
		/// To get an approximate of money to spend for the specified backtesting configuration.
		/// </summary>
		/// <param name="backtest">Backtesting session.</param>
		/// <returns>An approximate of money.</returns>
		decimal GetApproximateAmount(StrategyBacktest backtest);

		/// <summary>
		/// To start backtesing.
		/// </summary>
		/// <param name="backtest">Backtesting session.</param>
		void StartBacktest(StrategyBacktest backtest);

		/// <summary>
		/// To stop the backtesing.
		/// </summary>
		/// <param name="backtest">Backtesting session.</param>
		void StopBacktest(StrategyBacktest backtest);

		/// <summary>
		/// Get strategy info.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <returns>The strategy data.</returns>
		StrategyData GetDescription(long id);
	}
}