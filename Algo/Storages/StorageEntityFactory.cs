namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Storage based entity factory (<see cref="Security"/>, <see cref="Order"/> etc.).
	/// </summary>
	public class StorageEntityFactory : EntityFactory
	{
		private readonly IStorageRegistry _storageRegistry;
		private readonly Dictionary<string, Security> _securities;
		private readonly ISecurityStorage _securityStorage;
		private readonly IEntityRegistry _entityRegistry;

		private static readonly SynchronizedSet<Security> _notSavedSecurities = new SynchronizedSet<Security>();
		private static readonly SynchronizedSet<Portfolio> _notSavedPortfolios = new SynchronizedSet<Portfolio>();
		private static readonly SynchronizedSet<News> _notSavedNews = new SynchronizedSet<News>();

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageEntityFactory"/>.
		/// </summary>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		/// <param name="storageRegistry">The storage of market data.</param>
		public StorageEntityFactory(IEntityRegistry entityRegistry, IStorageRegistry storageRegistry)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException("entityRegistry");

			if (storageRegistry == null)
				throw new ArgumentNullException("storageRegistry");

			_entityRegistry = entityRegistry;
			_storageRegistry = storageRegistry;

			_securityStorage = storageRegistry.GetSecurityStorage();

			_securities = _securityStorage.LookupAll()
				.ToDictionary(s => s.Id, s => s, StringComparer.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// To create the portfolio by the account number.
		/// </summary>
		/// <param name="name">Account number.</param>
		/// <returns>Created portfolio.</returns>
		public override Portfolio CreatePortfolio(string name)
		{
			//_parent.AddInfoLog(LocalizedStrings.Str3621Params, name);

			lock (_notSavedPortfolios.SyncRoot)
			{
				var portfolio = _entityRegistry.Portfolios.ReadById(name);

				if (portfolio == null)
				{
					//_parent.AddInfoLog(LocalizedStrings.Str3622Params, name);

					portfolio = base.CreatePortfolio(name);
					_notSavedPortfolios.Add(portfolio);
				}

				return portfolio;
			}
		}

		/// <summary>
		/// To create the instrument by the identifier.
		/// </summary>
		/// <param name="id">Security ID.</param>
		/// <returns>Created instrument.</returns>
		public override Security CreateSecurity(string id)
		{
			//_parent.AddInfoLog(LocalizedStrings.Str3623Params, id);

			lock (_notSavedSecurities.SyncRoot)
			{
				var security = _entityRegistry.Securities.ReadById(id);

				if (security == null)
				{
					//_parent.AddInfoLog(LocalizedStrings.Str3624Params, id);

					security = base.CreateSecurity(id);
					_notSavedSecurities.Add(security);
				}

				return security;
			}
		}

		/// <summary>
		/// To create news.
		/// </summary>
		/// <returns>News.</returns>
		public override News CreateNews()
		{
			lock (_notSavedSecurities.SyncRoot)
			{
				//var news = _entityRegistry.News.ReadById(id);

				//if (news == null)
				//{
				var news = base.CreateNews();
				_notSavedNews.Add(news);
				//}

				return news;
			}
		}

		// TODO

		//private void OnSecurities(IEnumerable<Security> securities)
		//{
		//	lock (_notSavedSecurities.SyncRoot)
		//	{
		//		if (_notSavedSecurities.Count == 0)
		//			return;

		//		foreach (var s in securities)
		//		{
		//			var security = s;

		//			if (!_notSavedSecurities.Contains(security))
		//				continue;

		//			// NOTE Когда из Квика пришел инструмент, созданный по сделке
		//			if (security.Code.IsEmpty())
		//				continue;

		//			_parent.AddInfoLog(LocalizedStrings.Str3618Params, security.Id);

		//			var securityToSave = security.Clone();
		//			securityToSave.ExtensionInfo = new Dictionary<object, object>();
		//			_entityRegistry.Securities.Save(securityToSave);

		//			_notSavedSecurities.Remove(security);
		//		}
		//	}

		//	new NewSecuritiesCommand().Process(this);
		//}

		//private void OnPortfolios(IEnumerable<Portfolio> portfolios)
		//{
		//	//foreach (var portfolio in portfolios)
		//	//{
		//	//	_parent.AddInfoLog("Изменение портфеля {0}.", portfolio.Name);
		//	//}

		//	lock (_notSavedPortfolios.SyncRoot)
		//	{
		//		if (_notSavedPortfolios.Count == 0)
		//			return;

		//		foreach (var p in portfolios)
		//		{
		//			var portfolio = p;

		//			if (!_notSavedPortfolios.Contains(portfolio))
		//				continue;

		//			//если площадка у портфеля пустая, то необходимо дождаться ее заполнения
		//			//пустой площадка может быть когда в начале придет информация о заявке 
		//			//с портфелем или о позиции, и только потом придет сам портфель

		//			// mika: портфели могут быть универсальными и не принадлежать площадке

		//			//var board = portfolio.Board;
		//			//if (board == null)
		//			//	continue;

		//			_parent.AddInfoLog(LocalizedStrings.Str3619Params, portfolio.Name);

		//			//if (board != null)
		//			//	_entityRegistry.SaveExchangeBoard(board);

		//			_entityRegistry.Portfolios.Save(portfolio);

		//			_notSavedPortfolios.Remove(portfolio);
		//		}
		//	}
		//}

		//private void OnNews(News news)
		//{
		//	lock (_notSavedNews.SyncRoot)
		//	{
		//		if (_notSavedNews.Count == 0)
		//			return;

		//		if (!_notSavedNews.Contains(news))
		//			return;

		//		_parent.AddInfoLog(LocalizedStrings.Str3620Params, news.Headline);

		//		_entityRegistry.News.Add(news);
		//		_notSavedNews.Remove(news);
		//	}
		//}
	}
}