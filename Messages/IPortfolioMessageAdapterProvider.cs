namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	/// <summary>
	/// In memory implementation of <see cref="IMappingMessageAdapterProvider{String}"/>.
	/// </summary>
	public class InMemoryPortfolioMessageAdapterProvider : IMappingMessageAdapterProvider<string>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryPortfolioMessageAdapterProvider"/>.
		/// </summary>
		public InMemoryPortfolioMessageAdapterProvider()
		{
		}

		private readonly CachedSynchronizedDictionary<string, IMessageAdapter> _adapters = new CachedSynchronizedDictionary<string, IMessageAdapter>(StringComparer.InvariantCultureIgnoreCase);

		/// <inheritdoc />
		public virtual IEnumerable<KeyValuePair<string, IMessageAdapter>> Adapters => _adapters.CachedPairs;

		/// <inheritdoc />
		public event Action Changed;

		/// <inheritdoc />
		public virtual IMessageAdapter TryGetAdapter(string portfolioName)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException(nameof(portfolioName));

			return _adapters.TryGetValue(portfolioName);
		}

		/// <inheritdoc />
		public virtual void SetAdapter(string portfolioName, IMessageAdapter adapter)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException(nameof(portfolioName));

			IMessageAdapter prev;

			lock (_adapters.SyncRoot)
			{
				prev = _adapters.TryGetValue(portfolioName);
				_adapters[portfolioName] = adapter;
			}

			if (prev != adapter)
				Changed?.Invoke();
		}

		/// <inheritdoc />
		public virtual bool RemoveAssociation(string portfolioName)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException(nameof(portfolioName));

			if (!_adapters.Remove(portfolioName))
				return false;

			Changed?.Invoke();
			return true;
		}
	}
}