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
	using System.ServiceModel.Description;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;

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

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyClient"/>.
		/// </summary>
		public StrategyClient()
			: this(new Uri("http://stocksharp.com/services/strategyservice.svc"))
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
		/// Create WCF channel.
		/// </summary>
		/// <returns>WCF channel.</returns>
		protected override ChannelFactory<IStrategyService> CreateChannel()
		{
			var f = new ChannelFactory<IStrategyService>(new WSHttpBinding(SecurityMode.None)
			{
				OpenTimeout = TimeSpan.FromMinutes(5),
				SendTimeout = TimeSpan.FromMinutes(10),
				ReceiveTimeout = TimeSpan.FromMinutes(10),
				MaxReceivedMessageSize = int.MaxValue,
				ReaderQuotas =
				{
					MaxArrayLength = int.MaxValue,
					MaxBytesPerRead = int.MaxValue
				},
				MaxBufferPoolSize = int.MaxValue,
			}, new EndpointAddress(Address));

			foreach (var op in f.Endpoint.Contract.Operations)
			{
				var dataContractBehavior = op.Behaviors[typeof(DataContractSerializerOperationBehavior)] as DataContractSerializerOperationBehavior;

				if (dataContractBehavior != null)
					dataContractBehavior.MaxItemsInObjectGraph = int.MaxValue;
			}

			return f;
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

		private void EnsureInit()
		{
			if (_refreshTimer != null)
				return;

			Refresh();

			var subscriptions = Invoke(f => f.GetSubscriptions(SessionId, DateTime.MinValue));

			foreach (var subscription in subscriptions)
			{
				_subscriptions.Add(subscription.Id, subscription);
			}

			_refreshTimer = ThreadingHelper
				.Timer(Refresh)
				.Interval(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
		}

		private void Refresh()
		{
			var ids = Invoke(f => f.GetStrategies(_lastCheckTime)).ToArray();

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
		}

		private static void CopyTo(StrategyData source, StrategyData destination)
		{
			destination.RuName = source.RuName;
			destination.EnName = source.EnName;
			destination.RuDescription = source.RuDescription;
			destination.EnDescription = source.EnDescription;
			destination.Price = source.Price;
			destination.Revision = source.Revision;
			destination.DescriptionId = source.DescriptionId;
			destination.ContentName = source.ContentName;
			destination.ContentType = source.ContentType;
		}

		/// <summary>
		/// To add the strategy to the store.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void CreateStrategy(StrategyData strategy)
		{
			var id = Invoke(f => f.CreateStrategy(SessionId, strategy));

			if (id < 0)
				ValidateError((byte)-id, strategy.Id, strategy.Price);
			else
				strategy.Id = id;

			_strategies.Add(id, strategy);
		}

		/// <summary>
		/// To update the strategy in the store.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void UpdateStrategy(StrategyData strategy)
		{
			ValidateError(Invoke(f => f.UpdateStrategy(SessionId, strategy)), strategy.Id);
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
		}

		/// <summary>
		/// To get the the source and executable codes.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void Download(StrategyData strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			var content = Invoke(f => f.GetContent(SessionId, strategy.Id));

			strategy.Revision = content.Revision;
			strategy.Content = content.Content;
			strategy.ContentName = content.ContentName;
		}

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

		private static void ValidateError(byte errorCode, params object[] args)
		{
			((ErrorCodes)errorCode).ThrowIfError(args);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_refreshTimer?.Dispose();
			base.DisposeManaged();
		}
	}
}