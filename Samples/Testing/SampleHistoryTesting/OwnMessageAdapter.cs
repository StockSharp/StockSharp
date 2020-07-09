namespace SampleHistoryTesting
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Messages;

	class OwnMessageAdapter : MessageAdapter
	{
		public OwnMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddSupportedMarketDataType(DataType.CandleTimeFrame);

			this.AddSupportedResultMessage(MessageTypes.SecurityLookup);
		}

		/// <inheritdoc />
		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

		private readonly HashSet<TimeSpan> _timeFrames = new HashSet<TimeSpan>(new[] { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5) });

		/// <inheritdoc />
		protected override IEnumerable<TimeSpan> GetTimeFrames(SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to)
			=> _timeFrames;

		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					SendOutMessage(new ResetMessage());
					break;

				case MessageTypes.Connect:
					SendOutMessage(new ConnectMessage());
					break;

				case MessageTypes.Disconnect:
					SendOutMessage(new DisconnectMessage());
					break;

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;

					foreach (var id in new[] { "SBER@TQBR" })
					{
						SendOutMessage(new SecurityMessage
						{
							SecurityId = id.ToSecurityId(),
							OriginalTransactionId = lookupMsg.TransactionId,
						});	
					}

					SendSubscriptionResult(lookupMsg);
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					switch (mdMsg.DataType)
					{
						case MarketDataTypes.CandleTimeFrame:
						{
							if (mdMsg.IsSubscribe)
							{
								var start = mdMsg.From.Value;
								var stop = mdMsg.To.Value;

								var tf = mdMsg.GetTimeFrame();

								// sends subscribed successfully
								SendSubscriptionReply(mdMsg.TransactionId);

								const decimal step = 0.01m;

								for (var curr = start; curr < stop; curr += tf)
								{
									var price = RandomGen.GetInt(100, 110);

									var open = price + RandomGen.GetInt(10) * step;
									var high = open + RandomGen.GetInt(10) * step;
									var low = high - RandomGen.GetInt(10) * step;
									var close = low + RandomGen.GetInt(10) * step;

									if (high < low)
									{
										var d = high;
										high = low;
										low = d;
									}

									SendOutMessage(new TimeFrameCandleMessage
									{
										OriginalTransactionId = mdMsg.TransactionId,
										OpenPrice = open,
										HighPrice = high,
										LowPrice = low,
										ClosePrice = close,
										OpenTime = curr,
										State = CandleStates.Finished,
									});
								}
							
								SendSubscriptionResult(mdMsg);
							}
							else
							{
								// sends unsubscribed successfully
								SendSubscriptionReply(mdMsg.TransactionId);
							}

							break;
						}
						default:
							// not supported other data types
							SendSubscriptionNotSupported(mdMsg.TransactionId);
							break;
					}

					break;
				}
			
				default:
					return false;
			}

			return true;
		}
	}
}