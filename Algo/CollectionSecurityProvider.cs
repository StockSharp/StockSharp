namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

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

			CheckNullableItems = true;

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

		/// <inheritdoc />
		public IEnumerable<Security> Lookup(SecurityLookupMessage criteria) => this.Filter(criteria);
	}
}