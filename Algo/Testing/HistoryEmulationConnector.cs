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
				: base(parent.TransactionIdGenerator, new CandleBuilderProvider(new InMemoryExchangeInfoProvider()))
			{
				_parent = parent;
			}

			protected override bool CanAutoStorage => false;

			public override DateTimeOffset CurrentTime => _parent.CurrentTime;

			protected override bool OnSendInMessage(Message message)
			{
				if (!message.IsBack && message.Type == MessageTypes.MarketData)
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						if (mdMsg.From == null)
							mdMsg.From = _parent.HistoryMessageAdapterEx.StartDate;

						if (mdMsg.To == null)
							mdMsg.To = _parent.HistoryMessageAdapterEx.StopDate;
					}
				}

				return base.OnSendInMessage(message);
			}

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

					default:
					{
						if (message is CandleMessage)
						{
							if (message.Adapter != _parent.MarketDataAdapter)
								break;

							_parent.TransactionAdapter.SendInMessage(message);
							return;
						}

						break;
					}
				}

				base.OnInnerAdapterNewOutMessage(innerAdapter, message);
			}
		}

		private sealed class HistoryEmulationMessageChannel : Cloneable<IMessageChannel>, IMessageChannel
		{
			private readonly HistoryEmulationConnector _parent;
			private readonly MessageByLocalTimeQueue _messageQueue;

			public HistoryEmulationMessageChannel(HistoryEmulationConnector parent)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));

				_messageQueue = new MessageByLocalTimeQueue();
				_messageQueue.Close();
			}

			public event Action<Message> NewOutMessage;

			public bool IsOpened => !_messageQueue.IsClosed;

			public event Action StateChanged;

			public void Open()
			{
				_messageQueue.Open();
				StateChanged?.Invoke();

				var histAdapter = _parent.HistoryMessageAdapterEx;

				ThreadingHelper
					.Thread(() => CultureInfo.InvariantCulture.DoInCulture(() =>
					{
						while (!_messageQueue.IsClosed)
						{
							try
							{
								var sended = histAdapter.SendOutMessage();
								var processed = false;
								
								while (_messageQueue.TryDequeue(out var message, true, false))
								{
									NewOutMessage?.Invoke(message);
									processed = true;
								}

								if (!sended && !processed && !_messageQueue.IsClosed)
									Thread.Sleep(1000);
							}
							catch (Exception ex)
							{
								_parent.SendOutError(ex);
							}
						}

						StateChanged?.Invoke();
					}))
					.Name("History emulation channel thread.")
					.Launch();
			}

			public void Close()
			{
				_messageQueue.Close();
			}

			public bool SendInMessage(Message message)
			{
				if (!IsOpened)
					Open();

				_messageQueue.Enqueue(message);
				return true;
			}

			void IDisposable.Dispose()
			{
				Close();
			}

			public override IMessageChannel Clone()
			{
				return new HistoryEmulationMessageChannel(_parent);
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
		public HistoryEmulationConnector(ISecurityProvider securityProvider, IEnumerable<Portfolio> portfolios)
			: this(securityProvider, portfolios, new StorageRegistry())
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

			EntityFactory = new EmulationEntityFactory(securityProvider, portfolios);
			
			RiskManager = null;

			SupportBasketSecurities = true;

			InMessageChannel = new HistoryEmulationMessageChannel(this);
			OutMessageChannel = new PassThroughMessageChannel();

			Adapter = new HistoryBasketMessageAdapter(this);
			Adapter.InnerAdapters.Add(EmulationAdapter);
			Adapter.InnerAdapters.Add(new HistoryMessageAdapter(TransactionIdGenerator, securityProvider) { StorageRegistry = storageRegistry });

			Adapter.LatencyManager = null;
			Adapter.CommissionManager = null;
			Adapter.PnLManager = null;
			Adapter.SlippageManager = null;

			// при тестировании по свечкам, время меняется быстрее и таймаут должен быть больше 30с.
			//ReConnectionSettings.TimeOutInterval = TimeSpan.MaxValue;

			//MaxMessageCount = 1000;

			TradesKeepCount = 0;

			Adapter.SupportCandlesCompression = false;
			Adapter.SupportBuildingFromOrderLog = false;
			Adapter.SupportPartialDownload = false;
			Adapter.SupportLookupTracking = false;
			Adapter.SupportOrderBookTruncate = false;
			Adapter.ConnectDisconnectEventOnFirstAdapter = false;
		}

		/// <summary>
		/// Historical message adapter.
		/// </summary>
		public IHistoryMessageAdapter HistoryMessageAdapterEx => (IHistoryMessageAdapter)MarketDataAdapter;

		/// <summary>
		/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
		/// </summary>
		public HistoryMessageAdapter HistoryMessageAdapter => (HistoryMessageAdapter)MarketDataAdapter;

		///// <summary>
		///// The maximal size of the message queue, up to which history data are read. By default, it is equal to <see cref="Testing.HistoryMessageAdapter.DefaultMaxMessageCount"/>.
		///// </summary>
		//public int MaxMessageCount
		//{
		//	get => HistoryMessageAdapter.MaxMessageCount;
		//	set => HistoryMessageAdapter.MaxMessageCount = value;
		//}

		///// <summary>
		///// The number of loaded messages.
		///// </summary>
		//public int LoadedMessageCount => HistoryMessageAdapter.LoadedMessageCount;

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
			get => HistoryMessageAdapterEx.MarketTimeChangedInterval;
			set => HistoryMessageAdapterEx.MarketTimeChangedInterval = value;
		}

		/// <inheritdoc />
		public override void ClearCache()
		{
			base.ClearCache();

			//_series.Clear();
			//_subscribedCandles.Clear();

			IsFinished = false;
		}

		/// <inheritdoc />
		protected override void OnDisconnect()
		{
			if (State != EmulationStates.Stopped && State != EmulationStates.Stopping)
				SendEmulationState(EmulationStates.Stopping);
		}

		/// <inheritdoc />
		protected override void DisposeManaged()
		{
			base.DisposeManaged();

			MarketDataAdapter.DoDispose();
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

		/// <inheritdoc />
		protected override void OnProcessMessage(Message message)
		{
			try
			{
				switch (message.Type)
				{
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

		/// <summary>
		/// Register historical data source.
		/// </summary>
		/// <param name="security">Instrument. If passed <see langword="null"/> the source will be applied for all subscriptions.</param>
		/// <param name="dataType">Data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType"/> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="getMessages">Historical data source.</param>
		[Obsolete("Uses custom adapter implementation.")]
		public void RegisterHistorySource(Security security, MarketDataTypes dataType, object arg, Func<DateTimeOffset, IEnumerable<Message>> getMessages)
		{
			SendInHistorySourceMessage(security, dataType, arg, getMessages);
		}

		/// <summary>
		/// Unregister historical data source, previously registered by <see cref="RegisterHistorySource"/>.
		/// </summary>
		/// <param name="security">Instrument. If passed <see langword="null"/> the source will be removed for all subscriptions.</param>
		/// <param name="dataType">Data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType"/> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		[Obsolete]
		public void UnRegisterHistorySource(Security security, MarketDataTypes dataType, object arg)
		{
			SendInHistorySourceMessage(security, dataType, arg, null);
		}

		private void SendInHistorySourceMessage(Security security, MarketDataTypes dataType, object arg, Func<DateTimeOffset, IEnumerable<Message>> getMessages)
		{
			SendInMessage(new HistorySourceMessage
			{
				IsSubscribe = getMessages != null,
				SecurityId = security?.ToSecurityId(copyExtended: true) ?? default,
				DataType = dataType,
				Arg = arg,
				GetMessages = getMessages
			});
		}
	}
}