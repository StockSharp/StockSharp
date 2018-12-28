#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: BasketMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.Latency;
	using StockSharp.Algo.PnL;
	using StockSharp.Algo.Slippage;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The interface describing the list of adapters to trading systems with which the aggregator operates.
	/// </summary>
	public interface IInnerAdapterList : ISynchronizedCollection<IMessageAdapter>, INotifyList<IMessageAdapter>
	{
		/// <summary>
		/// Internal adapters sorted by operation speed.
		/// </summary>
		IEnumerable<IMessageAdapter> SortedAdapters { get; }

		/// <summary>
		/// The indexer through which speed priorities (the smaller the value, then adapter is faster) for internal adapters are set.
		/// </summary>
		/// <param name="adapter">The internal adapter.</param>
		/// <returns>The adapter priority. If the -1 value is set the adapter is considered to be off.</returns>
		int this[IMessageAdapter adapter] { get; set; }
	}

	/// <summary>
	/// Adapter-aggregator that allows simultaneously to operate multiple adapters connected to different trading systems.
	/// </summary>
	public class BasketMessageAdapter : MessageAdapter
	{
		private sealed class InnerAdapterList : CachedSynchronizedList<IMessageAdapter>, IInnerAdapterList
		{
			private readonly Dictionary<IMessageAdapter, int> _enables = new Dictionary<IMessageAdapter, int>();

			public IEnumerable<IMessageAdapter> SortedAdapters
			{
				get { return Cache.Where(t => this[t] != -1).OrderBy(t => this[t]); }
			}

			protected override bool OnAdding(IMessageAdapter item)
			{
				_enables.Add(item, 0);
				return base.OnAdding(item);
			}

			protected override bool OnInserting(int index, IMessageAdapter item)
			{
				_enables.Add(item, 0);
				return base.OnInserting(index, item);
			}

			protected override bool OnRemoving(IMessageAdapter item)
			{
				_enables.Remove(item);
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				_enables.Clear();
				return base.OnClearing();
			}

			public int this[IMessageAdapter adapter]
			{
				get
				{
					lock (SyncRoot)
						return _enables.TryGetValue2(adapter) ?? -1;
				}
				set
				{
					if (value < -1)
						throw new ArgumentOutOfRangeException();

					lock (SyncRoot)
					{
						if (!Contains(adapter))
							Add(adapter);

						_enables[adapter] = value;
						//_portfolioTraders.Clear();
					}
				}
			}
		}

		private readonly SynchronizedDictionary<long, MarketDataMessage> _subscriptionMessages = new SynchronizedDictionary<long, MarketDataMessage>();
		private readonly SynchronizedDictionary<long, IMessageAdapter> _subscriptionsById = new SynchronizedDictionary<long, IMessageAdapter>();
		private readonly Dictionary<long, HashSet<IMessageAdapter>> _subscriptionNonSupportedAdapters = new Dictionary<long, HashSet<IMessageAdapter>>();
		private readonly SynchronizedDictionary<Helper.SubscriptionKey, IMessageAdapter> _subscriptionsByKey = new SynchronizedDictionary<Helper.SubscriptionKey, IMessageAdapter>();
		private readonly SynchronizedDictionary<IMessageAdapter, HeartbeatMessageAdapter> _hearbeatAdapters = new SynchronizedDictionary<IMessageAdapter, HeartbeatMessageAdapter>();
		private readonly SyncObject _connectedResponseLock = new SyncObject();
		private readonly Dictionary<MessageTypes, CachedSynchronizedSet<IMessageAdapter>> _messageTypeAdapters = new Dictionary<MessageTypes, CachedSynchronizedSet<IMessageAdapter>>();
		private readonly HashSet<IMessageAdapter> _pendingConnectAdapters = new HashSet<IMessageAdapter>();
		private readonly Queue<Message> _pendingMessages = new Queue<Message>();
		private readonly HashSet<HeartbeatMessageAdapter> _connectedAdapters = new HashSet<HeartbeatMessageAdapter>();
		private bool _isFirstConnect;
		private readonly InnerAdapterList _innerAdapters;
		private readonly SynchronizedDictionary<long, RefTriple<long, bool?, IMessageAdapter>> _newsSubscriptions = new SynchronizedDictionary<long, RefTriple<long, bool?, IMessageAdapter>>();

		/// <summary>
		/// Adapters with which the aggregator operates.
		/// </summary>
		public IInnerAdapterList InnerAdapters => _innerAdapters;

		private INativeIdStorage _nativeIdStorage = new InMemoryNativeIdStorage();

		/// <summary>
		/// Security native identifier storage.
		/// </summary>
		public INativeIdStorage NativeIdStorage
		{
			get => _nativeIdStorage;
			set => _nativeIdStorage = value ?? throw new ArgumentNullException(nameof(value));
		}

		private ISecurityMappingStorage _securityMappingStorage;

		/// <summary>
		/// Security identifier mappings storage.
		/// </summary>
		public ISecurityMappingStorage SecurityMappingStorage
		{
			get => _securityMappingStorage;
			set => _securityMappingStorage = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Extended info <see cref="Message.ExtensionInfo"/> storage.
		/// </summary>
		public IExtendedInfoStorage ExtendedInfoStorage { get; set; }

		/// <summary>
		/// Orders registration delay calculation manager.
		/// </summary>
		public ILatencyManager LatencyManager { get; set; }

		/// <summary>
		/// The profit-loss manager.
		/// </summary>
		public IPnLManager PnLManager { get; set; }

		/// <summary>
		/// The commission calculating manager.
		/// </summary>
		public ICommissionManager CommissionManager { get; set; }

		/// <summary>
		/// Slippage manager.
		/// </summary>
		public ISlippageManager SlippageManager { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BasketMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		/// <param name="adapterProvider">The message adapter's provider.</param>
		/// <param name="candleBuilderProvider">Candle builders provider.</param>
		public BasketMessageAdapter(IdGenerator transactionIdGenerator, IPortfolioMessageAdapterProvider adapterProvider, CandleBuilderProvider candleBuilderProvider)
			: base(transactionIdGenerator)
		{
			_innerAdapters = new InnerAdapterList();
			AdapterProvider = adapterProvider ?? throw new ArgumentNullException(nameof(adapterProvider));
			CandleBuilderProvider = candleBuilderProvider ?? throw new ArgumentNullException(nameof(adapterProvider));

			LatencyManager = new LatencyManager();
			CommissionManager = new CommissionManager();
			//PnLManager = new PnLManager();
			SlippageManager = new SlippageManager();
		}

		/// <summary>
		/// The message adapter's provider.
		/// </summary>
		public IPortfolioMessageAdapterProvider AdapterProvider { get; }

		/// <summary>
		/// Candle builders provider.
		/// </summary>
		public CandleBuilderProvider CandleBuilderProvider { get; }

		/// <inheritdoc />
		public override MessageTypes[] SupportedMessages => GetSortedAdapters().SelectMany(a => a.SupportedMessages).Distinct().ToArray();

		/// <inheritdoc />
		public override bool PortfolioLookupRequired => GetSortedAdapters().Any(a => a.PortfolioLookupRequired);

		/// <inheritdoc />
		public override bool OrderStatusRequired => GetSortedAdapters().Any(a => a.OrderStatusRequired);

		/// <inheritdoc />
		public override bool SecurityLookupRequired => GetSortedAdapters().Any(a => a.SecurityLookupRequired);

		/// <inheritdoc />
		protected override bool IsSupportNativePortfolioLookup => true;

		/// <inheritdoc />
		protected override bool IsSupportNativeSecurityLookup => true;

		/// <inheritdoc />
		public override bool IsSupportSecuritiesLookupAll => GetSortedAdapters().Any(a => a.IsSupportSecuritiesLookupAll);

		/// <inheritdoc />
		public override MessageAdapterCategories Categories => GetSortedAdapters().Select(a => a.Categories).JoinMask();

		/// <summary>
		/// Restore subscription on reconnect.
		/// </summary>
		public bool IsRestoreSubscriptionOnReconnect { get; set; }

		/// <summary>
		/// Suppress reconnecting errors.
		/// </summary>
		public bool SuppressReconnectingErrors { get; set; } = true;

		/// <summary>
		/// Use <see cref="CandleBuilderMessageAdapter"/>.
		/// </summary>
		public bool SupportCandlesCompression { get; set; } = true;

		/// <summary>
		/// Use <see cref="OrderLogMessageAdapter"/>.
		/// </summary>
		public bool SupportBuildingFromOrderLog { get; set; } = true;

		/// <inheritdoc />
		public override IEnumerable<TimeSpan> TimeFrames
		{
			get { return GetSortedAdapters().SelectMany(a => a.TimeFrames); }
		}

		/// <inheritdoc />
		public override OrderCondition CreateOrderCondition() => throw new NotSupportedException();

		/// <inheritdoc />
		public override bool IsConnectionAlive() => throw new NotSupportedException();

		private void ProcessReset(ResetMessage message)
		{
			_hearbeatAdapters.Values.ForEach(a =>
			{
				a.SendInMessage(message);
				a.Dispose();
			});

			lock (_connectedResponseLock)
			{
				_connectedAdapters.Clear();
				_messageTypeAdapters.Clear();
				_pendingConnectAdapters.Clear();
				_pendingMessages.Clear();
				_subscriptionNonSupportedAdapters.Clear();
			}

			_hearbeatAdapters.Clear();
			_subscriptionsById.Clear();
			_subscriptionsByKey.Clear();
			_subscriptionMessages.Clear();
			_newsSubscriptions.Clear();
		}

		private IMessageAdapter CreateWrappers(IMessageAdapter adapter)
		{
			if (LatencyManager != null)
			{
				adapter = new LatencyMessageAdapter(adapter) { LatencyManager = LatencyManager.Clone() };
			}

			if (adapter.IsNativeIdentifiers)
			{
				adapter = new SecurityNativeIdMessageAdapter(adapter, NativeIdStorage);
			}

			if (SlippageManager != null)
			{
				adapter = new SlippageMessageAdapter(adapter) { SlippageManager = SlippageManager.Clone() };
			}

			if (PnLManager != null)
			{
				adapter = new PnLMessageAdapter(adapter) { PnLManager = PnLManager.Clone() };
			}

			if (CommissionManager != null)
			{
				adapter = new CommissionMessageAdapter(adapter) { CommissionManager = CommissionManager.Clone() };
			}

			if (SupportBuildingFromOrderLog)
			{
				adapter = new OrderLogMessageAdapter(adapter);
			}

			if (adapter.IsFullCandlesOnly)
			{
				adapter = new CandleHolderMessageAdapter(adapter);
			}

			if (adapter.IsSupportSubscriptions)
			{
				adapter = new SubscriptionMessageAdapter(adapter) { IsRestoreOnErrorReconnect = IsRestoreSubscriptionOnReconnect };
			}

			if (SupportCandlesCompression)
			{
				adapter = new CandleBuilderMessageAdapter(adapter, CandleBuilderProvider);
			}

			if (SecurityMappingStorage != null && !adapter.StorageName.IsEmpty())
			{
				adapter = new SecurityMappingMessageAdapter(adapter, SecurityMappingStorage);
			}

			if (ExtendedInfoStorage != null && !adapter.SecurityExtendedFields.IsEmpty())
			{
				adapter = new ExtendedInfoStorageMessageAdapter(adapter, ExtendedInfoStorage, adapter.StorageName, adapter.SecurityExtendedFields);
			}

			return adapter;
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnSendInMessage(Message message)
		{
			if (message.IsBack)
			{
				var adapter = message.Adapter;

				if (adapter == null)
					throw new InvalidOperationException();

				if (adapter == this)
				{
					message.Adapter = null;
					message.IsBack = false;
				}
				else
				{
					adapter.SendInMessage(message);
					return;	
				}
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
					ProcessReset((ResetMessage)message);
					break;

				case MessageTypes.Connect:
				{
					if (_isFirstConnect)
						_isFirstConnect = false;
					else
						ProcessReset(new ResetMessage());

					_hearbeatAdapters.AddRange(GetSortedAdapters().ToDictionary(a => a, a =>
					{
						lock (_connectedResponseLock)
							_pendingConnectAdapters.Add(a);

						var wrapper = CreateWrappers(a);
						var hearbeatAdapter = new HeartbeatMessageAdapter(wrapper) { SuppressReconnectingErrors = SuppressReconnectingErrors };
						((IMessageAdapter)hearbeatAdapter).Parent = this;
						hearbeatAdapter.NewOutMessage += m => OnInnerAdapterNewOutMessage(wrapper, m);
						return hearbeatAdapter;
					}));
					
					if (_hearbeatAdapters.Count == 0)
						throw new InvalidOperationException(LocalizedStrings.Str3650);

					_hearbeatAdapters.Values.ForEach(a =>
					{
						var u = GetUnderlyingAdapter(a);
						this.AddInfoLog("Connecting '{0}'.", u);

						a.SendInMessage(message);
					});
					break;
				}

				case MessageTypes.Disconnect:
				{
					lock (_connectedResponseLock)
					{
						_connectedAdapters.ToArray().ForEach(a =>
						{
							var u = GetUnderlyingAdapter(a);
							this.AddInfoLog("Disconnecting '{0}'.", u);

							a.SendInMessage(message);
						});
					}

					break;
				}

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;
					ProcessPortfolioMessage(pfMsg.PortfolioName, pfMsg);
					break;
				}

				case MessageTypes.PortfolioChange:
				{
					var pfMsg = (PortfolioChangeMessage)message;
					ProcessPortfolioMessage(pfMsg.PortfolioName, pfMsg);
					break;
				}

				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderCancel:
				case MessageTypes.OrderGroupCancel:
				{
					var ordMsg = (OrderMessage)message;
					ProcessAdapterMessage(ordMsg.PortfolioName, ordMsg);
					break;
				}

				case MessageTypes.OrderPairReplace:
				{
					var ordMsg = (OrderPairReplaceMessage)message;
					ProcessAdapterMessage(ordMsg.Message1.PortfolioName, ordMsg);
					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketDataRequest((MarketDataMessage)message);
					break;
				}

				case MessageTypes.ChangePassword:
				{
					var adapter = GetSortedAdapters().FirstOrDefault(a => a.SupportedMessages.Contains(MessageTypes.ChangePassword));

					if (adapter == null)
						throw new InvalidOperationException(LocalizedStrings.Str629Params.Put(message.Type));

					adapter.SendInMessage(message);
					break;
				}

				default:
				{
					ProcessOtherMessage(message);
					break;
				}
			}
		}

		private void ProcessOtherMessage(Message message)
		{
			if (message.Adapter != null)
			{
				message.Adapter.SendInMessage(message);
				return;
			}

			GetAdapters(message).ForEach(a => a.SendInMessage(message));
		}

		private IMessageAdapter[] GetAdapters(Message message)
		{
			IMessageAdapter[] adapters;

			lock (_connectedResponseLock)
			{
				adapters = _messageTypeAdapters.TryGetValue(message.Type)?.Cache;

				if (adapters != null)
				{
					if (message.Type == MessageTypes.MarketData)
					{
						var set = _subscriptionNonSupportedAdapters.TryGetValue(((MarketDataMessage)message).TransactionId);

						if (set != null)
						{
							adapters = adapters.Where(a => !set.Contains(GetUnderlyingAdapter(a))).ToArray();

							if (adapters.Length == 0)
								adapters = null;
						}
					}
					else if (message.Type == MessageTypes.SecurityLookup)
					{
						var isAll = ((SecurityLookupMessage)message).IsLookupAll();

						if (isAll)
							adapters = adapters.Where(a => a.IsSupportSecuritiesLookupAll).ToArray();
					}
				}

				if (adapters == null)
				{
					if (_pendingConnectAdapters.Count > 0)
					{
						_pendingMessages.Enqueue(message.Clone());
						return ArrayHelper.Empty<IMessageAdapter>();
					}
				}
			}

			if (adapters == null)
			{
				adapters = ArrayHelper.Empty<IMessageAdapter>();
			}

			if (adapters.Length == 0)
			{
				var msg = LocalizedStrings.Str629Params.Put(message);

				this.AddWarningLog(msg);

				if (message.Type == MessageTypes.SecurityLookup)
				{
					SendOutMessage(new SecurityLookupResultMessage
					{
						OriginalTransactionId = ((SecurityLookupMessage)message).TransactionId,
						Error = new InvalidOperationException(msg),
					});
				}

				//throw new InvalidOperationException(LocalizedStrings.Str629Params.Put(message));
			}

			return adapters;
		}

		private IEnumerable<IMessageAdapter> GetSubscriptionAdapters(MarketDataMessage mdMsg)
		{
			if (mdMsg.Adapter != null)
			{
				var wrapper = _hearbeatAdapters.TryGetValue(mdMsg.Adapter);

				if (wrapper != null)
					return new[] { (IMessageAdapter)wrapper };
			}

			var adapters = GetAdapters(mdMsg).Where(a =>
			{
				if (mdMsg.DataType != MarketDataTypes.CandleTimeFrame)
				{
					if (a.IsMarketDataTypeSupported(mdMsg.DataType))
						return true;
					else
					{
						switch (mdMsg.DataType)
						{
							case MarketDataTypes.Level1:
							case MarketDataTypes.OrderLog:
							case MarketDataTypes.News:
								return false;
							case MarketDataTypes.MarketDepth:
							{
								if (mdMsg.BuildMode != MarketDataBuildModes.Load)
								{
									switch (mdMsg.BuildFrom)
									{
										case MarketDataTypes.Level1:
											return a.IsMarketDataTypeSupported(MarketDataTypes.Level1);
										case MarketDataTypes.OrderLog:
											return a.IsMarketDataTypeSupported(MarketDataTypes.OrderLog);
									}
								}

								return false;
							}
							case MarketDataTypes.Trades:
								return a.IsMarketDataTypeSupported(MarketDataTypes.OrderLog);
							default:
							{
								if (CandleBuilderProvider.IsRegistered(mdMsg.DataType))
									return mdMsg.BuildMode != MarketDataBuildModes.Load;
								else
									throw new ArgumentOutOfRangeException(mdMsg.DataType.ToString());
							}
						}
					}
				}

				var original = (TimeSpan)mdMsg.Arg;
				var timeFrames = a.GetTimeFrames(mdMsg.SecurityId).ToArray();

				if (timeFrames.Contains(original) || a.CheckTimeFrameByRequest)
					return true;

				if (mdMsg.AllowBuildFromSmallerTimeFrame)
				{
					var smaller = timeFrames
					              .FilterSmallerTimeFrames(original)
					              .OrderByDescending()
					              .FirstOr();

					if (smaller != null)
						return true;
				}

				var buildFrom = mdMsg.BuildFrom ?? a.SupportedMarketDataTypes.Intersect(CandleHelper.CandleDataSources).FirstOr();

				return buildFrom != null && a.SupportedMarketDataTypes.Contains(buildFrom.Value);
			}).ToArray();

			if (adapters.Length == 0)
				throw new InvalidOperationException(LocalizedStrings.Str629Params.Put(mdMsg));

			return adapters;
		}

		private void ProcessMarketDataRequest(MarketDataMessage mdMsg)
		{
			if (mdMsg.TransactionId == 0)
				throw new InvalidOperationException("TransId == 0");

			switch (mdMsg.DataType)
			{
				case MarketDataTypes.News:
				{
					var dict = new Dictionary<IMessageAdapter, long>();

					if (mdMsg.IsSubscribe)
					{
						var adapters = GetSubscriptionAdapters(mdMsg);

						lock (_newsSubscriptions.SyncRoot)
						{
							foreach (var adapter in adapters)
							{
								var transId = TransactionIdGenerator.GetNextId();

								dict.Add(adapter, transId);

								_newsSubscriptions.Add(transId, RefTuple.Create(mdMsg.TransactionId, (bool?)null, adapter));
							}
						}
					}
					else
					{
						lock (_newsSubscriptions.SyncRoot)
						{
							var adapters = _newsSubscriptions.Select(p => p.Value.Third).Where(a => a != null).ToArray();

							foreach (var adapter in adapters)
							{
								var transId = TransactionIdGenerator.GetNextId();

								dict.Add(adapter, transId);

								_newsSubscriptions.Add(transId, RefTuple.Create(mdMsg.TransactionId, (bool?)null, adapter));
							}
						}
					}

					// sending to inner adapters unique subscriptions
					foreach (var pair in dict)
					{
						var clone = (MarketDataMessage)mdMsg.Clone();
						clone.TransactionId = pair.Value;

						_subscriptionMessages.Add(pair.Value, clone);
						pair.Key.SendInMessage(clone);
					}

					break;
				}

				default:
				{
					var key = mdMsg.CreateKey();

					var adapter = mdMsg.IsSubscribe
							? GetSubscriptionAdapters(mdMsg).First()
							: (_subscriptionsById.TryGetValue(mdMsg.OriginalTransactionId) ?? _subscriptionsByKey.TryGetValue(key));

					if (adapter == null)
						break;

					// if the message was looped back via IsBack=true
					_subscriptionMessages.TryAdd(mdMsg.TransactionId, (MarketDataMessage)mdMsg.Clone());
					adapter.SendInMessage(mdMsg);

					break;
				}
			}
		}

		private void ProcessAdapterMessage(string portfolioName, Message message)
		{
			var adapter = message.Adapter;

			if (adapter == null)
				ProcessPortfolioMessage(portfolioName, message);
			else
				adapter.SendInMessage(message);
		}

		private void ProcessPortfolioMessage(string portfolioName, Message message)
		{
			var adapter = portfolioName.IsEmpty() ? null : AdapterProvider.GetAdapter(portfolioName);

			if (adapter == null)
			{
				adapter = GetAdapters(message).FirstOrDefault();

				if (adapter == null)
					return;
			}
			else
			{
				var a = _hearbeatAdapters.TryGetValue(adapter);

				adapter = a ?? throw new InvalidOperationException(LocalizedStrings.Str1838Params.Put(adapter.GetType()));
			}

			adapter.SendInMessage(message);
		}

		/// <summary>
		/// The embedded adapter event <see cref="IMessageChannel.NewOutMessage"/> handler.
		/// </summary>
		/// <param name="innerAdapter">The embedded adapter.</param>
		/// <param name="message">Message.</param>
		protected virtual void OnInnerAdapterNewOutMessage(IMessageAdapter innerAdapter, Message message)
		{
			if (!message.IsBack)
			{
				if (message.Adapter == null)
					message.Adapter = innerAdapter;

				switch (message.Type)
				{
					case MessageTypes.Connect:
						ProcessConnectMessage(innerAdapter, (ConnectMessage)message);
						return;

					case MessageTypes.Disconnect:
						ProcessDisconnectMessage(innerAdapter, (DisconnectMessage)message);
						return;

					case MessageTypes.MarketData:
						ProcessMarketDataResponse(innerAdapter, (MarketDataMessage)message);
						return;

					case MessageTypes.Portfolio:
						var pfMsg = (PortfolioMessage)message;
						AdapterProvider.SetAdapter(pfMsg.PortfolioName, GetUnderlyingAdapter(innerAdapter));
						break;

					case MessageTypes.PortfolioChange:
						var pfChangeMsg = (PortfolioChangeMessage)message;
						AdapterProvider.SetAdapter(pfChangeMsg.PortfolioName, GetUnderlyingAdapter(innerAdapter));
						break;

					//case MessageTypes.Position:
					//	var posMsg = (PositionMessage)message;
					//	AdapterProvider.SetAdapter(posMsg.PortfolioName, GetUnderlyingAdapter(innerAdapter));
					//	break;

					case MessageTypes.PositionChange:
						var posChangeMsg = (PositionChangeMessage)message;
						AdapterProvider.SetAdapter(posChangeMsg.PortfolioName, GetUnderlyingAdapter(innerAdapter));
						break;
				}
			}

			SendOutMessage(message);
		}

		private static IMessageAdapter GetUnderlyingAdapter(IMessageAdapter adapter)
		{
			return adapter is IMessageAdapterWrapper wrapper ? (wrapper is IRealTimeEmulationMarketDataAdapter emuWrapper ? emuWrapper : GetUnderlyingAdapter(wrapper.InnerAdapter)) : adapter;
		}

		private void ProcessConnectMessage(IMessageAdapter innerAdapter, ConnectMessage message)
		{
			var underlyingAdapter = GetUnderlyingAdapter(innerAdapter);
			var heartbeatAdapter = _hearbeatAdapters[underlyingAdapter];

			var isError = message.Error != null;

			Message[] pendingMessages;

			if (isError)
				this.AddErrorLog(LocalizedStrings.Str625Params, underlyingAdapter, message.Error);
			else
				this.AddInfoLog("Connected to '{0}'.", underlyingAdapter);

			lock (_connectedResponseLock)
			{
				_pendingConnectAdapters.Remove(underlyingAdapter);

				if (isError)
				{
					_connectedAdapters.Remove(heartbeatAdapter);

					if (_pendingConnectAdapters.Count == 0)
					{
						pendingMessages = _pendingMessages.ToArray();
						_pendingMessages.Clear();
					}
					else
						pendingMessages = ArrayHelper.Empty<Message>();
				}
				else
				{
					foreach (var supportedMessage in innerAdapter.SupportedMessages)
					{
						_messageTypeAdapters.SafeAdd(supportedMessage).Add(heartbeatAdapter);
					}

					_connectedAdapters.Add(heartbeatAdapter);

					pendingMessages = _pendingMessages.ToArray();
					_pendingMessages.Clear();
				}
			}

			message.Adapter = underlyingAdapter;
			SendOutMessage(message);

			foreach (var pendingMessage in pendingMessages)
			{
				if (isError)
					SendOutError(LocalizedStrings.Str629Params.Put(pendingMessage.Type));
				else
				{
					pendingMessage.Adapter = this;
					pendingMessage.IsBack = true;
					SendOutMessage(pendingMessage);
				}
			}
		}

		private void ProcessDisconnectMessage(IMessageAdapter innerAdapter, DisconnectMessage message)
		{
			var underlyingAdapter = GetUnderlyingAdapter(innerAdapter);
			var heartbeatAdapter = _hearbeatAdapters[underlyingAdapter];

			if (message.Error == null)
				this.AddInfoLog("Disconnected from '{0}'.", underlyingAdapter);
			else
				this.AddErrorLog(LocalizedStrings.Str627Params, underlyingAdapter, message.Error);

			lock (_connectedResponseLock)
			{
				foreach (var supportedMessage in innerAdapter.SupportedMessages)
				{
					var list = _messageTypeAdapters.TryGetValue(supportedMessage);

					if (list == null)
						continue;

					list.Remove(heartbeatAdapter);

					if (list.Count == 0)
						_messageTypeAdapters.Remove(supportedMessage);
				}

				_connectedAdapters.Remove(heartbeatAdapter);
			}

			message.Adapter = underlyingAdapter;
			SendOutMessage(message);
		}

		private void ProcessMarketDataResponse(IMessageAdapter adapter, MarketDataMessage message)
		{
			var originalTransactionId = message.OriginalTransactionId;
			var originMsg = _subscriptionMessages.TryGetValue(originalTransactionId);

			if (originMsg == null)
			{
				SendOutMessage(message);
				return;
			}

			var error = message.Error;
			var isSubscribe = originMsg.IsSubscribe;

			if (originMsg.DataType == MarketDataTypes.News)
			{
				long? transId;
				var allError = true;

				lock (_newsSubscriptions.SyncRoot)
				{
					var tuple = _newsSubscriptions.TryGetValue(originalTransactionId);

					transId = tuple.First;
					tuple.Second = error == null && !message.IsNotSupported;

					foreach (var pair in _newsSubscriptions)
					{
						var t = pair.Value;

						if (t.First == tuple.First)
						{
							// one of adapter still not yet response.
							if (t.Second == null)
							{
								transId = null;
								break;
							}
							else if (t.Second == true)
								allError = false;
						}
					}
				}

				if (transId != null)
				{
					SendOutMessage(new MarketDataMessage
					{
						OriginalTransactionId = transId.Value,
						Adapter = adapter,
						IsSubscribe = isSubscribe,
						Error = allError ? new InvalidOperationException(LocalizedStrings.Str629Params.Put(originMsg)) : null,
					});
				}

				return;
			}

			var key = originMsg.CreateKey();

			if (message.IsNotSupported)
			{
				lock (_connectedResponseLock)
				{
					// try loopback only subscribe messages
					if (originMsg.IsSubscribe)
					{
						var set = _subscriptionNonSupportedAdapters.SafeAdd(originalTransactionId, k => new HashSet<IMessageAdapter>());
						set.Add(GetUnderlyingAdapter(adapter));

						originMsg.Adapter = this;
						originMsg.IsBack = true;
					}
					
					SendOutMessage(originMsg);
				}

				return;
			}
			
			if (error == null && isSubscribe)
			{
				// we can initiate multiple subscriptions with unique request id and same params
				_subscriptionsByKey.TryAdd(key, adapter);

				// TODO
				_subscriptionsById.TryAdd(originalTransactionId, adapter);
			}

			RaiseMarketDataMessage(adapter, originalTransactionId, error, isSubscribe);
		}

		private void RaiseMarketDataMessage(IMessageAdapter adapter, long originalTransactionId, Exception error, bool isSubscribe)
		{
			SendOutMessage(new MarketDataMessage
			{
				OriginalTransactionId = originalTransactionId,
				Error = error,
				Adapter = adapter,
				IsSubscribe = isSubscribe,
			});
		}

		/// <summary>
		/// To get adapters <see cref="IInnerAdapterList.SortedAdapters"/> sorted by the specified priority. By default, there is no sorting.
		/// </summary>
		/// <returns>Sorted adapters.</returns>
		protected IEnumerable<IMessageAdapter> GetSortedAdapters()
		{
			return _innerAdapters.SortedAdapters;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			lock (InnerAdapters.SyncRoot)
			{
				storage.SetValue(nameof(InnerAdapters), InnerAdapters.Select(a =>
				{
					var s = new SettingsStorage();

					s.SetValue("AdapterType", a.GetType().GetTypeName(false));
					s.SetValue("AdapterSettings", a.Save());
					s.SetValue("Priority", InnerAdapters[a]);

					return s;
				}).ToArray());

				var pairs = AdapterProvider
					.PortfolioAdapters
					.Where(p => InnerAdapters.Contains(p.Value))
					.Select(p => RefTuple.Create(p.Key, p.Value.Id))
					.ToArray();

				storage.SetValue(nameof(AdapterProvider), pairs);
			}

			if (LatencyManager != null)
				storage.SetValue(nameof(LatencyManager), LatencyManager.SaveEntire(false));

			if (CommissionManager != null)
				storage.SetValue(nameof(CommissionManager), CommissionManager.SaveEntire(false));

			if (PnLManager != null)
				storage.SetValue(nameof(PnLManager), PnLManager.SaveEntire(false));

			if (SlippageManager != null)
				storage.SetValue(nameof(SlippageManager), SlippageManager.SaveEntire(false));

			base.Save(storage);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			lock (InnerAdapters.SyncRoot)
			{
				InnerAdapters.Clear();

				var adapters = new Dictionary<Guid, IMessageAdapter>();

				foreach (var s in storage.GetValue<IEnumerable<SettingsStorage>>(nameof(InnerAdapters)))
				{
					try
					{
						var adapter = s.GetValue<Type>("AdapterType").CreateInstance<IMessageAdapter>(TransactionIdGenerator);
						adapter.Load(s.GetValue<SettingsStorage>("AdapterSettings"));
						InnerAdapters[adapter] = s.GetValue<int>("Priority");

						adapters.Add(adapter.Id, adapter);
					}
					catch (Exception e)
					{
						e.LogError();
					}
				}

				if (storage.ContainsKey(nameof(AdapterProvider)))
				{
					var mapping = storage.GetValue<RefPair<string, Guid>[]>(nameof(AdapterProvider));

					foreach (var tuple in mapping)
					{
						if (adapters.TryGetValue(tuple.Second, out var adapter))
							AdapterProvider.SetAdapter(tuple.First, adapter);
					}
				}
			}

			if (storage.ContainsKey(nameof(LatencyManager)))
				LatencyManager = storage.GetValue<SettingsStorage>(nameof(LatencyManager)).LoadEntire<ILatencyManager>();

			if (storage.ContainsKey(nameof(CommissionManager)))
				CommissionManager = storage.GetValue<SettingsStorage>(nameof(CommissionManager)).LoadEntire<ICommissionManager>();

			if (storage.ContainsKey(nameof(PnLManager)))
				PnLManager = storage.GetValue<SettingsStorage>(nameof(PnLManager)).LoadEntire<IPnLManager>();

			if (storage.ContainsKey(nameof(SlippageManager)))
				SlippageManager = storage.GetValue<SettingsStorage>(nameof(SlippageManager)).LoadEntire<ISlippageManager>();

			base.Load(storage);
		}

		/// <summary>
		/// To release allocated resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_hearbeatAdapters.Values.ForEach(a => ((IMessageAdapter)a).Parent = null);

			base.DisposeManaged();
		}

		/// <summary>
		/// Create a copy of <see cref="BasketMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			var clone = new BasketMessageAdapter(TransactionIdGenerator, AdapterProvider, CandleBuilderProvider)
			{
				ExtendedInfoStorage = ExtendedInfoStorage,
				SupportCandlesCompression = SupportCandlesCompression,
				SuppressReconnectingErrors = SuppressReconnectingErrors,
				IsRestoreSubscriptionOnReconnect = IsRestoreSubscriptionOnReconnect,
				SupportBuildingFromOrderLog = SupportBuildingFromOrderLog,
			};

			clone.Load(this.Save());

			return clone;
		}
	}
}