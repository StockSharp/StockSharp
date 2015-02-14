namespace StockSharp.AlfaDirect.Native
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Runtime.InteropServices;

	using ADLite;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	sealed class AlfaWrapper : Disposable
	{
		private readonly AlfaTable _tableDepth, _tableSecurities, _tableLevel1, _tableTrades, _tableOrders, _tablePositions, _tableMyTrades, _tableNews, _tableAccounts;

		public FieldList FieldsDepth      {get {return _tableDepth.Fields;}}
		public FieldList FieldsSecurities {get {return _tableSecurities.Fields;}}
		public FieldList FieldsLevel1     {get {return _tableLevel1.Fields;}}
		public FieldList FieldsTrades     {get {return _tableTrades.Fields;}}
		public FieldList FieldsOrders     {get {return _tableOrders.Fields;}}
		public FieldList FieldsPositions  {get {return _tablePositions.Fields;}}
		public FieldList FieldsMyTrades   {get {return _tableMyTrades.Fields;}}
		public FieldList FieldsNews       {get {return _tableNews.Fields;}}
		public FieldList FieldsAccounts   {get {return _tableAccounts.Fields;}}

		private readonly ILogReceiver _logReceiver;

		//private readonly PairSet<string, string> _securityCurrencies = new PairSet<string, string>();

		private readonly AlfaDirectClass _ad;
		private readonly CultureInfo _sysCulture;
		private readonly AlfaDirectSessionHolder _sessionHolder;

		private ConnectionStates _connState;

		public bool IsConnected
		{
			get { return _ad.Connected; }
		}

		public bool IsConnecting
		{
			get { return _connState == ConnectionStates.Connecting; }
		}

		public AlfaWrapper(AlfaDirectSessionHolder sessionHolder, ILogReceiver logReceiver)
		{
			if (logReceiver == null)
				throw new ArgumentNullException("logReceiver");

			_sessionHolder = sessionHolder;
			_logReceiver = logReceiver;

			_sysCulture = ThreadingHelper.GetSystemCulture();

			_logReceiver.AddInfoLog(LocalizedStrings.Str2270Params, _sysCulture);

			_ad = new AlfaDirectClass();
			_ad.OnConnectionChanged += OnConnectionChanged;
			_ad.OnTableChanged += OnTableChanged;
			_ad.OrderConfirmed += OrderConfirmed;

			_tableSecurities = new AlfaTable(_ad, _logReceiver, AlfaTable.TableName.papers, "paper_no, p_code, ansi_name, place_code, at_code, lot_size, i_last_update, expired, mat_date, price_step, base_paper_no, go_buy, go_sell, price_step_cost, curr_code, strike");
			_tableDepth      = new AlfaTable(_ad, _logReceiver, AlfaTable.TableName.queue,  "paper_no, sell_qty, price, buy_qty");
			_tableLevel1     = new AlfaTable(_ad, _logReceiver, AlfaTable.TableName.fin_info, "paper_no, status, go_buy, go_sell, open_pos_qty, open_price, close_price, sell, sell_qty, buy, buy_qty, min_deal, max_deal, lot_size, volatility, theor_price, last_price, last_qty, last_update_date, last_update_time, price_step, buy_sqty, sell_sqty, buy_count, sell_count", "trading_status");
			_tableTrades     = new AlfaTable(_ad, _logReceiver, AlfaTable.TableName.all_trades, "paper_no, trd_no, qty, price, ts_time, b_s", "b_s_num");
			_tableOrders     = new AlfaTable(_ad, _logReceiver, AlfaTable.TableName.orders, "ord_no, acc_code, paper_no, status, b_s, price, qty, rest, ts_time, comments, place_code, stop_price, avg_trd_price, blank, updt_grow_price, updt_down_price, updt_new_price, trailing_level, trailing_slippage", "order_status, b_s_str");
			_tablePositions  = new AlfaTable(_ad, _logReceiver, AlfaTable.TableName.balance, "acc_code, p_code, place_code, paper_no, income_rest, real_rest, forword_rest, pl, profit_vol, income_vol, real_vol, open_vol, var_margin, balance_price");
			_tableMyTrades   = new AlfaTable(_ad, _logReceiver, AlfaTable.TableName.trades, "trd_no, ord_no, treaty, paper_no, price, qty, b_s, ts_time", "b_s_str");
			_tableNews       = new AlfaTable(_ad, _logReceiver, AlfaTable.TableName.news, "new_no, provider, db_data, subject, body");
			_tableAccounts   = new AlfaTable(_ad, _logReceiver, AlfaTable.TableName.accounts, "treaty");

			_connState = IsConnected ? ConnectionStates.Connected : ConnectionStates.Disconnected;

			_logReceiver.AddInfoLog("AlfaDirect {0}", _ad.Version.ToString());
		}

		protected override void DisposeManaged()
		{
			_ad.OnConnectionChanged -= OnConnectionChanged;
			_ad.OnTableChanged -= OnTableChanged;
			_ad.OrderConfirmed -= OrderConfirmed;
			
			_logReceiver.AddInfoLog("Releasing AlfaDirect COM object...");
			
			try
			{
				Marshal.FinalReleaseComObject(_ad);
			}
			catch (Exception e)
			{
				_logReceiver.AddWarningLog("error releasing COM object: {0}", e);
			}

			base.DisposeManaged();
		}

		public event Action<string[]> ProcessLevel1;
		public event Action<long, string[]> ProcessSecurities;
		public event Action<string[]> ProcessTrades;
		public event Action<string, string[]> ProcessQuotes;
		public event Action<string[]> ProcessOrder;
		public event Action<int, int> ProcessOrderConfirmed;
		public event Action<long, string[]> ProcessPositions;
		public event Action<string[]> ProcessMyTrades;
		public event Action<string[]> ProcessNews;
		public event Action<MarketDataMessage, string[]> ProcessCandles;
		public event Action<int, string> ProcessOrderFailed;

		public event Action Connected;
		public event Action Disconnected;
		public event Action<Exception> ConnectionError;
		public event Action<Exception> Error;

		private void DoInSysCulture(Action action)
		{
			_sysCulture.DoInCulture(action);
		}

		private void OnTableChanged(string tableName, string tableParams, object data, object fieldTypes)
		{ 
			try
			{
				if (data == null)
					return;

				_logReceiver.AddDebugLog("OnTableChanged: TN={0} WR={1} DT={2} FT={3}", tableName, tableParams, data, fieldTypes);

				DoInSysCulture(() =>
				{
					var rows = ((string)data).ToRows();

					if (tableName == _tableDepth.Name)
						ProcessQuotes.SafeInvoke(tableParams, rows);
					else if (tableName == _tableTrades.Name)
						ProcessTrades.SafeInvoke(rows);
					else if (tableName == _tableSecurities.Name)
						ProcessSecurities.SafeInvoke(0, rows);
					else if (tableName == _tableLevel1.Name)
						ProcessLevel1.SafeInvoke(rows);
					else if (tableName == _tablePositions.Name)
						ProcessPositions.SafeInvoke(0, rows);
					else if (tableName == _tableOrders.Name)
						ProcessOrder.SafeInvoke(rows);
					else if (tableName == _tableMyTrades.Name)
						ProcessMyTrades.SafeInvoke(rows);
					else if (tableName == _tableNews.Name)
						ProcessNews.SafeInvoke(rows);
				});
			}
			catch (Exception e)
			{
				_logReceiver.AddErrorLog(LocalizedStrings.Str2271Params, e);
				Error.SafeInvoke(e);
			}
		}

		private void OnConnectionChanged(eConnectionState state)
		{
			try
			{
				_logReceiver.AddInfoLog("OnConnectionChanged {0}", state);

				switch (state)
				{
					case eConnectionState.Connected:
						_connState = ConnectionStates.Connected;
						Connected.SafeInvoke();
						break;
					case eConnectionState.Disconnected:
						if (_connState == ConnectionStates.Disconnecting)
							Disconnected.SafeInvoke();
						else
							ConnectionError.SafeInvoke(new AlfaException(ADLite.tagStateCodes.stcNotConnected, LocalizedStrings.Str1611));

						_connState = ConnectionStates.Disconnected;

						break;
					default:
						ConnectionError.SafeInvoke(new InvalidOperationException("Error eConnectionState: " + state));
						break;
				}
			}
			catch (Exception e)
			{
				_logReceiver.AddErrorLog(LocalizedStrings.Str2273Params, e);
				Error.SafeInvoke(e);
			}
		}

		public void Connect(string login, string password)
		{
			if (_ad.Connected)
			{
				_connState = ConnectionStates.Connected;
				Connected.SafeInvoke();
				return;
			}

			if (!login.IsEmpty() && !password.IsEmpty())
			{
				_ad.UserName = login;
				_ad.Password = password;
			}

			_connState = ConnectionStates.Connecting;
			_ad.Connected = true;
		}

		public void Disconnect()
		{
			_connState = ConnectionStates.Disconnecting;
			_ad.Connected = false;
		}

		private void ThrowInError(tagStateCodes code, string message = null)
		{
			if (code != tagStateCodes.stcSuccess)
				throw new AlfaException(code, !message.IsEmpty() ? message : _ad.LastResultMsg);
		}

		public int RegisterOrder(OrderRegisterMessage message)
		{
			var marketTime = _ad.SessionTime;

			var secCode = message.SecurityId.SecurityCode;
			var account = message.PortfolioName.AccountFromPortfolioName(); // Портфель
			var placeCode = _sessionHolder.GetSecurityClass(message.SecurityType, message.SecurityId.BoardCode);
			var endDate = (message.TillDate != DateTimeOffset.MaxValue
				? marketTime.Date.AddTicks(new TimeSpan(23, 55, 00).Ticks)
				: message.TillDate).ToLocalTime(TimeHelper.Moscow); // Срок действия поручения.
			var maxEndDate = DateTime.Now + TimeSpan.FromDays(365);
			if (endDate > maxEndDate)
				endDate = maxEndDate;
			var comments = message.TransactionId.To<string>(); // Комментарий.
			var currency = message.Currency == null || message.Currency == CurrencyTypes.RUB ? 
				"RUR" : message.Currency.ToString();//_securityCurrencies[message.SecurityId.SecurityCode]; // Валюта цены.
			var buySell = message.Side.ToAlfaDirect(); // Покупка/Продажа
			var quantity = (int)message.Volume; // Количество.
			var price = message.Price; // Цена.

			int id;

			_logReceiver.AddInfoLog("Register: {0} {1}/{2} tran={3}  {4} {5}@{6}, mtime={7}, cur={8}", account, secCode, placeCode, comments, buySell, quantity, price, marketTime, currency);

			if (placeCode == null)
				throw new InvalidOperationException(LocalizedStrings.Str2274Params.Put(message.TransactionId));

			if (message.OrderType == OrderTypes.Conditional)
			{
				var condition = (AlfaOrderCondition)message.Condition;

				if (condition.TargetPrice != 0)
				{
					id = _ad.CreateStopOrder(account, placeCode, secCode, endDate, comments, currency, buySell, quantity,
						(double)condition.StopPrice, (double)condition.Slippage, condition.TargetPrice, -1);
				}
				else if (condition.Level != 0)
				{
					id = _ad.CreateTrailingOrder(account, placeCode, secCode, endDate, comments, currency, buySell, quantity,
						(double)condition.StopPrice, (double)condition.Level, (double)condition.Slippage, -1);
				}
				else
				{
					id = _ad.CreateStopOrder(account, placeCode, secCode, endDate, comments, currency, buySell, quantity,
						(double)condition.StopPrice, (double)condition.Slippage, null, -1);
				}
			}
			else
			{
				if (message.OrderType == OrderTypes.Market)
					throw new InvalidOperationException(LocalizedStrings.Str2275);

				id = _ad.CreateLimitOrder(account, placeCode, secCode, endDate, comments, currency, buySell, quantity,
					(double)price, null, null, null, null, null, "Y", null, null, null, null, null, null, null, null, null, null, -1);
			}

			if (id == 0)
				ThrowInError(tagStateCodes.stcClientError, LocalizedStrings.Str2276Params.Put(message.TransactionId, _ad.LastResultMsg));

			return id;
		}

		public void CancelOrder(long orderId)
		{
			_ad.DropOrder(orderId, null, null, null, null, null, -1);
		}

		public void CancelOrders(bool? isStopOrder, string portfolioName, Sides? side, SecurityId securityId, SecurityTypes? securityType)
		{
			_logReceiver.AddDebugLog("CancelOrders: stop={0}, portf={1}, side={2}, id={3}", isStopOrder, portfolioName, side, securityId);

			var isBuySell = (side == null) ? null : side.Value.ToAlfaDirect();
			var account = portfolioName.IsEmpty() ? null : portfolioName.AccountFromPortfolioName();
			var pCode = securityId.SecurityCode.IsEmpty() ? null : securityId.SecurityCode;

			var treaties = new List<string>();
			var treaty = account.IsEmpty() ? null : account.TreatyFromAccount();

			if (!treaty.IsEmpty())
			{
				treaties.Add(treaty);
			}
			else
			{
				var data = _tableAccounts.GetLocalDbData();
				treaties.AddRange(data.Select(row => FieldsAccounts.Treaty.GetStrValue(row.ToColumns())));
			}

			if (!treaties.Any())
				throw new InvalidOperationException(LocalizedStrings.Str2277Params.Put(portfolioName));

			string placeCode = null;
			if (!securityId.IsDefault())
			{
				placeCode = _sessionHolder.GetSecurityClass(securityType, securityId.BoardCode);

				if (placeCode == null)
					throw new InvalidOperationException(LocalizedStrings.Str2278);
			}

			foreach (var t in treaties)
				_ad.DropOrder(null, isBuySell, t, account, placeCode, pCode, -1);
		}

		public void LookupCandles(MarketDataMessage message)
		{
			var placeCode = _sessionHolder.GetSecurityClass(message.SecurityType, message.SecurityId.BoardCode);
			_logReceiver.AddDebugLog("Candles SC={0} PC={1} TF={2} F={3} T={4}", message.SecurityId.SecurityCode, placeCode, message.Arg, message.From, message.To);

			if (placeCode == null)
				throw new InvalidOperationException(LocalizedStrings.Str2279);

			var timeFrame = (AlfaTimeFrames)(TimeSpan)message.Arg;
			//to = timeFrame.GetCandleBounds(series.Security).Min;

			var data = _ad.GetArchiveFinInfoFromDB(placeCode, message.SecurityId.SecurityCode, timeFrame.Interval, message.From.Convert(TimeHelper.Moscow), message.To.Convert(TimeHelper.Moscow));

			if (_ad.LastResult != StateCodes.stcSuccess)
				ThrowInError((tagStateCodes)_ad.LastResult);

			_logReceiver.AddDebugLog("Candles DT={0}", data);

			DoInSysCulture(() => ProcessCandles.SafeInvoke(message, data.ToRows()));
		}

		private void OrderConfirmed(int alfaTransactionId, int orderNum, string message, eCommandResult status)
		{
			try
			{
				_logReceiver.AddInfoLog("OrderConfirmed ID={0} Num={1} Msg={2} Status={3}", alfaTransactionId, orderNum, message, status);

				if (status == eCommandResult.crSuccess)
				{
					if (orderNum == 0)
						return; // group cancel confirmation

					DoInSysCulture(() => ProcessOrderConfirmed.SafeInvoke(alfaTransactionId, orderNum));
				}
				else
				{
					DoInSysCulture(() => ProcessOrderFailed.SafeInvoke(alfaTransactionId, message));
				}
			}
			catch (Exception e)
			{
				_logReceiver.AddErrorLog(LocalizedStrings.Str2280Params, e);
				Error.SafeInvoke(e);
			}
		}

		public void LookupSecurities(long transactionId)
		{
			const string where = "at_code in (A,P,FC,FD,I,OCM,OPM) and place_code in (MICEX_SHR_T,FORTS,RTS_STANDARD,INDEX,INDEX2)";

			// загрузить инструменты
			DoInSysCulture(() => ProcessSecurities.SafeInvoke(transactionId, _tableSecurities.GetLocalDbData(where)));

			_tableSecurities.ReRegisterTable();
		}

		public void LookupPortfolios(long transactionId)
		{
			// Прочитать позиции
			var data = _tablePositions.GetLocalDbData();
			DoInSysCulture(() => ProcessPositions.SafeInvoke(transactionId, data));
		}

		public void LookupOrders()
		{
			// Загрузить список всех заявок.
			DoInSysCulture(() => ProcessOrder.SafeInvoke(_tableOrders.GetLocalDbData()));

			// Загрузить список всех сделок.
			DoInSysCulture(() => ProcessMyTrades.SafeInvoke(_tableMyTrades.GetLocalDbData()));

			StartExportOrders();
			StartExportMyTrades();
		}

		#region export start/stop

		public void StartExportOrders()
		{
			_tableOrders.ReRegisterTable();
		}

		public void StopExportOrders()
		{
			_tableOrders.UnRegisterTable();
		}

		public void StartExportMyTrades()
		{
			_tableMyTrades.ReRegisterTable();
		}

		public void StopExportMyTrades()
		{
			_tableMyTrades.UnRegisterTable();
		}

		public void StartExportPortfolios()
		{
			_tablePositions.ReRegisterTable();
		}

		public void StopExportPortfolios()
		{
			_tablePositions.UnRegisterTable();
		}

		public void RegisterLevel1(int paperNo)
		{
			_tableLevel1.FilterPapers.Add(paperNo);
			_tableLevel1.ReRegisterTable();
		}

		public void UnRegisterLevel1(int paperNo)
		{
			_tableLevel1.FilterPapers.Remove(paperNo);
			_tableLevel1.ReRegisterTable();
		}

		public void RegisterMarketDepth(int paperNo)
		{
			_tableDepth.FilterPapers.Add(paperNo);
			_tableDepth.ReRegisterTable();
		}

		public void UnRegisterMarketDepth(int paperNo)
		{
			_tableDepth.FilterPapers.Remove(paperNo);
			_tableDepth.ReRegisterTable();
		}

		public void RegisterTrades(int paperNo)
		{
			_tableTrades.FilterPapers.Add(paperNo);
			_tableTrades.ReRegisterTable();
		}

		public void UnRegisterTrades(int paperNo)
		{
			_tableTrades.FilterPapers.Remove(paperNo);
			_tableTrades.ReRegisterTable();
		}

		public void StartExportNews()
		{
			// Загрузить новости
			var data = _tableNews.GetLocalDbData();
			DoInSysCulture(() => ProcessNews.SafeInvoke(data));
			_tableNews.ReRegisterTable();
		}

		public void StopExportNews()
		{
			_tableNews.UnRegisterTable();
		}

		#endregion
	}
}