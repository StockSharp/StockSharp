using System;
using StockSharp.Messages;

namespace StockSharp.Sterling
{
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
					SessionHolder.Session.OnStiOrderConfirm += SessionOnStiOrderConfirm;
					SessionHolder.Session.OnStiOrderReject += SessionOnStiOrderReject;
					SessionHolder.Session.OnStiOrderUpdate += SessionOnStiOrderUpdate;
					SessionHolder.Session.OnStiTradeUpdate += SessionOnStiTradeUpdate;
					SessionHolder.Session.OnStiAcctUpdate += SessionOnStiAcctUpdate;
					SessionHolder.Session.OnStiPositionUpdate += SessionOnStiPositionUpdate;
					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					SessionHolder.Session.OnStiQuoteUpdate += SessionOnStiQuoteUpdate;
					SessionHolder.Session.OnStiQuoteSnap += SessionOnStiQuoteSnap;
					SessionHolder.Session.OnStiQuoteRqst += SessionOnStiQuoteRqst;
					SessionHolder.Session.OnStil2Update += SessionOnStil2Update;
					SessionHolder.Session.OnStil2Reply += SessionOnStil2Reply;
					SessionHolder.Session.OnStiGreeksUpdate += SessionOnStiGreeksUpdate;
					SessionHolder.Session.OnStiNewsUpdate += SessionOnStiNewsUpdate;
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			SessionHolder.Session.OnStiShutdown += SessionOnOnStiShutdown;
		}

		private void OnSessionUnInitialize()
		{
			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
				{
					SessionHolder.Session.OnStiOrderConfirm -= SessionOnStiOrderConfirm;
					SessionHolder.Session.OnStiOrderReject -= SessionOnStiOrderReject;
					SessionHolder.Session.OnStiOrderUpdate -= SessionOnStiOrderUpdate;
					SessionHolder.Session.OnStiTradeUpdate -= SessionOnStiTradeUpdate;
					SessionHolder.Session.OnStiAcctUpdate -= SessionOnStiAcctUpdate;
					SessionHolder.Session.OnStiPositionUpdate -= SessionOnStiPositionUpdate;
					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					SessionHolder.Session.OnStiQuoteUpdate -= SessionOnStiQuoteUpdate;
					SessionHolder.Session.OnStiQuoteSnap -= SessionOnStiQuoteSnap;
					SessionHolder.Session.OnStiQuoteRqst -= SessionOnStiQuoteRqst;
					SessionHolder.Session.OnStil2Update -= SessionOnStil2Update;
					SessionHolder.Session.OnStil2Reply -= SessionOnStil2Reply;
					SessionHolder.Session.OnStiGreeksUpdate -= SessionOnStiGreeksUpdate;
					SessionHolder.Session.OnStiNewsUpdate -= SessionOnStiNewsUpdate;
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			SessionHolder.Session.OnStiShutdown -= SessionOnOnStiShutdown;
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
					if (SessionHolder.Session == null)
					{
						_isSessionOwner = true;
						SessionHolder.Session = new SterlingSessionHolder.SterlingSession();
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
					{
						SendOutMessage(new DisconnectMessage());
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketData((MarketDataMessage) message);
					break;
				}

				case MessageTypes.OrderRegister:
				{
					ProcessOrderRegisterMessage((OrderRegisterMessage) message);
					break;
				}

				case MessageTypes.OrderCancel:
				{
					ProcessOrderCancelMessage((OrderCancelMessage) message);
					break;
				}

				case MessageTypes.OrderReplace:
				{
					ProcessOrderReplaceMessage((OrderReplaceMessage) message);
					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					var portfolios = SessionHolder.Session.GetPortfolios();

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
					ProcessSecurityMessage((SecurityMessage) message);
					break;
				}

				case MessageTypes.Execution:
				{
					ProcessExecutionMessage((ExecutionMessage) message);
					break;
				}

				case MessageTypes.Position:
				{
					ProcessPositionMessage((PositionMessage) message);
					break;
				}

				case MessageTypes.PositionChange:
				{
					ProcessPositionChangeMessage((PositionChangeMessage) message);
					break;
				}
			}
		}
	}
}