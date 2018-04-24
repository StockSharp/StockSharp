#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: StrategyClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.ServiceModel;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;

	/// <summary>
	/// The client for access to <see cref="IStrategyService"/>.
	/// </summary>
	[CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
	public class StrategyClient : BaseCommunityClient<IStrategyService>
	{
		private Timer _refreshTimer;

		private DateTime _lastCheckTime;
		private readonly CachedSynchronizedDictionary<long, StrategyData> _strategies = new CachedSynchronizedDictionary<long, StrategyData>();
		private readonly CachedSynchronizedDictionary<long, StrategySubscription> _subscriptions = new CachedSynchronizedDictionary<long, StrategySubscription>();
		private readonly CachedSynchronizedDictionary<long, StrategyBacktest> _backtests = new CachedSynchronizedDictionary<long, StrategyBacktest>();
		private readonly CachedSynchronizedDictionary<StrategyBacktest, long> _backtestResults = new CachedSynchronizedDictionary<StrategyBacktest, long>();
		private readonly CachedSynchronizedDictionary<StrategyBacktest, int> _startedBacktests = new CachedSynchronizedDictionary<StrategyBacktest, int>();

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyClient"/>.
		/// </summary>
		public StrategyClient()
			: this(new Uri("https://stocksharp.com/services/strategyservice.svc"))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyClient"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		public StrategyClient(Uri address)
			: base(address, "strategy")
		{
		}

		/// <summary>
		/// All strategies.
		/// </summary>
		public IEnumerable<StrategyData> Strategies
		{
			get
			{
				EnsureInit();
				return _strategies.CachedValues;
			}
		}

		/// <summary>
		/// Strategy subscriptions signed by <see cref="Subscribe"/>.
		/// </summary>
		public IEnumerable<StrategySubscription> Subscriptions
		{
			get
			{
				EnsureInit();
				return _subscriptions.CachedValues;
			}
		}

		/// <summary>
		/// All strategy backtests.
		/// </summary>
		public IEnumerable<StrategyBacktest> StrategyBacktests
		{
			get
			{
				EnsureInit();
				return _backtests.CachedValues;
			}
		}

		/// <summary>
		/// A new strategy was created in the store.
		/// </summary>
		public event Action<StrategyData> StrategyCreated;

		/// <summary>
		/// Existing strategy was updated in the store.
		/// </summary>
		public event Action<StrategyData> StrategyUpdated;

		/// <summary>
		/// Existing strategy was deleted from the store.
		/// </summary>
		public event Action<StrategyData> StrategyDeleted;

		/// <summary>
		/// Strategy was subscribed.
		/// </summary>
		public event Action<StrategySubscription> StrategySubscribed;

		/// <summary>
		/// Strategy was unsubscribed.
		/// </summary>
		public event Action<StrategySubscription> StrategyUnSubscribed;

		/// <summary>
		/// Backtesting process has changed.
		/// </summary>
		public event Action<StrategyBacktest, int> BacktestProgressChanged;

		/// <summary>
		/// Backtesting process has stopped.
		/// </summary>
		public event Action<StrategyBacktest> BacktestStopped;

		private readonly SyncObject _syncObject = new SyncObject();
		private bool _isProcessing;

		private void EnsureInit()
		{
			lock (_syncObject)
			{
				if (_refreshTimer != null)
					return;

				var processSubscriptions = true;

				_refreshTimer = ThreadingHelper.Timer(() =>
				{
					lock (_syncObject)
					{
						if (_isProcessing)
							return;

						_isProcessing = true;
					}

					try
					{
						Refresh();

						if (processSubscriptions)
						{
							var subscriptions = Invoke(f => f.GetSubscriptions(SessionId, DateTime.MinValue));

							foreach (var subscription in subscriptions)
							{
								_subscriptions.Add(subscription.Id, subscription);
							}

							var backtests = Invoke(f => f.GetBacktests(SessionId, DateTime.Today - TimeSpan.FromDays(5), DateTime.UtcNow));

							foreach (var backtest in backtests)
							{
								_backtests.Add(backtest.Id, backtest);
							}

							processSubscriptions = false;
						}
					}
					catch (Exception ex)
					{
						ex.LogError();
					}
					finally
					{
						lock (_syncObject)
							_isProcessing = false;
					}
				}).Interval(TimeSpan.Zero, TimeSpan.FromMinutes(1));
			}
		}

		private void Refresh()
		{
			var ids = Invoke(f => f.GetStrategies(_lastCheckTime, IsEnglish)).ToArray();

			foreach (var tuple in ids.Where(t => t.Item2 < 0))
			{
				var strategy = _strategies.TryGetValue(tuple.Item1);

				if (strategy != null)
					StrategyDeleted?.Invoke(strategy);
			}

			var newIds = new List<long>();
			var updatedIds = new List<long>();

			foreach (var tuple in ids.Where(t => t.Item2 >= 0))
			{
				var strategy = _strategies.TryGetValue(tuple.Item1);

				if (strategy != null)
					updatedIds.Add(tuple.Item1);
				else
					newIds.Add(tuple.Item1);
			}

			var newStrategies = Invoke(f => f.GetDescription(newIds.ToArray()));

			foreach (var newStrategy in newStrategies)
			{
				_strategies.Add(newStrategy.Id, newStrategy);
				StrategyCreated?.Invoke(newStrategy);
			}

			var updatedStrategies = Invoke(f => f.GetDescription(updatedIds.ToArray()));

			foreach (var updatedStrategy in updatedStrategies)
			{
				var strategy = _strategies[updatedStrategy.Id];
				CopyTo(updatedStrategy, strategy);
				StrategyUpdated?.Invoke(strategy);
			}

			_lastCheckTime = DateTime.Now;

			foreach (var backtest in _backtests.CachedValues)
			{
				if (_backtestResults.ContainsKey(backtest))
					continue;

				var resultId = Invoke(f => f.GetBacktestResult(SessionId, backtest.Id));

				if (resultId == null)
					continue;

				_backtestResults.Add(backtest, resultId.Value);
				BacktestStopped?.Invoke(backtest);

				_startedBacktests.Remove(backtest);
			}

			foreach (var backtest in _startedBacktests.CachedKeys)
			{
				var count = Invoke(f => f.GetCompletedIterationCount(SessionId, backtest.Id));
				var prevCount = _startedBacktests[backtest];

				if (count == prevCount)
					continue;

				BacktestProgressChanged?.Invoke(backtest, count);

				if (count == backtest.Iterations.Length)
					_startedBacktests.Remove(backtest);
				else
					_startedBacktests[backtest] = count;
			}
		}

		private static void CopyTo(StrategyData source, StrategyData destination)
		{
			destination.Name = source.Name;
			//destination.EnName = source.EnName;
			destination.Description = source.Description;
			//destination.EnDescription = source.EnDescription;
			destination.Price = source.Price;
			destination.Revision = source.Revision;
			destination.DescriptionId = source.DescriptionId;
			destination.Content = source.Content;
			destination.ContentType = source.ContentType;
		}

		/// <summary>
		/// To add the strategy to the store.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void CreateStrategy(StrategyData strategy)
		{
			var id = Invoke(f => f.CreateStrategy(SessionId, IsEnglish, strategy));

			if (id < 0)
				ValidateError((byte)-id, strategy.Id, strategy.Price);
			else
				strategy.Id = id;

			_strategies.Add(id, strategy);
			StrategyCreated?.Invoke(strategy);
		}

		/// <summary>
		/// To update the strategy in the store.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void UpdateStrategy(StrategyData strategy)
		{
			ValidateError(Invoke(f => f.UpdateStrategy(SessionId, strategy)), strategy.Id);
			StrategyUpdated?.Invoke(strategy);
		}

		/// <summary>
		/// To remove the strategy from the store.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void DeleteStrategy(StrategyData strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			ValidateError(Invoke(f => f.DeleteStrategy(SessionId, strategy.Id)), strategy.Id);

			_strategies.Remove(strategy.Id);
			StrategyDeleted?.Invoke(strategy);
		}

		///// <summary>
		///// To get the source or executable codes.
		///// </summary>
		///// <param name="strategy">The strategy data.</param>
		//public void Download(StrategyData strategy)
		//{
		//	if (strategy == null)
		//		throw new ArgumentNullException(nameof(strategy));

		//	var content = Invoke(f => f.GetContent(SessionId, strategy.Id));

		//	strategy.Revision = content.Revision;
		//	strategy.Content = content.Content;
		//	strategy.ContentName = content.ContentName;
		//}

		/// <summary>
		/// To subscribe for the strategy.
		/// </summary>
		/// <param name="isAutoRenew">Is auto renewable subscription.</param>
		/// <param name="strategy">The strategy data.</param>
		/// <returns>The strategy subscription.</returns>
		public StrategySubscription Subscribe(StrategyData strategy, bool isAutoRenew)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			var subscription = Invoke(f => f.Subscribe(SessionId, strategy.Id, isAutoRenew));

			if (subscription.Id < 0)
				ValidateError((byte)-subscription.Id, strategy.Id);
			else
			{
				lock (_subscriptions.SyncRoot)
				{
					var prevSubscr = _subscriptions.TryGetValue(subscription.Id);

					if (prevSubscr == null)
						_subscriptions.Add(subscription.Id, subscription);
					else
						subscription = prevSubscr;
				}

				StrategySubscribed?.Invoke(subscription);
			}

			return subscription;
		}

		/// <summary>
		/// To unsubscribe from the strategy.
		/// </summary>
		/// <param name="subscription">The strategy subscription.</param>
		public void UnSubscribe(StrategySubscription subscription)
		{
			if (subscription == null)
				throw new ArgumentNullException(nameof(subscription));

			ValidateError(Invoke(f => f.UnSubscribe(SessionId, subscription.Id)));

			StrategyUnSubscribed?.Invoke(subscription);
			_subscriptions.Remove(subscription.Id);
		}

		/// <summary>
		/// To get an approximate of money to spend for the specified backtesting configuration.
		/// </summary>
		/// <param name="backtest">Backtesting session.</param>
		/// <returns>An approximate of money.</returns>
		public decimal GetApproximateAmount(StrategyBacktest backtest)
		{
			return Invoke(f => f.GetApproximateAmount(SessionId, backtest));
		}

		/// <summary>
		/// To start backtesing.
		/// </summary>
		/// <param name="backtest">Backtesting session.</param>
		public void StartBacktest(StrategyBacktest backtest)
		{
			backtest.Id = Invoke(f => f.StartBacktest(SessionId, backtest));
			_backtests.Add(backtest.Id, backtest);
			_startedBacktests.Add(backtest, 0);
		}

		/// <summary>
		/// To stop the backtesing.
		/// </summary>
		/// <param name="backtest">Backtesting session.</param>
		public void StopBacktest(StrategyBacktest backtest)
		{
			ValidateError(Invoke(f => f.StopBacktest(SessionId, backtest.Id)));
		}

		/// <summary>
		/// Get strategy info.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <returns>The strategy data.</returns>
		public StrategyData GetDescription(long id)
		{
			return Invoke(f => f.GetDescription(new[] { id }))?.FirstOrDefault();
		}

		private static void ValidateError(byte errorCode, params object[] args)
		{
			((ErrorCodes)errorCode).ThrowIfError(args);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			lock (_syncObject)
			{
				if (_refreshTimer != null)
				{
					_refreshTimer.Dispose();
					_refreshTimer = null;
				}
			}

			base.DisposeManaged();
		}
	}
}