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
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo.Storages;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The emulation connection. It uses historical data and/or occasionally generated.
	/// </summary>
	public class HistoryEmulationConnector : BaseEmulationConnector
	{
		private class EmulationEntityFactory : EntityFactory
		{
			private readonly ISecurityProvider _securityProvider;
			private readonly IDictionary<string, Portfolio> _portfolios;

			public EmulationEntityFactory(ISecurityProvider securityProvider, IEnumerable<Portfolio> portfolios)
			{
				_securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
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
				: base(parent.TransactionIdGenerator, new InMemoryMessageAdapterProvider(), new CandleBuilderProvider(new InMemoryExchangeInfoProvider()))
			{
				_parent = parent;
			}

			public override DateTimeOffset CurrentTime => _parent.CurrentTime;

			protected override void OnInnerAdapterNewOutMessage(IMessageAdapter innerAdapter, Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Security:
					case MessageTypes.Board:
					case MessageTypes.Level1Change:
					case MessageTypes.QuoteChange:
					case MessageTypes.Time:
					case MessageTypes.Execution:
					{
						if (message.Adapter == _parent.MarketDataAdapter)
							_parent.TransactionAdapter.SendInMessage(message);

						break;
					}

					case MessageTypes.CandlePnF:
					case MessageTypes.CandleRange:
					case MessageTypes.CandleRenko:
					case MessageTypes.CandleTick:
					case MessageTypes.CandleTimeFrame:
					case MessageTypes.CandleVolume:
					{
						if (message.Adapter != _parent.MarketDataAdapter)
							break;

						_parent.TransactionAdapter.SendInMessage(message);
						return;
					}
				}

				base.OnInnerAdapterNewOutMessage(innerAdapter, message);
			}
		}

		private sealed class HistoryEmulationMessageChannel : Cloneable<IMessageChannel>, IMessageChannel
		{
			private readonly MessagePriorityQueue _messageQueue = new MessagePriorityQueue();

			private readonly HistoryMessageAdapter _historyMessageAdapter;
			private readonly Action<Exception> _errorHandler;

			public HistoryEmulationMessageChannel(HistoryMessageAdapter historyMessageAdapter, Action<Exception> errorHandler)
			{
				_historyMessageAdapter = historyMessageAdapter ?? throw new ArgumentNullException(nameof(historyMessageAdapter));
				_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

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
								var processed = false;
								
								while (_messageQueue.TryDequeue(out var message, true, false))
								{
									NewOutMessage?.Invoke(message);
									processed = true;
								}

								if (!sended && !processed && !_messageQueue.IsClosed)
									Thread.Sleep(100);
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

				_messageQueue.Enqueue(message);
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
			
			RiskManager = null;

			SupportSubscriptionTracking = true;
			SupportBasketSecurities = true;

			HistoryMessageAdapter = new HistoryMessageAdapter(TransactionIdGenerator, securityProvider) { StorageRegistry = storageRegistry };

			InMessageChannel = new HistoryEmulationMessageChannel(HistoryMessageAdapter, SendOutError);
			OutMessageChannel = new PassThroughMessageChannel();

			Adapter = new HistoryBasketMessageAdapter(this);
			Adapter.InnerAdapters.Add(EmulationAdapter);
			Adapter.InnerAdapters.Add(HistoryMessageAdapter);

			Adapter.LatencyManager = null;
			Adapter.CommissionManager = null;
			Adapter.PnLManager = null;
			Adapter.SlippageManager = null;

			// при тестировании по свечкам, время меняется быстрее и таймаут должен быть больше 30с.
			ReConnectionSettings.TimeOutInterval = TimeSpan.MaxValue;

			//MaxMessageCount = 1000;

			TradesKeepCount = 0;

			RaiseConnectedOnFirstAdapter = false;
		}

		/// <summary>
		/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
		/// </summary>
		public HistoryMessageAdapter HistoryMessageAdapter { get; }

		/// <summary>
		/// The maximal size of the message queue, up to which history data are read. By default, it is equal to <see cref="Testing.HistoryMessageAdapter.DefaultMaxMessageCount"/>.
		/// </summary>
		public int MaxMessageCount
		{
			get => HistoryMessageAdapter.MaxMessageCount;
			set => HistoryMessageAdapter.MaxMessageCount = value;
		}

		private readonly Dictionary<Portfolio, decimal?> _initialMoney;

		/// <summary>
		/// The initial size of monetary funds on accounts.
		/// </summary>
		public IDictionary<Portfolio, decimal?> InitialMoney => _initialMoney;

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
			get => _state;
			private set
			{
				if (_state == value)
					return;

				bool throwError;

				switch (value)
				{
					case EmulationStates.Stopped:
						throwError = _state != EmulationStates.Stopping;
						break;
					case EmulationStates.Stopping:
						throwError = _state != EmulationStates.Started && _state != EmulationStates.Suspended
							&& State != EmulationStates.Starting;  // при ошибках при запуске эмуляции состояние может быть Starting
						break;
					case EmulationStates.Starting:
						throwError = _state != EmulationStates.Stopped && _state != EmulationStates.Suspended;
						break;
					case EmulationStates.Started:
						throwError = _state != EmulationStates.Starting;
						break;
					case EmulationStates.Suspending:
						throwError = _state != EmulationStates.Started;
						break;
					case EmulationStates.Suspended:
						throwError = _state != EmulationStates.Suspending;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);
				}

				if (throwError)
					throw new InvalidOperationException(LocalizedStrings.Str2189Params.Put(_state, value));

				_state = value;

				try
				{
					StateChanged?.Invoke();
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

		/// <inheritdoc />
		public override TimeSpan MarketTimeChangedInterval
		{
			get => HistoryMessageAdapter.MarketTimeChangedInterval;
			set => HistoryMessageAdapter.MarketTimeChangedInterval = value;
		}

		/// <summary>
		/// Clear cache.
		/// </summary>
		public override void ClearCache()
		{
			base.ClearCache();

			//_series.Clear();
			//_subscribedCandles.Clear();

			IsFinished = false;
		}

		/// <summary>
		/// Disconnect from trading system.
		/// </summary>
		protected override void OnDisconnect()
		{
			if (State != EmulationStates.Stopped && State != EmulationStates.Stopping)
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
				Disconnect();
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
					SendEmulationState(EmulationStates.Stopped);
					break;
				}

				case EmulationStates.Stopped:
				{
					// change ConnectionState to Disconnecting
					if (ConnectionState != ConnectionStates.Disconnecting)
						Disconnect();

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
						.TryAdd(PositionChangeTypes.BeginValue, money, true)
						.TryAdd(PositionChangeTypes.CurrentValue, money, true)
						.Add(PositionChangeTypes.BlockedValue, 0m));
		}

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
		/// Register historical data source.
		/// </summary>
		/// <param name="security">Instrument. If passed <see langword="null"/> the source will be applied for all subscriptions.</param>
		/// <param name="dataType">Data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType"/> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="getMessages">Historical data source.</param>
		public void RegisterHistorySource(Security security, MarketDataTypes dataType, object arg, Func<DateTimeOffset, IEnumerable<Message>> getMessages)
		{
			SendInHistorySourceMessage(security, dataType, arg, getMessages);
		}

		/// <summary>
		/// Unregister historical data source, previously registeted by <see cref="RegisterHistorySource"/>.
		/// </summary>
		/// <param name="security">Instrument. If passed <see langword="null"/> the source will be removed for all subscriptions.</param>
		/// <param name="dataType">Data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType"/> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		public void UnRegisterHistorySource(Security security, MarketDataTypes dataType, object arg)
		{
			SendInHistorySourceMessage(security, dataType, arg, null);
		}

		private void SendInHistorySourceMessage(Security security, MarketDataTypes dataType, object arg, Func<DateTimeOffset, IEnumerable<Message>> getMessages)
		{
			SendInMessage(new HistorySourceMessage
			{
				IsSubscribe = getMessages != null,
				SecurityId = security?.ToSecurityId() ?? default(SecurityId),
				DataType = dataType,
				Arg = arg,
				GetMessages = getMessages
			});
		}
	}
}