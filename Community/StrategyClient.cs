namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel;
	using System.Linq;

	using Ecng.Collections;

	/// <summary>
	/// Клиент для доступа к <see cref="IStrategyService"/>.
	/// </summary>
	[CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
	public class StrategyClient : BaseCommunityClient<IStrategyService>, IStrategyServiceCallback
	{
		private readonly CachedSynchronizedDictionary<long, StrategyData> _strategies = new CachedSynchronizedDictionary<long, StrategyData>();
		private readonly CachedSynchronizedList<long> _subscribedStrategies = new CachedSynchronizedList<long>(); 

		/// <summary>
		/// Создать <see cref="StrategyClient"/>.
		/// </summary>
		public StrategyClient()
			: this(new Uri("http://stocksharp.com/services/strategyservice.svc"))
		{
		}

		/// <summary>
		/// Создать <see cref="StrategyClient"/>.
		/// </summary>
		/// <param name="address">Адрес сервера.</param>
		public StrategyClient(Uri address)
			: base(address, "strategy", true)
		{
		}

		/// <summary>
		/// Стратегий, подписанные через <see cref="Subscribe"/>.
		/// </summary>
		public IEnumerable<StrategyData> SubscribedStrategies
		{
			get { return _subscribedStrategies.Cache.Select(id => _strategies[id]); }
		}

		/// <summary>
		/// Подключиться.
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
		/// Добавить стратегию в магазин.
		/// </summary>
		/// <param name="strategy">Данные о стратегии.</param>
		public void CreateStrategy(StrategyData strategy)
		{
			strategy.Id = Invoke(f => f.CreateStrategy(SessionId, strategy));
		}

		/// <summary>
		/// Обновить стратегию в магазине.
		/// </summary>
		/// <param name="strategy">Данные о стратегии.</param>
		public void UpdateStrategy(StrategyData strategy)
		{
			Invoke(f => f.UpdateStrategy(SessionId, strategy));
		}

		/// <summary>
		/// Удалить стратегию из магазина.
		/// </summary>
		/// <param name="strategy">Данные о стратегии.</param>
		public void DeleteStrategy(StrategyData strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			Invoke(f => f.DeleteStrategy(SessionId, strategy.Id));
		}

		/// <summary>
		/// Получить полное описание стратегии, включая исходный и исполняемый коды.
		/// </summary>
		/// <param name="strategy">Данные о стратегии.</param>
		public void Download(StrategyData strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			var info = Invoke(f => f.GetFullInfo(SessionId, strategy.Id));

			strategy.SourceCode = info.SourceCode;
			strategy.CompiledAssembly = info.CompiledAssembly;
		}

		/// <summary>
		/// Подписаться на стратегию.
		/// </summary>
		/// <param name="strategy">Данные о стратегии.</param>
		public void Subscribe(StrategyData strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			Invoke(f => f.Subscribe(SessionId, strategy.Id));
		}

		/// <summary>
		/// Отписаться от стратегии.
		/// </summary>
		/// <param name="strategy">Данные о стратегии.</param>
		public void UnSubscribe(StrategyData strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

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