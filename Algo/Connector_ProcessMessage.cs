#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: Connector_ProcessMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Risk;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class Connector
	{
		private readonly SyncObject _marketTimerSync = new SyncObject();
		private Timer _marketTimer;
		private readonly TimeMessage _marketTimeMessage = new TimeMessage();
		private bool _isMarketTimeHandled;

		private void CreateTimer()
		{
			lock (_marketTimerSync)
			{
				_isMarketTimeHandled = true;

				if (_marketTimer != null)
					return;

				_marketTimer = ThreadingHelper
					.Timer(() =>
					{
						try
						{
							// TimeMsg required for notify invoke MarketTimeChanged event (and active time based IMarketRule-s)
							// No need to put _marketTimeMessage again, if it still in queue.

							lock (_marketTimerSync)
							{
								if (_marketTimer == null || !_isMarketTimeHandled)
									return;

								_isMarketTimeHandled = false;
							}

							_marketTimeMessage.LocalTime = TimeHelper.NowWithOffset;
							SendOutMessage(_marketTimeMessage);
						}
						catch (Exception ex)
						{
							ex.LogError();
						}
					})
					.Interval(MarketTimeChangedInterval);
			}
		}

		private void CloseTimer()
		{
			lock (_marketTimerSync)
			{
				if (_marketTimer != null)
				{
					_marketTimer.Dispose();
					_marketTimer = null;
				}

				_isMarketTimeHandled = false;
			}
		}

		private readonly CachedSynchronizedDictionary<IMessageAdapter, ConnectionStates> _adapterStates = new CachedSynchronizedDictionary<IMessageAdapter, ConnectionStates>();
		
		private readonly ResetMessage _disposeMessage = new ResetMessage();

		private string AssociatedBoardCode => Adapter.AssociatedBoardCode;
		
		private void AdapterOnNewOutMessage(Message message)
		{
			if (message.IsBack)
			{
				//message.IsBack = false;

				if (message.Type == MessageTypes.MarketData)
				{
					var mdMsg = (MarketDataMessage)message;

					var security = mdMsg.DataType == MarketDataTypes.News ? null : GetSecurity(mdMsg.SecurityId);
					_subscriptionManager.ProcessRequest(security, mdMsg, true);
				}
				else if (message.Type == MessageTypes.OrderGroupCancel)
				{
					var cancelMsg = (OrderGroupCancelMessage)message;
					_entityCache.AddMassCancelationId(cancelMsg.TransactionId);
					SendInMessage(message);
				}
				else
					SendInMessage(message);
			}
			else
				SendOutMessage(message);
		}

		/// <summary>
		/// To call the <see cref="Connected"/> event when the first adapter connects to <see cref="Adapter"/>.
		/// </summary>
		public bool RaiseConnectedOnFirstAdapter { get; set; } = true;

		private IMessageChannel _inMessageChannel;

		/// <summary>
		/// Input message channel.
		/// </summary>
		public IMessageChannel InMessageChannel
		{
			get => _inMessageChannel;
			protected set
			{
				if (value == _inMessageChannel)
					return;

				if (_inMessageChannel != null)
				{
					_inMessageChannel.NewOutMessage -= InMessageChannelOnNewOutMessage;
					_inMessageChannel?.Dispose();
				}

				_inMessageChannel = value;

				if (_inMessageChannel != null)
					_inMessageChannel.NewOutMessage += InMessageChannelOnNewOutMessage;
			}
		}

		private IMessageChannel _outMessageChannel;

		/// <summary>
		/// Outgoing message channel.
		/// </summary>
		public IMessageChannel OutMessageChannel
		{
			get => _outMessageChannel;
			protected set
			{
				if (value == _outMessageChannel)
					return;

				if (_outMessageChannel != null)
				{
					_outMessageChannel.NewOutMessage -= OutMessageChannelOnNewOutMessage;
					_outMessageChannel?.Dispose();
				}

				_outMessageChannel = value;

				if (_outMessageChannel != null)
					_outMessageChannel.NewOutMessage += OutMessageChannelOnNewOutMessage;
			}
		}

		private void InMessageChannelOnNewOutMessage(Message message)
		{
			_inAdapter?.SendInMessage(message);

			if (message != _disposeMessage)
				return;

			InMessageChannel = null;
			Adapter = null;
			OutMessageChannel = null;
		}

		private void OutMessageChannelOnNewOutMessage(Message message)
		{
			OnProcessMessage(message);
		}

		private IMessageAdapter _inAdapter;

		/// <summary>
		/// Inner message adapter.
		/// </summary>
		public IMessageAdapter InnerAdapter
		{
			get => _inAdapter;
			set
			{
				if (_inAdapter == value)
					return;

				if (_inAdapter != null)
				{
					_inAdapter.NewOutMessage -= AdapterOnNewOutMessage;
				}

				if (_adapter != null)
				{
					_adapter.InnerAdapters.Added -= InnerAdaptersOnAdded;
					_adapter.InnerAdapters.Removed -= InnerAdaptersOnRemoved;
					_adapter.InnerAdapters.Cleared -= InnerAdaptersOnCleared;
				}

				_inAdapter = value;
				_adapter = null;
				StorageAdapter = null;

				if (_inAdapter == null)
					return;

				var adapter = _inAdapter as IMessageAdapterWrapper;

				while (adapter != null)
				{
					if (adapter is StorageMessageAdapter storage)
						StorageAdapter = storage;

					if (adapter.InnerAdapter is BasketMessageAdapter basket)
						_adapter = basket;

					adapter = adapter.InnerAdapter as IMessageAdapterWrapper;
				}

				if (_adapter != null)
				{
					_adapter.InnerAdapters.Added += InnerAdaptersOnAdded;
					_adapter.InnerAdapters.Removed += InnerAdaptersOnRemoved;
					_adapter.InnerAdapters.Cleared += InnerAdaptersOnCleared;

					_adapter.InnerAdapters.ForEach(InnerAdaptersOnAdded);
				}

				_inAdapter.NewOutMessage += AdapterOnNewOutMessage;
			}
		}

		private BasketMessageAdapter _adapter;

		/// <summary>
		/// Message adapter.
		/// </summary>
		public BasketMessageAdapter Adapter
		{
			get => _adapter;
			protected set
			{
				if (!_isDisposing && value == null)
					throw new ArgumentNullException(nameof(value));

				if (_adapter == value)
					return;

				if (_adapter != null)
				{
					_adapter.InnerAdapters.Added -= InnerAdaptersOnAdded;
					_adapter.InnerAdapters.Removed -= InnerAdaptersOnRemoved;
					_adapter.InnerAdapters.Cleared -= InnerAdaptersOnCleared;

					//SendInMessage(new ResetMessage());

					_inAdapter.NewOutMessage -= AdapterOnNewOutMessage;
					_inAdapter.Dispose();

					//if (_inAdapter != _adapter)
					//	_adapter.Dispose();
				}

				_adapter = value;
				_inAdapter = _adapter;

				if (_adapter != null)
				{
					_adapter.InnerAdapters.Added += InnerAdaptersOnAdded;
					_adapter.InnerAdapters.Removed += InnerAdaptersOnRemoved;
					_adapter.InnerAdapters.Cleared += InnerAdaptersOnCleared;

					_adapter.Parent = this;

					//_inAdapter = new ChannelMessageAdapter(_inAdapter, InMessageChannel, OutMessageChannel)
					//{
					//	//OwnOutputChannel = true,
					//	OwnInnerAdaper = true
					//};

					if (RiskManager != null)
						_inAdapter = new RiskMessageAdapter(_inAdapter) { RiskManager = RiskManager, OwnInnerAdaper = true };

					if (SupportOffline)
						_inAdapter = new OfflineMessageAdapter(_inAdapter) { OwnInnerAdaper = true };

					if (SecurityStorage != null && StorageRegistry != null && SnapshotRegistry != null)
					{
						_inAdapter = StorageAdapter = new StorageMessageAdapter(_inAdapter, SecurityStorage, PositionStorage, StorageRegistry, SnapshotRegistry, _adapter.CandleBuilderProvider)
						{
							OwnInnerAdaper = true,
							OverrideSecurityData = OverrideSecurityData
						};
					}

					if (SupportBasketSecurities)
						_inAdapter = new BasketSecurityMessageAdapter(this, BasketSecurityProcessorProvider, _entityCache.ExchangeInfoProvider, _inAdapter) { OwnInnerAdaper = true };

					if (SupportSubscriptionTracking)
						_inAdapter = new SubscriptionMessageAdapter(_inAdapter) { OwnInnerAdaper = true/*, IsRestoreOnReconnect = IsRestoreSubscriptionOnReconnect*/ };

					if (SupportLevel1DepthBuilder)
						_inAdapter = new Level1DepthBuilderAdapter(_inAdapter) { OwnInnerAdaper = true };

					if (SupportAssociatedSecurity)
						_inAdapter = new AssociatedSecurityAdapter(_inAdapter) { OwnInnerAdaper = true };

					if (SupportFilteredMarketDepth)
						_inAdapter = new FilteredMarketDepthAdapter(_inAdapter) { OwnInnerAdaper = true };

					_inAdapter.NewOutMessage += AdapterOnNewOutMessage;
				}
			}
		}

		/// <summary>
		/// Use <see cref="BasketSecurityMessageAdapter"/>.
		/// </summary>
		public bool SupportBasketSecurities { get; set; }

		private bool _supportOffline;

		/// <summary>
		/// Use <see cref="OfflineMessageAdapter"/>.
		/// </summary>
		public bool SupportOffline
		{
			get => _supportOffline;
			set
			{
				if (_supportOffline == value)
					return;

				if (value)
					EnableAdapter(a => new OfflineMessageAdapter(a) { OwnInnerAdaper = true }, typeof(StorageMessageAdapter), false);
				else
					DisableAdapter<OfflineMessageAdapter>();

				_supportOffline = value;
			}
		}

		private bool _supportSubscriptionTracking;

		/// <summary>
		/// Use <see cref="SubscriptionMessageAdapter"/>.
		/// </summary>
		public bool SupportSubscriptionTracking
		{
			get => _supportSubscriptionTracking;
			set
			{
				if (_supportSubscriptionTracking == value)
					return;

				if (value)
					EnableAdapter(a => new SubscriptionMessageAdapter(a) { OwnInnerAdaper = true }, typeof(OfflineMessageAdapter), false);
				else
					DisableAdapter<SubscriptionMessageAdapter>();

				_supportSubscriptionTracking = value;
			}
		}

		private bool _supportFilteredMarketDepth;

		/// <summary>
		/// Use <see cref="FilteredMarketDepthAdapter"/>.
		/// </summary>
		public bool SupportFilteredMarketDepth
		{
			get => _supportFilteredMarketDepth;
			set
			{
				if (_supportFilteredMarketDepth == value)
					return;

				if (value)
					EnableAdapter(a => new FilteredMarketDepthAdapter(a) { OwnInnerAdaper = true }, typeof(Level1DepthBuilderAdapter));
				else
					DisableAdapter<FilteredMarketDepthAdapter>();

				_supportFilteredMarketDepth = value;
			}
		}

		private bool _supportAssociatedSecurity;

		/// <summary>
		/// Use <see cref="AssociatedSecurityAdapter"/>.
		/// </summary>
		public bool SupportAssociatedSecurity
		{
			get => _supportAssociatedSecurity;
			set
			{
				if (_supportAssociatedSecurity == value)
					return;

				if (value)
					EnableAdapter(a => new AssociatedSecurityAdapter(a) { OwnInnerAdaper = true }, typeof(Level1DepthBuilderAdapter));
				else
					DisableAdapter<AssociatedSecurityAdapter>();

				_supportAssociatedSecurity = value;
			}
		}

		private bool _supportLevel1DepthBuilder;

		/// <summary>
		/// Use <see cref="Level1DepthBuilderAdapter"/>.
		/// </summary>
		public bool SupportLevel1DepthBuilder
		{
			get => _supportLevel1DepthBuilder;
			set
			{
				if (_supportLevel1DepthBuilder == value)
					return;

				if (value)
					EnableAdapter(a => new Level1DepthBuilderAdapter(a) { OwnInnerAdaper = true }, typeof(AssociatedSecurityAdapter), false);
				else
					DisableAdapter<Level1DepthBuilderAdapter>();

				_supportLevel1DepthBuilder = value;
			}
		}

		/// <summary>
		/// Send lookup messages on connect. By default is <see langword="true"/>.
		/// </summary>
		public bool LookupMessagesOnConnect { get; set; } = true;

		/// <summary>
		/// Send subscribe messages on connect. By default is <see langword="true"/>.
		/// </summary>
		public bool AutoPortfoliosSubscribe { get; set; } = true;

		private Tuple<IMessageAdapter, IMessageAdapter, IMessageAdapter> GetAdapter(Type type)
		{
			var adapter = _inAdapter;

			if (adapter == null)
				return null;

			var prev = (adapter as IMessageAdapterWrapper)?.InnerAdapter;
			var next = (IMessageAdapter)null;

			while (true)
			{
				if (adapter.GetType() == type)
					return Tuple.Create(prev, adapter, next);

				next = adapter;
				adapter = prev;

				if (adapter == null)
					return null;

				prev = (adapter as IMessageAdapterWrapper)?.InnerAdapter;
			}
		}

		private Tuple<IMessageAdapter, IMessageAdapter, IMessageAdapter> GetAdapter<T>()
			where T : IMessageAdapterWrapper
		{
			return GetAdapter(typeof(T));
		}

		private void EnableAdapter(Func<IMessageAdapter, IMessageAdapterWrapper> create, Type type = null, bool after = true)
		{
			if (_inAdapter == null)
				return;

			var tuple = type != null ? GetAdapter(type) : null;
			var adapter = tuple?.Item2;

			if (adapter != null)
			{
				if (after)
				{
					if (tuple.Item3 is IMessageAdapterWrapper nextWrapper)
						nextWrapper.InnerAdapter = create(adapter);
					else
						AddAdapter(create);
				}
				else
				{
					var prevWrapper = tuple.Item1;
					var nextWrapper = adapter as IMessageAdapterWrapper;

					if (prevWrapper == null)
						throw new InvalidOperationException("Adapter wrapper cannot be added to the beginning of the chain.");

					if (nextWrapper == null)
						throw new InvalidOperationException(LocalizedStrings.TypeNotImplemented.Put(adapter.GetType(), nameof(IMessageAdapterWrapper)));

					nextWrapper.InnerAdapter = create(prevWrapper);
				}
			}
			else
				AddAdapter(create);
		}

		private void AddAdapter(Func<IMessageAdapter, IMessageAdapterWrapper> create)
		{
			_inAdapter.NewOutMessage -= AdapterOnNewOutMessage;

			_inAdapter = create(_inAdapter);
			_inAdapter.NewOutMessage += AdapterOnNewOutMessage;
		}

		private void DisableAdapter<T>()
			where T : IMessageAdapterWrapper
		{
			var tuple = GetAdapter<T>();

			if (tuple == null)
				return;

			var adapter = tuple.Item2;
			var adapterWrapper = (MessageAdapterWrapper)adapter;

			var next = tuple.Item3;
			var nextWrapper = (MessageAdapterWrapper)next;

			if (next == null)
			{
				adapterWrapper.NewOutMessage -= AdapterOnNewOutMessage;

				_inAdapter = adapterWrapper.InnerAdapter;
				_inAdapter.NewOutMessage += AdapterOnNewOutMessage;
			}
			else
				nextWrapper.InnerAdapter = adapterWrapper.InnerAdapter;

			adapterWrapper.OwnInnerAdaper = false;
			adapterWrapper.Dispose();
		}

		private void InnerAdaptersOnAdded(IMessageAdapter adapter)
		{
			if (adapter.IsMessageSupported(MessageTypes.OrderRegister))
				TransactionAdapter = adapter;

			if (adapter.IsMessageSupported(MessageTypes.MarketData))
				MarketDataAdapter = adapter;
		}

		private void InnerAdaptersOnRemoved(IMessageAdapter adapter)
		{
			if (TransactionAdapter == adapter)
				TransactionAdapter = null;

			if (MarketDataAdapter == adapter)
				MarketDataAdapter = null;

			_adapterStates.Remove(adapter);
		}

		private void InnerAdaptersOnCleared()
		{
			TransactionAdapter = null;
			MarketDataAdapter = null;
		}

		/// <summary>
		/// Transactional adapter.
		/// </summary>
		public IMessageAdapter TransactionAdapter { get; private set; }

		/// <summary>
		/// Market-data adapter.
		/// </summary>
		public IMessageAdapter MarketDataAdapter { get; private set; }

		/// <summary>
		/// Storage adapter.
		/// </summary>
		public StorageMessageAdapter StorageAdapter { get; private set; }

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public void SendInMessage(Message message)
		{
			message.TryInitLocalTime(this);

			if (!InMessageChannel.IsOpened)
				InMessageChannel.Open();

			InMessageChannel.SendInMessage(message);
		}

		/// <summary>
		/// Send outgoing message.
		/// </summary>
		/// <param name="message">Message.</param>
		public void SendOutMessage(Message message)
		{
			message.TryInitLocalTime(this);

			if (!OutMessageChannel.IsOpened)
				OutMessageChannel.Open();

			OutMessageChannel.SendInMessage(message);
		}

		/// <summary>
		/// Send error message.
		/// </summary>
		/// <param name="error">Error details.</param>
		public void SendOutError(Exception error)
		{
			SendOutMessage(error.ToErrorMessage());
		}

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected virtual void OnProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Time && message.Type != MessageTypes.QuoteChange)
				this.AddVerboseLog("BP:{0}", message);

			ProcessTimeInterval(message);

			RaiseNewMessage(message);

			try
			{
				switch (message.Type)
				{
					case MessageTypes.QuoteChange:
						ProcessQuotesMessage((QuoteChangeMessage)message);
						break;

					case MessageTypes.Board:
						ProcessBoardMessage((BoardMessage)message);
						break;

					case MessageTypes.Security:
						ProcessSecurityMessage((SecurityMessage)message);
						break;

					case MessageTypes.SecurityLookupResult:
						ProcessSecurityLookupResultMessage((SecurityLookupResultMessage)message);
						break;

					case MessageTypes.BoardLookupResult:
						ProcessBoardLookupResultMessage((BoardLookupResultMessage)message);
						break;

					case MessageTypes.PortfolioLookupResult:
						ProcessPortfolioLookupResultMessage((PortfolioLookupResultMessage)message);
						break;

					case MessageTypes.Level1Change:
						ProcessLevel1ChangeMessage((Level1ChangeMessage)message);
						break;

					case MessageTypes.News:
						ProcessNewsMessage((NewsMessage)message);
						break;

					case MessageTypes.Execution:
						ProcessExecutionMessage((ExecutionMessage)message);
						break;

					case MessageTypes.Portfolio:
						ProcessPortfolioMessage((PortfolioMessage)message);
						break;

					case MessageTypes.PortfolioChange:
						ProcessPortfolioChangeMessage((PortfolioChangeMessage)message);
						break;

					//case MessageTypes.Position:
					//	ProcessPositionMessage((PositionMessage)message);
					//	break;

					case MessageTypes.PositionChange:
						ProcessPositionChangeMessage((PositionChangeMessage)message);
						break;

					//case MessageTypes.Time:
					//	var timeMsg = (TimeMessage)message;

					//	if (timeMsg.Shift != null)
					//		TimeShift = timeMsg.Shift;

					//	// TimeMessage могут пропускаться при наличии других месседжей, поэтому событие
					//	// MarketTimeChanged необходимо вызывать при обработке времени из любых месседжей.
					//	break;

					case MessageTypes.MarketData:
						ProcessMarketDataMessage((MarketDataMessage)message);
						break;

					case MessageTypes.Error:
						var errorMsg = (ErrorMessage)message;
						RaiseError(errorMsg.Error);
						break;

					case MessageTypes.Connect:
						ProcessConnectMessage((ConnectMessage)message);
						break;

					case MessageTypes.Disconnect:
						ProcessConnectMessage((DisconnectMessage)message);
						break;

					case MessageTypes.SecurityLookup:
					{
						var lookupMsg = (SecurityLookupMessage)message;
						_securityLookups.Add(lookupMsg.TransactionId, (SecurityLookupMessage)lookupMsg.Clone());
						SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = lookupMsg.TransactionId });
						break;
					}

					case MessageTypes.BoardState:
						ProcessBoardStateMessage((BoardStateMessage)message);
						break;

					case ExtendedMessageTypes.RemoveSecurity:
						ProcessSecurityRemoveMessage((SecurityRemoveMessage)message);
						break;

					case MessageTypes.CandleTimeFrame:
					case MessageTypes.CandlePnF:
					case MessageTypes.CandleRange:
					case MessageTypes.CandleRenko:
					case MessageTypes.CandleTick:
					case MessageTypes.CandleVolume:
						ProcessCandleMessage((CandleMessage)message);
						break;

					case MessageTypes.MarketDataFinished:
						ProcessMarketDataFinishedMessage((MarketDataFinishedMessage)message);
						break;

					// если адаптеры передают специфичные сообщения
					//default:
					//	throw new ArgumentOutOfRangeException(LocalizedStrings.Str2142Params.Put(message.Type));
				}
			}
			catch (Exception ex)
			{
				RaiseError(new InvalidOperationException(LocalizedStrings.Str681Params.Put(message), ex));
			}
		}

		private void ProcessMarketDataMessage(MarketDataMessage mdMsg)
		{
			//_subscriptionManager.ProcessResponse(mdMsg);

			////инструмент может быть не указан
			////и нет необходимости вызывать события MarketDataSubscriptionSucceeded/Failed
			//if (mdMsg.SecurityId.IsDefault())
			//{
			//	if (mdMsg.Error != null)
			//		RaiseError(mdMsg.Error);

			//	return;
			//}

			var error = mdMsg.Error;

			var security = _subscriptionManager.ProcessResponse(mdMsg.OriginalTransactionId, out var originalMsg);

			if (security == null && originalMsg?.DataType != MarketDataTypes.News)
			{
				if (error != null)
					RaiseError(error);

				return;
			}

			if (originalMsg.IsSubscribe)
			{
				if (error == null)
					RaiseMarketDataSubscriptionSucceeded(security, originalMsg);
				else
					RaiseMarketDataSubscriptionFailed(security, originalMsg, error);
			}
			else
			{
				if (error == null)
				{
					RaiseMarketDataUnSubscriptionSucceeded(security, originalMsg);
					ProcessCandleSeriesStopped(originalMsg.OriginalTransactionId);
				}
				else
					RaiseMarketDataUnSubscriptionFailed(security, originalMsg, error);
			}
		}

		private void ProcessSecurityRemoveMessage(SecurityRemoveMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var securityId = message.SecurityId;

			var removedSecurity = _entityCache.TryRemoveSecurity(CreateSecurityId(securityId.SecurityCode, securityId.BoardCode));

			if (removedSecurity != null)
				_removed?.Invoke(new[] { removedSecurity });
		}

		/// <inheritdoc />
		public Security LookupSecurity(SecurityId securityId)
		{
			var securityCode = securityId.SecurityCode;
			var boardCode = securityId.BoardCode;

			//if (boardCode.IsEmpty())
			//	boardCode = AssociatedBoardCode;

			var stockSharpId = CreateSecurityId(securityCode, boardCode);
			var security = _entityCache.GetSecurityById(stockSharpId);

			if (security == null)
			{
				if (EntityFactory is ISecurityProvider secProvider)
					security = secProvider.LookupById(stockSharpId);
			}

			if (security == null)
			{
				security = GetSecurity(securityId);

				if (security == null)
				{
					//if (!ignoreIfNotExist)
					throw new ArgumentException(nameof(securityId), LocalizedStrings.Str692Params.Put(securityId, this));

					//return;
				}
			}

			return security;
		}

		private void ProcessConnectMessage(BaseConnectionMessage message)
		{
			var isConnect = message is ConnectMessage;
			var adapter = message.Adapter;

			try
			{
				if (adapter == null)
				{
					if (message.Error != null)
						RaiseConnectionError(message.Error);

					return;
				}

				var state = _adapterStates[adapter];

				switch (state)
				{
					case ConnectionStates.Connecting:
					{
						if (isConnect)
						{
							if (message.Error == null)
							{
								SetAdapterConnected(adapter, message);
							}
							else
								SetAdapterFailed(adapter, message, ConnectionStates.Connecting, true);
						}
						else
							SetAdapterFailed(adapter, message, ConnectionStates.Connecting, false);

						return;
					}
					case ConnectionStates.Disconnecting:
					{
						if (!isConnect)
						{
							if (message.Error == null)
							{
								_adapterStates[adapter] = ConnectionStates.Disconnected;

								var isLast = _adapterStates.CachedValues.All(v => v != ConnectionStates.Disconnecting);

								// raise Disconnected only one time for the last adapter
								if (isLast)
									RaiseDisconnected();

								RaiseDisconnectedEx(adapter);
							}
							else
								SetAdapterFailed(adapter, message, ConnectionStates.Disconnecting, false);
						}
						else
							SetAdapterFailed(adapter, message, ConnectionStates.Disconnecting, false);

						return;
					}
					case ConnectionStates.Connected:
					{
						if (message.Error != null)
						{
							_adapterStates[adapter] = ConnectionStates.Failed;
							var error = new InvalidOperationException(LocalizedStrings.Str683, message.Error);
							RaiseConnectionError(error);
							RaiseConnectionErrorEx(adapter, error);
							return;
						}

						break;
					}
					case ConnectionStates.Disconnected:
					{
						break;
					}
					case ConnectionStates.Failed:
					{
						if (isConnect)
						{
							if (message.Error == null)
								SetAdapterConnected(adapter, message);

							return;
						}

						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}

				// так как соединение установлено, то выдаем ошибку через Error, чтобы не сбрасывать состояние
				var error2 = new InvalidOperationException(LocalizedStrings.Str685Params.Put(state, message.GetType().Name), message.Error);
				RaiseError(error2);
				RaiseConnectionErrorEx(adapter, error2);
			}
			finally
			{
				if (TimeChange && _adapterStates.Count > 0 && _adapterStates.CachedValues.All(s => s == ConnectionStates.Disconnected || s == ConnectionStates.Failed))
					CloseTimer();
			}
		}

		private void SetAdapterConnected(IMessageAdapter adapter, BaseConnectionMessage message)
		{
			_adapterStates[adapter] = ConnectionStates.Connected;

			var isRestored = message is RestoredConnectMessage;

			if (ConnectionState == ConnectionStates.Connecting)
			{
				if (RaiseConnectedOnFirstAdapter)
				{
					// raise Connected event only one time for the first adapter
					RaiseConnected();
				}
				else
					RaiseConnectedWhenAllConnected();
			}
			else if (ConnectionState == ConnectionStates.Failed && !isRestored)
			{
				RaiseConnectedWhenAllConnected();
			}

			RaiseConnectedEx(adapter);

			if (LookupMessagesOnConnect)
			{
				if (adapter.PortfolioLookupRequired)
					LookupPortfolios(new Portfolio(), adapter);

				if (adapter.OrderStatusRequired)
					LookupOrders(new Order(), adapter);

				if (adapter.SecurityLookupRequired)
					LookupSecurities(new Security(), adapter);
			}

			if (!isRestored)
			{
				if (AutoPortfoliosSubscribe && adapter.IsSupportSubscriptionByPortfolio)
				{
					var portfolioNames = Adapter
						.AdapterProvider
						.PortfolioAdapters
						.Where(p => p.Value == adapter)
						.Select(p => p.Key)
						.ToArray();

					foreach (var portfolioName in portfolioNames)
					{
						SendInMessage(new PortfolioMessage
						{
							PortfolioName = portfolioName,
							TransactionId = TransactionIdGenerator.GetNextId(),
							IsSubscribe = true,
							Adapter = adapter,
						});
					}
				}

				return;
			}

			var isAllConnected = _adapterStates.CachedValues.All(v => v == ConnectionStates.Connected);

			if (!isAllConnected)
				return;

			ConnectionState = ConnectionStates.Connected;
			RaiseRestored();
		}

		private void RaiseConnectedWhenAllConnected()
		{
			var isAllConnected = _adapterStates.CachedValues.All(v => v == ConnectionStates.Connected);

			// raise Connected event only one time when the last adapter connection successfully
			if (isAllConnected)
				RaiseConnected();
		}

		private void SetAdapterFailed(IMessageAdapter adapter, BaseConnectionMessage message, ConnectionStates checkState, bool raiseTimeOut)
		{
			_adapterStates[adapter] = ConnectionStates.Failed;

			var error = message.Error ?? new InvalidOperationException(message is ConnectMessage ? LocalizedStrings.Str683 : LocalizedStrings.Str684);

			// raise ConnectionError only one time
			if (ConnectionState == checkState)
			{
				RaiseConnectionError(error);

				if (raiseTimeOut)
				{
					if (error is TimeoutException)
						RaiseTimeOut();
				}
			}
			else
				RaiseError(error);

			RaiseConnectionErrorEx(adapter, error);
		}

		private void ProcessBoardStateMessage(BoardStateMessage message)
		{
			var board = _entityCache.ExchangeInfoProvider.GetOrCreateBoard(message.BoardCode);
			_boardStates[board] = message.State;
			SessionStateChanged?.Invoke(board, message.State);
		}

		private void ProcessBoardMessage(BoardMessage message)
		{
			_entityCache.ExchangeInfoProvider.GetOrCreateBoard(message.Code, code =>
			{
				var exchange = message.ToExchange(EntityFactory.CreateExchange(message.ExchangeCode));
				var board = EntityFactory.CreateBoard(code, exchange);
				return board.ApplyChanges(message);
			});
		}

		private void ProcessSecurityMessage(SecurityMessage message/*, string boardCode = null*/)
		{
			var secId = CreateSecurityId(message.SecurityId.SecurityCode, message.SecurityId.BoardCode);

			var security = GetSecurity(secId, s =>
			{
				if (!UpdateSecurityByDefinition)
					return false;

				s.ApplyChanges(message, _entityCache.ExchangeInfoProvider, OverrideSecurityData);
				return true;
			});

			if (message.OriginalTransactionId != 0)
				_lookupResult.Add(security);
		}

		private void ProcessSecurityLookupResultMessage(SecurityLookupResultMessage message)
		{
			if (message.Error != null)
				RaiseError(message.Error);

			var result = _lookupResult.CopyAndClear();
			SecurityLookupMessage criteria = null;

			if (result.Length == 0)
			{
				criteria = _securityLookups.TryGetValue(message.OriginalTransactionId);

				if (criteria != null)
				{
					_securityLookups.Remove(message.OriginalTransactionId);
					result = this.FilterSecurities(criteria, _entityCache.ExchangeInfoProvider).ToArray();
				}
			}

			RaiseLookupSecuritiesResult(criteria, message.Error, result);

			lock (_lookupQueue.SyncRoot)
			{
				if (_lookupQueue.Count == 0)
					return;

				//удаляем текущий запрос лукапа из очереди
				_lookupQueue.Dequeue();

				if (_lookupQueue.Count == 0)
					return;

				var nextCriteria = _lookupQueue.Peek();

				//если есть еще запросы, для которых нет инструментов, то отправляем следующий
				if (NeedLookupSecurities(nextCriteria.SecurityId))
					SendInMessage(nextCriteria);
				else
				{
					_securityLookups.Add(nextCriteria.TransactionId, (SecurityLookupMessage)nextCriteria.Clone());
					SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = nextCriteria.TransactionId });
				}
			}
		}

		private void ProcessBoardLookupResultMessage(BoardLookupResultMessage message)
		{
			if (message.Error != null)
				RaiseError(message.Error);

			var criteria = _boardLookups.TryGetValue(message.OriginalTransactionId);

			if (criteria == null)
				return;

			RaiseLookupBoardsResult(criteria, message.Error, ExchangeBoards.Filter(criteria.Like));
		}

		private void ProcessPortfolioLookupResultMessage(PortfolioLookupResultMessage message)
		{
			if (message.Error != null)
				RaiseError(message.Error);

			var criteria = _portfolioLookups.TryGetValue(message.OriginalTransactionId);

			if (criteria == null)
				return;

			RaiseLookupPortfoliosResult(criteria, message.Error, Portfolios.Where(pf => criteria.PortfolioName.IsEmpty() || pf.Name.ContainsIgnoreCase(criteria.PortfolioName)));
		}

		private void ProcessLevel1ChangeMessage(Level1ChangeMessage message)
		{
			var security = LookupSecurity(message.SecurityId);

			if (UpdateSecurityByLevel1)
			{
				security.ApplyChanges(message);
				RaiseSecurityChanged(security);
			}

			var values = GetSecurityValues(security);

			var lastTradeFound = false;
			var bestBidFound = false;
			var bestAskFound = false;

			lock (values.SyncRoot)
			{
				foreach (var change in message.Changes)
				{
					var field = change.Key;

					if (!lastTradeFound)
					{
						if (field.IsLastTradeField())
						{
							values[(int)Level1Fields.LastTradeUpDown] = null;
							values[(int)Level1Fields.LastTradeTime] = null;
							values[(int)Level1Fields.LastTradeId] = null;
							values[(int)Level1Fields.LastTradeOrigin] = null;
							values[(int)Level1Fields.LastTradePrice] = null;
							values[(int)Level1Fields.LastTradeVolume] = null;

							lastTradeFound = true;
						}
					}

					if (!bestBidFound)
					{
						if (field.IsBestBidField())
						{
							values[(int)Level1Fields.BestBidPrice] = null;
							values[(int)Level1Fields.BestBidTime] = null;
							values[(int)Level1Fields.BestBidVolume] = null;

							bestBidFound = true;
						}
					}

					if (!bestAskFound)
					{
						if (field.IsBestAskField())
						{
							values[(int)Level1Fields.BestAskPrice] = null;
							values[(int)Level1Fields.BestAskTime] = null;
							values[(int)Level1Fields.BestAskVolume] = null;

							bestAskFound = true;
						}
					}

					values[(int)field] = change.Value;
				}	
			}

			RaiseValuesChanged(security, message.Changes, message.ServerTime, message.LocalTime);
		}

		/// <summary>
		/// To get the portfolio by the name.
		/// </summary>
		/// <remarks>
		/// If the portfolio is not registered, it is created via <see cref="IEntityFactory.CreatePortfolio"/>.
		/// </remarks>
		/// <param name="name">Portfolio name.</param>
		/// <returns>Portfolio.</returns>
		public Portfolio GetPortfolio(string name)
		{
			return GetPortfolio(name, null);
		}

		/// <summary>
		/// To get the portfolio by the name.
		/// </summary>
		/// <remarks>
		/// If the portfolio is not registered, it is created via <see cref="IEntityFactory.CreatePortfolio"/>.
		/// </remarks>
		/// <param name="name">Portfolio name.</param>
		/// <param name="changePortfolio">Portfolio handler.</param>
		/// <returns>Portfolio.</returns>
		private Portfolio GetPortfolio(string name, Func<Portfolio, bool> changePortfolio)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			var result = _entityCache.ProcessPortfolio(name, changePortfolio);
			ProcessPortfolio(result);
			return result.Item1;
		}

		private void ProcessPortfolio(Tuple<Portfolio, bool, bool> info)
		{
			var portfolio = info.Item1;
			var isNew = info.Item2;
			var isChanged = info.Item3;

			if (isNew)
			{
				this.AddInfoLog(LocalizedStrings.Str1105Params, portfolio.Name);
				RaiseNewPortfolio(portfolio);

				if (AutoPortfoliosSubscribe)
				{
					var adapter = Adapter.AdapterProvider.GetAdapter(portfolio);

					if (adapter?.IsSupportSubscriptionByPortfolio == true && Adapter.InnerAdapters[adapter] != -1)
					{
						SendInMessage(new PortfolioMessage
						{
							PortfolioName = portfolio.Name,
							TransactionId = TransactionIdGenerator.GetNextId(),
							IsSubscribe = true,
							Adapter = adapter,
						});
					}
				}
			}
			else if (isChanged)
				RaisePortfolioChanged(portfolio);
		}

		private void ProcessPortfolioMessage(PortfolioMessage message)
		{
			GetPortfolio(message.PortfolioName, p =>
			{
				message.ToPortfolio(p, _entityCache.ExchangeInfoProvider);
				return true;
			});
		}

		private void ProcessPortfolioChangeMessage(PortfolioChangeMessage message)
		{
			GetPortfolio(message.PortfolioName, portfolio =>
			{
				portfolio.ApplyChanges(message, _entityCache.ExchangeInfoProvider);
				return true;
			});
		}

		//private void ProcessPositionMessage(PositionMessage message)
		//{
		//	var security = LookupSecurity(message.SecurityId);
		//	var portfolio = GetPortfolio(message.PortfolioName);
		//	var position = GetPosition(portfolio, security, message.ClientCode, message.DepoName, message.LimitType, message.Description);

		//	message.CopyExtensionInfo(position);
		//}

		private void ProcessPositionChangeMessage(PositionChangeMessage message)
		{
			var security = LookupSecurity(message.SecurityId);
			var portfolio = GetPortfolio(message.PortfolioName);

			var valueInLots = message.Changes.TryGetValue(PositionChangeTypes.CurrentValueInLots);
			if (valueInLots != null)
			{
				if (!message.Changes.ContainsKey(PositionChangeTypes.CurrentValue))
				{
					var currValue = (decimal)valueInLots / (security.VolumeStep ?? 1);
					message.Add(PositionChangeTypes.CurrentValue, currValue);
				}

				message.Changes.Remove(PositionChangeTypes.CurrentValueInLots);
			}

			var position = GetPosition(portfolio, security, message.ClientCode, message.DepoName, message.LimitType, message.Description);
			position.ApplyChanges(message);

			RaisePositionChanged(position);
		}

		private void ProcessNewsMessage(NewsMessage message)
		{
			var security = message.SecurityId == null ? null : LookupSecurity(message.SecurityId.Value);

			var news = _entityCache.ProcessNewsMessage(security, message);

			if (news.Item2)
				RaiseNewNews(news.Item1);
			else
				RaiseNewsChanged(news.Item1);
		}

		private void ProcessQuotesMessage(QuoteChangeMessage message)
		{
			var security = LookupSecurity(message.SecurityId);

			ProcessQuotesMessage(security, message);
		}

		private void ProcessQuotesMessage(Security security, QuoteChangeMessage message)
		{
			if (MarketDepthChanged != null || MarketDepthsChanged != null)
			{
				var marketDepth = GetMarketDepth(security, message.IsFiltered);

				message.ToMarketDepth(marketDepth, GetSecurity);

				if (!message.IsFiltered)
					RaiseMarketDepthChanged(marketDepth);
			}
			else
			{
				lock (_marketDepths.SyncRoot)
				{
					var info = _marketDepths.SafeAdd(Tuple.Create(security, message.IsFiltered), key => new MarketDepthInfo(EntityFactory.CreateMarketDepth(security)));

					info.First.LocalTime = message.LocalTime;
					info.First.LastChangeTime = message.ServerTime;

					info.Second = message.Bids;
					info.Third = message.Asks;
				}
			}

			if (message.IsFiltered)
				return;

			var bestBid = message.GetBestBid();
			var bestAsk = message.GetBestAsk();
			var fromLevel1 = message.IsByLevel1;

			if (!fromLevel1 && (bestBid != null || bestAsk != null))
			{
				var values = GetSecurityValues(security);
				var changes = new List<KeyValuePair<Level1Fields, object>>(4);

				lock (values.SyncRoot)
				{
					if (bestBid != null)
					{
						values[(int)Level1Fields.BestBidPrice] = bestBid.Price;
						changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.BestBidPrice, bestBid.Price));

						if (bestBid.Volume != 0)
						{
							values[(int)Level1Fields.BestBidVolume] = bestBid.Volume;
							changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.BestBidVolume, bestBid.Volume));
						}
					}

					if (bestAsk != null)
					{
						values[(int)Level1Fields.BestAskPrice] = bestAsk.Price;
						changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.BestAskPrice, bestAsk.Price));

						if (bestAsk.Volume != 0)
						{
							values[(int)Level1Fields.BestAskVolume] = bestAsk.Volume;
							changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.BestAskVolume, bestAsk.Volume));
						}
					}
				}

				RaiseValuesChanged(security, changes, message.ServerTime, message.LocalTime);
			}

			if (UpdateSecurityLastQuotes)
			{
				var updated = false;

				if (!fromLevel1 || bestBid != null)
				{
					updated = true;
					security.BestBid = bestBid == null ? null : new Quote(security, bestBid.Price, bestBid.Volume, Sides.Buy);
				}

				if (!fromLevel1 || bestAsk != null)
				{
					updated = true;
					security.BestAsk = bestAsk == null ? null : new Quote(security, bestAsk.Price, bestAsk.Volume, Sides.Sell);
				}

				if (updated)
				{
					security.LocalTime = message.LocalTime;
					security.LastChangeTime = message.ServerTime;

					RaiseSecurityChanged(security);

					// стаканы по ALL обновляют BestXXX по конкретным инструментам
					if (security.Board.Code == AssociatedBoardCode)
					{
						var changedSecurities = new Dictionary<Security, RefPair<bool, bool>>();

						foreach (var bid in message.Bids)
						{
							if (bid.BoardCode.IsEmpty())
								continue;

							var innerSecurity = GetSecurity(new SecurityId
							{
								SecurityCode = security.Code,
								BoardCode = bid.BoardCode
							});

							var info = changedSecurities.SafeAdd(innerSecurity);

							if (info.First)
								continue;

							info.First = true;

							innerSecurity.BestBid = new Quote(innerSecurity, bid.Price, bid.Volume, Sides.Buy);
							innerSecurity.LocalTime = message.LocalTime;
							innerSecurity.LastChangeTime = message.ServerTime;
						}

						foreach (var ask in message.Asks)
						{
							if (ask.BoardCode.IsEmpty())
								continue;

							var innerSecurity = GetSecurity(new SecurityId
							{
								SecurityCode = security.Code,
								BoardCode = ask.BoardCode
							});

							var info = changedSecurities.SafeAdd(innerSecurity);

							if (info.Second)
								continue;

							info.Second = true;

							innerSecurity.BestAsk = new Quote(innerSecurity, ask.Price, ask.Volume, Sides.Sell);
							innerSecurity.LocalTime = message.LocalTime;
							innerSecurity.LastChangeTime = message.ServerTime;
						}
						
						RaiseSecuritiesChanged(changedSecurities.Keys.ToArray());
					}
				}
			}
		}

		private void ProcessOrderLogMessage(Security security, ExecutionMessage message)
		{
			var trade = (message.TradeId != null || !message.TradeStringId.IsEmpty())
				? EntityFactory.CreateTrade(security, message.TradeId, message.TradeStringId)
				: null;

			var logItem = message.ToOrderLog(EntityFactory.CreateOrderLogItem(new Order { Security = security }, trade));
			//logItem.LocalTime = message.LocalTime;

			RaiseNewOrderLogItem(logItem);
		}

		private void ProcessTradeMessage(Security security, ExecutionMessage message)
		{
			var tuple = _entityCache.ProcessTradeMessage(security, message);

			var values = GetSecurityValues(security);

			var changes = new List<KeyValuePair<Level1Fields, object>>(4)
			{
				new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradeTime, message.ServerTime),
				new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradePrice, message.TradePrice)
			};

			lock (values.SyncRoot)
			{
				values[(int)Level1Fields.LastTradeTime] = message.ServerTime;

				if (message.TradePrice != null)
					values[(int)Level1Fields.LastTradePrice] = message.TradePrice.Value;

				if (message.IsSystem != null)
					values[(int)Level1Fields.IsSystem] = message.IsSystem.Value;

				if (message.TradeId != null)
				{
					values[(int)Level1Fields.LastTradeId] = message.TradeId.Value;
					changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradeId, message.TradeId.Value));
				}

				if (message.TradeVolume != null)
				{
					values[(int)Level1Fields.Volume] = message.TradeVolume.Value;
					changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradeVolume, message.TradeVolume.Value));
				}
			}

			if (tuple.Item2)
				RaiseNewTrade(tuple.Item1);

			RaiseValuesChanged(security, changes, message.ServerTime, message.LocalTime);

			if (!UpdateSecurityLastQuotes)
				return;

			security.LastTrade = tuple.Item1;
			security.LastChangeTime = tuple.Item1.Time;

			RaiseSecurityChanged(security);
		}

		private void ProcessMyTrades<T>(Order order, T id, Dictionary<T, List<ExecutionMessage>> nonOrderedMyTrades)
		{
			var value = nonOrderedMyTrades.TryGetValue(id);

			if (value == null)
				return;

			var retVal = new List<ExecutionMessage>();

			foreach (var message in value.ToArray())
			{
				// проверяем совпадение по дате, исключая ситуация сопоставления сделки с заявкой, имеющая неуникальный идентификатор
				if (message.ServerTime.Date != order.Time.Date)
					continue;

				retVal.Add(message);
				value.Remove(message);
			}

			if (value.IsEmpty())
				nonOrderedMyTrades.Remove(id);

			var trades = retVal
				.Select(t => _entityCache.ProcessMyTradeMessage(order, order.Security, t, _entityCache.GetTransactionId(t.OriginalTransactionId)))
				.Where(t => t != null && t.Item2)
				.Select(t => t.Item1);

			foreach (var trade in trades)
			{
				RaiseNewMyTrade(trade);
			}
		}

		private void ProcessOrderMessage(Order o, Security security, ExecutionMessage message, long transactionId, bool isStatusRequest)
		{
			if (message.OrderState != OrderStates.Failed && message.Error == null)
			{
				var changes = _entityCache.ProcessOrderMessage(o, security, message, transactionId, out var pfInfo);

				if (changes == null)
				{
					this.AddWarningLog(LocalizedStrings.Str1156Params, message.OrderId.To<string>() ?? message.OrderStringId);

					if (transactionId == 0 && !isStatusRequest)
					{
						if (message.OrderId != null)
						{
							this.AddInfoLog("{0} info suspended.", message.OrderId.Value);
							_nonAssociatedOrderIds.SafeAdd(message.OrderId.Value).Add((ExecutionMessage)message.Clone());
						}
						else if (!message.OrderStringId.IsEmpty())
						{
							this.AddInfoLog("{0} info suspended.", message.OrderStringId);
							_nonAssociatedStringOrderIds.SafeAdd(message.OrderStringId).Add((ExecutionMessage)message.Clone());
						}
					}
					
					return;
				}

				if (pfInfo != null)
					ProcessPortfolio(pfInfo);

				foreach (var change in changes)
				{
					var order = change.Order;

					//if (message.OrderType == OrderTypes.Conditional && (message.DerivedOrderId != null || !message.DerivedOrderStringId.IsEmpty()))
					//{
					//	var derivedOrder = _entityCache.GetOrder(order.Security, 0L, message.DerivedOrderId ?? 0, message.DerivedOrderStringId);

					//	if (derivedOrder == null)
					//		_orderStopOrderAssociations.Add(Tuple.Create(message.DerivedOrderId, message.DerivedOrderStringId), new RefPair<Order, Action<Order, Order>>(order, (s, o1) => s.DerivedOrder = o1));
					//	else
					//		order.DerivedOrder = derivedOrder;
					//}

					if (change.IsNew)
					{
						this.AddOrderInfoLog(order, "New order");

						if (order.Type == OrderTypes.Conditional)
							RaiseNewStopOrder(order);
						else
							RaiseNewOrder(order);

						if (isStatusRequest && order.State == OrderStates.Pending)
						{
							// TODO temp disabled (need more tests)
							//RegisterOrder(order, false);
						}
					}
					else if (change.IsChanged)
					{
						this.AddOrderInfoLog(order, "Order changed");

						if (order.Type == OrderTypes.Conditional)
							RaiseStopOrderChanged(order);
						else
							RaiseOrderChanged(order);
					}

					if (order.Id != null)
						ProcessMyTrades(order, order.Id.Value, _nonAssociatedByIdMyTrades);

					ProcessMyTrades(order, order.TransactionId, _nonAssociatedByTransactionIdMyTrades);

					if (!order.StringId.IsEmpty())
						ProcessMyTrades(order, order.StringId, _nonAssociatedByStringIdMyTrades);

					//ProcessConditionOrders(order);

					List<ExecutionMessage> suspended = null;

					if (order.Id != null)
						suspended = _nonAssociatedOrderIds.TryGetAndRemove(order.Id.Value);
					else if (!order.StringId.IsEmpty())
						suspended = _nonAssociatedStringOrderIds.TryGetAndRemove(order.StringId);

					if (suspended != null)
					{
						this.AddInfoLog("{0} resumed.", order.Id);

						foreach (var s in suspended)
						{
							ProcessOrderMessage(order, order.Security, s, transactionId, isStatusRequest);
						}
					}
				}
			}
			else
			{
				if (message.OriginalTransactionId == 0)
				{
					this.AddErrorLog("Unknown error response for order {0}: {1}.", o, message.Error);
					return;
				}

				foreach (var tuple in _entityCache.ProcessOrderFailMessage(o, security, message))
				{
					var fail = tuple.Item1;

					//TryProcessFilteredMarketDepth(fail.Order.Security, message);

					//var isRegisterFail = (fail.Order.Id == null && fail.Order.StringId.IsEmpty()) || fail.Order.Status == OrderStatus.RejectedBySystem;
					var isCancelTransaction = tuple.Item2;

					this.AddErrorLog(() => (isCancelTransaction ? "OrderCancelFailed" : "OrderRegisterFailed")
						+ Environment.NewLine + fail.Order + Environment.NewLine + fail.Error);

					var isStop = fail.Order.Type == OrderTypes.Conditional;

					if (!isCancelTransaction)
					{
						_entityCache.AddRegisterFail(fail);

						if (isStop)
							RaiseStopOrdersRegisterFailed(fail);
						else
							RaiseOrderRegisterFailed(fail);
					}
					else
					{
						_entityCache.AddCancelFail(fail);

						if (isStop)
							RaiseStopOrdersCancelFailed(fail);
						else
							RaiseOrderCancelFailed(fail);
					}
				}
			}
		}

		//private void ProcessConditionOrders(Order order)
		//{
		//	var changedStopOrders = new List<Order>();

		//	var key = Tuple.Create(order.Id, order.StringId);

		//	var collection = _orderStopOrderAssociations.TryGetValue(key);

		//	if (collection == null)
		//		return;

		//	foreach (var pair in collection)
		//	{
		//		pair.Second(pair.First, order);
		//		changedStopOrders.TryAdd(pair.First);
		//	}

		//	_orderStopOrderAssociations.Remove(key);

		//	if (changedStopOrders.Count > 0)
		//		RaiseStopOrdersChanged(changedStopOrders);
		//}

		private void ProcessMyTradeMessage(Order order, Security security, ExecutionMessage message, long transactionId)
		{
			var tuple = _entityCache.ProcessMyTradeMessage(order, security, message, transactionId);

			if (tuple == null)
			{
				List<ExecutionMessage> nonOrderedMyTrades;

				if (message.OrderId != null)
					nonOrderedMyTrades = _nonAssociatedByIdMyTrades.SafeAdd(message.OrderId.Value);
				else if (message.OriginalTransactionId != 0)
					nonOrderedMyTrades = _nonAssociatedByTransactionIdMyTrades.SafeAdd(message.OriginalTransactionId);
				else
					nonOrderedMyTrades = _nonAssociatedByStringIdMyTrades.SafeAdd(message.OrderStringId);

				this.AddInfoLog("My trade delayed: {0}", message);

				nonOrderedMyTrades.Add((ExecutionMessage)message.Clone());

				return;
			}

			if (!tuple.Item2)
			{
				this.AddWarningLog("Duplicate own trade message: {0}", message);
				return;
			}

			RaiseNewMyTrade(tuple.Item1);
		}

		private void ProcessTransactionMessage(Order order, Security security, ExecutionMessage message, long transactionId, bool isStatusRequest)
		{
			var processed = false;

			if (message.HasOrderInfo())
			{
				processed = true;
				ProcessOrderMessage(order, security, message, transactionId, isStatusRequest);
			}

			if (message.HasTradeInfo())
			{
				processed = true;
				ProcessMyTradeMessage(order, security, message, transactionId);
			}

			if (!processed)
				throw new ArgumentOutOfRangeException(nameof(message), message.ExecutionType, LocalizedStrings.Str1695Params.Put(message));
		}

		private void ProcessExecutionMessage(ExecutionMessage message)
		{
			if (message.ExecutionType == null)
				throw new ArgumentException(LocalizedStrings.Str688Params.Put(message));

			switch (message.ExecutionType)
			{
				case ExecutionTypes.Transaction:
				{
					var originId = message.OriginalTransactionId;

					if (_entityCache.IsMassCancelation(originId))
					{
						if (message.Error == null)
							RaiseMassOrderCanceled(originId);
						else
							RaiseMassOrderCancelFailed(originId, message.Error);

						break;
					}

					var isStatusRequest = _entityCache.IsOrderStatusRequest(originId);

					if (message.Error != null && isStatusRequest)
					{
						// TransId != 0 means contains failed order info (not just status response)
						if (message.TransactionId == 0)
						{
							RaiseOrderStatusFailed(originId, message.Error);
							break;
						}
					}

					var order = _entityCache.GetOrder(message, out var transactionId);

					if (order == null)
					{
						var security = LookupSecurity(message.SecurityId);

						if (transactionId == 0 && isStatusRequest)
							transactionId = TransactionIdGenerator.GetNextId();

						ProcessTransactionMessage(null, security, message, transactionId, isStatusRequest);
					}
					else
					{
						ProcessTransactionMessage(order, order.Security, message, transactionId, isStatusRequest);
					}

					break;
				}

				case ExecutionTypes.Tick:
				case ExecutionTypes.OrderLog:
				//case null:
				{
					var security = LookupSecurity(message.SecurityId);

					switch (message.ExecutionType)
					{
						case ExecutionTypes.Tick:
							ProcessTradeMessage(security, message);
							break;
						case ExecutionTypes.OrderLog:
							ProcessOrderLogMessage(security, message);
							break;
					}

					break;
				}
				
				default:
					throw new ArgumentOutOfRangeException(nameof(message), message.ExecutionType, LocalizedStrings.Str1695Params.Put(message));
			}
		}

		private void ProcessCandleMessage(CandleMessage message)
		{
			var series = _entityCache.UpdateCandle(message, out var candle);

			if (series == null)
				return;

			RaiseCandleSeriesProcessing(series, candle);
		}

		private CandleSeries ProcessCandleSeriesStopped(long originalTransactionId)
		{
			var series = _entityCache.RemoveCandleSeries(originalTransactionId);

			if (series != null)
				RaiseCandleSeriesStopped(series);

			return series;
		}

		private void ProcessMarketDataFinishedMessage(MarketDataFinishedMessage message)
		{
			var series = ProcessCandleSeriesStopped(message.OriginalTransactionId);
			var security = series?.Security ?? _subscriptionManager.TryGetSecurity(message.OriginalTransactionId);
			RaiseMarketDataSubscriptionFinished(security, message);
		}
	}
}