namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using Gma.DataStructures.StringSearch;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Provider of information about instruments supporting search using <see cref="SuffixTrie{T}"/>.
	/// </summary>
	public class FilterableSecurityProvider : Disposable, ISecurityProvider
	{
		private readonly SyncObject _sync = new SyncObject();
		private readonly List<Security> _allSecurities = new List<Security>();
		private readonly ITrie<Security> _trie = new SuffixTrie<Security>(1);

		///// <summary>
		///// Filter for instruments exclusion.
		///// </summary>
		//public Func<Security, bool> ExcludeFilter { get; private set; }

		///// <summary>
		///// The number of excluded instruments by filter <see cref="ExcludeFilter"/>.
		///// </summary>
		//public int ExcludedCount { get; private set; }

		private readonly ISecurityProvider _provider;
		private readonly bool _ownProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="FilterableSecurityProvider"/>.
		/// </summary>
		/// <param name="provider">Security meta info provider.</param>
		/// <param name="ownProvider"><see langword="true"/> to leave the <paramref name="provider"/> open after the <see cref="FilterableSecurityProvider"/> object is disposed; otherwise, <see langword="false"/>.</param>
		///// <param name="excludeFilter">Filter for instruments exclusion.</param>
		public FilterableSecurityProvider(ISecurityProvider provider, bool ownProvider = false/*, Func<Security, bool> excludeFilter = null*/)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");

			_provider = provider;
			_ownProvider = ownProvider;

			//ExcludeFilter = excludeFilter;

			_provider.Added += AddSecurity;
			_provider.Removed += RemoveSecurity;
			_provider.Cleared += ClearSecurities;

			_provider.LookupAll().ForEach(AddSecurity);
		}

		/// <summary>
		/// Gets the number of instruments contained in the <see cref="ISecurityProvider"/>.
		/// </summary>
		public int Count
		{
			get
			{
				lock (_sync)
					return _allSecurities.Count;
			}
		}

		/// <summary>
		/// New instrument created.
		/// </summary>
		public event Action<Security> Added;

		/// <summary>
		/// Instrument deleted.
		/// </summary>
		public event Action<Security> Removed;

		/// <summary>
		/// The storage was cleared.
		/// </summary>
		public event Action Cleared;

		/// <summary>
		/// Lookup securities by criteria <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Found instruments.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException("criteria");

			var filter = criteria.Id.IsEmpty()
				? (criteria.IsLookupAll() ? string.Empty : criteria.Code.ToLowerInvariant())
				: criteria.Id.ToLowerInvariant();

			IEnumerable<Security> securities;

			lock (_sync)
			{
				if (filter.IsEmpty())
				{
					securities = _allSecurities;
				}
				else
				{
					securities = _trie.Retrieve(filter);

					if (!criteria.Id.IsEmpty())
						securities = securities.Where(s => s.Id.CompareIgnoreCase(criteria.Id));
					
				}

				securities = securities.ToArray();
			}

			return securities;
			//return ExcludeFilter == null ? securities : securities.Where(s => !ExcludeFilter(s));
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}

		private void AddSecurity(Security security)
		{
			lock (_sync)
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

				_allSecurities.Add(security);
			}

			//if (ExcludeFilter != null && ExcludeFilter(security))
			//	ExcludedCount++;

			Added.SafeInvoke(security);
		}

		private void AddSuffix(string text, Security security)
		{
			if (text.IsEmpty())
				return;

			_trie.Add(text.ToLowerInvariant(), security);
		}

		private void RemoveSecurity(Security security)
		{
			lock (_sync)
			{
				_trie.Remove(security);
				_allSecurities.Remove(security);
			}

			//if (ExcludeFilter != null && ExcludeFilter(security))
			//	ExcludedCount--;

			Removed.SafeInvoke(security);
		}

		private void ClearSecurities()
		{
			lock (_sync)
			{
				_trie.Clear();
				_allSecurities.Clear();
			}

			//ExcludedCount = 0;

			Cleared.SafeInvoke();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_provider.Added -= AddSecurity;
			_provider.Removed -= RemoveSecurity;
			_provider.Cleared -= ClearSecurities;

			if (_ownProvider)
				_provider.Dispose();

			base.DisposeManaged();
		}
	}
}
