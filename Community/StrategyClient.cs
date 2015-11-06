namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel;
	using System.Linq;

	using Ecng.Collections;

	/// <summary>
	/// The client for access to <see cref="IStrategyService"/>.
	/// </summary>
	[CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
	public class StrategyClient : BaseCommunityClient<IStrategyService>, IStrategyServiceCallback
	{
		private readonly CachedSynchronizedDictionary<long, StrategyData> _strategies = new CachedSynchronizedDictionary<long, StrategyData>();
		private readonly CachedSynchronizedList<long> _subscribedStrategies = new CachedSynchronizedList<long>(); 

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
			: base(address, "strategy", true)
		{
		}

		/// <summary>
		/// Strategies signed by <see cref="Subscribe"/>.
		/// </summary>
		public IEnumerable<StrategyData> SubscribedStrategies
		{
			get { return _subscribedStrategies.Cache.Select(id => _strategies[id]); }
		}

		/// <summary>
		/// Connect.
		/// </summary>
		public override void Connect()
		{
			base.Connect();

			var ids = Invoke(f => f.GetStrategies(SessionId));
			var strategies = Invoke(f => f.GetLiteInfo(SessionId, ids.ToArray()));

			foreach (var strategy in strategies)
				_strategies.Add(strategy.Id, strategy);

			_subscribedStrategies.AddRange(Invoke(f => f.GetSubscribedStrategies(SessionId)));
		}

		/// <summary>
		/// To add the strategy to the store .
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void CreateStrategy(StrategyData strategy)
		{
			strategy.Id = Invoke(f => f.CreateStrategy(SessionId, strategy));
		}

		/// <summary>
		/// To update the strategy in the store.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void UpdateStrategy(StrategyData strategy)
		{
			Invoke(f => f.UpdateStrategy(SessionId, strategy));
		}

		/// <summary>
		/// To remove the strategy from the store.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void DeleteStrategy(StrategyData strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			Invoke(f => f.DeleteStrategy(SessionId, strategy.Id));
		}

		/// <summary>
		/// To get the complete description of the strategy, including the source and executable codes.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void Download(StrategyData strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			var info = Invoke(f => f.GetFullInfo(SessionId, strategy.Id));

			strategy.SourceCode = info.SourceCode;
			strategy.CompiledAssembly = info.CompiledAssembly;
		}

		/// <summary>
		/// To subscribe for the strategy.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void Subscribe(StrategyData strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			Invoke(f => f.Subscribe(SessionId, strategy.Id));
		}

		/// <summary>
		/// To unsubscribe from the strategy.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		public void UnSubscribe(StrategyData strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			Invoke(f => f.UnSubscribe(SessionId, strategy.Id));
		}

		void IStrategyServiceCallback.Created(StrategyData strategy)
		{
			
		}

		void IStrategyServiceCallback.Deleted(long strategyId)
		{
			
		}

		void IStrategyServiceCallback.Updated(StrategyData strategy)
		{
			
		}

		void IStrategyServiceCallback.Subscribed(long strategyId, long userId)
		{
			
		}

		void IStrategyServiceCallback.UnSubscribed(long strategyId, long userId)
		{
			
		}
	}
}