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

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using SourceKey = System.Tuple<Messages.SecurityId, Messages.MarketDataTypes, object>;

	/// <summary>
	/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
	/// </summary>
	public class HistoryMessageAdapter : MessageAdapter, IHistoryMessageAdapter
	{
		private readonly Dictionary<SourceKey, MarketDataGenerator> _generators = new Dictionary<SourceKey, MarketDataGenerator>();
		private readonly Dictionary<SourceKey, Func<DateTimeOffset, IEnumerable<Message>>> _historySources = new Dictionary<SourceKey, Func<DateTimeOffset, IEnumerable<Message>>>();

		private readonly CachedBasketMarketDataStorage<Message> _basketStorage;

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
			get => _basketStorage.PostTradeMarketTimeChangedCount;
			set => _basketStorage.PostTradeMarketTimeChangedCount = value;
		}

		/// <summary>
		/// Market data storage.
		/// </summary>
		public IStorageRegistry StorageRegistry { get; set; }

		/// <summary>
		/// The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.
		/// </summary>
		public IMarketDataDrive Drive { get; set; }

		private IMarketDataDrive DriveInternal => Drive ?? StorageRegistry?.DefaultDrive;

		/// <summary>
		/// The format of market data. <see cref="StorageFormats.Binary"/> is used by default.
		/// </summary>
		public StorageFormats StorageFormat { get; set; }

		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public ISecurityProvider SecurityProvider { get; }

		/// <inheritdoc />
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.TimeIntervalKey)]
		[DescriptionLoc(LocalizedStrings.Str195Key)]
		public virtual TimeSpan MarketTimeChangedInterval
		{
			get => _basketStorage.MarketTimeChangedInterval;
			set => _basketStorage.MarketTimeChangedInterval = value;
		}

		/// <summary>
		/// Default value of <see cref="MaxMessageCount"/>.
		/// </summary>
		public const int DefaultMaxMessageCount = 1000000;

		/// <summary>
		/// The maximal size of the message queue, up to which history data are read. By default, it is equal to <see cref="DefaultMaxMessageCount"/>.
		/// </summary>
		public int MaxMessageCount
		{
			get => _basketStorage.MaxMessageCount;
			set => _basketStorage.MaxMessageCount = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public HistoryMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			_basketStorage = new CachedBasketMarketDataStorage<Message>(transactionIdGenerator)
			{
				Boards = Enumerable.Empty<ExchangeBoard>(),
				Parent = this
			};

			MaxMessageCount = DefaultMaxMessageCount;

			StartDate = DateTimeOffset.MinValue;
			StopDate = DateTimeOffset.MaxValue;

			this.AddMarketDataSupport();
			this.AddSupportedMessage(ExtendedMessageTypes.EmulationState, null);
			this.AddSupportedMessage(ExtendedMessageTypes.HistorySource, true);
			this.AddSupportedMessage(ExtendedMessageTypes.Generator, true);
			this.AddSupportedMessage(ExtendedMessageTypes.ChangeTimeInterval, null);
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

			_basketStorage.Boards = SecurityProvider
				.LookupAll()
				.Select(s => s.Board)
				.Distinct();
		}

		/// <inheritdoc />
		public DateTimeOffset StartDate { get; set; }

		/// <inheritdoc />
		public DateTimeOffset StopDate { get; set; }

		/// <summary>
		/// Check loading dates are they tradable.
		/// </summary>
		public bool CheckTradableDates
		{
			get => _basketStorage.CheckTradableDates;
			set => _basketStorage.CheckTradableDates = value;
		}

		/// <summary>
		/// Order book builders.
		/// </summary>
		public IDictionary<SecurityId, IOrderLogMarketDepthBuilder> OrderLogMarketDepthBuilders { get; } = new Dictionary<SecurityId, IOrderLogMarketDepthBuilder>();

		/// <inheritdoc />
		public override IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		{
			return OrderLogMarketDepthBuilders[securityId];
		}

		private DateTimeOffset _currentTime;
		
		/// <inheritdoc />
		public override DateTimeOffset CurrentTime => _currentTime;

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_basketStorage.Dispose();

			base.DisposeManaged();
		}

		private IEnumerable<DataType> _supportedMarketDataTypes;

		/// <inheritdoc />
		public override IEnumerable<DataType> SupportedMarketDataTypes
		{
			get
			{
				if (_supportedMarketDataTypes == null)
				{
					var drive = DriveInternal;

					var dataTypes = drive.GetAvailableDataTypes(default, StorageFormat);

					_supportedMarketDataTypes = dataTypes
						//.Select(dt => dt.ToMarketDataType())
						//.Where(t => t != null)
						//.Select(t => t.Value)
						.Distinct()
						.ToArray();
				}
				
				return _supportedMarketDataTypes;
			}
		}

		/// <inheritdoc />
		public override bool IsFullCandlesOnly => false;

		/// <inheritdoc />
		public override IEnumerable<object> GetCandleArgs(Type candleType, SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to)
		{
			var drive = DriveInternal;

			if (drive == null)
				return Enumerable.Empty<object>();

			var dataType = candleType.ToCandleMarketDataType();

			var args = _historySources
	             .Where(t => t.Key.Item2 == dataType && (t.Key.Item1 == securityId || t.Key.Item1.IsDefault()))
	             .Select(s => s.Key.Item3)
	             .ToArray();

			if (args.Length > 0)
				return args;

			args = _generators
	             .Where(t => t.Key.Item2 == dataType && (t.Key.Item1 == securityId || t.Key.Item1.IsDefault()))
	             .Select(s => s.Key.Item3)
	             .ToArray();

			if (args.Length > 0)
				return args;

			return drive.GetCandleArgs(StorageFormat, candleType, securityId, from, to);
		}

		/// <inheritdoc />
		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_isSuspended = false;
					_currentTime = default;

					_generators.Clear();
					_historySources.Clear();

                    _basketStorage.Reset();
					
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
					break;
				}

				case MessageTypes.Disconnect:
				{
					_isSuspended = false;

					if (_isStarted)
						SendOutMessage(new LastMessage { LocalTime = StopDate });

					SendOutMessage(new DisconnectMessage { LocalTime = StopDate });
					//SendOutMessage(new ResetMessage());

					_basketStorage.Reset();
					_isStarted = false;

					break;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;

					var securities = lookupMsg.SecurityId.IsDefault() 
							? SecurityProvider.LookupAll() 
							: SecurityProvider.Lookup(lookupMsg);

					foreach (var security in securities)
					{
						SendOutMessage(security.Board.ToMessage());
						SendOutMessage(security.ToMessage(originalTransactionId: lookupMsg.TransactionId));
					}

					SendSubscriptionResult(lookupMsg);

					break;
				}

				case MessageTypes.MarketData:
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;

				case ExtendedMessageTypes.HistorySource:
				{
					var sourceMsg = (HistorySourceMessage)message;

					var key = Tuple.Create(sourceMsg.SecurityId, sourceMsg.DataType, sourceMsg.Arg);

					if (sourceMsg.IsSubscribe)
						_historySources[key] = sourceMsg.GetMessages;
					else
						_historySources.Remove(key);

					break;
				}

				case ExtendedMessageTypes.EmulationState:
				{
					var stateMsg = (EmulationStateMessage)message;
					var isSuspended = false;

					switch (stateMsg.State)
					{
						case EmulationStates.Starting:
						{
							if (_isStarted)
								_isSuspended = false;
							else
							{
								_isStarted = true;
								_basketStorage.Start(stateMsg.StartDate.IsDefault() ? StartDate : stateMsg.StartDate, stateMsg.StopDate.IsDefault() ? StopDate : stateMsg.StopDate);
							}

							break;
						}

						case EmulationStates.Suspending:
						{
							_isSuspended = true;
							isSuspended = true;
							break;
						}

						case EmulationStates.Stopping:
						{
							_isSuspended = false;
							_basketStorage.Stop();
							break;
						}
					}

					SendOutMessage(message);

					if (isSuspended)
						SendOutMessage(new EmulationStateMessage { State = EmulationStates.Suspended });

					break;
				}

				case ExtendedMessageTypes.Generator:
				{
					var generatorMsg = (GeneratorMessage)message;
					var item = Tuple.Create(generatorMsg.SecurityId, generatorMsg.DataType, generatorMsg.Arg);

					if (generatorMsg.IsSubscribe)
						_generators.Add(item, generatorMsg.Generator);
					else
						_generators.Remove(item);

					break;
				}

				case ExtendedMessageTypes.ChangeTimeInterval:
				{
					var intervalMsg = (ChangeTimeIntervalMessage)message;
					_basketStorage.MarketTimeChangedInterval = intervalMsg.Interval;
					break;
				}

				default:
					return false;
			}

			return true;

			//SendOutMessage(message);
		}

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			var isSubscribe = message.IsSubscribe;
			var securityId = message.SecurityId;
			var dataType = message.DataType;
			var arg = message.Arg;
			var transId = message.TransactionId;
			var originId = message.OriginalTransactionId;

			if (SecurityProvider.LookupById(securityId) == null)
			{
				SendSubscriptionReply(transId, new InvalidOperationException(LocalizedStrings.Str704Params.Put(securityId)));
				return;
			}

			if (StorageRegistry == null)
			{
				SendSubscriptionReply(transId, new InvalidOperationException(LocalizedStrings.Str1117Params.Put(dataType, securityId)));
				return;
			}

			Func<DateTimeOffset, IEnumerable<Message>> GetHistorySource()
			{
				Func<DateTimeOffset, IEnumerable<Message>> GetHistorySource2(SecurityId s)
				{
					return _historySources.TryGetValue(Tuple.Create(s, dataType, arg));
				}

				return GetHistorySource2(securityId) ?? GetHistorySource2(default);
			}

			Exception error = null;

			switch (dataType)
			{
				case MarketDataTypes.Level1:
				{
					if (_generators.ContainsKey(Tuple.Create(securityId, dataType, arg)))
						break;

					if (isSubscribe)
					{
						var historySource = GetHistorySource();

						if (historySource == null)
						{
							_basketStorage.AddStorage(StorageRegistry.GetLevel1MessageStorage(securityId, Drive, StorageFormat), transId);

							//BasketStorage.AddStorage(new InMemoryMarketDataStorage<ClearingMessage>(security, null, date => new[]
							//{
							//	new ClearingMessage
							//	{
							//		LocalTime = date.Date + security.Board.ExpiryTime,
							//		SecurityId = securityId,
							//		ClearMarketDepth = true
							//	}
							//}), message.TransactionId);
						}
						else
						{
							_basketStorage.AddStorage(new InMemoryMarketDataStorage<Level1ChangeMessage>(securityId, null, historySource), transId);
						}
					}
					else
					{
						_basketStorage.RemoveStorage(originId);
						//BasketStorage.RemoveStorage<InMemoryMarketDataStorage<ClearingMessage>>(security, ExtendedMessageTypes.Clearing, null);
					}

					break;
				}

				case MarketDataTypes.MarketDepth:
				{
					if (_generators.ContainsKey(Tuple.Create(securityId, dataType, arg)))
						break;

					if (isSubscribe)
					{
						var historySource = GetHistorySource();

						_basketStorage.AddStorage(historySource == null
							? StorageRegistry.GetQuoteMessageStorage(securityId, Drive, StorageFormat)
							: new InMemoryMarketDataStorage<QuoteChangeMessage>(securityId, null, historySource),
							transId);
					}
					else
						_basketStorage.RemoveStorage(originId);
					
					break;
				}

				case MarketDataTypes.Trades:
				{
					if (_generators.ContainsKey(Tuple.Create(securityId, dataType, arg)))
						break;

					if (isSubscribe)
					{
						var historySource = GetHistorySource();

						_basketStorage.AddStorage(historySource == null
							? StorageRegistry.GetTickMessageStorage(securityId, Drive, StorageFormat)
							: new InMemoryMarketDataStorage<ExecutionMessage>(securityId, null, historySource),
							transId);
					}
					else
						_basketStorage.RemoveStorage(originId);
					
					break;
				}

				case MarketDataTypes.OrderLog:
				{
					if (_generators.ContainsKey(Tuple.Create(securityId, dataType, arg)))
						break;

					if (isSubscribe)
					{
						var historySource = GetHistorySource();

						_basketStorage.AddStorage(historySource == null
							? StorageRegistry.GetOrderLogMessageStorage(securityId, Drive, StorageFormat)
							: new InMemoryMarketDataStorage<ExecutionMessage>(securityId, null, historySource),
							transId);
					}
					else
						_basketStorage.RemoveStorage(originId);

					break;
				}

				default:
				{
					if (dataType.IsCandleDataType())
					{
						if (_generators.ContainsKey(Tuple.Create(securityId, MarketDataTypes.Trades, arg)))
						{
							if (isSubscribe)
								SendSubscriptionNotSupported(transId);

							return;
						}

						if (isSubscribe)
						{
							var historySource = GetHistorySource();
							var candleType = dataType.ToCandleMessage();

							_basketStorage.AddStorage(historySource == null
									? StorageRegistry.GetCandleMessageStorage(candleType, securityId, arg, Drive, StorageFormat)
									: new InMemoryMarketDataStorage<CandleMessage>(securityId, arg, historySource, candleType),
								transId);
						}
						else
							_basketStorage.RemoveStorage(originId);

						break;
					}

					error = new InvalidOperationException(LocalizedStrings.Str1118Params.Put(dataType));
					break;
				}
			}

			SendSubscriptionReply(transId, error);
		}

		/// <inheritdoc />
		bool IHistoryMessageAdapter.SendOutMessage()
		{
			if (!_isStarted || _isSuspended)
				return false;

			if (!_basketStorage.MoveNext())
				return false;

			var msg = _basketStorage.Current;

			SendOutMessage(msg);

			return true;
		}

		void IHistoryMessageAdapter.SendOutMessage(Message message) => SendOutMessage(message);

		/// <inheritdoc cref="MessageAdapter" />
		protected override void SendOutMessage(Message message)
		{
			LoadedMessageCount++;
			
			var serverTime = message.TryGetServerTime();

			if (serverTime != null)
				_currentTime = serverTime.Value;

			base.SendOutMessage(message);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return LocalizedStrings.Str1127Params.Put(StartDate, StopDate);
		}
	}
}