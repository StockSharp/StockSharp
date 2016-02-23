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

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using EntityFactory = StockSharp.Algo.EntityFactory;

	/// <summary>
	/// The simulational connection, intended for strategy testing with real connection to trading system through <see cref="RealTimeEmulationTrader{T}.UnderlyngMarketDataAdapter"/>, but without real resitering orders on stock. Execution of orders and their trades are emulated by connection, using information by order books, coming from real connection.
	/// </summary>
	/// <typeparam name="TUnderlyingMarketDataAdapter">The type <see cref="IMessageAdapter"/>, through which market data will be received.</typeparam>
	public class RealTimeEmulationTrader<TUnderlyingMarketDataAdapter> : BaseEmulationConnector
		where TUnderlyingMarketDataAdapter : IMessageAdapter
	{
		private sealed class EmulationEntityFactory : EntityFactory
		{
			private readonly Portfolio _portfolio;
			//private readonly Connector _connector;

			public EmulationEntityFactory(Portfolio portfolio/*, Connector connector*/)
			{
				_portfolio = portfolio;
				//_connector = connector;
			}

			public override Portfolio CreatePortfolio(string name)
			{
				return _portfolio.Name.CompareIgnoreCase(name) ? _portfolio : base.CreatePortfolio(name);
			}

			//public override Security CreateSecurity(string id)
			//{
			//	return _connector.LookupById(id);
			//}
		}

		private readonly Portfolio _portfolio;
		private readonly bool _ownAdapter;

		/// <summary>
		/// Initializes a new instance of the <see cref="RealTimeEmulationTrader{T}"/>.
		/// </summary>
		/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, through which market data will be got.</param>
		public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter)
			: this(underlyngMarketDataAdapter, new Portfolio
			{
				Name = LocalizedStrings.Str1209,
				BeginValue = 1000000
			})
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RealTimeEmulationTrader{T}"/>.
		/// </summary>
		/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, through which market data will be got.</param>
		/// <param name="portfolio">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		/// <param name="ownAdapter">Track the connection <paramref name="underlyngMarketDataAdapter" /> lifetime.</param>
		public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter, Portfolio portfolio, bool ownAdapter = true)
		{
			if (underlyngMarketDataAdapter == null)
				throw new ArgumentNullException(nameof(underlyngMarketDataAdapter));

			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			UnderlyngMarketDataAdapter = underlyngMarketDataAdapter;
			UnderlyngMarketDataAdapter.RemoveTransactionalSupport();

			UpdateSecurityByLevel1 = false;
			UpdateSecurityLastQuotes = false;

			_portfolio = portfolio;
			EntityFactory = new EmulationEntityFactory(_portfolio);

			_ownAdapter = ownAdapter;

			//MarketEmulator.Settings.UseMarketDepth = true;

			Adapter.InnerAdapters.Add(underlyngMarketDataAdapter);

			if (_ownAdapter)
				UnderlyngMarketDataAdapter.Log += RaiseLog;

			Adapter.InnerAdapters.Add(EmulationAdapter);
		}

		/// <summary>
		/// <see cref="IMessageAdapter"/>, through which market data will be got.
		/// </summary>
		public TUnderlyingMarketDataAdapter UnderlyngMarketDataAdapter { get; }

		/// <summary>
		/// To process the message, containing market data.
		/// </summary>
		/// <param name="message">The message, containing market data.</param>
		protected override void OnProcessMessage(Message message)
		{
			if (message.Type == MessageTypes.Connect && message.Adapter == TransactionAdapter)
			{
				// передаем первоначальное значение размера портфеля в эмулятор
				TransactionAdapter.SendInMessage(_portfolio.ToMessage());
				TransactionAdapter.SendInMessage(new PortfolioChangeMessage
				{
					PortfolioName = _portfolio.Name
				}.Add(PositionChangeTypes.BeginValue, _portfolio.BeginValue));
			}
			else if (message.Adapter == MarketDataAdapter)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					case MessageTypes.Disconnect:
					case MessageTypes.MarketData:
					case MessageTypes.SecurityLookupResult:
						break;
					default:
						TransactionAdapter.SendInMessage(message);
						break;
				}
			}

			base.OnProcessMessage(message);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			if (_ownAdapter)
				UnderlyngMarketDataAdapter.Load(storage.GetValue<SettingsStorage>(nameof(UnderlyngMarketDataAdapter)));

			//LagTimeout = storage.GetValue<TimeSpan>(nameof(LagTimeout));

			base.Load(storage);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			if (_ownAdapter)
				storage.SetValue(nameof(UnderlyngMarketDataAdapter), UnderlyngMarketDataAdapter.Save());

			//storage.SetValue(nameof(LagTimeout), LagTimeout);

			base.Save(storage);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			//UnderlyingTrader.NewMessage -= NewMessageHandler;

			if (_ownAdapter)
			{
				MarketDataAdapter.Log -= RaiseLog;
				MarketDataAdapter.Dispose();
			}

			base.DisposeManaged();
		}
	}
}