#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Sterling.Sterling
File: SterlingMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Sterling
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The message adapter for Sterling.
	/// </summary>
	public partial class SterlingMessageAdapter : MessageAdapter
	{
		private SterlingClient _client;

		/// <summary>
		/// Initializes a new instance of the <see cref="SterlingMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public SterlingMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.RemoveSupportedMessage(MessageTypes.SecurityLookup);
		}

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <returns>Order condition. If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new SterlingOrderCondition();
		}

		private void SessionOnOnStiShutdown()
		{
			SendOutError("Sterling is shutdown.");
		}

		/// <summary>
		/// Gets a value indicating whether the connector supports position lookup.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup => true;

		private void DisposeClient()
		{
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
		}

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
					if (_client != null)
					{
						try
						{
							DisposeClient();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_client = null;
					}

					SendOutMessage(new ResetMessage());

					break;
				}

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

					DisposeClient();
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