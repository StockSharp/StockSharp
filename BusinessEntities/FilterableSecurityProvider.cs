namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using Gma.DataStructures.StringSearch;

	using MoreLinq;

	/// <summary>
	/// Интерфейс, описывающий список элементов.
	/// </summary>
	public interface ISecurityList : INotifyList<Security>, ICollectionEx<Security>, ISynchronizedCollection<Security>
	{
	}

	/// <summary>
	/// Поставщик информации об инструментах с поддержкой поиска с помощью <see cref="SuffixTrie{T}"/>.
	/// </summary>
	public class FilterableSecurityProvider : Disposable, ISecurityProvider
	{
		private class SecurityList : SynchronizedList<Security>, ISecurityList
		{
		}

		private readonly ITrie<Security> _trie = new SuffixTrie<Security>(1);

		/// <summary>
		/// Доступные инструменты.
		/// </summary>
		public ISecurityList Securities { get; private set; }

		private IConnector _connector;

		/// <summary>
		/// Подключение к торговой системе.
		/// </summary>
		public IConnector Connector
		{
			get { return _connector; }
			private set
			{
				if (_connector == value)
					return;

				if (_connector != null)
				{
					_connector.NewSecurities -= OnNewSecurities;
					Securities.Clear();
				}

				_connector = value;

				if (_connector == null)
					return;

				if (!OnlyNewSecurities)
					OnNewSecurities(_connector.Securities);

				_connector.NewSecurities += OnNewSecurities;
			}
		}

		/// <summary>
		/// Получать только новые инструменты из торговой системы.
		/// </summary>
		public bool OnlyNewSecurities { get; set; }

		/// <summary>
		/// Фильтр для исключения инструментов.
		/// </summary>
		public Func<Security, bool> ExcludeFilter { get; private set; }

		/// <summary>
		/// Количество исключенных инструментов по фильтру <see cref="ExcludeFilter"/>.
		/// </summary>
		public int ExcludedCount { get; private set; }

		/// <summary>
		/// Событие изменения набора доступных инструментов.
		/// </summary>
		public event Action<NotifyCollectionChangedAction, Security> SecuritiesChanged;

		/// <summary>
		/// Создать <see cref="FilterableSecurityProvider"/>.
		/// </summary>
		/// <param name="securities">Инструменты.</param>
		/// <param name="excludeFilter">Фильтр для исключения инструментов.</param>
		public FilterableSecurityProvider(ISecurityList securities, Func<Security, bool> excludeFilter = null)
		{
			if (securities == null)
				throw new ArgumentNullException("securities");

			Securities = securities;
			ExcludeFilter = excludeFilter;

			Securities.Added += AddSuffix;
			Securities.Inserted += (i, s) => AddSuffix(s);

			Securities.Removed += s =>
			{
				lock (_trie)
					_trie.Remove(s);

				if (ExcludeFilter != null && ExcludeFilter(s))
					ExcludedCount--;

				SecuritiesChanged.SafeInvoke(NotifyCollectionChangedAction.Remove, s);
			};

			Securities.Cleared += () =>
			{
				lock (_trie)
					_trie.Clear();

				ExcludedCount = 0;
				SecuritiesChanged.SafeInvoke(NotifyCollectionChangedAction.Reset, null);
			};

			Securities.ForEach(AddSuffix);
		}

		/// <summary>
		/// Создать <see cref="FilterableSecurityProvider"/>.
		/// </summary>
		/// <param name="excludeFilter">Фильтр для исключения инструментов.</param>
		public FilterableSecurityProvider(Func<Security, bool> excludeFilter = null)
			: this(new SecurityList(), excludeFilter)
		{
		}

		/// <summary>
		/// Создать <see cref="FilterableSecurityProvider"/>.
		/// </summary>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="onlyNewSecurities">Получать только новые инструменты из торговой системы.</param>
		/// <param name="excludeFilter">Фильтр для исключения инструментов.</param>
		public FilterableSecurityProvider(IConnector connector, bool onlyNewSecurities = false, Func<Security, bool> excludeFilter = null)
			: this(excludeFilter)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			OnlyNewSecurities = onlyNewSecurities;
			Connector = connector;
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

			var filter = criteria.Id.IsEmpty()
				? (criteria.Code == "*" ? string.Empty : criteria.Code.ToLowerInvariant())
				: criteria.Id.ToLowerInvariant();

			IEnumerable<Security> securities;

			if (filter.IsEmpty())
			{
				var syncCollection = Securities as ISynchronizedCollection<Security>;

				securities = syncCollection != null
					? syncCollection.SyncGet(c => c.ToArray())
					: Securities.ToArray();
			}
			else
			{
				lock (_trie)
				{
					securities = _trie.Retrieve(filter).ToArray();

					if (!criteria.Id.IsEmpty())
						securities = securities.Where(s => s.Id.CompareIgnoreCase(criteria.Id));
				}
			}

			return ExcludeFilter == null ? securities : securities.Where(s => !ExcludeFilter(s));
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}

		private void OnNewSecurities(IEnumerable<Security> securities)
		{
			Securities.AddRange(securities);
		}

		private void AddSuffix(Security security)
		{
			lock (_trie)
			{
				AddSuffix(security.Id, security);
				AddSuffix(security.Code, security);
				AddSuffix(security.Name, security);
				AddSuffix(security.ShortName, security);
				AddSuffix(security.ExternalId.Bloomberg, security);
				AddSuffix(security.ExternalId.Cusip, security);
				AddSuffix(security.ExternalId.Isin, security);
				AddSuffix(security.ExternalId.Ric, security);
				AddSuffix(security.ExternalId.Sedol, security);
			}

			if (ExcludeFilter != null && ExcludeFilter(security))
				ExcludedCount++;

			SecuritiesChanged.SafeInvoke(NotifyCollectionChangedAction.Add, security);
		}

		private void AddSuffix(string text, Security security)
		{
			if (text.IsEmpty())
				return;

			_trie.Add(text.ToLowerInvariant(), security);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			Connector = null;

			base.DisposeManaged();
		}
	}
}
