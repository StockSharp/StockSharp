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

	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using SubscriptionInfo = System.Tuple<Messages.MarketDataTypes, Messages.SecurityId, object, System.DateTimeOffset?, System.DateTimeOffset?, long?, int?>;

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

		private enum SubscriptionStates
		{
			Subscribed,
			Subscribing,
			Unsubscribing,
		}

		private readonly SynchronizedDictionary<SubscriptionInfo, SubscriptionStates> _subscriptionStates = new SynchronizedDictionary<SubscriptionInfo, SubscriptionStates>();
		private readonly SynchronizedPairSet<SubscriptionInfo, IEnumerator<IMessageAdapter>> _subscriptionQueue = new SynchronizedPairSet<SubscriptionInfo, IEnumerator<IMessageAdapter>>();
		private readonly SynchronizedDictionary<long, SubscriptionInfo> _subscriptionKeys = new SynchronizedDictionary<long, SubscriptionInfo>();
		private readonly SynchronizedDictionary<SubscriptionInfo, IMessageAdapter> _subscriptions = new SynchronizedDictionary<SubscriptionInfo, IMessageAdapter>();
		//private readonly SynchronizedDictionary<IMessageAdapter, RefPair<bool, Exception>> _adapterStates = new SynchronizedDictionary<IMessageAdapter, RefPair<bool, Exception>>();
		private readonly SynchronizedDictionary<IMessageAdapter, HeartbeatMessageAdapter> _hearbeatAdapters = new SynchronizedDictionary<IMessageAdapter, HeartbeatMessageAdapter>();
		private readonly SynchronizedDictionary<MessageTypes, CachedSynchronizedList<IMessageAdapter>> _messageTypeAdapters = new SynchronizedDictionary<MessageTypes, CachedSynchronizedList<IMessageAdapter>>();
		private readonly CachedSynchronizedSet<HeartbeatMessageAdapter> _connectedAdapters = new CachedSynchronizedSet<HeartbeatMessageAdapter>();
		private bool _isFirstConnect;
		private readonly InnerAdapterList _innerAdapters;

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
			get { return _nativeIdStorage; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_nativeIdStorage = value;
			}
		}

		/// <summary>
		/// Extended info <see cref="Message.ExtensionInfo"/> storage.
		/// </summary>
		public IExtendedInfoStorage ExtendedInfoStorage { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BasketMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public BasketMessageAdapter(IdGenerator transactionIdGenerator)
			: this(transactionIdGenerator, new InMemoryMessageAdapterProvider())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BasketMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		/// <param name="adapterProvider">The message adapter's provider.</param>
		public BasketMessageAdapter(IdGenerator transactionIdGenerator, IMessageAdapterProvider adapterProvider)
			: base(transactionIdGenerator)
		{
			if (adapterProvider == null)
				throw new ArgumentNullException(nameof(adapterProvider));

			_innerAdapters = new InnerAdapterList();
			AdapterProvider = adapterProvider;
		}

		/// <summary>
		/// The message adapter's provider.
		/// </summary>
		public IMessageAdapterProvider AdapterProvider { get; }

		/// <summary>
		/// Supported by adapter message types.
		/// </summary>
		public override MessageTypes[] SupportedMessages
		{
			get { return GetSortedAdapters().SelectMany(a => a.SupportedMessages).Distinct().ToArray(); }
		}

		/// <summary>
		/// <see cref="PortfolioLookupMessage"/> required to get portfolios and positions.
		/// </summary>
		public override bool PortfolioLookupRequired
		{
			get { return GetSortedAdapters().Any(a => a.PortfolioLookupRequired); }
		}

		/// <summary>
		/// <see cref="OrderStatusMessage"/> required to get orders and ow trades.
		/// </summary>
		public override bool OrderStatusRequired
		{
			get { return GetSortedAdapters().Any(a => a.OrderStatusRequired); }
		}

		/// <summary>
		/// <see cref="SecurityLookupMessage"/> required to get securities.
		/// </summary>
		public override bool SecurityLookupRequired
		{
			get { return GetSortedAdapters().Any(a => a.SecurityLookupRequired); }
		}

		/// <summary>
		/// Gets a value indicating whether the connector supports position lookup.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup => true;

		/// <summary>
		/// Gets a value indicating whether the connector supports security lookup.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup => true;

		/// <summary>
		/// Restore subscription on reconnect.
		/// </summary>
		public bool IsRestorSubscriptioneOnReconnect { get; set; }

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <returns>Order condition. If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Check the connection is alive. Uses only for connected states.
		/// </summary>
		/// <returns><see langword="true" />, is the connection still alive, <see langword="false" />, if the connection was rejected.</returns>
		public override bool IsConnectionAlive()
		{
			throw new NotSupportedException();
		}

		private void ProcessReset(Message message)
		{
			_hearbeatAdapters.Values.ForEach(a =>
			{
				a.SendInMessage(message);
				a.Dispose();
			});

			_connectedAdapters.Clear();
			_messageTypeAdapters.Clear();
			_hearbeatAdapters.Clear();
			_subscriptionQueue.Clear();
			_subscriptions.Clear();
			_subscriptionKeys.Clear();
			_subscriptionStates.Clear();
		}

		private IMessageAdapter CreateWrappers(IMessageAdapter adapter)
		{
			if (adapter.IsNativeIdentifiers)
			{
				adapter = new SecurityNativeIdMessageAdapter(adapter, NativeIdStorage);
			}

			if (ExtendedInfoStorage != null && !adapter.SecurityExtendedFields.IsEmpty())
			{
				adapter = new ExtendedInfoStorageMessageAdapter(adapter, ExtendedInfoStorage.Create(adapter.StorageName, adapter.SecurityExtendedFields));
			}

			return new SubscriptionMessageAdapter(adapter) { IsRestoreOnReconnect = IsRestorSubscriptioneOnReconnect };
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

				adapter.SendInMessage(message);
				return;
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
					ProcessReset(message);
					break;

				case MessageTypes.Connect:
				{
					if (_isFirstConnect)
						_isFirstConnect = false;
					else
						ProcessReset(new ResetMessage());

					_hearbeatAdapters.AddRange(GetSortedAdapters().Select(CreateWrappers).ToDictionary(a => a, a =>
					{
						var hearbeatAdapter = new HeartbeatMessageAdapter(a);
						((IMessageAdapter)hearbeatAdapter).Parent = this;
						hearbeatAdapter.NewOutMessage += m => OnInnerAdapterNewOutMessage(a, m);
						return hearbeatAdapter;
					}));
					
					if (_hearbeatAdapters.Count == 0)
						throw new InvalidOperationException(LocalizedStrings.Str3650);

					_hearbeatAdapters.Values.ForEach(a => a.SendInMessage(message));
					break;
				}

				case MessageTypes.Disconnect:
				{
					_connectedAdapters.Cache.ForEach(a => a.SendInMessage(message));
					break;
				}

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;
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
					IMessageAdapter[] adapters = null;

					if (message.Adapter != null)
					{
						var wrapper = _hearbeatAdapters.Values.FirstOrDefault(w => GetUnderlyingAdapter(w) == message.Adapter);

						if (wrapper != null)
							adapters = new IMessageAdapter[] {wrapper};
					}

					if (adapters == null)
						adapters = _messageTypeAdapters.TryGetValue(message.Type)?.Cache;

					if (adapters == null)
						throw new InvalidOperationException(LocalizedStrings.Str629Params.Put(message.Type));

					var mdMsg = (MarketDataMessage)message;
					
					switch (mdMsg.DataType)
					{
						case MarketDataTypes.News:
							adapters.ForEach(a => a.SendInMessage(mdMsg));
							break;

						default:
						{
							var key = mdMsg.CreateKey();

							var state = _subscriptionStates.TryGetValue2(key);

							if (mdMsg.IsSubscribe)
							{
								if (state != null)
								{
									RaiseMarketDataMessage(null, mdMsg.OriginalTransactionId, new InvalidOperationException(state.Value.ToString()), true);
									break;
								}
								else
									_subscriptionStates.Add(key, SubscriptionStates.Subscribing);
							}
							else
							{
								var canProcess = false;

								switch (state)
								{
									case SubscriptionStates.Subscribed:
										canProcess = true;
										_subscriptionStates[key] = SubscriptionStates.Unsubscribing;
										break;
									case SubscriptionStates.Subscribing:
									case SubscriptionStates.Unsubscribing:
										RaiseMarketDataMessage(null, mdMsg.OriginalTransactionId, new InvalidOperationException(state.Value.ToString()), false);
										break;
									case null:
										RaiseMarketDataMessage(null, mdMsg.OriginalTransactionId, null, false);
										break;
									default:
										throw new ArgumentOutOfRangeException();
								}

								if (!canProcess)
									break;
							}

							if (mdMsg.TransactionId != 0)
								_subscriptionKeys.Add(mdMsg.TransactionId, key);

							if (mdMsg.IsSubscribe)
							{
								//if (_subscriptionQueue.ContainsKey(key))
								//	return;

								var enumerator = adapters.Cast<IMessageAdapter>().GetEnumerator();

								_subscriptionQueue.Add(key, enumerator);
								ProcessSubscriptionAction(enumerator, mdMsg, mdMsg.TransactionId);
							}
							else
							{
								var adapter = _subscriptions.TryGetValue(key);

								if (adapter != null)
								{
									_subscriptions.Remove(key);
									adapter.SendInMessage(message);
								}
							}

							break;
						}
					}
					
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
					if (message.Adapter != null)
					{
						message.Adapter.SendInMessage(message);
						break;
					}

					var adapters = _messageTypeAdapters.TryGetValue(message.Type)?.Cache;

					if (adapters == null)
						throw new InvalidOperationException(LocalizedStrings.Str629Params.Put(message.Type));

					adapters.ForEach(a => a.SendInMessage(message));
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
				var adapters = _messageTypeAdapters.TryGetValue(message.Type)?.Cache;

				if (adapters == null || adapters.Length != 1)
					throw new InvalidOperationException(LocalizedStrings.Str623Params.Put(portfolioName));

				adapter = adapters.First();
			}
			else
			{
				var a = _hearbeatAdapters.TryGetValue(adapter);

				if (a == null)
					throw new InvalidOperationException(LocalizedStrings.Str1838Params.Put(adapter.GetType()));

				adapter = a;
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
						ProcessMarketDataMessage(innerAdapter, (MarketDataMessage)message);
						return;

					case MessageTypes.Portfolio:
						var pfMsg = (PortfolioMessage)message;
						AdapterProvider.SetAdapter(pfMsg.PortfolioName, innerAdapter);
						break;

					case MessageTypes.PortfolioChange:
						var pfChangeMsg = (PortfolioChangeMessage)message;
						AdapterProvider.SetAdapter(pfChangeMsg.PortfolioName, innerAdapter);
						break;

					case MessageTypes.Position:
						var posMsg = (PositionMessage)message;
						AdapterProvider.SetAdapter(posMsg.PortfolioName, innerAdapter);
						break;

					case MessageTypes.PositionChange:
						var posChangeMsg = (PositionChangeMessage)message;
						AdapterProvider.SetAdapter(posChangeMsg.PortfolioName, innerAdapter);
						break;
				}
			}

			SendOutMessage(message);
		}

		private static IMessageAdapter GetUnderlyingAdapter(IMessageAdapter adapter)
		{
			var wrapper = adapter as IMessageAdapterWrapper;
			return wrapper != null ? GetUnderlyingAdapter(wrapper.InnerAdapter) : adapter;
		}

		private void ProcessConnectMessage(IMessageAdapter innerAdapter, ConnectMessage message)
		{
			var underlyingAdapter = GetUnderlyingAdapter(innerAdapter);

			if (message.Error != null)
				this.AddErrorLog(LocalizedStrings.Str625Params, underlyingAdapter.GetType().Name, message.Error);
			else
			{
				var heartbeatAdapter = _hearbeatAdapters[innerAdapter];

				foreach (var supportedMessage in innerAdapter.SupportedMessages)
				{
					_messageTypeAdapters.SafeAdd(supportedMessage).Add(heartbeatAdapter);
				}

				_connectedAdapters.Add(heartbeatAdapter);
			}

			message.Adapter = underlyingAdapter;
			SendOutMessage(message);
		}

		private void ProcessDisconnectMessage(IMessageAdapter innerAdapter, DisconnectMessage message)
		{
			var underlyingAdapter = GetUnderlyingAdapter(innerAdapter);

			if (message.Error != null)
				this.AddErrorLog(LocalizedStrings.Str627Params, underlyingAdapter.GetType().Name, message.Error);

			message.Adapter = underlyingAdapter;
			SendOutMessage(message);
		}

		private void ProcessSubscriptionAction(IEnumerator<IMessageAdapter> enumerator, MarketDataMessage message, long originalTransactionId)
		{
			if (enumerator.MoveNext())
				enumerator.Current.SendInMessage(message);
			else
			{
				_subscriptionQueue.RemoveByValue(enumerator);

				var key = _subscriptionKeys.TryGetValue(message.OriginalTransactionId);

				if (key == null)
					key = message.CreateKey();
				else
					_subscriptionKeys.Remove(originalTransactionId);

				_subscriptionStates.Remove(key);
				RaiseMarketDataMessage(null, originalTransactionId, new ArgumentException(LocalizedStrings.Str629Params.Put(key.Item1 + " " + key.Item2), nameof(message)), true);
			}
		}

		private void ProcessMarketDataMessage(IMessageAdapter adapter, MarketDataMessage message)
		{
			var key = _subscriptionKeys.TryGetValue(message.OriginalTransactionId) ?? message.CreateKey();
			
			var enumerator = _subscriptionQueue.TryGetValue(key);
			var state = _subscriptionStates.TryGetValue2(key);
			var error = message.Error;
			var isOk = !message.IsNotSupported && error == null;

			var isSubscribe = message.IsSubscribe;

			switch (state)
			{
				case SubscriptionStates.Subscribed:
					break;
				case SubscriptionStates.Subscribing:
					isSubscribe = true;
					if (isOk)
					{
						_subscriptions.Add(key, adapter);
						_subscriptionStates[key] = SubscriptionStates.Subscribed;
					}
					else if (error != null)
					{
						_subscriptions.Remove(key);
						_subscriptionStates.Remove(key);
					}
					break;
				case SubscriptionStates.Unsubscribing:
					isSubscribe = false;
					_subscriptions.Remove(key);
					_subscriptionStates.Remove(key);
					break;
				case null:
					if (isOk)
					{
						if (message.IsSubscribe)
						{
							_subscriptions.Add(key, adapter);
							_subscriptionStates.Add(key, SubscriptionStates.Subscribed);
							break;
						}
					}

					_subscriptions.Remove(key);
					_subscriptionStates.Remove(key);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (message.IsNotSupported)
			{
				if (enumerator != null)
					ProcessSubscriptionAction(enumerator, message, message.OriginalTransactionId);
				else
				{
					if (error == null)
						error = new InvalidOperationException(LocalizedStrings.Str633Params.Put(message.SecurityId, message.DataType));
				}
			}

			_subscriptionQueue.Remove(key);
			_subscriptionKeys.Remove(message.OriginalTransactionId);

			RaiseMarketDataMessage(adapter, message.OriginalTransactionId, error, isSubscribe);
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
			}

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

				foreach (var s in storage.GetValue<IEnumerable<SettingsStorage>>(nameof(InnerAdapters)))
				{
					var adapter = s.GetValue<Type>("AdapterType").CreateInstance<IMessageAdapter>(TransactionIdGenerator);
					adapter.Load(s.GetValue<SettingsStorage>("AdapterSettings"));
					InnerAdapters[adapter] = s.GetValue<int>("Priority");
				}
			}

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
	}
}