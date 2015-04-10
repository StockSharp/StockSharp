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
		/// <see langword="true"/>, если сессия используется для получения маркет-данных, иначе, <see langword="false"/>.
		/// </summary>
		public override bool IsMarketDataEnabled
		{
			get { return true; }
		}

		/// <summary>
		/// <see langword="true"/>, если сессия используется для отправки транзакций, иначе, <see langword="false"/>.
		/// </summary>
		public override bool IsTransactionEnabled
		{
			get { return true; }
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
					var portfolios = _client.GetPortfolios();

					foreach (var portfolio in portfolios)
					{
						SendOutMessage(new PortfolioMessage
						{
							PortfolioName = portfolio.bstrAcct,
							State = PortfolioStates.Active // ???
						});
					}

					break;
				}

				case MessageTypes.Security:
				{
					ProcessSecurityMessage((SecurityMessage)message);
					break;
				}

				case MessageTypes.Execution:
				{
					ProcessExecutionMessage((ExecutionMessage)message);
					break;
				}

				case MessageTypes.Position:
				{
					ProcessPositionMessage((PositionMessage)message);
					break;
				}

				case MessageTypes.PositionChange:
				{
					ProcessPositionChangeMessage((PositionChangeMessage)message);
					break;
				}
			}
		}
	}
}