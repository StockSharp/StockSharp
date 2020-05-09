#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: EmulationMessageAdapter.cs
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
	using Ecng.Serialization;

    using StockSharp.BusinessEntities;
    using StockSharp.Messages;

	/// <summary>
	/// The interface of the real time market data adapter.
	/// </summary>
	public interface IEmulationMessageAdapter : IMessageAdapterWrapper
	{
	}

	/// <summary>
	/// Emulation message adapter.
	/// </summary>
	public class EmulationMessageAdapter : MessageAdapterWrapper, IEmulationMessageAdapter
	{
		private readonly SynchronizedSet<long> _subscriptionIds = new SynchronizedSet<long>();
		private readonly SynchronizedSet<long> _realSubscribeIds = new SynchronizedSet<long>();
		private readonly SynchronizedSet<long> _emuOrderIds = new SynchronizedSet<long>();

		private readonly IMessageAdapter _inAdapter;
		private readonly bool _isEmulationOnly;

		/// <summary>
		/// Initialize <see cref="EmulationMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		/// <param name="inChannel">Incoming messages channel.</param>
		/// <param name="isEmulationOnly">Send <see cref="TimeMessage"/> to emulator.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		public EmulationMessageAdapter(IMessageAdapter innerAdapter, IMessageChannel inChannel, bool isEmulationOnly, ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider)
			: base(innerAdapter)
		{
			Emulator = new MarketEmulator(securityProvider, portfolioProvider)
			{
				Parent = this,
				Settings =
				{
					ConvertTime = true,
					InitialOrderId = DateTime.Now.Ticks,
					InitialTradeId = DateTime.Now.Ticks,
				}
			};

			InChannel = inChannel;

			_inAdapter = new SubscriptionOnlineMessageAdapter(Emulator);
			_inAdapter = new ChannelMessageAdapter(_inAdapter, inChannel, new PassThroughMessageChannel());
			_inAdapter.NewOutMessage += OnMarketEmulatorNewOutMessage;

			_isEmulationOnly = isEmulationOnly;
		}

		/// <summary>
		/// Emulator.
		/// </summary>
		public IMarketEmulator Emulator { get; }

		/// <summary>
		/// Settings of exchange emulator.
		/// </summary>
		public MarketEmulatorSettings Settings => Emulator.Settings;

		/// <summary>
		/// Incoming messages channel.
		/// </summary>
		public IMessageChannel InChannel { get; }

		/// <inheritdoc />
		public override IEnumerable<MessageTypes> SupportedInMessages => InnerAdapter.SupportedInMessages.Concat(Emulator.SupportedInMessages).Except(OwnInnerAdapter ? ArrayHelper.Empty<MessageTypes>() : new[] { MessageTypes.SecurityLookup }).Distinct().ToArray();
		
		/// <inheritdoc />
		public override IEnumerable<MessageTypes> SupportedOutMessages => InnerAdapter.SupportedOutMessages.Concat(Emulator.SupportedOutMessages).Distinct().ToArray();

		private void SendToEmulator(Message message)
		{
			_inAdapter.SendInMessage(message);
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;
					ProcessOrderMessage(regMsg.PortfolioName, regMsg);
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
					SendToEmulator(message);
					return true;
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
					var transId = ((ISubscriptionMessage)message).TransactionId;
					_subscriptionIds.Add(transId);

					if (!OwnInnerAdapter)
						_realSubscribeIds.Add(transId);

					SendToEmulator(message);
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
			if (OwnInnerAdapter || !message.IsBack())
				base.InnerAdapterNewOutMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message.IsBack())
			{
				if (OwnInnerAdapter)
					base.OnInnerAdapterNewOutMessage(message);

				return;
			}

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
					// responses for own adapter will be send via MarketEmulator
					//else
					//	base.OnInnerAdapterNewOutMessage(message);

					break;
				}
				case MessageTypes.Connect:
				case MessageTypes.Disconnect:
				case MessageTypes.Reset:
				//case MessageTypes.BoardState:
				case MessageTypes.Portfolio:
				case MessageTypes.PositionChange:
				{
					if (OwnInnerAdapter)
						base.OnInnerAdapterNewOutMessage(message);

					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.IsMarketData())
						TrySendToEmulator((ISubscriptionIdMessage)message);
					else
					{
						if (OwnInnerAdapter)
							base.OnInnerAdapterNewOutMessage(message);
					}

					break;
				}

				case MessageTypes.Security:
				case MessageTypes.Board:
				{
					if (OwnInnerAdapter)
						base.OnInnerAdapterNewOutMessage(message);

					SendToEmulator(message);
					//TrySendToEmulator((ISubscriptionIdMessage)message);
					break;
				}

				case ExtendedMessageTypes.Last:
					SendToEmulator(message);
					break;

				case MessageTypes.Time:
				{
					if (OwnInnerAdapter)
					{
						if (_isEmulationOnly)
							SendToEmulator(message);
						else
							base.OnInnerAdapterNewOutMessage(message);
					}

					break;
				}

				default:
				{
					if (message is ISubscriptionIdMessage subscrMsg)
						TrySendToEmulator(subscrMsg);
					else
					{
						if (OwnInnerAdapter)
							base.OnInnerAdapterNewOutMessage(message);
					}

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
						pfMsg.TransactionId = TransactionIdGenerator.GetNextId();
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
			}

			RaiseNewOutMessage(message);
		}

		private void ProcessOrderMessage(string portfolioName, OrderMessage message)
		{
			if (OwnInnerAdapter)
			{
				if (_isEmulationOnly || portfolioName.CompareIgnoreCase(Extensions.SimulatorPortfolioName))
				{
					if (!_isEmulationOnly)
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

		private void ProcessOrderMessage(long transId, Message message)
		{
			if (_isEmulationOnly || _emuOrderIds.Contains(transId))
				SendToEmulator(message);
			else
				base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(MarketEmulator), Settings.Save());
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Settings.Load(storage.GetValue<SettingsStorage>(nameof(MarketEmulator)));
		}

		/// <summary>
		/// Create a copy of <see cref="EmulationMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
			=> new EmulationMessageAdapter(InnerAdapter.TypedClone(), InChannel, _isEmulationOnly, Emulator.SecurityProvider, Emulator.PortfolioProvider);
	}
}