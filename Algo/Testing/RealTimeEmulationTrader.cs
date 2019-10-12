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
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Logging;

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
		private sealed class EmulationEntityFactory : EntityFactory
		{
			private readonly IPortfolioProvider _portfolioProvider;

			public EmulationEntityFactory(IPortfolioProvider portfolioProvider)
			{
				_portfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
			}

			public override Portfolio CreatePortfolio(string name)
			{
				return _portfolioProvider.GetPortfolio(name) ?? base.CreatePortfolio(name);
			}
		}

		private class RealTimeEmulationMarketDataAdapter : MessageAdapterWrapper, IRealTimeEmulationMarketDataAdapter
		{
			private readonly RealTimeEmulationTrader<TUnderlyingMarketDataAdapter> _connector;

			public RealTimeEmulationMarketDataAdapter(RealTimeEmulationTrader<TUnderlyingMarketDataAdapter> connector, IMessageAdapter innerAdapter)
				: base(innerAdapter)
			{
				_connector = connector ?? throw new ArgumentNullException(nameof(connector));
			}

			public override bool SecurityLookupRequired => _connector._ownAdapter && base.SecurityLookupRequired;
			public override bool PortfolioLookupRequired => false;
			public override bool OrderStatusRequired => false;
			public override bool IsSupportSubscriptionByPortfolio => false;
			public override OrderCancelVolumeRequireTypes? OrderCancelVolumeRequired => null;
			public override IEnumerable<MessageTypes> SupportedMessages => InnerAdapter.SupportedMessages.Except(Extensions.TransactionalMessageTypes).ToArray();

			private ILogSource _parent;

			public override ILogSource Parent
			{
				get => _connector._ownAdapter ? base.Parent : _parent;
				set
				{
					if (_connector._ownAdapter)
						base.Parent = value;
					else
						_parent = value;
				}
			}

			protected override void OnSendInMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Reset:
					{
						if (!_connector._ownAdapter)
						{
							RaiseNewOutMessage(new ResetMessage());
							return;
						}

						break;
					}

					case MessageTypes.Connect:
					{
						if (!_connector._ownAdapter)
						{
							RaiseNewOutMessage(new ConnectMessage());
							return;
						}

						break;
					}

					case MessageTypes.Disconnect:
					{
						if (!_connector._ownAdapter)
						{
							RaiseNewOutMessage(new DisconnectMessage());
							return;
						}

						break;
					}

					case MessageTypes.MarketData:
					case MessageTypes.ChangePassword:
					{
						if (!_connector._ownAdapter)
							return;

						break;
					}
				}

				InnerAdapter.SendInMessage(message);
			}

			protected override void OnInnerAdapterNewOutMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Reset:
					case MessageTypes.Connect:
					case MessageTypes.Disconnect:
					case MessageTypes.ChangePassword:
					case MessageTypes.MarketData:
					{
						if (_connector._ownAdapter)
							break;

						return;
					}

					case MessageTypes.OrderStatus:
					case MessageTypes.Portfolio:
					case MessageTypes.PortfolioChange:
					case MessageTypes.PortfolioLookupResult:
					case MessageTypes.PositionChange:
						return;

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;

						switch (execMsg.ExecutionType)
						{
							case ExecutionTypes.Transaction:
								return;
						}

						break;
					}
				}

				//var clone = message.Clone();
				//clone.Adapter = this;
				base.OnInnerAdapterNewOutMessage(message);
			}

			public override IMessageChannel Clone()
			{
				return new RealTimeEmulationMarketDataAdapter(_connector, InnerAdapter);
			}
		}

		private readonly IPortfolioProvider _portfolioProvider;
		private readonly bool _ownAdapter;

		/// <summary>
		/// Initializes a new instance of the <see cref="RealTimeEmulationTrader{T}"/>.
		/// </summary>
		/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, through which market data will be got.</param>
		public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter)
			: this(underlyngMarketDataAdapter, Portfolio.CreateSimulator())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RealTimeEmulationTrader{T}"/>.
		/// </summary>
		/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, through which market data will be got.</param>
		/// <param name="portfolio">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		/// <param name="ownAdapter">Track the connection <paramref name="underlyngMarketDataAdapter" /> lifetime.</param>
		public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter, Portfolio portfolio, bool ownAdapter = true)
			: this(underlyngMarketDataAdapter, new CollectionPortfolioProvider(new[] { portfolio }), ownAdapter)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RealTimeEmulationTrader{T}"/>.
		/// </summary>
		/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, through which market data will be got.</param>
		/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		/// <param name="ownAdapter">Track the connection <paramref name="underlyngMarketDataAdapter" /> lifetime.</param>
		public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter, IPortfolioProvider portfolioProvider, bool ownAdapter = true)
		{
			UnderlyngMarketDataAdapter = underlyngMarketDataAdapter ?? throw new ArgumentNullException(nameof(underlyngMarketDataAdapter));

			UpdateSecurityByLevel1 = false;
			UpdateSecurityLastQuotes = false;

			_portfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
			EntityFactory = new EmulationEntityFactory(_portfolioProvider);

			_ownAdapter = ownAdapter;

			//MarketEmulator.Settings.UseMarketDepth = true;

			Adapter.InnerAdapters.Add(new RealTimeEmulationMarketDataAdapter(this, underlyngMarketDataAdapter));
			Adapter.InnerAdapters.Add(EmulationAdapter);

			foreach (var adapter in Adapter.InnerAdapters)
			{
				Adapter.ApplyHeartbeat(adapter, false);
			}

			//if (_ownAdapter)
			//	UnderlyngMarketDataAdapter.Log += RaiseLog;
		}

		/// <summary>
		/// <see cref="IMessageAdapter"/>, through which market data will be got.
		/// </summary>
		public TUnderlyingMarketDataAdapter UnderlyngMarketDataAdapter { get; }

		/// <inheritdoc />
		protected override void OnProcessMessage(Message message)
		{
			if (message.Adapter == TransactionAdapter)
			{
				if (message.Type == MessageTypes.Connect)
				{
					foreach (var portfolio in _portfolioProvider.Portfolios)
					{
						// passing into initial values
						TransactionAdapter.SendInMessage(portfolio.ToMessage());
						TransactionAdapter.SendInMessage(new PortfolioChangeMessage
						{
							PortfolioName = portfolio.Name
						}.TryAdd(PositionChangeTypes.BeginValue, portfolio.BeginValue, true));	
					}
				}
			}
			else
			{
				var mdAdapter = (IMessageAdapter)UnderlyngMarketDataAdapter;

				if (message.Adapter == mdAdapter || message.Adapter?.Parent == mdAdapter)
				{
					switch (message.Type)
					{
						case MessageTypes.Connect:
						case MessageTypes.Disconnect:
						case MessageTypes.MarketData:
						case MessageTypes.SecurityLookupResult:
						//case MessageTypes.Session:
						case MessageTypes.ChangePassword:
							break;

						case MessageTypes.Security:
						case MessageTypes.CandleTimeFrame:
						case MessageTypes.CandlePnF:
						case MessageTypes.CandleRange:
						case MessageTypes.CandleRenko:
						case MessageTypes.CandleTick:
						case MessageTypes.CandleVolume:
							TransactionAdapter.SendInMessage(message);
							break;

						default:
							TransactionAdapter.SendInMessage(message);

							// ignore emu connector "raw" (without emu orders) market data
							return;
					}
				}
			}

			base.OnProcessMessage(message);
		}

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