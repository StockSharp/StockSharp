namespace StockSharp.Studio.StrategyRunner
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	class StrategyEntityFactory : EntityFactory, ISecurityProvider
	{
		private class SecurityList : SynchronizedList<Security>, ISecurityList
		{
		}

		private readonly CachedSynchronizedDictionary<string, Security> _securities = new CachedSynchronizedDictionary<string, Security>();
		private readonly CachedSynchronizedDictionary<string, Portfolio> _portfolios = new CachedSynchronizedDictionary<string, Portfolio>();

		private readonly SecurityList _securityList = new SecurityList();

		public ISecurityList Securities { get { return _securityList; } }

		/// <summary>
		/// Создать инструмент по идентификатору.
		/// </summary>
		/// <param name="id">Идентификатор инструмента.</param>
		/// <returns>Созданный инструмент.</returns>
		public override Security CreateSecurity(string id)
		{
			return _securities.SafeAdd(id, key =>
			{
				var security = base.CreateSecurity(key);
				_securityList.Add(security);
				return security;
			});
		}

		/// <summary>
		/// Создать портфель по номеру счета.
		/// </summary>
		/// <param name="name">Номер счета.</param>
		/// <returns>Созданный портфель.</returns>
		public override Portfolio CreatePortfolio(string name)
		{
			return _portfolios.SafeAdd(name, key => base.CreatePortfolio(key));
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Найденные инструменты.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException("criteria");

			if (criteria.Code == "*")
				return _securities.CachedValues;

			//TODO criteria.Id = null

			return new[] { CreateSecurity(criteria.Id) };
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}

		public Portfolio LookupPortfolio(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			return CreatePortfolio(name);
		}
	}
}