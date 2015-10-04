namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using SubscriptionInfo = System.Tuple<Messages.SecurityId, Messages.MarketDataTypes, System.DateTimeOffset?, System.DateTimeOffset?, long?, int?>;

	/// <summary>
	/// Интерфейс, описывающий список адаптеров к торговым системам, с которыми оперирует агрегатор.
	/// </summary>
	public interface IInnerAdapterList : ISynchronizedCollection<IMessageAdapter>, INotifyList<IMessageAdapter>
	{
		/// <summary>
		/// Внутренние адаптеры, отсортированные по скорости работы.
		/// </summary>
		IEnumerable<IMessageAdapter> SortedAdapters { get; }

		/// <summary>
		/// Индексатор, через который задаются приоритеты скорости (чем меньше значение, те быстрее адаптер) на внутренние адаптеры.
		/// </summary>
		/// <param name="adapter">Внутренний адаптер.</param>
		/// <returns>Приоритет адаптера. Если задается значение -1, то адаптер считается выключенным.</returns>
		int this[IMessageAdapter adapter] { get; set; }
	}

	/// <summary>
	/// Адаптер-агрегатор, позволяющий оперировать одновременно несколькими адаптерами, подключенных к разным торговым системам.
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
		private readonly SynchronizedDictionary<IMessageAdapter, IMessageAdapter> _hearbeatAdapters = new SynchronizedDictionary<IMessageAdapter, IMessageAdapter>();
		private readonly CachedSynchronizedDictionary<MessageTypes, CachedSynchronizedList<IMessageAdapter>> _connectedAdapters = new CachedSynchronizedDictionary<MessageTypes, CachedSynchronizedList<IMessageAdapter>>();
		private bool _isFirstConnect;
		private readonly InnerAdapterList _innerAdapters;

		/// <summary>
		/// Адаптеры, с которыми оперирует агрегатор.
		/// </summary>
		public IInnerAdapterList InnerAdapters
		{
			get { return _innerAdapters; }
		}

		/// <summary>
		/// Портфели, которые используются для отправки транзакций.
		/// </summary>
		public IDictionary<string, IMessageAdapter> Portfolios { get; private set; }

		/// <summary>
		/// Создать <see cref="BasketMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public BasketMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			_innerAdapters = new InnerAdapterList();
			Portfolios = new SynchronizedDictionary<string, IMessageAdapter>(StringComparer.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Поддерживаемые типы сообщений, который может обработать адаптер.
		/// </summary>
		public override MessageTypes[] SupportedMessages
		{
			get { return GetSortedAdapters().SelectMany(a => a.SupportedMessages).ToArray(); }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		public override bool PortfolioLookupRequired
		{
			get { return GetSortedAdapters().Any(a => a.PortfolioLookupRequired); }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="OrderStatusMessage"/> для получения списка заявок и собственных сделок.
		/// </summary>
		public override bool OrderStatusRequired
		{
			get { return GetSortedAdapters().Any(a => a.OrderStatusRequired); }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		public override bool SecurityLookupRequired
		{
			get { return GetSortedAdapters().Any(a => a.SecurityLookupRequired); }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск портфелей.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup
		{
			get { return true; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск инструментов.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup
		{
			get { return true; }
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено <see langword="null"/>.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Проверить, установлено ли еще соединение. Проверяется только в том случае, если было успешно установлено подключение.
		/// </summary>
		/// <returns><see langword="true"/>, если соединение еще установлено, <see langword="false"/>, если торговая система разорвала подключение.</returns>
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
			_hearbeatAdapters.Clear();
			_subscriptionQueue.Clear();
			_subscriptions.Clear();
			_subscriptionKeys.Clear();
			_subscriptionStates.Clear();
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					ProcessReset(message);
					break;

				case MessageTypes.Connect:
					if (_isFirstConnect)
						_isFirstConnect = false;
					else
						ProcessReset(new ResetMessage());

					_hearbeatAdapters.AddRange(GetSortedAdapters().ToDictionary(a => a, a =>
					{
						var hearbeatAdapter = (IMessageAdapter)new HeartbeatAdapter(a);
						hearbeatAdapter.Parent = this;
						hearbeatAdapter.NewOutMessage += m => OnInnerAdapterNewMessage(a, m);
						return hearbeatAdapter;
					}));

					if (_hearbeatAdapters.Count == 0)
						throw new InvalidOperationException(LocalizedStrings.Str3650);

					_hearbeatAdapters.Values.ForEach(a => a.SendInMessage(message));
					break;

				case MessageTypes.Disconnect:
					_connectedAdapters
						.CachedValues
						.SelectMany(c => c.Cache)
						.Distinct()
						.ForEach(a => a.SendInMessage(message));
					break;

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
					ProcessPortfolioMessage(ordMsg.PortfolioName, ordMsg);
					break;
				}

				case MessageTypes.OrderPairReplace:
				{
					var ordMsg = (OrderPairReplaceMessage)message;
					ProcessPortfolioMessage(ordMsg.Message1.PortfolioName, ordMsg);
					break;
				}

				case MessageTypes.MarketData:
				{
					var adapters = _connectedAdapters.TryGetValue(message.Type);

					if (adapters == null)
						throw new InvalidOperationException(LocalizedStrings.Str629Params.Put(message.Type));

					var mdMsg = (MarketDataMessage)message;
					
					switch (mdMsg.DataType)
					{
						case MarketDataTypes.News:
							adapters.Cache.ForEach(a => a.SendInMessage(mdMsg));
							break;

						default:
						{
							var key = CreateKey(mdMsg);

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

								var enumerator = adapters.Cache.Cast<IMessageAdapter>().GetEnumerator();

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

				default:
				{
					var adapters = _connectedAdapters.TryGetValue(message.Type);

					if (adapters == null)
						throw new InvalidOperationException(LocalizedStrings.Str629Params.Put(message.Type));

					adapters.Cache.ForEach(a => a.SendInMessage(message));
					break;
				}
			}
		}

		private void ProcessPortfolioMessage(string portfolioName, Message message)
		{
			var adapter = Portfolios.TryGetValue(portfolioName);

			if (adapter == null)
			{
				var adapters = _connectedAdapters.TryGetValue(message.Type);

				if (adapters == null || adapters.Count != 1)
					throw new InvalidOperationException(LocalizedStrings.Str623Params.Put(portfolioName));

				adapter = adapters.Cache.First();
			}
			else
				adapter = _hearbeatAdapters[adapter];

			adapter.SendInMessage(message);
		}

		/// <summary>
		/// Обработчик события <see cref="IMessageChannel.NewOutMessage"/> вложенного адаптера.
		/// </summary>
		/// <param name="innerAdapter">Вложенный адаптер.</param>
		/// <param name="message">Сообщение.</param>
		protected virtual void OnInnerAdapterNewMessage(IMessageAdapter innerAdapter, Message message)
		{
			if (message.IsBack)
			{
				message.IsBack = false;
				innerAdapter.SendInMessage(message);
				return;
			}

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
			}

			SendOutMessage(message);
		}

		private void ProcessConnectMessage(IMessageAdapter innerAdapter, ConnectMessage message)
		{
			if (message.Error != null)
				this.AddErrorLog(LocalizedStrings.Str625Params, innerAdapter.GetType().Name, message.Error);
			else
			{
				var adapter = _hearbeatAdapters[innerAdapter];

				foreach (var supportedMessage in adapter.SupportedMessages)
				{
					_connectedAdapters.SafeAdd(supportedMessage).Add(adapter);
				}
			}

			SendOutMessage(message);
		}

		private void ProcessDisconnectMessage(IMessageAdapter innerAdapter, DisconnectMessage message)
		{
			if (message.Error != null)
				this.AddErrorLog(LocalizedStrings.Str627Params, innerAdapter.GetType().Name, message.Error);

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
					key = CreateKey(message);
				else
					_subscriptionKeys.Remove(originalTransactionId);

				_subscriptionStates.Remove(key);
				RaiseMarketDataMessage(null, originalTransactionId, new ArgumentException(LocalizedStrings.Str629Params.Put(key.Item1 + " " + key.Item2), "message"), true);
			}
		}

		private static SubscriptionInfo CreateKey(MarketDataMessage message)
		{
			return Tuple.Create(message.SecurityId, message.DataType, message.From, message.To, message.Count, message.MaxDepth);
		}

		private void ProcessMarketDataMessage(IMessageAdapter adapter, MarketDataMessage message)
		{
			var key = _subscriptionKeys.TryGetValue(message.OriginalTransactionId) ?? CreateKey(message);
			
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
		/// Получить адаптеры <see cref="IInnerAdapterList.SortedAdapters"/>, отсортированные в зависимости от заданного приоритета. По-умолчанию сортировка отсутствует.
		/// </summary>
		/// <returns>Отсортированные адаптеры.</returns>
		protected IEnumerable<IMessageAdapter> GetSortedAdapters()
		{
			return _innerAdapters.SortedAdapters;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("InnerAdapters", _innerAdapters.Cache.Select(a =>
			{
				var s = new SettingsStorage();

				s.SetValue("Priority", _innerAdapters[a]);
				s.SetValue("Adapter", a.Save());

				return s;
			}).ToArray());

			base.Save(storage);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			var index = 0;

			foreach (var s in storage.GetValue<IEnumerable<SettingsStorage>>("InnerAdapters"))
			{
				var adapter = _innerAdapters.Cache[index++];

				_innerAdapters[adapter] = s.GetValue<int>("Priority");
				adapter.Load(s.GetValue<SettingsStorage>("Adapter"));
			}

			base.Load(storage);
		}

		/// <summary>
		/// Create a copy of <see cref="MessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			var clone = new BasketMessageAdapter(TransactionIdGenerator);

			foreach (var adapter in InnerAdapters)
			{
				clone.InnerAdapters[adapter] = InnerAdapters[adapter];
			}

			return clone;
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			_hearbeatAdapters.Values.ForEach(a => a.Parent = null);

			base.DisposeManaged();
		}
	}
}