namespace StockSharp.Sterling
{
	using System;

	using Ecng.Common;

	using SterlingLib;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для Sterling.
	/// </summary>
	public partial class SterlingMessageAdapter : MessageAdapter<SterlingSessionHolder>
	{
		private bool _isSessionOwner;

		/// <summary>
		/// Создать <see cref="SterlingMessageAdapter"/>.
		/// </summary>
		/// <param name="type">Тип адаптера.</param>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public SterlingMessageAdapter(MessageAdapterTypes type, SterlingSessionHolder sessionHolder)
			: base(type, sessionHolder)
		{
			SessionHolder.Initialize += OnSessionInitialize;
			SessionHolder.UnInitialize += OnSessionUnInitialize;
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			SessionHolder.Initialize -= OnSessionInitialize;
			SessionHolder.UnInitialize -= OnSessionUnInitialize;

			base.DisposeManaged();
		}

		private void OnSessionInitialize()
		{
			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
				{
					SessionHolder.Session.OnSTIOrderConfirmMsg += SessionOnStiOrderConfirmMsg;
					SessionHolder.Session.OnSTIOrderRejectMsg += SessionOnStiOrderRejectMsg;
					SessionHolder.Session.OnSTIOrderUpdateMsg += SessionOnStiOrderUpdateMsg;
					SessionHolder.Session.OnSTITradeUpdateMsg += SessionOnStiTradeUpdateMsg;
					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void OnSessionUnInitialize()
		{
			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
				{
					SessionHolder.Session.OnSTIOrderConfirmMsg -= SessionOnStiOrderConfirmMsg;
					SessionHolder.Session.OnSTIOrderRejectMsg -= SessionOnStiOrderRejectMsg;
					SessionHolder.Session.OnSTIOrderUpdateMsg -= SessionOnStiOrderUpdateMsg;
					SessionHolder.Session.OnSTITradeUpdateMsg -= SessionOnStiTradeUpdateMsg;
					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
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
				{
					if (SessionHolder.Session == null)
					{
						_isSessionOwner = true;
						SessionHolder.Session = new STIEventsClass();
						SendOutMessage(new ConnectMessage());
					}
					else
					{
						SendOutMessage(new ConnectMessage());
					}

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_isSessionOwner)
					{
						SessionHolder.Session = null;
						SendOutMessage(new DisconnectMessage());
					}
					else
						SendOutMessage(new DisconnectMessage());

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					switch (mdMsg.DataType)
					{
						case MarketDataTypes.Level1:
						{
							
							break;
						}
						case MarketDataTypes.MarketDepth:
							break;
						case MarketDataTypes.Trades:
							break;
						case MarketDataTypes.OrderLog:
							break;
						case MarketDataTypes.CandleTimeFrame:
							break;
						default:
							throw new ArgumentOutOfRangeException("message", mdMsg.DataType, LocalizedStrings.Str1618);
					}

					SendOutMessage(new MarketDataMessage
					{
						OriginalTransactionId = mdMsg.TransactionId,
					});

					break;
				}

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;
					var condition = (SterlingOrderCondition)regMsg.Condition;

					STIOrder order = new STIOrderClass();
					order.Account = regMsg.PortfolioName;
					order.Quantity = (int)regMsg.Volume;
					order.Display = (int)regMsg.VisibleVolume;
					order.ClOrderID = regMsg.TransactionId.To<string>();
					order.LmtPrice = (double)regMsg.Price;
					order.Symbol = regMsg.SecurityId.SecurityCode;
					order.Destination = regMsg.SecurityId.BoardCode;
					order.Tif = regMsg.TimeInForce.ToSterlingTif(regMsg.TillDate);
					order.PriceType = regMsg.OrderType.ToSterlingPriceType(condition);
					order.User = regMsg.Comment;

					if (regMsg.TillDate != DateTimeOffset.MaxValue)
						order.EndTime = regMsg.TillDate.ToString("yyyyMMdd");

					if (regMsg.Currency != null)
						order.Currency = regMsg.Currency.ToString();

					if (regMsg.OrderType == OrderTypes.Conditional)
					{
						//order.Discretion = condition.Discretion;
						//order.ExecInst = condition.ExecutionInstruction;
						//order.ExecBroker = condition.ExecutionBroker;
						//order.ExecPriceLmt = condition.ExecutionPriceLimit;
						//order.PegDiff = condition.PegDiff;
						//order.TrailAmt = condition.TrailingVolume;
						//order.TrailInc = condition.TrailingIncrement;
						//order.StpPrice = (double)(condition.StopPrice ?? 0);
						//order.MinQuantity = condition.MinVolume;
						//order.AvgPriceLmt = condition.AveragePriceLimit;
						//order.Duration = condition.Duration;

						//order.LocateBroker = condition.LocateBroker;
						//order.LocateQty = condition.LocateVolume;
						//order.LocateTime = condition.LocateTime;

						//order.OpenClose = condition.Options.IsOpen;
						//order.Maturity = condition.Options.Maturity;
						//order.PutCall = condition.Options.Type;
						//order.Underlying = condition.Options.UnderlyingCode;
						//order.CoverUncover = condition.Options.IsCover;
						//order.Instrument = condition.Options.UnderlyingType;
						//order.StrikePrice = condition.Options.StrikePrice;
					}

					order.SubmitOrder();
					break;
				}

				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;
					//new STIOrderMaintClass().

					break;
				}

				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;
					

					break;
				}
			}
		}
	}
}