#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: PortfolioDataSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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
				throw new ArgumentNullException(nameof(provider));

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