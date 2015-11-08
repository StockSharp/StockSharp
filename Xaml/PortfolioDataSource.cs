namespace StockSharp.Xaml
{
	using System;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Data source for portfolio based controls/
	/// </summary>
	public class PortfolioDataSource : ThreadSafeObservableCollection<Portfolio>, IDisposable
	{
		private readonly IPortfolioProvider _provider;

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioDataSource"/>.
		/// </summary>
		/// <param name="provider">The portfolio provider interface.</param>
		public PortfolioDataSource(IPortfolioProvider provider)
			: base(new ObservableCollectionEx<Portfolio>())
		{
			if (provider == null)
				throw new ArgumentNullException("provider");

			AddRange(provider.Portfolios);

			_provider = provider;
			_provider.NewPortfolio += Add;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			_provider.NewPortfolio -= Add;
		}
	}
}