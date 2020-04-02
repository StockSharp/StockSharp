namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Storage processor.
	/// </summary>
	public class StorageProcessor
	{
		private readonly SynchronizedSet<long> _fullyProcessedSubscriptions = new SynchronizedSet<long>();
		
		/// <summary>
		/// Initializes a new instance of the <see cref="StorageProcessor"/>.
		/// </summary>
		/// <param name="settings">Storage settings.</param>
		/// <param name="candleBuilderProvider">Candle builders provider.</param>
		public StorageProcessor(StorageCoreSettings settings, CandleBuilderProvider candleBuilderProvider)
		{
			Settings = settings ?? throw new ArgumentNullException(nameof(settings));
			CandleBuilderProvider = candleBuilderProvider ?? throw new ArgumentNullException(nameof(candleBuilderProvider));
		}

		/// <summary>
		/// Storage settings.
		/// </summary>
		public StorageCoreSettings Settings { get; }

		/// <summary>
		/// Candle builders provider.
		/// </summary>
		public CandleBuilderProvider CandleBuilderProvider { get; }

		private IMarketDataStorage<CandleMessage> GetTimeFrameCandleMessageStorage(SecurityId securityId, TimeSpan timeFrame, bool allowBuildFromSmallerTimeFrame)
		{
			if (!allowBuildFromSmallerTimeFrame)
				return (IMarketDataStorage<CandleMessage>)Settings.GetStorage(securityId, typeof(TimeFrameCandleMessage), timeFrame);

			return CandleBuilderProvider.GetCandleMessageBuildableStorage(Settings.StorageRegistry, securityId, timeFrame, Settings.Drive, Settings.Format);
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public void Reset()
		{
			_fullyProcessedSubscriptions.Clear();
		}

		/// <summary>
		/// Process <see cref="MarketDataMessage"/>.
		/// </summary>
		/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <param name="newOutMessage">New message event.</param>
		/// <returns>Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</returns>
		public MarketDataMessage ProcessMarketData(MarketDataMessage message, Action<Message> newOutMessage)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (newOutMessage is null)
				throw new ArgumentNullException(nameof(newOutMessage));

			if (message.From == null && Settings.DaysLoad == TimeSpan.Zero)
				return message;

			if (message.IsSubscribe)
			{
				var transactionId = message.TransactionId;

				var lastTime = LoadMessages(message, message.From, message.To, transactionId, newOutMessage);

				if (message.To != null && lastTime != null && message.To <= lastTime)
				{
					_fullyProcessedSubscriptions.Add(transactionId);
					newOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = transactionId });

					return null;
				}

				if (lastTime != null)
				{
					if (!(message.DataType == MarketDataTypes.MarketDepth && message.From == null && message.To == null))
					{
						var clone = (MarketDataMessage)message.Clone();
						clone.From = lastTime;
						message = clone;
						message.ValidateBounds();
					}
				}
			}
			else
			{
				if (_fullyProcessedSubscriptions.Remove(message.OriginalTransactionId))
				{
					newOutMessage(new SubscriptionResponseMessage
					{
						OriginalTransactionId = message.TransactionId,
					});

					return null;
				}
			}

			return message;
		}

		private DateTimeOffset? LoadMessages(MarketDataMessage msg, DateTimeOffset? from, DateTimeOffset? to, long transactionId, Action<Message> newOutMessage)
		{
			void SendReply() => newOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = transactionId });
			void SendOut(Message message)
			{
				message.OfflineMode = MessageOfflineModes.Ignore;
				newOutMessage(message);
			}

			DateTimeOffset? lastTime = null;

			var secId = msg.SecurityId;

			switch (msg.DataType)
			{
				case MarketDataTypes.Level1:
					if (msg.BuildMode != MarketDataBuildModes.Build)
					{
						if (Settings.IsMode(StorageModes.Incremental))
							lastTime = LoadMessages(Settings.GetStorage<Level1ChangeMessage>(secId, null), from, to, TimeSpan.Zero, transactionId, SendReply, SendOut);
					}
					else
					{
						if (msg.BuildFrom == MarketDataTypes.OrderLog)
						{
							var storage = Settings.GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog);

							var range = GetRange(storage, from, to, TimeSpan.Zero);

							if (range != null)
							{
								lastTime = LoadMessages(storage
									.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
									.ToLevel1(msg.DepthBuilder, msg.RefreshSpeed ?? default, msg.MaxDepth ?? int.MaxValue), range.Item1, transactionId, SendReply, SendOut);
							}
						}
						else if (msg.BuildFrom == MarketDataTypes.MarketDepth)
						{
							var storage = Settings.GetStorage<QuoteChangeMessage>(secId, null);

							var range = GetRange(storage, from, to, TimeSpan.Zero);

							if (range != null)
							{
								lastTime = LoadMessages(storage
									.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
									.ToLevel1(), range.Item1, transactionId, SendReply, SendOut);
							}
						}
					}
						
					break;

				case MarketDataTypes.MarketDepth:
					if (msg.BuildMode != MarketDataBuildModes.Build)
					{
						if (Settings.IsMode(StorageModes.Incremental))
							lastTime = LoadMessages(Settings.GetStorage<QuoteChangeMessage>(secId, null), from, to, TimeSpan.Zero, transactionId, SendReply, SendOut);
					}
					else
					{
						if (msg.BuildFrom == MarketDataTypes.OrderLog)
						{
							var storage = Settings.GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog);

							var range = GetRange(storage, from, to, TimeSpan.Zero);

							if (range != null)
							{
								lastTime = LoadMessages(storage
									.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
									.ToOrderBooks(msg.DepthBuilder, msg.RefreshSpeed ?? default, msg.MaxDepth ?? int.MaxValue), range.Item1, transactionId, SendReply, SendOut);
							}
						}
						else if (msg.BuildFrom == MarketDataTypes.Level1)
						{
							var storage = Settings.GetStorage<Level1ChangeMessage>(secId, null);

							var range = GetRange(storage, from, to, TimeSpan.Zero);

							if (range != null)
							{
								lastTime = LoadMessages(storage
									.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
									.ToOrderBooks(), range.Item1, transactionId, SendReply, SendOut);
							}
						}
					}

					break;

				case MarketDataTypes.Trades:
					if (msg.BuildMode != MarketDataBuildModes.Build)
						lastTime = LoadMessages(Settings.GetStorage<ExecutionMessage>(secId, ExecutionTypes.Tick), from, to, Settings.DaysLoad, transactionId, SendReply, SendOut);
					else
					{
						if (msg.BuildFrom == MarketDataTypes.OrderLog)
						{
							var storage = Settings.GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog);

							var range = GetRange(storage, from, to, TimeSpan.Zero);

							if (range != null)
							{
								lastTime = LoadMessages(storage
									.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
									.ToTicks(), range.Item1, transactionId, SendReply, SendOut);
							}
						}
						else if (msg.BuildFrom == MarketDataTypes.Level1)
						{
							var storage = Settings.GetStorage<Level1ChangeMessage>(secId, null);

							var range = GetRange(storage, from, to, TimeSpan.Zero);

							if (range != null)
							{
								lastTime = LoadMessages(storage
									.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
									.ToTicks(), range.Item1, transactionId, SendReply, SendOut);
							}
						}
					}

					break;

				case MarketDataTypes.OrderLog:
					lastTime = LoadMessages(Settings.GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog), from, to, Settings.DaysLoad, transactionId, SendReply, SendOut);
					break;

				case MarketDataTypes.News:
					lastTime = LoadMessages(Settings.GetStorage<NewsMessage>(default, null), from, to, Settings.DaysLoad, transactionId, SendReply, SendOut);
					break;

				case MarketDataTypes.Board:
					lastTime = LoadMessages(Settings.GetStorage<BoardStateMessage>(default, null), from, to, Settings.DaysLoad, transactionId, SendReply, SendOut);
					break;

				case MarketDataTypes.CandleTimeFrame:
					var tf = msg.GetTimeFrame();

					if (msg.IsBuildOnly())
					{
						IMarketDataStorage storage;

						switch (msg.BuildFrom)
						{
							case null:
							case MarketDataTypes.Trades:
								storage = Settings.GetStorage<ExecutionMessage>(secId, ExecutionTypes.Tick);
								break;

							case MarketDataTypes.OrderLog:
								storage = Settings.GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog);
								break;

							case MarketDataTypes.Level1:
								storage = Settings.GetStorage<Level1ChangeMessage>(secId, null);
								break;

							case MarketDataTypes.MarketDepth:
								storage = Settings.GetStorage<QuoteChangeMessage>(secId, null);
								break;

							default:
								throw new ArgumentOutOfRangeException(nameof(msg), msg.BuildFrom, LocalizedStrings.Str1219);
						}

						var range = GetRange(storage, from, to, TimeSpan.FromDays(2));

						if (range != null)
						{
							var mdMsg = (MarketDataMessage)msg.Clone();
							mdMsg.From = mdMsg.To = null;

							switch (msg.BuildFrom)
							{
								case null:
								case MarketDataTypes.Trades:
									lastTime = LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
									                        .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
									                        .ToCandles(mdMsg, candleBuilderProvider: CandleBuilderProvider), range.Item1, transactionId, SendReply, SendOut);

									break;

								case MarketDataTypes.OrderLog:
								{
									switch (msg.BuildField)
									{
										case null:
										case Level1Fields.LastTradePrice:
											lastTime = LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
											                        .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
											                        .ToCandles(mdMsg, candleBuilderProvider: CandleBuilderProvider), range.Item1, transactionId, SendReply, SendOut);

											break;
											
										// TODO
										//case Level1Fields.SpreadMiddle:
										//	lastTime = LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
										//	    .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
										//		.ToOrderBooks(OrderLogBuilders.Plaza2.CreateBuilder(security.ToSecurityId()))
										//	    .ToCandles(mdMsg, false, exchangeInfoProvider: exchangeInfoProvider), range.Item1, transactionId, SendReply, SendOut);
										//	break;
									}

									break;
								}

								case MarketDataTypes.Level1:
									switch (msg.BuildField)
									{
										case null:
										case Level1Fields.LastTradePrice:
											lastTime = LoadMessages(((IMarketDataStorage<Level1ChangeMessage>)storage)
											                        .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
											                        .ToTicks()
											                        .ToCandles(mdMsg, candleBuilderProvider: CandleBuilderProvider), range.Item1, transactionId, SendReply, SendOut);
											break;

										case Level1Fields.BestBidPrice:
										case Level1Fields.BestAskPrice:
										case Level1Fields.SpreadMiddle:
											lastTime = LoadMessages(((IMarketDataStorage<Level1ChangeMessage>)storage)
											                        .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
											                        .ToOrderBooks()
											                        .ToCandles(mdMsg, msg.BuildField.Value, candleBuilderProvider: CandleBuilderProvider), range.Item1, transactionId, SendReply, SendOut);
											break;
									}
									
									break;

								case MarketDataTypes.MarketDepth:
									lastTime = LoadMessages(((IMarketDataStorage<QuoteChangeMessage>)storage)
									                        .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
									                        .ToCandles(mdMsg, msg.BuildField ?? Level1Fields.SpreadMiddle, candleBuilderProvider: CandleBuilderProvider), range.Item1, transactionId, SendReply, SendOut);
									break;

								default:
									throw new ArgumentOutOfRangeException(nameof(msg), msg.BuildFrom, LocalizedStrings.Str1219);
							}
						}
					}
					else
					{
						var days = Settings.DaysLoad;

						//if (tf.Ticks > 1)
						//{
						//	if (tf.TotalMinutes < 15)
						//		days = TimeSpan.FromTicks(tf.Ticks * 10000);
						//	else if (tf.TotalHours < 2)
						//		days = TimeSpan.FromTicks(tf.Ticks * 1000);
						//	else if (tf.TotalDays < 2)
						//		days = TimeSpan.FromTicks(tf.Ticks * 100);
						//	else
						//		days = TimeSpan.FromTicks(tf.Ticks * 50);	
						//}

						lastTime = LoadMessages(GetTimeFrameCandleMessageStorage(secId, tf, msg.AllowBuildFromSmallerTimeFrame), from, to, days, transactionId, SendReply, SendOut);
					}
					
					break;

				default:
				{
					if (msg.DataType.IsCandleDataType())
					{
						var storage = (IMarketDataStorage<CandleMessage>)Settings.GetStorage(secId, msg.DataType.ToCandleMessage(), msg.Arg);

						var range = GetRange(storage, from, to, Settings.DaysLoad);

						if (range != null)
						{
							var messages = storage.Load(range.Item1.Date, range.Item2.Date.EndOfDay());
							lastTime = LoadMessages(messages, range.Item1, transactionId, SendReply, SendOut);
						}
					}

					break;
					// throw new ArgumentOutOfRangeException(nameof(msg), msg.DataType, LocalizedStrings.Str721);
				}
			}

			return lastTime;
		}

		private static Tuple<DateTimeOffset, DateTimeOffset> GetRange(IMarketDataStorage storage, DateTimeOffset? from, DateTimeOffset? to, TimeSpan daysLoad)
		{
			var last = storage.Dates.LastOr();

			if (last == null)
				return null;

			if (to == null)
				to = last.Value;

			if (from == null)
				from = to.Value - daysLoad;

			return Tuple.Create(from.Value, to.Value);
		}

		private DateTimeOffset? LoadMessages<TMessage>(IMarketDataStorage<TMessage> storage, DateTimeOffset? from, DateTimeOffset? to, TimeSpan daysLoad, long transactionId, Action sendReply, Action<Message> newOutMessage) 
			where TMessage : Message, ISubscriptionIdMessage, IServerTimeMessage
		{
			var range = GetRange(storage, from, to, daysLoad);

			if (range == null)
				return null;

			var messages = storage.Load(range.Item1.Date, range.Item2.Date.EndOfDay());

			return LoadMessages(messages, range.Item1, transactionId, sendReply, newOutMessage);
		}

		private DateTimeOffset? LoadMessages<TMessage>(IEnumerable<TMessage> messages, DateTimeOffset lastTime, long transactionId, Action sendReply, Action<Message> newOutMessage)
			where TMessage : Message, ISubscriptionIdMessage, IServerTimeMessage
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

			if (sendReply == null)
				throw new ArgumentNullException(nameof(sendReply));

			var replySent = false;

			foreach (var message in messages)
			{
				if (!replySent)
				{
					sendReply();
					replySent = true;
				}

				message.OriginalTransactionId = transactionId;
				message.SetSubscriptionIds(subscriptionId: transactionId);

				lastTime = message.ServerTime;

				newOutMessage(message);
			}

			return lastTime;
		}
	}
}