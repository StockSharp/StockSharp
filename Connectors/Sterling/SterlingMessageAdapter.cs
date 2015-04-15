namespace StockSharp.Sterling
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Адаптер сообщений для Sterling.
	/// </summary>
	public partial class SterlingMessageAdapter : MessageAdapter
	{
		private SterlingClient _client;

		/// <summary>
		/// Создать <see cref="SterlingMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public SterlingMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			CreateAssociatedSecurity = true;
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new SterlingOrderCondition();
		}

		private void SessionOnOnStiShutdown()
		{
			SendOutMessage(new ErrorMessage
			{
				Error = new Exception("Sterling is shutdown.")
			});
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		public override bool PortfolioLookupRequired
		{
			get { return IsTransactionEnabled; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="OrderStatusMessage"/> для получения списка заявок и собственных сделок.
		/// </summary>
		public override bool OrderStatusRequired
		{
			get { return IsTransactionEnabled; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск портфелей.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup
		{
			get { return true; }
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
					if (_client != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_client = new SterlingClient();

					_client.OnStiOrderConfirm += SessionOnStiOrderConfirm;
					_client.OnStiOrderReject += SessionOnStiOrderReject;
					_client.OnStiOrderUpdate += SessionOnStiOrderUpdate;
					_client.OnStiTradeUpdate += SessionOnStiTradeUpdate;
					_client.OnStiAcctUpdate += SessionOnStiAcctUpdate;
					_client.OnStiPositionUpdate += SessionOnStiPositionUpdate;

					_client.OnStiQuoteUpdate += SessionOnStiQuoteUpdate;
					_client.OnStiQuoteSnap += SessionOnStiQuoteSnap;
					_client.OnStiQuoteRqst += SessionOnStiQuoteRqst;
					_client.OnStil2Update += SessionOnStil2Update;
					_client.OnStil2Reply += SessionOnStil2Reply;
					_client.OnStiGreeksUpdate += SessionOnStiGreeksUpdate;
					_client.OnStiNewsUpdate += SessionOnStiNewsUpdate;

					_client.OnStiShutdown += SessionOnOnStiShutdown;

					SendOutMessage(new ConnectMessage());

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_client == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					_client.OnStiOrderConfirm -= SessionOnStiOrderConfirm;
					_client.OnStiOrderReject -= SessionOnStiOrderReject;
					_client.OnStiOrderUpdate -= SessionOnStiOrderUpdate;
					_client.OnStiTradeUpdate -= SessionOnStiTradeUpdate;
					_client.OnStiAcctUpdate -= SessionOnStiAcctUpdate;
					_client.OnStiPositionUpdate -= SessionOnStiPositionUpdate;

					_client.OnStiQuoteUpdate -= SessionOnStiQuoteUpdate;
					_client.OnStiQuoteSnap -= SessionOnStiQuoteSnap;
					_client.OnStiQuoteRqst -= SessionOnStiQuoteRqst;
					_client.OnStil2Update -= SessionOnStil2Update;
					_client.OnStil2Reply -= SessionOnStil2Reply;
					_client.OnStiGreeksUpdate -= SessionOnStiGreeksUpdate;
					_client.OnStiNewsUpdate -= SessionOnStiNewsUpdate;

					_client.OnStiShutdown -= SessionOnOnStiShutdown;
					_client = null;

					SendOutMessage(new DisconnectMessage());

					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketData((MarketDataMessage)message);
					break;
				}

				case MessageTypes.OrderRegister:
				{
					ProcessOrderRegisterMessage((OrderRegisterMessage)message);
					break;
				}

				case MessageTypes.OrderCancel:
				{
					ProcessOrderCancelMessage((OrderCancelMessage)message);
					break;
				}

				case MessageTypes.OrderReplace:
				{
					ProcessOrderReplaceMessage((OrderReplaceMessage)message);
					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					ProcessPortfolioLookupMessage((PortfolioLookupMessage)message);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					ProcessOrderStatusMessage();
					break;
				}
			}
		}
	}
}