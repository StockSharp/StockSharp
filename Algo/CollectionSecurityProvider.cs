namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The supplier of information on instruments, getting data from the collection.
	/// </summary>
	public class CollectionSecurityProvider : SynchronizedList<Security>, ISecurityProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionSecurityProvider"/>.
		/// </summary>
		public CollectionSecurityProvider()
			: this(Enumerable.Empty<Security>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionSecurityProvider"/>.
		/// </summary>
		/// <param name="securities">The instruments collection.</param>
		public CollectionSecurityProvider(IEnumerable<Security> securities)
		{
			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			AddRange(securities);

			AddedRange += s => _added?.Invoke(s);
			RemovedRange += s => _removed?.Invoke(s);

			if (securities is INotifyList<Security> notifyList)
			{
				notifyList.Added += Add;
				notifyList.Removed += s => Remove(s);
				notifyList.Cleared += Clear;
			}
		}

		private Action<IEnumerable<Security>> _added;

		event Action<IEnumerable<Security>> ISecurityProvider.Added
		{
			add => _added += value;
			remove => _added -= value;
		}

		private Action<IEnumerable<Security>> _removed;

		event Action<IEnumerable<Security>> ISecurityProvider.Removed
		{
			add => _removed += value;
			remove => _removed -= value;
		}

		void IDisposable.Dispose()
		{
		}

		/// <summary>
		/// Lookup securities by criteria <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Found instruments.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			//var provider = Securities as ISecurityProvider;
			//return provider == null ? Securities.Filter(criteria) : provider.Lookup(criteria);
			return this.Filter(criteria);
		}
	}
}