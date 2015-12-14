#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.AlfaDirect.AlfaDirect
File: AlfaDirectMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.AlfaDirect
{
	using System;

	using Ecng.Common;
	using Ecng.Interop;

	using StockSharp.AlfaDirect.Native;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для AlfaDirect.
	/// </summary>
	public partial class AlfaDirectMessageAdapter : MessageAdapter
	{
		private AlfaWrapper _wrapper;

		/// <summary>
		/// Создать <see cref="AlfaDirectMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public AlfaDirectMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Platform = Platforms.x86;

			this.AddMarketDataSupport();
			this.AddTransactionalSupport();

			SecurityClassInfo.Add("FORTS", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.Forts.Code));
			SecurityClassInfo.Add("INDEX", RefTuple.Create(SecurityTypes.Index, ExchangeBoard.Micex.Code));
			SecurityClassInfo.Add("INDEX2", RefTuple.Create(SecurityTypes.Index, "INDEX"));
			SecurityClassInfo.Add("MICEX_SHR_T", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.Micex.Code));
			SecurityClassInfo.Add("RTS_STANDARD", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.Forts.Code));
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено <see langword="null"/>.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new AlfaOrderCondition();
		}

		private AlfaWrapper Wrapper
		{
			get { return _wrapper; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск портфелей.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup
		{
			get { return true; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск инструментов.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup
		{
			get { return true; }
		}

		private void DisposeWrapper()
		{
			_wrapper.StopExportOrders();
			_wrapper.StopExportPortfolios();
			_wrapper.StopExportMyTrades();

			_wrapper.Connected -= OnWrapperConnected;
			_wrapper.Disconnected -= OnWrapperDisconnected;
			_wrapper.ConnectionError -= OnConnectionError;

			_wrapper.ProcessOrder -= OnProcessOrders;
			_wrapper.ProcessOrderConfirmed -= OnProcessOrderConfirmed;
			_wrapper.ProcessOrderFailed -= OnProcessOrderFailed;
			_wrapper.ProcessPositions -= OnProcessPositions;
			_wrapper.ProcessMyTrades -= OnProcessMyTrades;

			_wrapper.ProcessNews -= OnProcessNews;
			_wrapper.ProcessSecurities += OnProcessSecurities;
			_wrapper.ProcessLevel1 -= OnProcessLevel1;
			_wrapper.ProcessQuotes -= OnProcessQuotes;
			_wrapper.ProcessTrades -= OnProcessTrades;
			_wrapper.ProcessCandles -= OnProcessCandles;

			_wrapper.Dispose();
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_alfaIds.Clear();
					_localIds.Clear();

					if (_wrapper != null)
					{
						try
						{
							DisposeWrapper();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_wrapper = null;
					}

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_wrapper != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_wrapper = new AlfaWrapper(this);

					_wrapper.Connected += OnWrapperConnected;
					_wrapper.Disconnected += OnWrapperDisconnected;
					_wrapper.ConnectionError += OnConnectionError;
					_wrapper.Error += SendOutError;

					_wrapper.ProcessOrder += OnProcessOrders;
					_wrapper.ProcessOrderConfirmed += OnProcessOrderConfirmed;
					_wrapper.ProcessOrderFailed += OnProcessOrderFailed;
					_wrapper.ProcessPositions += OnProcessPositions;
					_wrapper.ProcessMyTrades += OnProcessMyTrades;

					_wrapper.ProcessNews += OnProcessNews;
					_wrapper.ProcessSecurities += OnProcessSecurities;
					_wrapper.ProcessLevel1 += OnProcessLevel1;
					_wrapper.ProcessQuotes += OnProcessQuotes;
					_wrapper.ProcessTrades += OnProcessTrades;
					_wrapper.ProcessCandles += OnProcessCandles;

					if (_wrapper.IsConnected)
						SendOutMessage(new ConnectMessage());
					else if (!Wrapper.IsConnecting)
						_wrapper.Connect(Login, Password.To<string>());

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_wrapper == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					DisposeWrapper();
					_wrapper = null;
					
					SendOutMessage(new DisconnectMessage());

					break;
				}

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					// чтобы не было дедлока, RegisterOrder должен использовать только асинхронные
					// вызовы AlfaDirect, как CreateLimitOrder(... timeout=-1)
					lock (_localIds.SyncRoot)
					{
						var alfaTransactionId = Wrapper.RegisterOrder(regMsg);
						_localIds.Add(regMsg.TransactionId, alfaTransactionId);
					}

					break;
				}

				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					if (cancelMsg.OrderId == null)
						throw new InvalidOperationException(LocalizedStrings.Str2252Params.Put(cancelMsg.OrderTransactionId));

					Wrapper.CancelOrder(cancelMsg.OrderId.Value);
					break;
				}

				case MessageTypes.OrderGroupCancel:
				{
					var groupMsg = (OrderGroupCancelMessage)message;
					Wrapper.CancelOrders(groupMsg.IsStop, groupMsg.PortfolioName, groupMsg.Side, groupMsg.SecurityId, groupMsg.SecurityType);
					break;
				}

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;

					if (pfMsg.IsSubscribe)
						Wrapper.StartExportPortfolios();
					else
						this.AddWarningLog("ignore portfolios unsubscribe");

					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;
					Wrapper.LookupSecurities(lookupMsg.TransactionId);
					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					var lookupMsg = (PortfolioLookupMessage)message;
					Wrapper.LookupPortfolios(lookupMsg.TransactionId);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					Wrapper.LookupOrders();
					break;
				}
			}
		}

		private void OnWrapperDisconnected()
		{
			this.AddInfoLog(LocalizedStrings.Str2254);
			SendOutMessage(new DisconnectMessage());
		}

		private void OnWrapperConnected()
		{
			this.AddInfoLog(LocalizedStrings.Str2255);
			SendOutMessage(new ConnectMessage());
		}

		private void OnConnectionError(Exception ex)
		{
			this.AddInfoLog(LocalizedStrings.Str3458Params.Put(ex.Message));
			SendOutMessage(new ConnectMessage { Error = ex });
		}
	}
}