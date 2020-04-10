#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: RealTimeEmulationTrader.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	using EntityFactory = StockSharp.Algo.EntityFactory;

	/// <summary>
	/// The interface of the real time market data adapter.
	/// </summary>
	public interface IRealTimeEmulationMarketDataAdapter : IMessageAdapterWrapper
	{
	}

	/// <summary>
	/// The simulation connection, intended for strategy testing with real connection to trading system through <see cref="RealTimeEmulationTrader{T}.UnderlyngMarketDataAdapter"/>, but without real registering orders on stock. Execution of orders and their trades are emulated by connection, using information by order books, coming from real connection.
	/// </summary>
	/// <typeparam name="TUnderlyingMarketDataAdapter">The type <see cref="IMessageAdapter"/>, through which market data will be received.</typeparam>
	public class RealTimeEmulationTrader<TUnderlyingMarketDataAdapter> : BaseEmulationConnector
		where TUnderlyingMarketDataAdapter : class, IMessageAdapter
	{
		private class EmulationEntityFactory : EntityFactory
		{
			private readonly ISecurityProvider _securityProvider;
			private readonly IPortfolioProvider _portfolioProvider;

			public EmulationEntityFactory(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider)
			{
				_securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
				_portfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
			}

			public override Security CreateSecurity(string id)
			{
				return _securityProvider.LookupById(id) ?? base.CreateSecurity(id);
			}

			public override Portfolio CreatePortfolio(string name)
			{
				return _portfolioProvider.GetPortfolio(name) ?? base.CreatePortfolio(name);
			}
		}

		private readonly bool _ownAdapter;

		/// <summary>
		/// Initializes a new instance of the <see cref="RealTimeEmulationTrader{T}"/>.
		/// </summary>
		/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, through which market data will be got.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter, ISecurityProvider securityProvider)
			: this(underlyngMarketDataAdapter, securityProvider, Portfolio.CreateSimulator())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RealTimeEmulationTrader{T}"/>.
		/// </summary>
		/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, through which market data will be got.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolio">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		/// <param name="ownAdapter">Track the connection <paramref name="underlyngMarketDataAdapter" /> lifetime.</param>
		public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter, ISecurityProvider securityProvider, Portfolio portfolio, bool ownAdapter = true)
			: this(underlyngMarketDataAdapter, securityProvider, new CollectionPortfolioProvider(new[] { portfolio }), ownAdapter)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RealTimeEmulationTrader{T}"/>.
		/// </summary>
		/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, through which market data will be got.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		/// <param name="ownAdapter">Track the connection <paramref name="underlyngMarketDataAdapter" /> lifetime.</param>
		public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter, ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, bool ownAdapter = true)
		{
			UnderlyngMarketDataAdapter = underlyngMarketDataAdapter ?? throw new ArgumentNullException(nameof(underlyngMarketDataAdapter));

			UpdateSecurityByLevel1 = false;
			UpdateSecurityLastQuotes = false;

			EntityFactory = new EmulationEntityFactory(securityProvider ?? throw new ArgumentNullException(nameof(securityProvider)), portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider)));

			_ownAdapter = ownAdapter;

			var emuAdapter = new RealTimeEmulationAdapter(underlyngMarketDataAdapter) { OwnInnerAdapter = ownAdapter };
			Adapter.InnerAdapters.Add(emuAdapter);
			Adapter.ApplyHeartbeat(emuAdapter, ownAdapter);

			Adapter.IgnoreExtraAdapters = true;

			//if (_ownAdapter)
			//	UnderlyngMarketDataAdapter.Log += RaiseLog;
		}

		/// <summary>
		/// <see cref="IMessageAdapter"/>, through which market data will be got.
		/// </summary>
		public TUnderlyingMarketDataAdapter UnderlyngMarketDataAdapter { get; }

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			if (_ownAdapter)
				UnderlyngMarketDataAdapter.Load(storage.GetValue<SettingsStorage>(nameof(UnderlyngMarketDataAdapter)));

			base.Load(storage);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			if (_ownAdapter)
				storage.SetValue(nameof(UnderlyngMarketDataAdapter), UnderlyngMarketDataAdapter.Save());

			base.Save(storage);
		}

		/// <inheritdoc />
		protected override void DisposeManaged()
		{
			if (_ownAdapter)
			{
				//UnderlyngMarketDataAdapter.Log -= RaiseLog;
				UnderlyngMarketDataAdapter.Dispose();
			}

			base.DisposeManaged();
		}
	}
}