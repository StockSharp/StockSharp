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

	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.Latency;
	using StockSharp.Algo.PnL;
	using StockSharp.Algo.Risk;
	using StockSharp.Algo.Slippage;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class Connector
	{
		private class TimeAdapter : MessageAdapterWrapper
		{
			private readonly Connector _parent;
			private readonly SyncObject _marketTimerSync = new SyncObject();
			private Timer _marketTimer;
			private readonly TimeMessage _marketTimeMessage = new TimeMessage();
			private bool _isMarketTimeHandled;

			public TimeAdapter(Connector parent, IMessageAdapter innerAdapter)
				: base(innerAdapter)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				_parent = parent;
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

			public override void SendInMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Reset:
					{
						CloseTimer();
						break;
					}

					case MessageTypes.Connect:
					{
						if (_marketTimer != null)
							throw new InvalidOperationException(LocalizedStrings.Str1619);

						lock (_marketTimerSync)
						{
							_isMarketTimeHandled = true;

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

										_marketTimeMessage.LocalTime = TimeHelper.Now;
										RaiseNewOutMessage(_marketTimeMessage);
									}
									catch (Exception ex)
									{
										ex.LogError();
									}
								})
								.Interval(_parent.MarketTimeChangedInterval);
						}
						break;
					}

					case MessageTypes.Disconnect:
					{
						if (_marketTimer == null)
							throw new InvalidOperationException(LocalizedStrings.Str1856);

						CloseTimer();
						break;
					}
				}

				base.SendInMessage(message);
			}

			protected override void OnInnerAdapterNewOutMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					{
						var connectMsg = (ConnectMessage)message;

						if (connectMsg.Error != null)
							CloseTimer();

						break;
					}

					case MessageTypes.Disconnect:
					{
						CloseTimer();
						break;
					}
				}

				base.OnInnerAdapterNewOutMessage(message);
			}

			public override IMessageChannel Clone()
			{
				return new TimeAdapter(_parent, (IMessageAdapter)InnerAdapter.Clone());
			}

			public void HandleTimeMessage(Message message)
			{
				if (message != _marketTimeMessage)
					return;

				lock (_marketTimerSync)
					_isMarketTimeHandled = true;
			}
		}

		private readonly Dictionary<Security, IOrderLogMarketDepthBuilder> _olBuilders = new Dictionary<Security, IOrderLogMarketDepthBuilder>();
		private readonly CachedSynchronizedDictionary<IMessageAdapter, ConnectionStates> _adapterStates = new CachedSynchronizedDictionary<IMessageAdapter, ConnectionStates>();
		
		private readonly ResetMessage _disposeMessage = new ResetMessage();

		private string AssociatedBoardCode => Adapter.AssociatedBoardCode;
		
		private void AdapterOnNewOutMessage(Message message)
		{
			if (message.IsBack)
			{
				//message.IsBack = false;
				SendInMessage(message);
			}
			else
				SendOutMessage(message);
		}

		/// <summary>
		/// To call the <see cref="Connected"/> event when the first adapter connects to <see cref="Adapter"/>.
		/// </summary>
		protected virtual bool RaiseConnectedOnFirstAdapter => true;

		private IMessageChannel _inMessageChannel;

		/// <summary>
		/// Input message channel.
		/// </summary>
		public IMessageChannel InMessageChannel
		{
			get { return _inMessageChannel; }
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
			get { return _outMessageChannel; }
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
		private TimeAdapter _timeAdapter;

		/// <summary>
		/// Inner message adapter.
		/// </summary>
		public IMessageAdapter InnerAdapter
		{
			get { return _inAdapter; }
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
				_timeAdapter = null;
				StorageAdapter = null;

				if (_inAdapter == null)
					return;

				var adapter = _inAdapter as IMessageAdapterWrapper;

				while (adapter != null)
				{
					adapter.DoIf<IMessageAdapter, TimeAdapter>(a => _timeAdapter = a);
					adapter.DoIf<IMessageAdapter, StorageMessageAdapter>(a => StorageAdapter = a);

					adapter.InnerAdapter.DoIf<IMessageAdapter, BasketMessageAdapter>(a => _adapter = a);

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
			get { return _adapter; }
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
				_timeAdapter = null;

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

					if (TimeChange)
						_inAdapter = _timeAdapter = new TimeAdapter(this, _inAdapter) { OwnInnerAdaper = true };

					if (LatencyManager != null)
						_inAdapter = new LatencyMessageAdapter(_inAdapter) { LatencyManager = LatencyManager, OwnInnerAdaper = true };

					if (SlippageManager != null)
						_inAdapter = new SlippageMessageAdapter(_inAdapter) { SlippageManager = SlippageManager, OwnInnerAdaper = true };

					if (PnLManager != null)
						_inAdapter = new PnLMessageAdapter(_inAdapter) { PnLManager = PnLManager, OwnInnerAdaper = true };

					if (CommissionManager != null)
						_inAdapter = new CommissionMessageAdapter(_inAdapter) { CommissionManager = CommissionManager, OwnInnerAdaper = true };

					if (RiskManager != null)
						_inAdapter = new RiskMessageAdapter(_inAdapter) { RiskManager = RiskManager, OwnInnerAdaper = true };

					if (_supportOffline)
						_inAdapter = new OfflineMessageAdapter(_inAdapter) { OwnInnerAdaper = true };

					if (_entityRegistry != null && _storageRegistry != null)
						_inAdapter = StorageAdapter = new StorageMessageAdapter(_inAdapter, _entityRegistry, _storageRegistry) { OwnInnerAdaper = true };

					if (_supportCandleBuilder)
						_inAdapter = new CandleBuilderMessageAdapter(_inAdapter, _entityCache.ExchangeInfoProvider) { OwnInnerAdaper = true };

					if (_supportLevel1DepthBuilder)
						_inAdapter = new Level1DepthBuilderAdapter(_inAdapter) { OwnInnerAdaper = true };

					if (_supportAssociatedSecurity)
						_inAdapter = new AssociatedSecurityAdapter(_inAdapter) { OwnInnerAdaper = true };

					if (_supportFilteredMarketDepth)
						_inAdapter = new FilteredMarketDepthAdapter(_inAdapter) { OwnInnerAdaper = true };

					_inAdapter.NewOutMessage += AdapterOnNewOutMessage;
				}
			}
		}

		private bool _supportOffline;

		/// <summary>
		/// Use <see cref="OfflineMessageAdapter"/>.
		/// </summary>
		public bool SupportOffline
		{
			get { return _supportOffline; }
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

		private bool _supportFilteredMarketDepth;

		/// <summary>
		/// Use <see cref="FilteredMarketDepthAdapter"/>.
		/// </summary>
		public bool SupportFilteredMarketDepth
		{
			get { return _supportFilteredMarketDepth; }
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
			get { return _supportAssociatedSecurity; }
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
			get { return _supportLevel1DepthBuilder; }
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

		private bool _supportCandleBuilder;

		/// <summary>
		/// Use <see cref="CandleBuilderMessageAdapter"/>.
		/// </summary>
		public bool SupportCandleBuilder
		{
			get { return _supportCandleBuilder; }
			set
			{
				if (_supportCandleBuilder == value)
					return;

				if (value)
					EnableAdapter(a => new CandleBuilderMessageAdapter(a, _entityCache.ExchangeInfoProvider) { OwnInnerAdaper = true }, typeof(StorageMessageAdapter));
				else
					DisableAdapter<CandleBuilderMessageAdapter>();

				_supportCandleBuilder = value;
			}
		}

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
					var nextWrapper = tuple.Item3 as IMessageAdapterWrapper;

					if (nextWrapper != null)
						nextWrapper.InnerAdapter = create(adapter);
					else
						AddAdapter(create);
				}
				else
				{
					var prevWrapper = tuple.Item1;
					var nextWrapper = adapter as IMessageAdapterWrapper;

					if (prevWrapper == null)
						throw new InvalidOperationException("Adapter wrapper can not be added to the beginning of the chain.");

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
			if (message.LocalTime.IsDefault())
				message.LocalTime = CurrentTime;

			if (!OutMessageChannel.IsOpened)
				OutMessageChannel.Open();

			OutMessageChannel.SendInMessage(message);
		}

		/// <summary>
		/// Send error message.
		/// </summary>
		/// <param name="error">Error detais.</param>
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
				this.AddDebugLog("BP:{0}", message);

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

					case MessageTypes.Position:
						ProcessPositionMessage((PositionMessage)message);
						break;

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

					case MessageTypes.Session:
						ProcessSessionMessage((SessionMessage)message);
						break;

					case ExtendedMessageTypes.RemoveSecurity:
						ProcessSecurityRemoveMessage((SecurityRemoveMessage)message);
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

			MarketDataMessage originalMsg;

			var security = _subscriptionManager.ProcessResponse(mdMsg.OriginalTransactionId, out originalMsg);

			if (security == null)
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
					RaiseMarketDataUnSubscriptionSucceeded(security, originalMsg);
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

		private Security LookupSecurity(SecurityId securityId)
		{
			var securityCode = securityId.SecurityCode;
			var boardCode = securityId.BoardCode;

			//if (boardCode.IsEmpty())
			//	boardCode = AssociatedBoardCode;

			var stockSharpId = CreateSecurityId(securityCode, boardCode);
			var security = _entityCache.GetSecurityById(stockSharpId);

			if (security == null)
			{
				var secProvider = EntityFactory as ISecurityProvider;

				if (secProvider != null)
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
							_adapterStates[adapter] = ConnectionStates.Connected;

							if (ConnectionState == ConnectionStates.Connecting)
							{
								if (RaiseConnectedOnFirstAdapter)
								{
									// raise Connected event only one time for the first adapter
									RaiseConnected();
								}
								else
								{
									var isAllConnected = _adapterStates.CachedValues.All(v => v == ConnectionStates.Connected);

									// raise Connected event only one time when the last adapter connection successfully
									if (isAllConnected)
										RaiseConnected();
								}
							}

							RaiseConnectedEx(adapter);

							if (adapter.PortfolioLookupRequired)
								SendInMessage(new PortfolioLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });

							if (adapter.OrderStatusRequired)
							{
								var transactionId = TransactionIdGenerator.GetNextId();
								_entityCache.AddOrderStatusTransactionId(transactionId);
								SendInMessage(new OrderStatusMessage { TransactionId = transactionId });
							}

							if (adapter.SecurityLookupRequired)
								SendInMessage(new SecurityLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });

							if (message is RestoredConnectMessage)
								RaiseRestored();
						}
						else
						{
							_adapterStates[adapter] = ConnectionStates.Failed;

							// raise ConnectionError only one time
							if (ConnectionState == ConnectionStates.Connecting)
							{
								RaiseConnectionError(message.Error);

								if (message.Error is TimeoutException)
									RaiseTimeOut();
							}
							else
								RaiseError(message.Error);

							RaiseConnectionErrorEx(adapter, message.Error);
						}
					}
					else
					{
						_adapterStates[adapter] = ConnectionStates.Failed;

						// raise ConnectionError only one time
						if (ConnectionState == ConnectionStates.Connecting)
							RaiseConnectionError(new InvalidOperationException(LocalizedStrings.Str683, message.Error));
						else
							RaiseError(message.Error);

						RaiseConnectionErrorEx(adapter, message.Error);
					}

					return;
				}
				case ConnectionStates.Disconnecting:
				{
					if (isConnect)
					{
						_adapterStates[adapter] = ConnectionStates.Failed;

						var error = new InvalidOperationException(LocalizedStrings.Str684, message.Error);

						// raise ConnectionError only one time
						if (ConnectionState == ConnectionStates.Disconnecting)
							RaiseConnectionError(error);
						else
							RaiseError(error);

						RaiseConnectionErrorEx(adapter, message.Error);
					}
					else
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
						{
							_adapterStates[adapter] = ConnectionStates.Failed;

							// raise ConnectionError only one time
							if (ConnectionState == ConnectionStates.Disconnecting)
								RaiseConnectionError(message.Error);
							else
								RaiseError(message.Error);

							RaiseConnectionErrorEx(adapter, message.Error);
						}
					}

					return;
				}
				case ConnectionStates.Connected:
				{
					if (isConnect && message.Error != null)
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
				case ConnectionStates.Failed:
				{
					//StopMarketTimer();
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

		private void ProcessSessionMessage(SessionMessage message)
		{
			var board = _entityCache.ExchangeInfoProvider.GetOrCreateBoard(message.BoardCode);
			_sessionStates[board] = message.State;
			SessionStateChanged?.Invoke(board, message.State);
		}

		private void ProcessBoardMessage(BoardMessage message)
		{
			_entityCache.ExchangeInfoProvider.GetOrCreateBoard(message.Code, code =>
			{
				var exchange = message.ToExchange(EntityFactory.CreateExchange(message.ExchangeCode));
				return message.ToBoard(EntityFactory.CreateBoard(code, exchange));
			});
		}

		private void ProcessSecurityMessage(SecurityMessage message/*, string boardCode = null*/)
		{
			var secId = CreateSecurityId(message.SecurityId.SecurityCode, message.SecurityId.BoardCode);

			var security = GetSecurity(secId, s =>
			{
				s.ApplyChanges(message, _entityCache.ExchangeInfoProvider);
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

			if (result.Length == 0)
			{
				var criteria = _securityLookups.TryGetValue(message.OriginalTransactionId);

				if (criteria != null)
				{
					_securityLookups.Remove(message.OriginalTransactionId);
					result = this.FilterSecurities(criteria, _entityCache.ExchangeInfoProvider).ToArray();
				}
			}

			RaiseLookupSecuritiesResult(message.Error, result);

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

		private void ProcessPortfolioLookupResultMessage(PortfolioLookupResultMessage message)
		{
			if (message.Error != null)
				RaiseError(message.Error);

			var criteria = _portfolioLookups.TryGetValue(message.OriginalTransactionId);

			if (criteria == null)
				return;

			RaiseLookupPortfoliosResult(message.Error, Portfolios.Where(pf => pf.Name.CompareIgnoreCase(criteria.PortfolioName)));
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

			lock (values.SyncRoot)
			{
				foreach (var change in message.Changes)
					values[(int)change.Key] = change.Value;	
			}

			RaiseValuesChanged(security, message.Changes, message.ServerTime, message.LocalTime);
		}

		/// <summary>
		/// To get the portfolio by the name. If the portfolio is not registered, it is created via <see cref="IEntityFactory.CreatePortfolio"/>.
		/// </summary>
		/// <param name="name">Portfolio name.</param>
		/// <param name="changePortfolio">Portfolio handler.</param>
		/// <returns>Portfolio.</returns>
		private Portfolio GetPortfolio(string name, Func<Portfolio, bool> changePortfolio = null)
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

		private void ProcessPositionMessage(PositionMessage message)
		{
			var security = LookupSecurity(message.SecurityId);
			var portfolio = GetPortfolio(message.PortfolioName);
			var position = GetPosition(portfolio, security, message.ClientCode, message.DepoName, message.LimitType, message.Description);

			message.CopyExtensionInfo(position);
		}

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

			if (message.IsSystem == false)
				return;

			if (CreateDepthFromOrdersLog)
			{
				try
				{
					var builder = _olBuilders.SafeAdd(security, key => MarketDataAdapter.CreateOrderLogMarketDepthBuilder(message.SecurityId));

					if (builder == null)
						throw new InvalidOperationException();

					var updated = builder.Update(message);

					if (updated)
					{
						RaiseNewMessage(builder.Depth.Clone());
						ProcessQuotesMessage(security, builder.Depth);
					}
				}
				catch (Exception ex)
				{
					// если ОЛ поврежден, то не нарушаем весь цикл обработки сообщения
					// а только выводим сообщение в лог
					RaiseError(ex);
				}
			}

			if (trade != null && CreateTradesFromOrdersLog)
			{
				var tuple = _entityCache.GetTrade(security, message.TradeId, message.TradeStringId, (id, stringId) =>
				{
					var t = trade.Clone();
					t.OrderDirection = message.Side.Invert();
					return t;
				});

				if (tuple.Item2)
					RaiseNewTrade(tuple.Item1);
			}
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

		private void ProcessOrderMessage(Order o, Security security, ExecutionMessage message, long transactionId)
		{
			if (message.OrderState != OrderStates.Failed)
			{
				Tuple<Portfolio, bool, bool> pfInfo;
				var tuples = _entityCache.ProcessOrderMessage(o, security, message, transactionId, out pfInfo);

				if (tuples == null)
				{
					this.AddWarningLog(LocalizedStrings.Str1156Params, message.OrderId.To<string>() ?? message.OrderStringId);
					return;
				}

				if (pfInfo != null)
					ProcessPortfolio(pfInfo);

				foreach (var tuple in tuples)
				{
					var order = tuple.Item1;
					var isNew = tuple.Item2;
					var isChanged = tuple.Item3;

					if (message.OrderType == OrderTypes.Conditional && (message.DerivedOrderId != null || !message.DerivedOrderStringId.IsEmpty()))
					{
						var derivedOrder = _entityCache.GetOrder(order.Security, 0L, message.DerivedOrderId ?? 0, message.DerivedOrderStringId);

						if (derivedOrder == null)
							_orderStopOrderAssociations.Add(Tuple.Create(message.DerivedOrderId, message.DerivedOrderStringId), new RefPair<Order, Action<Order, Order>>(order, (s, o1) => s.DerivedOrder = o1));
						else
							order.DerivedOrder = derivedOrder;
					}

					if (isNew)
					{
						this.AddOrderInfoLog(order, "New order");

						if (order.Type == OrderTypes.Conditional)
							RaiseNewStopOrder(order);
						else
							RaiseNewOrder(order);
					}
					else if (isChanged)
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

					ProcessConditionOrders(order);
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

		private void ProcessConditionOrders(Order order)
		{
			var changedStopOrders = new List<Order>();

			var key = Tuple.Create(order.Id, order.StringId);

			var collection = _orderStopOrderAssociations.TryGetValue(key);

			if (collection == null)
				return;

			foreach (var pair in collection)
			{
				pair.Second(pair.First, order);
				changedStopOrders.TryAdd(pair.First);
			}

			_orderStopOrderAssociations.Remove(key);

			if (changedStopOrders.Count > 0)
				RaiseStopOrdersChanged(changedStopOrders);
		}

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
				return;

			RaiseNewMyTrade(tuple.Item1);
		}

		private void ProcessTransactionMessage(Order order, Security security, ExecutionMessage message, long transactionId)
		{
			var processed = false;

			if (message.HasOrderInfo())
			{
				processed = true;
				ProcessOrderMessage(order, security, message, transactionId);
			}

			if (message.HasTradeInfo())
			{
				processed = true;
				ProcessMyTradeMessage(order, security, message, transactionId);
			}

			if (!processed)
				throw new ArgumentOutOfRangeException(LocalizedStrings.Str1695Params.Put(message.ExecutionType));
		}

		private void ProcessExecutionMessage(ExecutionMessage message)
		{
			if (message.ExecutionType == null)
				throw new ArgumentException(LocalizedStrings.Str688Params.Put(message));

			switch (message.ExecutionType)
			{
				case ExecutionTypes.Transaction:
				{
					if (_entityCache.IsMassCancelation(message.OriginalTransactionId))
					{
						if (message.Error == null)
							RaiseMassOrderCanceled(message.OriginalTransactionId);
						else
							RaiseMassOrderCancelFailed(message.OriginalTransactionId, message.Error);

						break;
					}

					if (message.Error != null && _entityCache.IsOrderStatusRequest(message.OriginalTransactionId))
					{
						RaiseOrderStatusFailed(message.OriginalTransactionId, message.Error);
						break;
					}

					long transactionId;
					var order = _entityCache.GetOrder(message, out transactionId);

					if (order == null)
					{
						var security = LookupSecurity(message.SecurityId);
						ProcessTransactionMessage(null, security, message, transactionId);
					}
					else
					{
						ProcessTransactionMessage(order, order.Security, message, transactionId);
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
					throw new ArgumentOutOfRangeException(LocalizedStrings.Str1695Params.Put(message.ExecutionType));
			}
		}
	}
}