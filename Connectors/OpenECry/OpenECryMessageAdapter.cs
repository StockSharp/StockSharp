#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.OpenECry.OpenECry
File: OpenECryMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.OpenECry
{
	using System;
	using System.Security;
	using System.Threading;

	using Ecng.Common;

	using OEC;
	using OEC.API;
	using OEC.Data;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class OpenECryMessageAdapter : MessageAdapter
	{
		private class MarshallingMessage : Message
		{
			private readonly APIAction _action;

			public const MessageTypes Marshalling = (MessageTypes)(-1000);

			public MarshallingMessage(APIAction action)
				: base(Marshalling)
			{
				if (action == null)
					throw new ArgumentNullException(nameof(action));

				_action = action;
			}

			public void Activate()
			{
				_action();
			}
		}

		private class InPlaceThreadPolicy : Disposable, ThreadingPolicy
		{
			private readonly OpenECryMessageAdapter _adapter;
			private readonly int _threadOwner;

			public InPlaceThreadPolicy(OpenECryMessageAdapter adapter)
			{
				if (adapter == null)
					throw new ArgumentNullException(nameof(adapter));

				_adapter = adapter;
				_threadOwner = Thread.CurrentThread.ManagedThreadId;
			}

			private EventHandler _timer;

			public void InvokeTimer()
			{
				_timer?.Invoke(this, EventArgs.Empty);
			}

			object ThreadingPolicy.SetTimer(int timeout, EventHandler action)
			{
				_timer = action;
				return _timer;
			}

			void ThreadingPolicy.KillTimer(object timerObject)
			{
				(timerObject as Timer)?.Dispose();
			}

			void ThreadingPolicy.Send(APIAction action)
			{
				//action();
				throw new NotSupportedException();
			}

			void ThreadingPolicy.Post(APIAction action)
			{
				if (_threadOwner == Thread.CurrentThread.ManagedThreadId)
					action();
				else
					_adapter.SendOutMessage(new MarshallingMessage(action) { IsBack = true });
			}
		}

		class OECLogger : ILogImpl
		{
			private readonly OpenECryMessageAdapter _adapter;

			public OECLogger(OpenECryMessageAdapter adapter)
			{
				if (adapter == null)
					throw new ArgumentNullException(nameof(adapter));

				_adapter = adapter;
			}

			void ILogImpl.Start(string path)
			{
			}

			void ILogImpl.Stop()
			{
			}

			void ILogImpl.WriteLine(Severity severity, string format, params object[] args)
			{
				switch (severity)
				{
					case Severity.N_A:
					case Severity.Prf:
					case Severity.Dbg:
						_adapter.AddDebugLog(format, args);
						break;
					case Severity.Inf:
						_adapter.AddInfoLog(format, args);
						break;
					case Severity.Wrn:
						_adapter.AddWarningLog(format, args);
						break;
					case Severity.Err:
					case Severity.Crt:
						_adapter.AddErrorLog(format, args);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(severity));
				}
			}
		}

		private OECClient _client;
		private InPlaceThreadPolicy _threadPolicy;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenECryMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public OpenECryMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();

			Uuid = DefaultUuid;

			HeartbeatInterval = TimeSpan.FromMilliseconds(10);
		}

		/// <summary>
		/// Default unique software ID.
		/// </summary>
		public static readonly SecureString DefaultUuid = "d05c09e4-9659-4040-b03b-87719d28dc5b".To<SecureString>();

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <returns>Order condition. If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new OpenECryOrderCondition();
		}

		private void SessionOnLoginComplete()
		{
			SendOutMessage(new ConnectMessage());
		}

		private void SessionOnLoginFailed(FailReason reason)
		{
			SendOutMessage(new ConnectMessage
			{
				Error = new InvalidOperationException(reason.GetDescription())
			});
		}

		private void SessionOnDisconnected(bool unexpected)
		{
			SendOutMessage(new DisconnectMessage
			{
				Error = unexpected ? new InvalidOperationException(LocalizedStrings.Str2551) : null
			});

			DisposeClient();

			_client = null;
			_threadPolicy = null;
		}

		private void SessionOnError(Exception exception)
		{
			if (_client != null)
			{
				if (!_client.CompleteConnected)
					SessionOnDisconnected(true);
				else
					SendOutError(exception);
			}
		}

		private void SessionOnBeginEvents()
		{
		}

		private void SessionOnEndEvents()
		{
		}

		/// <summary>
		/// <see cref="SecurityLookupMessage"/> required to get securities.
		/// </summary>
		public override bool SecurityLookupRequired => false;

		/// <summary>
		/// Gets a value indicating whether the connector supports position lookup.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup => true;

		/// <summary>
		/// Gets a value indicating whether the connector supports security lookup.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup => true;

		private bool _isClientSubscribed;

		private void SubscribeClient()
		{
			if (_isClientSubscribed)
				return;

			_isClientSubscribed = true;

			_client.OnLoginComplete += SessionOnLoginComplete;
			_client.OnLoginFailed += SessionOnLoginFailed;
			_client.OnDisconnected += SessionOnDisconnected;
			_client.OnBeginEvents += SessionOnBeginEvents;
			_client.OnEndEvents += SessionOnEndEvents;
			_client.OnError += SessionOnError;

			_client.OnAccountRiskLimitChanged += SessionOnAccountRiskLimitChanged;
			_client.OnAccountSummaryChanged += SessionOnAccountSummaryChanged;
			_client.OnAllocationBlocksChanged += SessionOnAllocationBlocksChanged;
			_client.OnAvgPositionChanged += SessionOnAvgPositionChanged;
			_client.OnBalanceChanged += SessionOnBalanceChanged;
			_client.OnCommandUpdated += SessionOnCommandUpdated;
			_client.OnCompoundPositionGroupChanged += SessionOnCompoundPositionGroupChanged;
			_client.OnOrderConfirmed += SessionOnOrderConfirmed;
			_client.OnOrderFilled += SessionOnOrderFilled;
			_client.OnOrderStateChanged += SessionOnOrderStateChanged;
			_client.OnDetailedPositionChanged += SessionOnDetailedPositionChanged;
			_client.OnMarginCalculationCompleted += SessionOnMarginCalculationCompleted;
			_client.OnPortfolioMarginChanged += SessionOnPortfolioMarginChanged;
			_client.OnPostAllocation += SessionOnPostAllocation;
			_client.OnRiskLimitDetailsReceived += SessionOnRiskLimitDetailsReceived;

			_client.OnBarsReceived += SessionOnBarsReceived;
			_client.OnContinuousContractRuleChanged += SessionOnContinuousContractRuleChanged;
			_client.OnContractChanged += SessionOnContractChanged;
			_client.OnContractCreated += SessionOnContractCreated;
			_client.OnContractRiskLimitChanged += SessionOnContractRiskLimitChanged;
			_client.OnContractsChanged += SessionOnContractsChanged;
			_client.OnCurrencyPriceChanged += SessionOnCurrencyPriceChanged;
			_client.OnDOMChanged += SessionOnDomChanged;
			_client.OnDealQuoteUpdated += SessionOnDealQuoteUpdated;
			_client.OnHistogramReceived += SessionOnHistogramReceived;
			_client.OnHistoryReceived += SessionOnHistoryReceived;
			_client.OnIndexComponentsReceived += SessionOnIndexComponentsReceived;
			_client.OnLoggedUserClientsChanged += SessionOnLoggedUserClientsChanged;
			_client.OnNewsMessage += SessionOnNewsMessage;
			_client.OnOsmAlgoListLoaded += SessionOnOsmAlgoListLoaded;
			_client.OnOsmAlgoListUpdated += SessionOnOsmAlgoListUpdated;
			_client.OnPitGroupsChanged += SessionOnPitGroupsChanged;
			_client.OnPriceChanged += SessionOnPriceChanged;
			_client.OnPriceTick += SessionOnPriceTick;
			_client.OnProductCalendarUpdated += SessionOnProductCalendarUpdated;
			_client.OnQuoteDetailsChanged += SessionOnQuoteDetailsChanged;
			_client.OnRelationsChanged += SessionOnRelationsChanged;
			_client.OnSymbolLookupReceived += SessionOnSymbolLookupReceived;
			_client.OnTicksReceived += SessionOnTicksReceived;
			_client.OnTradersChanged += SessionOnTradersChanged;
			_client.OnUserMessage += SessionOnUserMessage;
			_client.OnUserStatusChanged += SessionOnUserStatusChanged;
		}

		private void UnsubscribeClient()
		{
			if (!_isClientSubscribed)
				return;

			_isClientSubscribed = false;

			_client.OnLoginComplete -= SessionOnLoginComplete;
			_client.OnLoginFailed -= SessionOnLoginFailed;
			_client.OnDisconnected -= SessionOnDisconnected;
			_client.OnBeginEvents -= SessionOnBeginEvents;
			_client.OnEndEvents -= SessionOnEndEvents;
			_client.OnError -= SessionOnError;

			_client.OnAccountRiskLimitChanged -= SessionOnAccountRiskLimitChanged;
			_client.OnAccountSummaryChanged -= SessionOnAccountSummaryChanged;
			_client.OnAllocationBlocksChanged -= SessionOnAllocationBlocksChanged;
			_client.OnAvgPositionChanged -= SessionOnAvgPositionChanged;
			_client.OnBalanceChanged -= SessionOnBalanceChanged;
			_client.OnCommandUpdated -= SessionOnCommandUpdated;
			_client.OnCompoundPositionGroupChanged -= SessionOnCompoundPositionGroupChanged;
			_client.OnOrderConfirmed -= SessionOnOrderConfirmed;
			_client.OnOrderFilled -= SessionOnOrderFilled;
			_client.OnOrderStateChanged -= SessionOnOrderStateChanged;
			_client.OnDetailedPositionChanged -= SessionOnDetailedPositionChanged;
			_client.OnMarginCalculationCompleted -= SessionOnMarginCalculationCompleted;
			_client.OnPortfolioMarginChanged -= SessionOnPortfolioMarginChanged;
			_client.OnPostAllocation -= SessionOnPostAllocation;
			_client.OnRiskLimitDetailsReceived -= SessionOnRiskLimitDetailsReceived;

			_client.OnBarsReceived -= SessionOnBarsReceived;
			_client.OnContinuousContractRuleChanged -= SessionOnContinuousContractRuleChanged;
			_client.OnContractChanged -= SessionOnContractChanged;
			_client.OnContractCreated -= SessionOnContractCreated;
			_client.OnContractRiskLimitChanged -= SessionOnContractRiskLimitChanged;
			_client.OnContractsChanged -= SessionOnContractsChanged;
			_client.OnCurrencyPriceChanged -= SessionOnCurrencyPriceChanged;
			_client.OnDOMChanged -= SessionOnDomChanged;
			_client.OnDealQuoteUpdated -= SessionOnDealQuoteUpdated;
			_client.OnHistogramReceived -= SessionOnHistogramReceived;
			_client.OnHistoryReceived -= SessionOnHistoryReceived;
			_client.OnIndexComponentsReceived -= SessionOnIndexComponentsReceived;
			_client.OnLoggedUserClientsChanged -= SessionOnLoggedUserClientsChanged;
			_client.OnNewsMessage -= SessionOnNewsMessage;
			_client.OnOsmAlgoListLoaded -= SessionOnOsmAlgoListLoaded;
			_client.OnOsmAlgoListUpdated -= SessionOnOsmAlgoListUpdated;
			_client.OnPitGroupsChanged -= SessionOnPitGroupsChanged;
			_client.OnPriceChanged -= SessionOnPriceChanged;
			_client.OnPriceTick -= SessionOnPriceTick;
			_client.OnProductCalendarUpdated -= SessionOnProductCalendarUpdated;
			_client.OnQuoteDetailsChanged -= SessionOnQuoteDetailsChanged;
			_client.OnRelationsChanged -= SessionOnRelationsChanged;
			_client.OnSymbolLookupReceived -= SessionOnSymbolLookupReceived;
			_client.OnTicksReceived -= SessionOnTicksReceived;
			_client.OnTradersChanged -= SessionOnTradersChanged;
			_client.OnUserMessage -= SessionOnUserMessage;
			_client.OnUserStatusChanged -= SessionOnUserStatusChanged;
		}

		private void DisposeClient()
		{
			UnsubscribeClient();
			_client.Dispose();
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_subscriptionDataBySid.Clear();
					_subscriptionsByKey.Clear();
					_processedSecurities.Clear();

					if (_client != null)
					{
						try
						{
							DisposeClient();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_client = null;
						_threadPolicy = null;
					}

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Time:
				{
					_threadPolicy?.InvokeTimer();
					break;
				}

				case MarshallingMessage.Marshalling:
				{
					var msg = (MarshallingMessage)message;
					msg.Activate();
					break;
				}

				case MessageTypes.Connect:
				{
					if (_client != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_subscriptionDataBySid.Clear();
					_subscriptionsByKey.Clear();

					switch (Remoting)
					{
						case OpenECryRemoting.None:
						case OpenECryRemoting.Primary:
							_threadPolicy = new InPlaceThreadPolicy(this);
							
							_client = new OECClient(_threadPolicy)
							{
								UUID = Uuid.To<string>(),
								EventBatchInterval = 0,
								RemoteHostingEnabled = Remoting == OpenECryRemoting.Primary,
								//PriceHost = "",
								//AutoSubscribe = false
							};
							break;
						case OpenECryRemoting.Secondary:
							_client = OECClient.CreateInstance(true);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					if (EnableOECLogging)
					{
						if (Remoting == OpenECryRemoting.Secondary)
						{
							this.AddWarningLog(LocalizedStrings.Str2552);
						}
						else
						{
							OEC.Log.ConsoleOutput = false;
							_client.SetLoggingConfig(new LoggingConfiguration { Level = OEC.API.LogLevel.All });
							OEC.Log.Initialize(new OECLogger(this));
							OEC.Log.Start();
						}
					}

					SubscribeClient();

					_client.Connect(Address.GetHost(), Address.GetPort(), Login, Password.To<string>(), UseNativeReconnect);

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_client == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					_client.Disconnect();
					SendOutMessage(new TimeMessage { IsBack = true });
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					ProcessSecurityLookup((SecurityLookupMessage)message);
					break;
				}

				case MessageTypes.OrderRegister:
				{
					ProcessOrderRegister((OrderRegisterMessage)message);
					break;
				}

				case MessageTypes.OrderCancel:
				{
					ProcessOrderCancel((OrderCancelMessage)message);
					break;
				}

				case MessageTypes.OrderReplace:
				{
					ProcessOrderReplace((OrderReplaceMessage)message);
					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					ProcessPortfolioLookupMessage((PortfolioLookupMessage)message);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					ProcessOrderStatusMessage();
					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;
				}

				case MessageTypes.News:
				{
					var newsMsg = (Messages.NewsMessage)message;
					_client.SendMessage(_client.Users[newsMsg.Source], newsMsg.Headline);
					break;
				}
			}
		}
	}
}