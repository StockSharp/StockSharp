namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for access to the position storage.
	/// </summary>
	public interface IPositionStorage : IPositionProvider
	{
		/// <summary>
		/// Save portfolio.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		void Save(Portfolio portfolio);

		/// <summary>
		/// Delete portfolio.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		void Delete(Portfolio portfolio);

		/// <summary>
		/// Save position.
		/// </summary>
		/// <param name="position">Position.</param>
		void Save(Position position);

		/// <summary>
		/// Delete position.
		/// </summary>
		/// <param name="position">Position.</param>
		void Delete(Position position);
	}

	/// <summary>
	/// In memory implementation of <see cref="IPositionStorage"/>.
	/// </summary>
	public class InMemoryPositionStorage : IPositionStorage
	{
		private readonly IPortfolioProvider _underlying;
		private readonly CachedSynchronizedDictionary<Tuple<Security, Portfolio>, Position> _inner = new CachedSynchronizedDictionary<Tuple<Security, Portfolio>, Position>();

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryPositionStorage"/>.
		/// </summary>
		public InMemoryPositionStorage()
			: this(new CollectionPortfolioProvider())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryPositionStorage"/>.
		/// </summary>
		/// <param name="underlying">Underlying provider.</param>
		public InMemoryPositionStorage(IPortfolioProvider underlying)
		{
			_underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
		}

		/// <inheritdoc />
		public IEnumerable<Position> Positions => _inner.CachedValues;

		/// <inheritdoc />
		public IEnumerable<Portfolio> Portfolios => _underlying.Portfolios;

		/// <inheritdoc />
		public event Action<Position> NewPosition;

		/// <inheritdoc />
		public event Action<Position> PositionChanged;

		/// <inheritdoc />
		public event Action<Portfolio> NewPortfolio;

		/// <inheritdoc />
		public event Action<Portfolio> PortfolioChanged;

		/// <inheritdoc />
		public void Delete(Portfolio portfolio)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public void Delete(Position position)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Position GetPosition(Portfolio portfolio, Security security, string clientCode = "", string depoName = "")
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Portfolio LookupByPortfolioName(string name)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public void Save(Portfolio portfolio)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public void Save(Position position)
		{
			throw new NotImplementedException();
		}
	}
}