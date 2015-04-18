namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

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

		private readonly SynchronizedPairSet<Tuple<SecurityId, MarketDataTypes>, IEnumerator<IMessageAdapter>> _subscriptionQueue = new SynchronizedPairSet<Tuple<SecurityId, MarketDataTypes>, IEnumerator<IMessageAdapter>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, MarketDataTypes>, IMessageAdapter> _subscriptions = new SynchronizedDictionary<Tuple<SecurityId, MarketDataTypes>, IMessageAdapter>();
		private readonly SynchronizedDictionary<IMessageAdapter, RefPair<bool, Exception>> _adapterStates = new SynchronizedDictionary<IMessageAdapter, RefPair<bool, Exception>>();
		private readonly CachedSynchronizedList<IMessageAdapter> _connectedAdapters = new CachedSynchronizedList<IMessageAdapter>();
		private readonly SynchronizedDictionary<IMessageAdapter, IMessageAdapter> _enabledAdapters = new SynchronizedDictionary<IMessageAdapter, IMessageAdapter>();

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
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
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

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
					_adapterStates.Clear();
					_connectedAdapters.Clear();
					_enabledAdapters.Clear();
					_subscriptionQueue.Clear();
					_subscriptions.Clear();
					_enabledAdapters.AddRange(GetSortedAdapters().ToDictionary(a => a, a =>
					{
						var hearbeatAdapter = (IMessageAdapter)new HeartbeatAdapter(a);
						hearbeatAdapter.Parent = this;
						hearbeatAdapter.NewOutMessage += m => OnInnerAdapterNewMessage(a, m);
						return hearbeatAdapter;
					}));

					if (_enabledAdapters.Count == 0)
						throw new InvalidOperationException(LocalizedStrings.Str3650);

					_enabledAdapters.Values.ForEach(a => a.SendInMessage(message));
					break;

				case MessageTypes.Disconnect:
					_adapterStates.Clear();
					_connectedAdapters.Cache.ForEach(a => a.SendInMessage(message));
					break;

				case MessageTypes.SecurityLookup:
					GetMarketDataAdapters().ForEach(a => a.SendInMessage(message));
					break;

				case MessageTypes.OrderStatus:
				case MessageTypes.PortfolioLookup:
					GetTransactionAdapters().ForEach(a => a.SendInMessage(message));
					break;

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;
					var error = ProcessPortfolioMessage(pfMsg.PortfolioName, pfMsg);

					if (error != null)
						SendOutError(error);

					break;
				}

				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderCancel:
				case MessageTypes.OrderGroupCancel:
				{
					var ordMsg = (OrderMessage)message;
					var error = ProcessPortfolioMessage(ordMsg.PortfolioName, ordMsg);

					if (error != null)
					{
						var execMsg = ordMsg.ToExecutionMessage();
						execMsg.Error = error;
						execMsg.OrderState = OrderStates.Failed;
						SendOutMessage(execMsg);
					}

					break;
				}

				case MessageTypes.OrderPairReplace:
				{
					var ordMsg = (OrderPairReplaceMessage)message;
					var error = ProcessPortfolioMessage(ordMsg.Message1.PortfolioName, ordMsg);

					if (error != null)
					{
						var execMsg = ordMsg.ToExecutionMessage();
						execMsg.Error = error;
						execMsg.OrderState = OrderStates.Failed;
						SendOutMessage(execMsg);
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;
					
					switch (mdMsg.DataType)
					{
						case MarketDataTypes.News:
							GetMarketDataAdapters().ForEach(a => a.SendInMessage(mdMsg));
							break;

						case MarketDataTypes.Level1:
						case MarketDataTypes.MarketDepth:
						case MarketDataTypes.Trades:
						case MarketDataTypes.OrderLog:
						case MarketDataTypes.CandleTimeFrame:
						case MarketDataTypes.CandleTick:
						case MarketDataTypes.CandleVolume:
						case MarketDataTypes.CandleRange:
						case MarketDataTypes.CandlePnF:
						case MarketDataTypes.CandleRenko:
						{
							var key = Tuple.Create(mdMsg.SecurityId, mdMsg.DataType);

							if (mdMsg.IsSubscribe)
							{
								if (_subscriptionQueue.ContainsKey(key))
									return;

								var enumerator = GetMarketDataAdapters().ToArray().Cast<IMessageAdapter>().GetEnumerator();

								_subscriptionQueue.Add(key, enumerator);

								ProcessSubscriptionAction(enumerator, mdMsg);
							}
							else
							{
								//lock (_subscriptionQueue.SyncRoot)
								//{
								//	var tuple = _subscriptionQueue.TryGetValue(key);
									
								//	if (tuple != null)
								//	{
								//		tuple.Second = true;
								//		return;
								//	}
								//}

								var adapter = _subscriptions.TryGetValue(key);

								if (adapter != null)
								{
									_subscriptions.Remove(key);
									adapter.SendInMessage(message);
								}
							}

							break;
						}

						default:
							RaiseMarketDataMessage(null, mdMsg.TransactionId, new InvalidOperationException(LocalizedStrings.Str624Params.Put(mdMsg.DataType)));
							break;
					}
					
					break;
				}
			}
		}

		private Exception ProcessPortfolioMessage(string portfolioName, Message message)
		{
			var adapter = Portfolios.TryGetValue(portfolioName);

			if (adapter == null)
				return new InvalidOperationException(LocalizedStrings.Str623Params.Put(portfolioName));

			_enabledAdapters[adapter].SendInMessage(message);
			return null;
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

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Order:
						case ExecutionTypes.Trade:
						{
							if (!execMsg.PortfolioName.IsEmpty())
								SetPortfolioAdapter(execMsg.PortfolioName, innerAdapter);

							break;
						}
					}

					break;
				}

				case MessageTypes.Portfolio:
					SetPortfolioAdapter(((PortfolioMessage)message).PortfolioName, innerAdapter);
					break;

				case MessageTypes.PortfolioChange:
					SetPortfolioAdapter(((PortfolioChangeMessage)message).PortfolioName, innerAdapter);
					break;

				case MessageTypes.Position:
					SetPortfolioAdapter(((PositionMessage)message).PortfolioName, innerAdapter);
					break;

				case MessageTypes.PositionChange:
					SetPortfolioAdapter(((PositionChangeMessage)message).PortfolioName, innerAdapter);
					break;
			}

			SendOutMessage(innerAdapter, message);
		}

		private void SetPortfolioAdapter(string portfolio, IMessageAdapter adapter)
		{
			if (!Portfolios.ContainsKey(portfolio))
				Portfolios[portfolio] = adapter;
		}
		
		private void ProcessConnectMessage(IMessageAdapter innerAdapter, ConnectMessage message)
		{
			if (message.Error != null)
				this.AddErrorLog(LocalizedStrings.Str625Params, innerAdapter.GetType().Name, message.Error);

			var error = message.Error;
			bool canProcess;

			var isConnected = error == null;

			_connectedAdapters.Add(_enabledAdapters[innerAdapter]);

			lock (_adapterStates.SyncRoot)
			{
				_adapterStates[innerAdapter] = RefTuple.Create(isConnected, message.Error);

				if (_adapterStates.Count == _enabledAdapters.Count)
				{
					canProcess = true;

					var errors = _adapterStates.Values.Select(p => p.Second).Where(e => e != null).ToArray();

					if (errors.Length > 0)
						error = new AggregateException(LocalizedStrings.Str626, errors);
				}
				else
				{
					canProcess = isConnected;
				}
			}

			if (canProcess)
				SendOutMessage(innerAdapter, new ConnectMessage { Error = error, LocalTime = message.LocalTime });
		}

		private void ProcessDisconnectMessage(IMessageAdapter innerAdapter, DisconnectMessage message)
		{
			if (message.Error != null)
				this.AddErrorLog(LocalizedStrings.Str627Params, innerAdapter.GetType().Name, message.Error);

			var error = message.Error;
			var canProcess = false;

			lock (_adapterStates.SyncRoot)
			{
				_adapterStates[innerAdapter] = RefTuple.Create(false, message.Error);

				if (_adapterStates.Count == _connectedAdapters.Cache.Length)
				{
					var errors = _adapterStates.Values.Select(p => p.Second).Where(e => e != null).ToArray();

					if (errors.Length > 0)
						error = new AggregateException(LocalizedStrings.Str628, errors);

					canProcess = true;
				}
			}

			if (canProcess)
				SendOutMessage(innerAdapter, new DisconnectMessage { Error = error, LocalTime = message.LocalTime });
		}

		private void ProcessSubscriptionAction(IEnumerator<IMessageAdapter> enumerator, MarketDataMessage message)
		{
			if (enumerator.MoveNext())
				enumerator.Current.SendInMessage(message);
			else
			{
				_subscriptionQueue.RemoveByValue(enumerator);
				RaiseSubscriptionFailed(null, message.TransactionId, new ArgumentException(LocalizedStrings.Str629Params.Put(message.SecurityId), "message"));
			}
		}

		private void ProcessMarketDataMessage(IMessageAdapter adapter, MarketDataMessage message)
		{
			var key = Tuple.Create(message.SecurityId, message.DataType);
			
			var enumerator = _subscriptionQueue.TryGetValue(key);
			//var cancel = tuple != null && tuple.Second;

			if (message.Error == null)
			{
				if (message.IsNotSupported)
				{
					if (enumerator != null)
						ProcessSubscriptionAction(enumerator, message);
					else
						RaiseSubscriptionFailed(adapter, 0, new InvalidOperationException(LocalizedStrings.Str633Params.Put(message.SecurityId, message.DataType)));
				}
				else
				{
					this.AddDebugLog(LocalizedStrings.Str630Params, message.SecurityId, adapter);
					_subscriptionQueue.Remove(key);
					RaiseMarketDataMessage(adapter, 0, null);
				}

				//if (!cancel)
				//	return;

				////в процессе подписки пользователь отменил ее - надо отписаться от получения данных
				//var cancelMessage = (MarketDataMessage)message.Clone();
				//cancelMessage.IsSubscribe = false;
				//adapter.SendInMessage(cancelMessage);
			}
			else
			{
				//this.AddDebugLog(LocalizedStrings.Str631Params, adapter, message.SecurityId, message.DataType, message.Error);
				_subscriptionQueue.Remove(key);
				RaiseSubscriptionFailed(adapter, 0, message.Error);
			}
		}

		private void RaiseMarketDataMessage(IMessageAdapter adapter, long originalTransactionId, Exception error)
		{
			SendOutMessage(adapter, new MarketDataMessage
			{
				OriginalTransactionId = originalTransactionId,
				Error = error
			});
		}

		private void RaiseSubscriptionFailed(IMessageAdapter adapter, long originalTransactionId, Exception error)
		{
			//_subscriptionQueue.Remove(Tuple.Create(message.SecurityId, message.DataType));
			//this.AddDebugLog(LocalizedStrings.Str634Params, message.SecurityId, message.DataType, error);
			RaiseMarketDataMessage(adapter, originalTransactionId, error);
		}

		private void SendOutMessage(IMessageAdapter adapter, Message message)
		{
			if (message.LocalTime.IsDefault())
				message.LocalTime = adapter.CurrentTime.LocalDateTime;

			SendOutMessage(new BasketMessage(message, adapter));
		}

		/// <summary>
		/// Получить адаптеры <see cref="IInnerAdapterList.SortedAdapters"/>, отсортированные в зависимости от заданного приоритета. По-умолчанию сортировка отсутствует.
		/// </summary>
		/// <returns>Отсортированные адаптеры.</returns>
		protected IEnumerable<IMessageAdapter> GetSortedAdapters()
		{
			return _innerAdapters.SortedAdapters;
		}

		private IEnumerable<IMessageAdapter> GetTransactionAdapters()
		{
			return _connectedAdapters.Cache.Where(a => a.IsTransactionEnabled);
		}

		private IEnumerable<IMessageAdapter> GetMarketDataAdapters()
		{
			return _connectedAdapters.Cache.Where(a => a.IsMarketDataEnabled);
		}
	}
}