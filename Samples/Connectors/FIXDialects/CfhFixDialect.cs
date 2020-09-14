namespace StockSharp.Fix.Dialects
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Fix.Native;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// CFH FIX protocol dialect.
	/// </summary>
	[MediaIcon("CFH_logo.png")]
	[DisplayNameLoc(LocalizedStrings.CFHKey)]
#if !IGNORE_LICENSE
	[Licensing.LicenseFeature("CFH")]
#endif
	public class CfhFixDialect : BaseFixDialect
	{
		private static class CfhFixTags
		{
			public const FixTags QuoteRequestAction = (FixTags)5002;
			public const FixTags Balance = (FixTags)5020;
			public const FixTags AvailableForMarginTrading = (FixTags)5021;
			public const FixTags CreditLimit = (FixTags)5022;
			public const FixTags SecurityDeposit = (FixTags)5023;
			public const FixTags ClosedPnL = (FixTags)5024;
			public const FixTags OpenPnL = (FixTags)5025;
			public const FixTags MarginRequirement = (FixTags)5026;
			public const FixTags NetOpenPosition = (FixTags)5027;
		}

		private static class CfhFixMessages
		{
			public const string AccountInfoRequest = "AAA";
			public const string AccountInfo = "AAB";
		}

		private readonly FastDateTimeParser _expiryDateParser = new FastDateTimeParser("yyyyMMdd");

		/// <summary>
		/// Initializes a new instance of the <see cref="CfhFixDialect"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public CfhFixDialect(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator, Encoding.UTF8)
		{
		}

		/// <inheritdoc />
		public override IEnumerable<MessageTypeInfo> PossibleSupportedMessages { get; } = new[]
		{
			MessageTypes.MarketData.ToInfo(),
			//MessageTypes.SecurityLookup.ToInfo(),

			MessageTypes.Portfolio.ToInfo(),
			//MessageTypes.PortfolioLookup.ToInfo(),
			MessageTypes.OrderRegister.ToInfo(),
			MessageTypes.OrderReplace.ToInfo(),
			MessageTypes.OrderCancel.ToInfo(),
			//MessageTypes.OrderGroupCancel.ToInfo(),
			MessageTypes.OrderStatus.ToInfo(),

			//MessageTypes.ChangePassword.ToInfo(),

			FixMessageTypes.SeqReset.ToInfo(),
			FixMessageTypes.ResendRequest.ToInfo(),
		};

		/// <inheritdoc />
		public override IEnumerable<DataType> SupportedMarketDataTypes { get; set; } = new[]
		{
			DataType.MarketDepth,
			DataType.Level1,
		};

		/// <inheritdoc />
		protected override string OnWrite(IFixWriter writer, Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					switch (regMsg.OrderType)
					{
						case null:
						case OrderTypes.Limit:
						case OrderTypes.Market:
						case OrderTypes.Conditional:
							break;
						default:
							throw new NotSupportedException(LocalizedStrings.Str1601Params.Put(regMsg.OrderType, regMsg.TransactionId));
					}

					var securityId = regMsg.SecurityId;

					writer.Write(FixTags.ClOrdID);
					writer.Write(regMsg.TransactionId);

					writer.WriteHandlInst(regMsg);

					writer.Write(FixTags.Symbol);
					writer.Write(securityId.SecurityCode);

					writer.WriteSide(regMsg.Side);

					writer.WriteTransactTime(TimeStampParser);

					writer.Write(FixTags.OrderQty);
					writer.Write(regMsg.Volume);

					writer.Write(FixTags.OrdType);
					writer.Write(regMsg.GetFixType());

					if (regMsg.OrderType != OrderTypes.Market)
					{
						writer.Write(FixTags.Price);
						writer.Write(regMsg.Price);
					}

					var condition = (FixOrderCondition)regMsg.Condition;

					if (condition?.StopPrice != null)
					{
						writer.Write(FixTags.StopPx);
						writer.Write(condition.StopPrice.Value);
					}

					if (regMsg.Currency != null)
					{
						writer.Write(FixTags.Currency);
						writer.Write(regMsg.Currency.Value.To<string>());
					}

					var tif = regMsg.GetFixTimeInForce();

					writer.Write(FixTags.TimeInForce);
					writer.Write(tif);

					if (tif == FixTimeInForce.GoodTillDate)
					{
						writer.WriteExpiryDate(regMsg, _expiryDateParser, TimeZone);
					}

					WriteAccount(writer, regMsg);

					if (!regMsg.ClientCode.IsEmpty())
					{
						writer.Write(FixTags.NoPartyIDs);
						writer.Write(1);

						writer.Write(FixTags.PartyID);
						writer.Write(regMsg.ClientCode);

						writer.Write(FixTags.PartyIDSource);
						writer.Write(PartyIDSource.Mic);

						writer.Write(FixTags.PartyRole);
						writer.Write((int)PartyRole.ClientId);
					}

					return FixMessages.NewOrderSingle;
				}

				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;
					var securityId = cancelMsg.SecurityId;

					writer.Write(FixTags.ClOrdID);
					writer.Write(cancelMsg.TransactionId);

					if (cancelMsg.OriginalTransactionId != 0)
					{
						writer.Write(FixTags.OrigClOrdID);
						WriteClOrdId(writer, cancelMsg.OriginalTransactionId);
					}

					if (cancelMsg.OrderId != null)
					{
						writer.Write(FixTags.OrderID);
						writer.Write(cancelMsg.OrderId.Value);
					}

					writer.Write(FixTags.Symbol);
					writer.Write(securityId.SecurityCode);

					if (cancelMsg.Side != null)
					{
						writer.WriteSide(cancelMsg.Side.Value);
					}

					if (cancelMsg.Volume != null)
					{
						writer.Write(FixTags.OrderQty);
						writer.Write(cancelMsg.Volume.Value);
					}

					writer.WriteTransactTime(TimeStampParser);

					return FixMessages.OrderCancelRequest;
				}

				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;
					var securityId = replaceMsg.SecurityId;

					writer.Write(FixTags.ClOrdID);
					writer.Write(replaceMsg.TransactionId);

					if (replaceMsg.OriginalTransactionId != 0)
					{
						writer.Write(FixTags.OrigClOrdID);
						WriteClOrdId(writer, replaceMsg.OriginalTransactionId);
					}

					if (replaceMsg.OldOrderId != null)
					{
						writer.Write(FixTags.OrderID);
						writer.Write(replaceMsg.OldOrderId.Value);
					}

					writer.Write(FixTags.Symbol);
					writer.Write(securityId.SecurityCode);

					writer.WriteSide(replaceMsg.Side);

					writer.Write(FixTags.OrderQty);
					writer.Write(replaceMsg.Volume);

					writer.Write(FixTags.OrdType);
					writer.Write(replaceMsg.GetFixType());

					if (replaceMsg.OrderType != OrderTypes.Market)
					{
						writer.Write(FixTags.Price);
						writer.Write(replaceMsg.Price);
					}

					var condition = (FixOrderCondition)replaceMsg.Condition;

					if (condition?.StopPrice != null)
					{
						writer.Write(FixTags.StopPx);
						writer.Write(condition.StopPrice.Value);
					}

					var tif = replaceMsg.GetFixTimeInForce();

					writer.Write(FixTags.TimeInForce);
					writer.Write(tif);

					if (tif == FixTimeInForce.GoodTillDate)
					{
						writer.WriteExpiryDate(replaceMsg, _expiryDateParser, TimeZone);
					}

					writer.WriteTransactTime(TimeStampParser);

					return FixMessages.OrderCancelReplaceRequest;
				}

				case MessageTypes.OrderStatus:
				{
					var statusMsg = (OrderStatusMessage)message;

					if (statusMsg.OrderId != null || statusMsg.OriginalTransactionId != 0)
					{
						if (statusMsg.OrderId != null)
						{
							writer.Write(FixTags.OrderID);
							writer.Write(statusMsg.OrderId.Value);
						}

						if (statusMsg.OriginalTransactionId != 0)
						{
							writer.Write(FixTags.ClOrdID);
							writer.Write(statusMsg.OriginalTransactionId);
						}

						return FixMessages.OrderStatusRequest;
					}
					else
					{
						if (statusMsg.TransactionId != 0)
						{
							writer.Write(FixTags.MassStatusReqID);
							writer.Write(statusMsg.TransactionId);
						}

						writer.Write(FixTags.MassStatusReqType);
						writer.Write((int)MassStatusReqType.StatusForAllOrders);

						return FixMessages.OrderMassStatusRequest;
					}
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (!(mdMsg.DataType2 == DataType.Level1 || mdMsg.DataType2 == DataType.MarketDepth))
						return null;

					writer.Write(FixTags.QuoteReqID);
					writer.Write(mdMsg.TransactionId);

					writer.Write(CfhFixTags.QuoteRequestAction);
					writer.Write(mdMsg.IsSubscribe ? 0 : 1);

					return FixMessages.QuoteRequest;
				}

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;

					if (!pfMsg.IsSubscribe)
						return null;
					
					WriteAccount(writer, pfMsg);

					return CfhFixMessages.AccountInfoRequest;
				}

				default:
					return base.OnWrite(writer, message);
			}
		}

		/// <inheritdoc />
		protected override bool? OnRead(IFixReader reader, string msgType, Action<Message> messageHandler)
		{
			switch (msgType)
			{
				// reading custom CFH message (not compatible with FIX standard)
				case CfhFixMessages.AccountInfo:
				{
					string account = null;
					var sendingTime = default(DateTimeOffset);
					decimal? closedPnL = null;
					decimal? openPnL = null;
					decimal? balance = null;
					CurrencyTypes? currency = null;

					var isOk = reader.ReadMessage(tag =>
					{
						switch (tag)
						{
							case FixTags.Account:
								account = reader.ReadString();
								return true;
							case FixTags.SendingTime:
								sendingTime = reader.ReadUtc(TimeStampParser);
								return true;
							case CfhFixTags.ClosedPnL:
								closedPnL = reader.ReadDecimal();
								return true;
							case CfhFixTags.OpenPnL:
								openPnL = reader.ReadDecimal();
								return true;
							case CfhFixTags.Balance:
								balance = reader.ReadDecimal();
								return true;
							case FixTags.Currency:
								currency = reader.ReadString().FromMicexCurrencyName(this.AddErrorLog);
								return true;
							default:
								return false;
						}
					});

					if (!isOk)
						return null;

					var msg = new PositionChangeMessage
					{
						SecurityId = SecurityId.Money,
						PortfolioName = account,
						ServerTime = sendingTime
					}
					.TryAdd(PositionChangeTypes.RealizedPnL, closedPnL, true)
					.TryAdd(PositionChangeTypes.UnrealizedPnL, openPnL, true)
					.TryAdd(PositionChangeTypes.CurrentValue, balance, true);

					if (currency != null)
						msg.Add(PositionChangeTypes.Currency, currency.Value);

					messageHandler(msg);
					return true;
				}
				default:
					return base.OnRead(reader, msgType, messageHandler);
			}
		}
	}
}