#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.BitStamp
File: BitStampMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.BitStamp.Native;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The message adapter for BitStamp.
	/// </summary>
	public partial class BitStampMessageAdapter : MessageAdapter
	{
		private static readonly SecurityId _btcUsd = new SecurityId
		{
			SecurityCode = "BTC/USD",
			BoardCode = ExchangeBoard.BitStamp.Code,
		};

		private static readonly SecurityId _eurUsd = new SecurityId
		{
			SecurityCode = "EUR/USD",
			BoardCode = ExchangeBoard.BitStamp.Code,
		};

		private long _lastMyTradeId;
		private bool _hasActiveOrders;
		private bool _hasMyTrades;
		private bool _requestOrderFirst;
		private readonly Dictionary<long, RefPair<long, decimal>> _orderInfo = new Dictionary<long, RefPair<long, decimal>>();
		
		private HttpClient _httpClient;
		private PusherClient _pusherClient;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="BitStampMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public BitStampMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			// https://www.bitstamp.net/api/
			// Do not make more than 600 request per 10 minutes or we will ban your IP address.
			HeartbeatInterval = TimeSpan.FromSeconds(1);

			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
		}

		/// <summary>
		/// Gets a value indicating whether the connector supports security lookup.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup => true;

		/// <summary>
		/// Gets a value indicating whether the connector supports position lookup.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup => true;

		private void DisposePusherClient()
		{
			_pusherClient.PusherConnected -= SessionOnPusherConnected;
			_pusherClient.PusherDisconnected -= SessionOnPusherDisconnected;
			_pusherClient.PusherError -= SessionOnPusherError;
			_pusherClient.NewOrderBook -= SessionOnNewOrderBook;
			_pusherClient.NewTrade -= SessionOnNewTrade;

			_pusherClient.DisconnectPusher();
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
					_lastMyTradeId = 0;
					_hasActiveOrders = false;
					_hasMyTrades = false;
					_requestOrderFirst = false;
					_prevLevel1Time = default(DateTimeOffset);

					if (_httpClient != null)
					{
						try
						{
							_httpClient.Dispose();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_httpClient = null;
					}

					if (_pusherClient != null)
					{
						try
						{
							DisposePusherClient();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_pusherClient = null;
					}

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_httpClient != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					if (_pusherClient != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_httpClient = new HttpClient(ClientId, Key, Secret);

					_pusherClient = new PusherClient();

					_pusherClient.PusherConnected += SessionOnPusherConnected;
					_pusherClient.PusherDisconnected += SessionOnPusherDisconnected;
					_pusherClient.PusherError += SessionOnPusherError;
					_pusherClient.NewOrderBook += SessionOnNewOrderBook;
					_pusherClient.NewTrade += SessionOnNewTrade;

					_pusherClient.ConnectPusher();
					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_httpClient == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					if (_pusherClient == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					DisposePusherClient();
					_pusherClient = null;

					_httpClient.Dispose();
					_httpClient = null;

					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					ProcessPortfolioLookup((PortfolioLookupMessage)message);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					_lastMyTradeId = 0;
					_hasActiveOrders = true;
					_hasMyTrades = true;
					_requestOrderFirst = true;

					ProcessOrderStatus();
					break;
				}

				case MessageTypes.OrderRegister:
				{
					ProcessOrderRegister((OrderRegisterMessage)message);
					break;
				}

				case MessageTypes.OrderCancel:
				{
					ProcessOrderCancel((OrderCancelMessage)message);
					break;
				}

				case MessageTypes.Time:
				{
					if (_hasActiveOrders || _hasMyTrades)
					{
						ProcessOrderStatus();
						ProcessPortfolioLookup(null);
					}

					ProcessLevel1();
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					ProcessSecurityLookup((SecurityLookupMessage)message);
					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketData((MarketDataMessage)message);
					break;
				}
			}
		}

		private void SessionOnPusherConnected()
		{
			SendOutMessage(new ConnectMessage());
		}

		private void SessionOnPusherError(Exception exception)
		{
			SendOutError(exception);
		}

		private void SessionOnPusherDisconnected()
		{
			SendOutMessage(new DisconnectMessage());
		}
	}
}