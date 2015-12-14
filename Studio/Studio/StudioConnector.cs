#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StudioPublic
File: StudioConnector.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	internal class StudioConnector : Connector, IStudioConnector
	{
		private sealed class StudioMarketDataAdapter : BasketMessageAdapter
		{
			private readonly Dictionary<object, SecurityId> _securityIds = new Dictionary<object, SecurityId>();

			protected override bool IsSupportNativeSecurityLookup
			{
				get { return true; }
			}

			public StudioMarketDataAdapter(IdGenerator transactionIdGenerator)
				: base(transactionIdGenerator)
			{
			}

			public override void SendOutMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Security:
					{
						var secMsg = (SecurityMessage)message;

						if (secMsg.SecurityId.Native != null)
							_securityIds[secMsg.SecurityId.Native] = secMsg.SecurityId;

						break;
					}

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;
						ReplaceSecurityId(execMsg.SecurityId, id => execMsg.SecurityId = id);
						break;
					}

					case MessageTypes.QuoteChange:
					{
						var quoteMsg = (QuoteChangeMessage)message;
						ReplaceSecurityId(quoteMsg.SecurityId, id => quoteMsg.SecurityId = id);
						break;
					}

					case MessageTypes.Level1Change:
					{
						var level1Msg = (Level1ChangeMessage)message;
						ReplaceSecurityId(level1Msg.SecurityId, id => level1Msg.SecurityId = id);
						break;
					}
				}

				base.SendOutMessage(message);
			}

			private void ReplaceSecurityId(SecurityId securityId, Action<SecurityId> setSecurityId)
			{
				if (securityId.Native == null)
					return;

				SecurityId id;
				if (!_securityIds.TryGetValue(securityId.Native, out id))
					return;

				setSecurityId(new SecurityId { SecurityCode = id.SecurityCode, BoardCode = id.BoardCode, Native = securityId.Native });
			}
		}

		//private sealed class StudioHistorySessionHolder : HistorySessionHolder
		//{
		//	public StudioHistorySessionHolder(IdGenerator transactionIdGenerator)
		//		: base(transactionIdGenerator)
		//	{
		//		IsTransactionEnabled = true;
		//		IsMarketDataEnabled = false;
		//	}
		//}

		private sealed class StudioEmulationAdapter : EmulationMessageAdapter
		{
			public StudioEmulationAdapter(IdGenerator transactionIdGenerator)
				: base(transactionIdGenerator)
			{
			}

			public void ProcessMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					case MessageTypes.Disconnect:
						return;

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;

						if (execMsg.ExecutionType == ExecutionTypes.Trade || execMsg.ExecutionType == ExecutionTypes.Order)
							return;

						break;
					}
				}

				if (!IsDisposed)
					SendInMessage(message);
			}

			public override void SendOutMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					case MessageTypes.Disconnect:
						break;

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;

						if (execMsg.ExecutionType != ExecutionTypes.Order && execMsg.ExecutionType != ExecutionTypes.Trade)
							return;

						break;
					}

					case MessageTypes.PortfolioLookupResult:
					case MessageTypes.Portfolio:
					case MessageTypes.PortfolioChange:
					case MessageTypes.Position:
					case MessageTypes.PositionChange:
						break;

					default:
						return;
				}

				base.SendOutMessage(message);
			}
		}

		private readonly CachedSynchronizedDictionary<Security, SynchronizedDictionary<MarketDataTypes, bool>> _exports = new CachedSynchronizedDictionary<Security, SynchronizedDictionary<MarketDataTypes, bool>>();

		private readonly StudioMarketDataAdapter _marketDataAdapter;

		private bool _newsRegistered;

		public StudioConnector()
		{
			//EntityFactory = new StorageEntityFactory(ConfigManager.GetService<IEntityRegistry>(), ConfigManager.GetService<IStorageRegistry>());

			_marketDataAdapter = new StudioMarketDataAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(new StorageMessageAdapter(_marketDataAdapter, ConfigManager.GetService<IEntityRegistry>(), ConfigManager.GetService<IStorageRegistry>()));

			CreateEmulationSessionHolder();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

			cmdSvc.Register<LookupSecuritiesCommand>(this, false, cmd => LookupSecurities(cmd.Criteria));
			cmdSvc.Register<RequestTradesCommand>(this, false, cmd => new NewTradesCommand(Trades).Process(this));
			//cmdSvc.Register<RequestPortfoliosCommand>(this, cmd => Portfolios.ForEach(pf => new PortfolioCommand(pf, true).Process(this)));
			//cmdSvc.Register<RequestPositionsCommand>(this, false, cmd => Positions.ForEach(pos => new PositionCommand(CurrentTime, pos, true).Process(this)));
			cmdSvc.Register<RequestMarketDataCommand>(this, false, cmd => AddExport(cmd.Security, cmd.Type));
			cmdSvc.Register<RefuseMarketDataCommand>(this, false, cmd => RemoveExport(cmd.Security, cmd.Type));

			//NewPortfolios += portfolios => portfolios.ForEach(pf => new PortfolioCommand(pf, true).Process(this));
			PortfoliosChanged += portfolios => portfolios.ForEach(pf => new PortfolioCommand(pf, false).Process(this));
			//NewPositions += positions => positions.ForEach(pos => new PositionCommand(CurrentTime, pos, true).Process(this));
			//PositionsChanged += positions => positions.ForEach(pos => new PositionCommand(CurrentTime, pos, false).Process(this));
			NewTrades += trades => new NewTradesCommand(trades).Process(this);
			NewNews += news => new NewNewsCommand(news).Process(this);
			LookupSecuritiesResult += securities => new LookupSecuritiesResultCommand(securities).Process(this);
			//LookupPortfoliosResult += portfolios => new LookupPortfoliosResultCommand(portfolios).Process(this);

			UpdateSecurityLastQuotes = false;
			UpdateSecurityByLevel1 = false;
		}

		private void CreateEmulationSessionHolder()
		{
			var emulationSessionHolder = Adapter.InnerAdapters.OfType<StudioEmulationAdapter>().FirstOrDefault();

			if (emulationSessionHolder == null)
			{
				emulationSessionHolder = new StudioEmulationAdapter(TransactionIdGenerator);
				Adapter.InnerAdapters[emulationSessionHolder] = 1;
			}

			//if (!_transactionAdapter.Portfolios.ContainsKey("Simulator"))
			//	_transactionAdapter.Portfolios.Add("Simulator", emulationSessionHolder);
		}

		private IEnumerable<Portfolio> GetEmulationPortfolios()
		{
			var emu = Adapter.InnerAdapters.OfType<EmulationMessageAdapter>().FirstOrDefault();

			if (emu == null)
				yield break;

			var portfolios = ConfigManager.GetService<IStudioEntityRegistry>().Portfolios.ToArray();

			foreach (var portfolio in portfolios)
			{
				var adapter = Adapter.Portfolios.TryGetValue(portfolio.Name);

				if (adapter != emu)
					continue;

				yield return portfolio;
			}
		}

		private void SendPortfoliosToEmulator()
		{
			var portfolios = GetEmulationPortfolios();

			var messages = new List<Message>
			{
				new ResetMessage()
			};

			foreach (var tmp in portfolios)
			{
				var portfolio = tmp;

				messages.Add(portfolio.ToChangeMessage());

				messages.AddRange(ConfigManager
					.GetService<IStudioEntityRegistry>()
					.Positions
					.Where(p => p.Portfolio == portfolio)
					.Select(p => p.ToChangeMessage()));
			}

			SendToEmulator(messages);
		}

		public void SendToEmulator(IEnumerable<Message> messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

			var emu = Adapter.InnerAdapters.OfType<EmulationMessageAdapter>().FirstOrDefault();

			if (emu == null)
			{
				this.AddWarningLog(LocalizedStrings.Str3625);
				return;
			}

			messages.ForEach(emu.SendInMessage);
		}

		private void TrySubscribeMarketData()
		{
			foreach (var pair in _exports.CachedPairs)
			{
				foreach (var type in pair.Value.Where(type => !type.Value))
					base.SubscribeMarketData(pair.Key, type.Key);
			}

			if (_newsRegistered)
				base.OnRegisterNews();
		}

		private void ResetMarketDataSubscriptions()
		{
			foreach (var pair in _exports.CachedPairs)
			{
				var dict = pair.Value;

				foreach (var type in dict.Keys.ToArray())
					dict[type] = false;
			}
		}

		public override void SubscribeMarketData(Security security, MarketDataTypes type)
		{
			AddExport(security, type);
		}

		public override void UnSubscribeMarketData(Security security, MarketDataTypes type)
		{
			RemoveExport(security, type);
		}

		protected override void OnRegisterNews()
		{
			_newsRegistered = true;

			if (ConnectionState == ConnectionStates.Connected)
				base.OnRegisterNews();
		}

		protected override void OnUnRegisterNews()
		{
			_newsRegistered = false;

			if (ConnectionState == ConnectionStates.Connected)
				base.OnUnRegisterNews();
		}

		protected override void OnConnect()
		{
			CreateEmulationSessionHolder();
			base.OnConnect();
		}

		private void AddExport(Security security, MarketDataTypes type)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			_exports.SafeAdd(security).SafeAdd(type);

			if (ConnectionState == ConnectionStates.Connected)
				base.SubscribeMarketData(security, type);
		}

		private void RemoveExport(Security security, MarketDataTypes type)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			_exports.SyncDo(d =>
			{
				var types = d.TryGetValue(security);

				if (types == null)
					return;

				types.Remove(type);
			});

			base.UnSubscribeMarketData(security, type);
		}

		protected override void OnProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					if (message.Adapter == MarketDataAdapter)
					{
						if (((ConnectMessage)message).Error == null)
						{
							SendPortfoliosToEmulator();
							TrySubscribeMarketData();	
						}
					}
					else
					{
						var emu = Adapter.InnerAdapters.OfType<StudioEmulationAdapter>().FirstOrDefault();

						if (emu == null)
						{
							this.AddWarningLog(LocalizedStrings.Str3625);
							break;
						}

						emu.Emulator.Settings.ConvertTime = true;
						emu.Emulator.Settings.InitialOrderId = DateTime.Now.Ticks;
						emu.Emulator.Settings.InitialTradeId = DateTime.Now.Ticks;

						_marketDataAdapter.NewOutMessage += emu.ProcessMessage;
					}

					break;					
				}

				case MessageTypes.Disconnect:
				{
					if (message.Adapter == MarketDataAdapter)
						ResetMarketDataSubscriptions();
					else
					{
						var emu = Adapter.InnerAdapters.OfType<StudioEmulationAdapter>().FirstOrDefault();

						if (emu != null)
							_marketDataAdapter.NewOutMessage -= emu.ProcessMessage;
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;
					var securityId = mdMsg.SecurityId;

					if (mdMsg.Error == null && !securityId.SecurityCode.IsDefault() && !securityId.BoardCode.IsDefault())
					{
						var security = GetSecurity(securityId);
						var types = _exports.TryGetValue(security);

						if (types != null && !types.TryGetValue(mdMsg.DataType))
						{
							types[mdMsg.DataType] = true;
						}
					}

					break;
				}

				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;
					var board = ExchangeBoard.GetBoard(secMsg.SecurityId.BoardCode);

					if (board != null)
						SendToEmulator(new[] { board.ToMessage() });

					break;
				}
			}

			base.OnProcessMessage(message);
		}
	}
}
