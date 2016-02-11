#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: HistoryMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
	/// </summary>
	public class HistoryMessageAdapter : MessageAdapter
	{
		private bool _isSuspended;
		private bool _isStarted;

		/// <summary>
		/// The number of loaded events.
		/// </summary>
		public int LoadedMessageCount { get; private set; }

		/// <summary>
		/// The number of the event <see cref="IConnector.MarketTimeChanged"/> calls after end of trading. By default it is equal to 2.
		/// </summary>
		/// <remarks>
		/// It is required for activation of post-trade rules (rules, basing on events, occurring after end of trading).
		/// </remarks>
		public int PostTradeMarketTimeChangedCount
		{
			get { return BasketStorage.PostTradeMarketTimeChangedCount; }
			set { BasketStorage.PostTradeMarketTimeChangedCount = value; }
		}

		private IStorageRegistry _storageRegistry;

		/// <summary>
		/// Market data storage.
		/// </summary>
		public IStorageRegistry StorageRegistry
		{
			get { return _storageRegistry; }
			set
			{
				_storageRegistry = value;

				if (value != null)
					Drive = value.DefaultDrive;
			}
		}

		private IMarketDataDrive _drive;

		/// <summary>
		/// The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.
		/// </summary>
		public IMarketDataDrive Drive
		{
			get { return _drive; }
			set
			{
				if (value == null && StorageRegistry != null)
					throw new ArgumentNullException();

				_drive = value;
			}
		}

		/// <summary>
		/// The format of market data. <see cref="StorageFormats.Binary"/> is used by default.
		/// </summary>
		public StorageFormats StorageFormat { get; set; }

		/// <summary>
		/// The aggregator-storage.
		/// </summary>
		public CachedBasketMarketDataStorage<Message> BasketStorage { get; }

		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public ISecurityProvider SecurityProvider { get; }

		/// <summary>
		/// The interval of message <see cref="TimeMessage"/> generation. By default, it is equal to 1 sec.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.TimeIntervalKey)]
		[DescriptionLoc(LocalizedStrings.Str195Key)]
		public virtual TimeSpan MarketTimeChangedInterval
		{
			get { return BasketStorage.MarketTimeChangedInterval; }
			set { BasketStorage.MarketTimeChangedInterval = value; }
		}

		/// <summary>
		/// Max message queue count.
		/// </summary>
		/// <remarks>
		/// The default value is -1, which corresponds to the size without limitations.
		/// </remarks>
		public int MaxMessageCount
		{
			get { return BasketStorage.MaxMessageCount; }
			set { BasketStorage.MaxMessageCount = value; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public HistoryMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			BasketStorage = new CachedBasketMarketDataStorage<Message>
			{
				Boards = Enumerable.Empty<ExchangeBoard>()
			};

			StartDate = DateTimeOffset.MinValue;
			StopDate = DateTimeOffset.MaxValue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		public HistoryMessageAdapter(IdGenerator transactionIdGenerator, ISecurityProvider securityProvider)
			: this(transactionIdGenerator)
		{
			SecurityProvider = securityProvider;
			BasketStorage.Boards = SecurityProvider
				.LookupAll()
				.Select(s => s.Board)
				.Distinct();

			this.AddMarketDataSupport();
			this.AddSupportedMessage(ExtendedMessageTypes.EmulationState);
			this.AddSupportedMessage(ExtendedMessageTypes.HistorySource);
		}

		/// <summary>
		/// Date in history for starting the paper trading.
		/// </summary>
		public DateTimeOffset StartDate
		{
			get { return BasketStorage.StartDate; }
			set { BasketStorage.StartDate = value; }
		}

		/// <summary>
		/// Date in history to stop the paper trading (date is included).
		/// </summary>
		public DateTimeOffset StopDate
		{
			get { return BasketStorage.StopDate; }
			set { BasketStorage.StopDate = value; }
		}

		/// <summary>
		/// Order book builders.
		/// </summary>
		public IDictionary<SecurityId, IOrderLogMarketDepthBuilder> OrderLogMarketDepthBuilders { get; } = new Dictionary<SecurityId, IOrderLogMarketDepthBuilder>();

		/// <summary>
		/// Create market depth builder.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Order log to market depth builder.</returns>
		public override IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		{
			return OrderLogMarketDepthBuilders[securityId];
		}

		private DateTimeOffset _currentTime;
		
		/// <summary>
		/// The current time.
		/// </summary>
		public override DateTimeOffset CurrentTime => _currentTime;

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			BasketStorage.Dispose();

			base.DisposeManaged();
		}

		/// <summary>
		/// <see cref="SecurityLookupMessage"/> required to get securities.
		/// </summary>
		public override bool SecurityLookupRequired => true;

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_isSuspended = false;
					BasketStorage.Reset();
					
					LoadedMessageCount = 0;

					if (!_isStarted)
						SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_isStarted)
						throw new InvalidOperationException(LocalizedStrings.Str1116);

					SendOutMessage(new ConnectMessage { LocalTime = StartDate });
					return;
				}

				case MessageTypes.Disconnect:
				{
					_isSuspended = false;

					if(_isStarted)
						SendOutMessage(new LastMessage { LocalTime = StopDate });

					SendOutMessage(new DisconnectMessage { LocalTime = StopDate });
					SendOutMessage(new ResetMessage());

					BasketStorage.Reset();
					_isStarted = false;
					return;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;

					//ThreadingHelper.Thread(() =>
					//{
					//	try
					//	{
							SecurityProvider.LookupAll().ForEach(security =>
							{
								SendOutMessage(security.Board.ToMessage());

								var secMsg = security.ToMessage();
								secMsg.OriginalTransactionId = lookupMsg.TransactionId;
								SendOutMessage(secMsg);

								//SendOutMessage(new Level1ChangeMessage { SecurityId = security.ToSecurityId() }
								//	.Add(Level1Fields.StepPrice, security.StepPrice)
								//	.Add(Level1Fields.MinPrice, security.MinPrice)
								//	.Add(Level1Fields.MaxPrice, security.MaxPrice)
								//	.Add(Level1Fields.MarginBuy, security.MarginBuy)
								//	.Add(Level1Fields.MarginSell, security.MarginSell));
							});

							SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = lookupMsg.TransactionId });
					//	}
					//	catch (Exception ex)
					//	{
					//		SendOutError(ex);
					//	}
					//}).Name("History sec lookup").Start();
					return;
				}

				case MessageTypes.MarketData:
				case ExtendedMessageTypes.HistorySource:
					ProcessMarketDataMessage((MarketDataMessage)message);
					return;

				case ExtendedMessageTypes.EmulationState:
					var stateMsg = (EmulationStateMessage)message;
					var isSuspended = false;

					switch (stateMsg.State)
					{
						case EmulationStates.Starting:
						{
							if (_isStarted)
								_isSuspended = false;
							else
								_isStarted = true;

							break;
						}

						case EmulationStates.Suspending:
						{
							_isSuspended = true;
							isSuspended = true;
							break;
						}
					}

					SendOutMessage(message);

					if (isSuspended)
						SendOutMessage(new EmulationStateMessage { State = EmulationStates.Suspended });

					return;
			}

			//SendOutMessage(message);
		}

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			var securityId = message.SecurityId;
			var security = SecurityProvider.LookupById(securityId.ToStringId());

			if (security == null)
			{
				RaiseMarketDataMessage(message, new InvalidOperationException(LocalizedStrings.Str704Params.Put(securityId)));
				return;
			}

			if (StorageRegistry == null)
			{
				RaiseMarketDataMessage(message, new InvalidOperationException(LocalizedStrings.Str1117Params.Put(message.DataType, securityId)));
				return;
			}

			var history = message as HistorySourceMessage;

			Exception error = null;

			switch (message.DataType)
			{
				case MarketDataTypes.Level1:
				{
					if (message.IsSubscribe)
					{
						if (history == null)
						{
							BasketStorage.AddStorage(StorageRegistry.GetLevel1MessageStorage(security, Drive, StorageFormat));

							BasketStorage.AddStorage(new InMemoryMarketDataStorage<ClearingMessage>(security, null, date => new[]
							{
								new ClearingMessage
								{
									LocalTime = date.Date + security.Board.ExpiryTime,
									SecurityId = securityId,
									ClearMarketDepth = true
								}
							}));
						}
						else
						{
							BasketStorage.AddStorage(new InMemoryMarketDataStorage<Level1ChangeMessage>(security, null, history.GetMessages));
						}
					}
					else
					{
						BasketStorage.RemoveStorage<IMarketDataStorage<Level1ChangeMessage>>(security, MessageTypes.Level1Change, null);
						BasketStorage.RemoveStorage<InMemoryMarketDataStorage<ClearingMessage>>(security, ExtendedMessageTypes.Clearing, null);
					}

					break;
				}

				case MarketDataTypes.MarketDepth:
				{
					if (message.IsSubscribe)
					{
						if (history == null)
							BasketStorage.AddStorage((IMarketDataStorage<QuoteChangeMessage>)StorageRegistry.GetMarketDepthStorage(security, Drive, StorageFormat));
						else
							BasketStorage.AddStorage(new InMemoryMarketDataStorage<QuoteChangeMessage>(security, null, history.GetMessages));
					}
					else
						BasketStorage.RemoveStorage<IMarketDataStorage<QuoteChangeMessage>>(security, MessageTypes.QuoteChange, null);
					
					break;
				}

				case MarketDataTypes.Trades:
				{
					if (message.IsSubscribe)
					{
						if (history == null)
							BasketStorage.AddStorage((IMarketDataStorage<ExecutionMessage>)StorageRegistry.GetTradeStorage(security, Drive, StorageFormat));
						else
							BasketStorage.AddStorage(new InMemoryMarketDataStorage<ExecutionMessage>(security, null, history.GetMessages));
					}
					else
						BasketStorage.RemoveStorage<IMarketDataStorage<ExecutionMessage>>(security, MessageTypes.Execution, ExecutionTypes.Tick);
					
					break;
				}

				case MarketDataTypes.OrderLog:
				{
					if (message.IsSubscribe)
					{
						if (history == null)
							BasketStorage.AddStorage((IMarketDataStorage<ExecutionMessage>)StorageRegistry.GetOrderLogStorage(security, Drive, StorageFormat));
						else
							BasketStorage.AddStorage(new InMemoryMarketDataStorage<ExecutionMessage>(security, null, history.GetMessages));
					}
					else
						BasketStorage.RemoveStorage<IMarketDataStorage<ExecutionMessage>>(security, MessageTypes.Execution, ExecutionTypes.OrderLog);

					break;
				}

				case MarketDataTypes.CandleTimeFrame:
				case MarketDataTypes.CandleTick:
				case MarketDataTypes.CandleVolume:
				case MarketDataTypes.CandleRange:
				case MarketDataTypes.CandlePnF:
				case MarketDataTypes.CandleRenko:
				{
					var msgType = message.DataType.ToCandleMessageType();

					if (message.IsSubscribe)
					{
						var candleType = message.DataType.ToCandleMessage();

						if (history == null)
							BasketStorage.AddStorage(StorageRegistry.GetCandleMessageStorage(candleType, security, message.Arg, Drive, StorageFormat));
						else
							BasketStorage.AddStorage(new InMemoryMarketDataStorage<CandleMessage>(security, message.Arg, history.GetMessages, candleType));
					}
					else
						BasketStorage.RemoveStorage<IMarketDataStorage<CandleMessage>>(security, msgType, message.Arg);

					break;
				}

				default:
					error = new InvalidOperationException(LocalizedStrings.Str1118Params.Put(message.DataType));
					break;
			}

			RaiseMarketDataMessage(message, error);
		}

		private void RaiseMarketDataMessage(MarketDataMessage message, Exception error)
		{
			var reply = (MarketDataMessage)message.Clone();
			reply.OriginalTransactionId = message.TransactionId;
			reply.Error = error;
			SendOutMessage(reply);
		}

		/// <summary>
		/// Send next outgoing message.
		/// </summary>
		/// <returns><see langword="true" />, if message was sent, otherwise, <see langword="false" />.</returns>
		public bool SendOutMessage()
		{
			if (!_isStarted || _isSuspended)
				return false;

			if (!BasketStorage.MoveNext())
				return false;

			var msg = BasketStorage.Current;

			SendOutMessage(msg);

			return true;
		}

		/// <summary>
		/// Send outgoing message and raise <see cref="MessageAdapter.NewOutMessage"/> event.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendOutMessage(Message message)
		{
			LoadedMessageCount++;
			
			var serverTime = message.GetServerTime();

			if (serverTime != null)
				_currentTime = serverTime.Value;

			base.SendOutMessage(message);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str1127Params.Put(StartDate, StopDate);
		}
	}
}