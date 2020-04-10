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

    using Ecng.Common;
	using Ecng.Collections;

    using StockSharp.BusinessEntities;
    using StockSharp.Messages;
	using StockSharp.Logging;
	using Ecng.Serialization;

	/// <summary>
	/// Real-time (simulation, paper-trading) emulation adapter.
	/// </summary>
	public class RealTimeEmulationAdapter : MessageAdapterWrapper, IRealTimeEmulationMarketDataAdapter
	{
		private readonly IMarketEmulator _emulator;

		private readonly SynchronizedSet<long> _subscriptionIds = new SynchronizedSet<long>();
		private readonly SynchronizedSet<long> _realSubscribeIds = new SynchronizedSet<long>();
		private readonly SynchronizedSet<long> _emuOrderIds = new SynchronizedSet<long>();

		private readonly IMessageAdapter _channelEmulator;

		/// <summary>
		/// Initialize <see cref="RealTimeEmulationAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public RealTimeEmulationAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
			_emulator = new MarketEmulator
			{
				Parent = this,
				//SendBackSecurities = true,
			};
			_emulator.Settings.ConvertTime = true;
			_emulator.Settings.InitialOrderId = DateTime.Now.Ticks;
			_emulator.Settings.InitialTradeId = DateTime.Now.Ticks;

			_channelEmulator = new ChannelMessageAdapter(new SubscriptionOnlineMessageAdapter(_emulator),
					new InMemoryMessageChannel(new MessageByOrderQueue(), "Emulator In", this.AddErrorLog),
					new InMemoryMessageChannel(new MessageByOrderQueue(), "Emulator Out", this.AddErrorLog));

			_channelEmulator.NewOutMessage += OnMarketEmulatorNewOutMessage;
		}

		/// <summary>
		/// Emulation enabled.
		/// </summary>
		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// Settings of exchange emulator.
		/// </summary>
		public MarketEmulatorSettings Settings => _emulator.Settings;

		/// <inheritdoc />
		public override IEnumerable<MessageTypes> SupportedInMessages => InnerAdapter.SupportedInMessages.Except(OwnInnerAdapter ? ArrayHelper.Empty<MessageTypes>() : new[] { MessageTypes.SecurityLookup }).Concat(Extensions.TransactionalMessageTypes).ToArray();
		
		/// <inheritdoc />
		public override IEnumerable<MessageTypes> SupportedOutMessages => InnerAdapter.SupportedOutMessages.Concat(new[] { MessageTypes.Portfolio, MessageTypes.PortfolioRoute, MessageTypes.PositionChange }).ToArray();

		private void SendToEmulator(Message message)
		{
			_channelEmulator.SendInMessage(message);
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;
					ProcessPortfolioMessage(regMsg.PortfolioName, regMsg);
					return true;
				}
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderCancel:
				{
					ProcessOrderMessage(((OrderMessage)message).OriginalTransactionId, message);
					return true;
				}

				case MessageTypes.OrderPairReplace:
				{
					var ordMsg = (OrderPairReplaceMessage)message;
					ProcessOrderMessage(ordMsg.Message1.OriginalTransactionId, message);
					return true;
				}

				case MessageTypes.OrderGroupCancel:
				{
					if (IsEnabled)
					{
						SendToEmulator(message);
						return true;
					}
					else
						return base.OnSendInMessage(message);
				}

				case MessageTypes.Reset:
				case MessageTypes.Connect:
				case MessageTypes.Disconnect:
				{
					SendToEmulator(message);

					if (message.Type == MessageTypes.Reset)
					{
						_subscriptionIds.Clear();
						_realSubscribeIds.Clear();
						_emuOrderIds.Clear();
					}

					if (OwnInnerAdapter)
						return base.OnSendInMessage(message);
					else
						return true;
				}

				case MessageTypes.PortfolioLookup:
				case MessageTypes.Portfolio:
				case MessageTypes.OrderStatus:
				{
					if (OwnInnerAdapter)
						base.OnSendInMessage(message);

					SendToEmulator(message);
					return true;
				}

				case MessageTypes.SecurityLookup:
				case MessageTypes.TimeFrameLookup:
				case MessageTypes.BoardLookup:
				{
					if (OwnInnerAdapter)
						base.OnSendInMessage(message);
					else
					{
						_subscriptionIds.Add(((ISubscriptionMessage)message).TransactionId);
						SendToEmulator(message);
					}

					return true;
				}

				case MessageTypes.MarketData:
				{
					if (IsEnabled)
					{
						var transId = ((ISubscriptionMessage)message).TransactionId;
						_subscriptionIds.Add(transId);
						_realSubscribeIds.Add(transId);

						SendToEmulator(message);
					}

					return base.OnSendInMessage(message);
				}

				default:
				{
					if (OwnInnerAdapter)
						return base.OnSendInMessage(message);

					return true;
				}
			}
		}

		/// <inheritdoc />
		protected override void InnerAdapterNewOutMessage(Message message)
		{
			if (OwnInnerAdapter || !message.IsBack)
				base.InnerAdapterNewOutMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (OwnInnerAdapter)
				base.OnInnerAdapterNewOutMessage(message);

			if (!IsEnabled || message.IsBack)
				return;

			switch (message.Type)
			{
				case MessageTypes.SubscriptionResponse:
				case MessageTypes.SubscriptionFinished:
				case MessageTypes.SubscriptionOnline:
				{
					if (!OwnInnerAdapter)
					{
						var originId = (IOriginalTransactionIdMessage)message;
					
						if (_realSubscribeIds.Contains(originId.OriginalTransactionId))
							base.OnInnerAdapterNewOutMessage(message);
					}

					break;
				}
				case MessageTypes.Connect:
				case MessageTypes.Disconnect:
				case MessageTypes.Reset:
				//case MessageTypes.BoardState:
				case MessageTypes.Portfolio:
				case MessageTypes.PositionChange:
				{
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.IsMarketData())
						TrySendToEmulator((ISubscriptionIdMessage)message);

					break;
				}

				default:
				{
					if (message is ISubscriptionIdMessage || message is SecurityMessage)
						TrySendToEmulator((ISubscriptionIdMessage)message);

					break;
				}
			}
		}

		private void TrySendToEmulator(ISubscriptionIdMessage message)
		{
			foreach (var id in message.GetSubscriptionIds())
			{
				if (_subscriptionIds.Contains(id))
				{
					SendToEmulator((Message)message);
					break;
				}
			}
		}

		private void OnMarketEmulatorNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					var connectMsg = (ConnectMessage)message;

					if (connectMsg.Error == null)
					{
						var pf = Portfolio.CreateSimulator();

						var pfMsg = pf.ToMessage();
						pfMsg.IsSubscribe = true;
						SendToEmulator(pfMsg);
						SendToEmulator(pf.ToChangeMessage());
					}

					if (OwnInnerAdapter)
						return;

					break;
				}
				case MessageTypes.Reset:
				case MessageTypes.Disconnect:
				{
					if (OwnInnerAdapter)
						return;

					break;
				}

				case MessageTypes.SubscriptionOnline:
				case MessageTypes.SubscriptionFinished:
				case MessageTypes.SubscriptionResponse:
				{
					var originId = (IOriginalTransactionIdMessage)message;
					
					if (_realSubscribeIds.Contains(originId.OriginalTransactionId))
						return;

					break;
				}
			}

			RaiseNewOutMessage(message);
		}

		private void ProcessPortfolioMessage(string portfolioName, OrderMessage message)
		{
			if (IsEnabled)
			{
				if (OwnInnerAdapter)
				{
					if (portfolioName == Extensions.SimulatorPortfolioName)
					{
						_emuOrderIds.Add(message.TransactionId);
						SendToEmulator(message);
					}
					else
						base.OnSendInMessage(message);
				}
				else
				{
					_emuOrderIds.Add(message.TransactionId);
					SendToEmulator(message);
				}
			}
			else
				base.OnSendInMessage(message);
		}

		private void ProcessOrderMessage(long transId, Message message)
		{
			if (_emuOrderIds.Contains(transId))
				SendToEmulator(message);
			else
				base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(MarketEmulator), _emulator.Settings.Save());
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			_emulator.Settings.Load(storage.GetValue<SettingsStorage>(nameof(MarketEmulator)));
		}

		/// <summary>
		/// Create a copy of <see cref="RealTimeEmulationAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone() => new RealTimeEmulationAdapter(InnerAdapter);
	}
}