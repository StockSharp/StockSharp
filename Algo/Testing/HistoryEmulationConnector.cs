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

	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo.Storages;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Algo.Risk;

	/// <summary>
	/// The emulation connection. It uses historical data and/or occasionally generated.
	/// </summary>
	public class HistoryEmulationConnector : BaseEmulationConnector
	{
		private readonly bool _ownInnerAdapter;
		
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
			: this(new CollectionSecurityProvider(securities), new CollectionPortfolioProvider(portfolios), storageRegistry)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
		public HistoryEmulationConnector(ISecurityProvider securityProvider, IEnumerable<Portfolio> portfolios)
			: this(securityProvider, new CollectionPortfolioProvider(portfolios), new StorageRegistry())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		public HistoryEmulationConnector(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider)
			: this(securityProvider, portfolioProvider, new StorageRegistry())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		public HistoryEmulationConnector(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IStorageRegistry storageRegistry)
			: this(new HistoryMessageAdapter(new IncrementalIdGenerator(), securityProvider) { StorageRegistry = storageRegistry }, true, new InMemoryMessageChannel(new MessageByLocalTimeQueue(), "Emulator in", err => err.LogError()), securityProvider, portfolioProvider)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		/// <param name="ownInnerAdapter">Control <paramref name="innerAdapter"/> lifetime.</param>
		/// <param name="inChannel">Incoming messages channel.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
		public HistoryEmulationConnector(IMessageAdapter innerAdapter, bool ownInnerAdapter, IMessageChannel inChannel, ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider)
			: base(new EmulationMessageAdapter(innerAdapter, inChannel, true) { OwnInnerAdapter = true }, false, securityProvider, portfolioProvider)
		{
			// чтобы каждый раз при повторной эмуляции получать одинаковые номера транзакций
			TransactionIdGenerator = innerAdapter.TransactionIdGenerator;

			Adapter.LatencyManager = null;
			Adapter.CommissionManager = null;
			Adapter.PnLManager = null;
			Adapter.SlippageManager = null;

			InMessageChannel = new PassThroughMessageChannel();
			OutMessageChannel = new PassThroughMessageChannel();

			// при тестировании по свечкам, время меняется быстрее и таймаут должен быть больше 30с.
			//ReConnectionSettings.TimeOutInterval = TimeSpan.MaxValue;

			//MaxMessageCount = 1000;

			//Adapter.SupportCandlesCompression = false;
			Adapter.SupportBuildingFromOrderLog = false;
			Adapter.SupportPartialDownload = false;
			Adapter.SupportLookupTracking = false;
			Adapter.SupportOrderBookTruncate = false;
			Adapter.ConnectDisconnectEventOnFirstAdapter = false;

			_ownInnerAdapter = ownInnerAdapter;
		}

		/// <inheritdoc />
		public override IRiskManager RiskManager => null;

		/// <inheritdoc />
		public override bool SupportBasketSecurities => true;

		/// <summary>
		/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
		/// </summary>
		public HistoryMessageAdapter HistoryMessageAdapter => (HistoryMessageAdapter)EmulationAdapter.InnerAdapter;

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

						if (_ownInnerAdapter)
							EmulationAdapter.InChannel.Close();

						break;
					case EmulationStates.Stopping:
						throwError = _state != EmulationStates.Started && _state != EmulationStates.Suspended
							&& State != EmulationStates.Starting;  // при ошибках при запуске эмуляции состояние может быть Starting

						if (_ownInnerAdapter)
						{
							EmulationAdapter.InChannel.Clear();

							if (_state == EmulationStates.Suspended)
								EmulationAdapter.InChannel.Resume();
						}

						break;
					case EmulationStates.Starting:
						throwError = _state != EmulationStates.Stopped && _state != EmulationStates.Suspended;

						if (_ownInnerAdapter && _state == EmulationStates.Suspended)
							EmulationAdapter.InChannel.Resume();

						break;
					case EmulationStates.Started:
						throwError = _state != EmulationStates.Starting;
						break;
					case EmulationStates.Suspending:
						throwError = _state != EmulationStates.Started;

						if (_ownInnerAdapter)
							EmulationAdapter.InChannel.Suspend();

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

		/// <inheritdoc />
		public override void ClearCache()
		{
			base.ClearCache();

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

							// все данные пришли без ошибок или в процессе чтения произошла ошибка - начинаем остановку
							SendEmulationState(EmulationStates.Stopping);
						}

						if (State == EmulationStates.Stopping)
						{
							// тестирование было отменено и пришли все ранее прочитанные данные
							SendEmulationState(EmulationStates.Stopped);
						}

						break;
					}

					//case ExtendedMessageTypes.Clearing:
					//	break;

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

				case EmulationStates.Suspending:
				{
					SendEmulationState(EmulationStates.Suspended);
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