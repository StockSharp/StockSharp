namespace StockSharp.Messages
{
	using System.Collections.Generic;

	using Ecng.Collections;

	/// <summary>
	/// The message adapter's provider interface. 
	/// </summary>
	public interface IMessageAdapterProvider
	{
		/// <summary>
		/// All available adapters.
		/// </summary>
		IEnumerable<IMessageAdapter> Adapters { get; }
	}

	/// <summary>
	/// The message adapter's provider interface. 
	/// </summary>
	public interface IPortfolioMessageAdapterProvider : IMessageAdapterProvider
	{
		/// <summary>
		/// All available adapters.
		/// </summary>
		IEnumerable<KeyValuePair<string, IMessageAdapter>> PortfolioAdapters { get; }

		/// <summary>
		/// Get adapter by portfolio name.
		/// </summary>
		/// <param name="portfolioName">Portfolio name.</param>
		/// <returns>The found adapter.</returns>
		IMessageAdapter GetAdapter(string portfolioName);

		/// <summary>
		/// Make association adapter and portfolio name.
		/// </summary>
		/// <param name="portfolioName">Portfolio name.</param>
		/// <param name="adapter">The adapter.</param>
		void SetAdapter(string portfolioName, IMessageAdapter adapter);

		/// <summary>
		/// Remove association between portfolio name and adapter.
		/// </summary>
		/// <param name="portfolioName">Portfolio name.</param>
		/// <returns><see langword="true"/> if the association is successfully removed, otherwise, <see langword="false"/>.</returns>
		bool RemoveAssociation(string portfolioName);
	}

	/// <summary>
	/// In memory message adapter's provider.
	/// </summary>
	public class InMemoryMessageAdapterProvider : IPortfolioMessageAdapterProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryMessageAdapterProvider"/>.
		/// </summary>
		public InMemoryMessageAdapterProvider()
		{
		}

		private readonly CachedSynchronizedDictionary<string, IMessageAdapter> _adapters = new CachedSynchronizedDictionary<string, IMessageAdapter>();

		/// <inheritdoc />
		public IEnumerable<IMessageAdapter> Adapters => _adapters.CachedValues;

		/// <inheritdoc />
		public virtual IEnumerable<KeyValuePair<string, IMessageAdapter>> PortfolioAdapters => _adapters.CachedPairs;

		/// <inheritdoc />
		public virtual IMessageAdapter GetAdapter(string portfolioName)
		{
			return _adapters.TryGetValue(portfolioName);
		}

		/// <inheritdoc />
		public virtual void SetAdapter(string portfolioName, IMessageAdapter adapter)
		{
			lock (_adapters.SyncRoot)
				_adapters.TryAdd(portfolioName, adapter);
		}

		/// <inheritdoc />
		public virtual bool RemoveAssociation(string portfolioName)
		{
			return _adapters.Remove(portfolioName);
		}
	}
}