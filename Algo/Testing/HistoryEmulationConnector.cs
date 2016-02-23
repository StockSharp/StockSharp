#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: HistoryEmulationConnector.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Candles;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The emulational connection. It uses historic data and/or occasionally generated.
	/// </summary>
	public class HistoryEmulationConnector : BaseEmulationConnector, IExternalCandleSource
	{
		private class EmulationEntityFactory : EntityFactory
		{
			private readonly ISecurityProvider _securityProvider;
			private readonly IDictionary<string, Portfolio> _portfolios;

			public EmulationEntityFactory(ISecurityProvider securityProvider, IEnumerable<Portfolio> portfolios)
			{
				_securityProvider = securityProvider;
				_portfolios = portfolios.ToDictionary(p => p.Name, p => p, StringComparer.InvariantCultureIgnoreCase);
			}

			public override Security CreateSecurity(string id)
			{
				return _securityProvider.LookupById(id) ?? base.CreateSecurity(id);
			}

			public override Portfolio CreatePortfolio(string name)
			{
				return _portfolios.TryGetValue(name) ?? base.CreatePortfolio(name);
			}
		}

		private class HistoryBasketMessageAdapter : BasketMessageAdapter
		{
			private readonly HistoryEmulationConnector _parent;

			public HistoryBasketMessageAdapter(HistoryEmulationConnector parent)
				: base(parent.TransactionIdGenerator)
			{
				_parent = parent;
			}

			public override DateTimeOffset CurrentTime => _parent.CurrentTime;
		}

		private sealed class HistoryEmulationMessageChannel : Cloneable<IMessageChannel>, IMessageChannel
		{
			private class BlockingPriorityQueue : BaseBlockingQueue<KeyValuePair<DateTimeOffset, Message>, OrderedPriorityQueue<DateTimeOffset, Message>>
			{
				public BlockingPriorityQueue()
					: base(new OrderedPriorityQueue<DateTimeOffset, Message>())
				{
				}

				protected override void OnEnqueue(KeyValuePair<DateTimeOffset, Message> item, bool force)
				{
					InnerCollection.Enqueue(item.Key, item.Value);
				}

				protected override KeyValuePair<DateTimeOffset, Message> OnDequeue()
				{
					return InnerCollection.Dequeue();
				}

				protected override KeyValuePair<DateTimeOffset, Message> OnPeek()
				{
					return InnerCollection.Peek();
				}
			}

			private readonly BlockingPriorityQueue _messageQueue = new BlockingPriorityQueue();

			private readonly HistoryMessageAdapter _historyMessageAdapter;
			private readonly Action<Exception> _errorHandler;

			public HistoryEmulationMessageChannel(HistoryMessageAdapter historyMessageAdapter, Action<Exception> errorHandler)
			{
				if (historyMessageAdapter == null)
					throw new ArgumentNullException(nameof(historyMessageAdapter));

				if (errorHandler == null)
					throw new ArgumentNullException(nameof(errorHandler));

				_historyMessageAdapter = historyMessageAdapter;
				_errorHandler = errorHandler;

				_messageQueue.Close();
			}

			public event Action<Message> NewOutMessage;

			public bool IsOpened => !_messageQueue.IsClosed;

			public void Open()
			{
				_messageQueue.Open();

				ThreadingHelper
					.Thread(() => CultureInfo.InvariantCulture.DoInCulture(() =>
					{
						while (!_messageQueue.IsClosed)
						{
							try
							{
								var sended = _historyMessageAdapter.SendOutMessage();

								KeyValuePair<DateTimeOffset, Message> pair;

								if (!_messageQueue.TryDequeue(out pair, true, !sended))
								{
									if (!sended)
										break;
								}
								else
									NewOutMessage.SafeInvoke(pair.Value);
							}
							catch (Exception ex)
							{
								_errorHandler(ex);
							}
						}
					}))
					.Name("History emulation channel thread.")
					.Launch();
			}

			public void Close()
			{
				_messageQueue.Close();
			}

			public void SendInMessage(Message message)
			{
				if (!IsOpened)
					Open();

				_messageQueue.Enqueue(new KeyValuePair<DateTimeOffset, Message>(message.LocalTime, message));
			}

			void IDisposable.Dispose()
			{
				Close();
			}

			public override IMessageChannel Clone()
			{
				return new HistoryEmulationMessageChannel(_historyMessageAdapter, _errorHandler);
			}
		}

		private readonly CachedSynchronizedDictionary<Tuple<SecurityId, MarketDataTypes, object>, int> _subscribedCandles = new CachedSynchronizedDictionary<Tuple<SecurityId, MarketDataTypes, object>, int>();
		private readonly CachedSynchronizedDictionary<Tuple<SecurityId, MarketDataTypes, object>, int> _historySourceSubscriptions = new CachedSynchronizedDictionary<Tuple<SecurityId, MarketDataTypes, object>, int>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, MarketDataTypes, object>, CandleSeries> _series = new SynchronizedDictionary<Tuple<SecurityId, MarketDataTypes, object>, CandleSeries>();
		
		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
		/// </summary>
		/// <param name="securities">Instruments, which will be sent through the <see cref="IConnector.NewSecurities"/> event.</param>
		/// <param name="portfolios">Portfolios, which will be sent through the <see cref="IConnector.NewPortfolios"/> event.</param>
		public HistoryEmulationConnector(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios)
			: this(securities, portfolios, new StorageRegistry())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
		/// </summary>
		/// <param name="securities">Instruments, the operation will be performed with.</param>
		/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		public HistoryEmulationConnector(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
			: this((ISecurityProvider)new CollectionSecurityProvider(securities), portfolios, storageRegistry)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		public HistoryEmulationConnector(ISecurityProvider securityProvider, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
		{
			if (securityProvider == null)
				throw new ArgumentNullException(nameof(securityProvider));

			if (portfolios == null)
				throw new ArgumentNullException(nameof(portfolios));

			if (storageRegistry == null)
				throw new ArgumentNullException(nameof(storageRegistry));

			// чтобы каждый раз при повторной эмуляции получать одинаковые номера транзакций
			TransactionIdGenerator = new IncrementalIdGenerator();

			_initialMoney = portfolios.ToDictionary(pf => pf, pf => pf.BeginValue);
			EntityFactory = new EmulationEntityFactory(securityProvider, _initialMoney.Keys);

			LatencyManager = null;
			RiskManager = null;
			CommissionManager = null;
			PnLManager = null;
			SlippageManager = null;

			HistoryMessageAdapter = new HistoryMessageAdapter(TransactionIdGenerator, securityProvider) { StorageRegistry = storageRegistry };

			InMessageChannel = new HistoryEmulationMessageChannel(HistoryMessageAdapter, SendOutError);
			OutMessageChannel = new PassThroughMessageChannel();

			Adapter = new HistoryBasketMessageAdapter(this);
			Adapter.InnerAdapters.Add(EmulationAdapter);
			Adapter.InnerAdapters.Add(HistoryMessageAdapter);

			// при тестировании по свечкам, время меняется быстрее и таймаут должен быть больше 30с.
			ReConnectionSettings.TimeOutInterval = TimeSpan.MaxValue;

			//MaxMessageCount = 1000;

			TradesKeepCount = 0;
		}

		/// <summary>
		/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
		/// </summary>
		public HistoryMessageAdapter HistoryMessageAdapter { get; }

		/// <summary>
		/// The maximal size of the message queue, up to which history data are red. By default, it is equal to 1000.
		/// </summary>
		public int MaxMessageCount
		{
			get { return HistoryMessageAdapter.MaxMessageCount; }
			set { HistoryMessageAdapter.MaxMessageCount = value; }
		}

		private readonly Dictionary<Portfolio, decimal> _initialMoney;

		/// <summary>
		/// The initial size of monetary funds on accounts.
		/// </summary>
		public IDictionary<Portfolio, decimal> InitialMoney => _initialMoney;

		/// <summary>
		/// The number of loaded messages.
		/// </summary>
		public int LoadedMessageCount => HistoryMessageAdapter.LoadedMessageCount;

		/// <summary>
		/// The number of processed messages.
		/// </summary>
		public int ProcessedMessageCount => EmulationAdapter.ProcessedMessageCount;

		private EmulationStates _state = EmulationStates.Stopped;

		/// <summary>
		/// The emulator state.
		/// </summary>
		public EmulationStates State
		{
			get { return _state; }
			private set
			{
				if (_state == value)
					return;

				bool throwError;

				switch (value)
				{
					case EmulationStates.Stopped:
						throwError = (_state != EmulationStates.Stopping);
						break;
					case EmulationStates.Stopping:
						throwError = (_state != EmulationStates.Started && _state != EmulationStates.Suspended
							&& State == EmulationStates.Starting);  // при ошибках при запуске эмуляции состояние может быть Starting
						break;
					case EmulationStates.Starting:
						throwError = (_state != EmulationStates.Stopped && _state != EmulationStates.Suspended);
						break;
					case EmulationStates.Started:
						throwError = (_state != EmulationStates.Starting);
						break;
					case EmulationStates.Suspending:
						throwError = (_state != EmulationStates.Started);
						break;
					case EmulationStates.Suspended:
						throwError = (_state != EmulationStates.Suspending);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(value));
				}

				if (throwError)
					throw new InvalidOperationException(LocalizedStrings.Str2189Params.Put(_state, value));

				_state = value;

				try
				{
					StateChanged.SafeInvoke();
				}
				catch (Exception ex)
				{
					SendOutError(ex);
				}
			}
		}

		/// <summary>
		/// The event on the emulator state change <see cref="State"/>.
		/// </summary>
		public event Action StateChanged;

		/// <summary>
		/// Has the emulator ended its operation due to end of data, or it was interrupted through the <see cref="IConnector.Disconnect"/>method.
		/// </summary>
		public bool IsFinished { get; private set; }

		/// <summary>
		/// To enable the possibility to give out candles directly into <see cref="ICandleManager"/>. It accelerates operation, but candle change events will not be available. By default it is disabled.
		/// </summary>
		public bool UseExternalCandleSource { get; set; }

		/// <summary>
		/// Clear cache.
		/// </summary>
		public override void ClearCache()
		{
			base.ClearCache();
			IsFinished = false;
		}

		/// <summary>
		/// To call the <see cref="Connector.Connected"/> event when the first adapter connects to <see cref="Connector.Adapter"/>.
		/// </summary>
		protected override bool RaiseConnectedOnFirstAdapter => false;

		/// <summary>
		/// Disconnect from trading system.
		/// </summary>
		protected override void OnDisconnect()
		{
			SendEmulationState(EmulationStates.Stopping);
		}

		/// <summary>
		/// To release allocated resources. In particular, to disconnect from the trading system via <see cref="Connector.Disconnect"/>.
		/// </summary>
		protected override void DisposeManaged()
		{
			base.DisposeManaged();

			HistoryMessageAdapter.Dispose();
		}

		/// <summary>
		/// To start the emulation.
		/// </summary>
		public void Start()
		{
			SendEmulationState(EmulationStates.Starting);
		}

		/// <summary>
		/// To suspend the emulation.
		/// </summary>
		public void Suspend()
		{
			SendEmulationState(EmulationStates.Suspending);
		}

		private void SendEmulationState(EmulationStates state)
		{
			SendInMessage(new EmulationStateMessage { State = state });
		}

		/// <summary>
		/// To process the message, containing market data.
		/// </summary>
		/// <param name="message">The message, containing market data.</param>
		protected override void OnProcessMessage(Message message)
		{
			try
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					{
						base.OnProcessMessage(message);

						if (message.Adapter == TransactionAdapter)
							_initialMoney.ForEach(p => SendPortfolio(p.Key));

						break;
					}

					case ExtendedMessageTypes.Last:
					{
						var lastMsg = (LastMessage)message;

						if (State == EmulationStates.Started)
						{
							IsFinished = !lastMsg.IsError;

							// все данных пришли без ошибок или в процессе чтения произошла ошибка - начинаем остановку
							SendEmulationState(EmulationStates.Stopping);
							SendEmulationState(EmulationStates.Stopped);
						}

						if (State == EmulationStates.Stopping)
						{
							// тестирование было отменено и пришли все ранее прочитанные данные
							SendEmulationState(EmulationStates.Stopped);
						}

						break;
					}

					case ExtendedMessageTypes.Clearing:
						break;

					case ExtendedMessageTypes.EmulationState:
						ProcessEmulationStateMessage(((EmulationStateMessage)message).State);
						break;

					case MessageTypes.Security:
					case MessageTypes.Board:
					case MessageTypes.Level1Change:
					case MessageTypes.QuoteChange:
					case MessageTypes.Time:
					case MessageTypes.CandlePnF:
					case MessageTypes.CandleRange:
					case MessageTypes.CandleRenko:
					case MessageTypes.CandleTick:
					case MessageTypes.CandleTimeFrame:
					case MessageTypes.CandleVolume:
					case MessageTypes.Execution:
					{
						if (message.Adapter == MarketDataAdapter)
							TransactionAdapter.SendInMessage(message);
						else if (message.Adapter == TransactionAdapter)
						{
							var candleMsg = message as CandleMessage;

							if (candleMsg != null)
							{
								if (!UseExternalCandleSource)
									break;

								var series = _series.TryGetValue(Tuple.Create(candleMsg.SecurityId, candleMsg.Type.ToCandleMarketDataType(), candleMsg.Arg));

								if (series != null)
								{
									_newCandles.SafeInvoke(series, new[] { candleMsg.ToCandle(series) });

									if (candleMsg.IsFinished)
										_stopped.SafeInvoke(series);
								}

								break;
							}
						}

						base.OnProcessMessage(message);
						break;
					}

					default:
					{
						if (State == EmulationStates.Stopping && message.Type != MessageTypes.Disconnect)
							break;

						base.OnProcessMessage(message);
						break;
					}
				}
			}
			catch (Exception ex)
			{
				SendOutError(ex);
				SendEmulationState(EmulationStates.Stopping);
			}
		}

		private void ProcessEmulationStateMessage(EmulationStates newState)
		{
			this.AddInfoLog(LocalizedStrings.Str1121Params, State, newState);

			State = newState;

			switch (newState)
			{
				case EmulationStates.Stopping:
				{
					SendInMessage(new DisconnectMessage());
					break;
				}

				case EmulationStates.Starting:
				{
					SendEmulationState(EmulationStates.Started);
					break;
				}
			}
		}

		private void SendPortfolio(Portfolio portfolio)
		{
			SendInMessage(portfolio.ToMessage());

			var money = _initialMoney[portfolio];

			SendInMessage(
				EmulationAdapter
					.CreatePortfolioChangeMessage(portfolio.Name)
						.Add(PositionChangeTypes.BeginValue, money)
						.Add(PositionChangeTypes.CurrentValue, money)
						.Add(PositionChangeTypes.BlockedValue, 0m));
		}

		//private void InitOrderLogBuilders(DateTime loadDate)
		//{
		//	if (StorageRegistry == null || !MarketEmulator.Settings.UseMarketDepth)
		//		return;

		//	foreach (var security in RegisteredMarketDepths)
		//	{
		//		var builder = _orderLogBuilders.TryGetValue(security);

		//		if (builder == null)
		//			continue;

		//		// стакан из ОЛ строиться начиная с 18.45 предыдущей торговой сессии
		//		var olDate = loadDate.Date;

		//		do
		//		{
		//			olDate -= TimeSpan.FromDays(1);
		//		}
		//		while (!ExchangeBoard.Forts.WorkingTime.IsTradeDate(olDate));

		//		olDate += new TimeSpan(18, 45, 0);

		//		foreach (var item in StorageRegistry.GetOrderLogStorage(security, Drive).Load(olDate, loadDate - TimeSpan.FromTicks(1)))
		//		{
		//			builder.Update(item);
		//		}
		//	}
		//}

		///// <summary>
		///// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		///// </summary>
		///// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		///// <returns>Найденные инструменты.</returns>
		//public override IEnumerable<Security> Lookup(Security criteria)
		//{
		//	var securities = _historyAdapter.SecurityProvider.Lookup(criteria);

		//	if (State == EmulationStates.Started)
		//	{
		//		foreach (var security in securities)
		//			SendSecurity(security);	
		//	}

		//	return securities;
		//}

		/// <summary>
		/// Subscribe on the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for subscription.</param>
		protected override void OnRegisterPortfolio(Portfolio portfolio)
		{
			_initialMoney.TryAdd(portfolio, portfolio.BeginValue);

			if (State == EmulationStates.Started)
				SendPortfolio(portfolio);
		}

		/// <summary>
		/// Зарегистрировать исторические данные.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="dataType">Тип данных.</param>
		/// <param name="arg">Параметр, ассоциированный с типом <paramref name="dataType"/>. Например, <see cref="Candle.Arg"/>.</param>
		/// <param name="getMessages">Функция получения исторических данных.</param>
		public void RegisterHistorySource(Security security, MarketDataTypes dataType, object arg, Func<DateTimeOffset, IEnumerable<Message>> getMessages)
		{
			SendInHistorySourceMessage(security, dataType, arg, getMessages);
		}
		
		/// <summary>
		/// Удалить регистрацию, ранее осуществленную через <see cref="RegisterHistorySource"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="dataType">Тип данных.</param>
		/// <param name="arg">Параметр, ассоциированный с типом <paramref name="dataType"/>. Например, <see cref="Candle.Arg"/>.</param>
		public void UnRegisterHistorySource(Security security, MarketDataTypes dataType, object arg)
		{
			SendInHistorySourceMessage(security, dataType, arg, null);
		}

		private void SendInHistorySourceMessage(Security security, MarketDataTypes dataType, object arg, Func<DateTimeOffset, IEnumerable<Message>> getMessages)
		{
			var isSubscribe = getMessages != null;

			if (isSubscribe)
			{
				if (_historySourceSubscriptions.ChangeSubscribers(Tuple.Create(security.ToSecurityId(), dataType, arg), true) != 1)
					return;
			}
			else
			{
				if (_historySourceSubscriptions.ChangeSubscribers(Tuple.Create(security.ToSecurityId(), dataType, arg), false) != 0)
					return;
			}

			SendInMessage(new HistorySourceMessage
			{
				IsSubscribe = isSubscribe,
				SecurityId = security.ToSecurityId(),
				DataType = dataType,
				Arg = arg,
				GetMessages = getMessages
			});
		}

		IEnumerable<Range<DateTimeOffset>> IExternalCandleSource.GetSupportedRanges(CandleSeries series)
		{
			if (!UseExternalCandleSource)
				yield break;

			var securityId = series.Security.ToSecurityId();
			var messageType = series.CandleType.ToCandleMessageType();
			var dataType = messageType.ToCandleMarketDataType();

			if (_historySourceSubscriptions.ContainsKey(Tuple.Create(securityId, dataType, series.Arg)))
			{
				yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
				yield break;
			}

			var types = HistoryMessageAdapter.Drive.GetAvailableDataTypes(securityId, HistoryMessageAdapter.StorageFormat);

			foreach (var tuple in types)
			{
				if (tuple.MessageType != messageType || !tuple.Arg.Equals(series.Arg))
					continue;

				var dates = HistoryMessageAdapter.StorageRegistry.GetCandleMessageStorage(tuple.MessageType, series.Security, series.Arg, HistoryMessageAdapter.Drive, HistoryMessageAdapter.StorageFormat).Dates.ToArray();

				if (dates.Any())
					yield return new Range<DateTimeOffset>(dates.First().ApplyTimeZone(TimeZoneInfo.Utc), dates.Last().ApplyTimeZone(TimeZoneInfo.Utc));

				break;
			}
		}

		private Action<CandleSeries, IEnumerable<Candle>> _newCandles;

		event Action<CandleSeries, IEnumerable<Candle>> IExternalCandleSource.NewCandles
		{
			add { _newCandles += value; }
			remove { _newCandles -= value; }
		}

		private Action<CandleSeries> _stopped;

		event Action<CandleSeries> IExternalCandleSource.Stopped
		{
			add { _stopped += value; }
			remove { _stopped -= value; }
		}

		void IExternalCandleSource.SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			var securityId = GetSecurityId(series.Security);
			var dataType = series.CandleType.ToCandleMessageType().ToCandleMarketDataType();
			var key = Tuple.Create(securityId, dataType, series.Arg);

			if (!_historySourceSubscriptions.ContainsKey(key))
			{
				if (_subscribedCandles.ChangeSubscribers(key, true) != 1)
					return;

				MarketDataAdapter.SendInMessage(new MarketDataMessage
				{
					//SecurityId = securityId,
					DataType = dataType,
					Arg = series.Arg,
					IsSubscribe = true,
				}.FillSecurityInfo(this, series.Security));
			}

			_series.Add(key, series);
		}

		void IExternalCandleSource.UnSubscribeCandles(CandleSeries series)
		{
			var securityId = GetSecurityId(series.Security);
			var dataType = series.CandleType.ToCandleMessageType().ToCandleMarketDataType();
			var key = Tuple.Create(securityId, dataType, series.Arg);

			if (!_historySourceSubscriptions.ContainsKey(key))
			{
				if (_subscribedCandles.ChangeSubscribers(key, false) != 0)
					return;

				MarketDataAdapter.SendInMessage(new MarketDataMessage
				{
					//SecurityId = securityId,
					DataType = MarketDataTypes.CandleTimeFrame,
					Arg = series.Arg,
					IsSubscribe = false,
				}.FillSecurityInfo(this, series.Security));
			}

			_series.Remove(key);
		}
	}
}