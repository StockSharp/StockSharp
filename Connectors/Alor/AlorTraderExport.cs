namespace StockSharp.Alor
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Alor.Metadata;
	using StockSharp.BusinessEntities;

	using TEClientLib;

	using StockSharp.Localization;

	public sealed partial class AlorTrader
	{
		private void Async(Action handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			handler.DoAsync(RaiseProcessDataError);
		}

		private void StartExportTables()
		{
			OpenTable(SecuritiesTable, OnSecurity);
			OpenTable(OrdersTable, OnOrder);
			OpenTable(StopOrdersTable, OnStopOrder);
			OpenTable(TradesTable, OnTrade);
			OpenTable(MyTradesTable, OnMyTrade);
			OpenTable(PortfoliosTable, OnPortfolio);
			OpenTable(HoldingTable, OnHolding);
			OpenTable(MoneyPositionTable, OnMoneyPosition);
		}

		/// <summary>
		/// Запустить экспорт данных из торговой системы в программу (получение портфелей, инструментов, заявок и т.д.).
		/// </summary>
		protected override void OnStartExport()
		{
			StartExportTables();
			RaiseExportStarted();
		}

		/// <summary>
		/// Остановить экспорт данных из торговой системы в программу, запущенный через <see cref="M:StockSharp.BusinessEntities.ITrader.StartExport"/>.
		/// </summary>
		protected override void OnStopExport()
		{
			CloseTable(SecuritiesTable);
			CloseTable(OrdersTable);
			CloseTable(StopOrdersTable);
			CloseTable(TradesTable);
			CloseTable(MyTradesTable);
			CloseTable(PortfoliosTable);
			CloseTable(HoldingTable);
			CloseTable(MoneyPositionTable);

			RaiseExportStopped();
		}

		private void Synchronized()
		{
			if (ConnectionState == ConnectionStates.Connected)
			{
				_serverType = _slot.ServerType;
				OpenTable(TimeTable, OnTime);

				if (ExportState == ConnectionStates.Connected)
					StartExportTables();
			}
		}

		private void OpenTable(AlorTable table, Action<int, object[]> handler, bool openTable = true)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			var slotTables = (ISlotTables)_slot.tables;
			if (slotTables == null)
				throw new ArgumentException(LocalizedStrings.Str3701);

			for (var i = 1; i < (slotTables.Count + 1); i++)
			{
				var metaTable = (SlotTable)slotTables.Item[i];

				if (table.Name == metaTable.Name)
				{
					table.MetaTable = metaTable;
					break;
				}
			}

			var meta = table.MetaTable;

			if (meta == null)
				throw new ArgumentException(LocalizedStrings.Str3702Params.Put(table.Name));

			if (openTable && handler == null)
				throw new ArgumentNullException("handler");

			Action<int, int, object> safeHandler = (openIdLocal, rowIdLocal, fieldsLocal) => Async(() =>
			{
				RaiseNewDataExported();
				ProcessEvents(() => handler(openIdLocal, (object[])fieldsLocal));
			});

			table.Columns = ManagerColumns.GetColumnsBy(table.Type).ToList();

			if (openTable)
			{
				meta.AddRow += (openId, rowId, fields) => safeHandler(openId, rowId, fields);
				meta.UpdateRow += (openId, rowId, fields) => safeHandler(openId, rowId, fields);
				meta.Error += (openId, errorCode, errorDescription) => RaiseProcessDataError(new AlorException(errorDescription, (SFE)errorCode));

				meta.Open(_slot.ID, meta.Name);

				if (meta.ID == 0)
					throw new ArgumentException(LocalizedStrings.Str3703Params.Put(table.Name), "table");
			}
			else
			{
				meta.AddRow += OnQuote;
				meta.UpdateRow += OnQuote;
			}
		}

		private Security GetSecurity(AlorTable table, object[] values, AlorColumn codeColumn, AlorColumn boardColumn)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			if (values == null)
				throw new ArgumentNullException("values");

			if (codeColumn == null)
				throw new ArgumentNullException("codeColumn");

			if (boardColumn == null)
				throw new ArgumentNullException("boardColumn");

			var secCode = table.GetValue<string>(values, codeColumn);
			var secBoard = ExchangeBoard.GetBoard(table.GetValue<string>(values, boardColumn));

			var id = SecurityIdGenerator.GenerateId(secCode, secBoard);

			return GetSecurity(id, name =>
			{
				var security = EntityFactory.CreateSecurity(name);
				security.Board = secBoard;
				security.Code = secCode;
				return security;
			}, security => false);
		}

		private Security GetSecurityBySecCode(string secCode)
		{
			var sfxSecurity = new SfxSecurity
			{
				SlotID = _slot.ID,
				SecCode = secCode
			};

			sfxSecurity.Load().ThrowIfNeed(LocalizedStrings.Str3704);
			secCode = sfxSecurity.SecCode;

			var secBoard = ExchangeBoard.GetBoard(sfxSecurity.SecBoard);
			var id = SecurityIdGenerator.GenerateId(secCode, secBoard);

			return GetSecurity(id, name =>
			{
				var security = EntityFactory.CreateSecurity(name);

				security.Board = secBoard;
				security.Code = secCode;
	
				return security;

			}, security => false);
		}

		private void OnSecurity(int tableId, object[] values)
		{
			var table = SecuritiesTable;

			var secCode = table.GetValue<string>(values, AlorSecurityColumns.Code);
			var secBoard = ExchangeBoard.GetBoard(table.GetValue<string>(values, AlorSecurityColumns.Board));

			var id = SecurityIdGenerator.GenerateId(secCode, secBoard);

			GetSecurity(id, name =>
			{
				var security = EntityFactory.CreateSecurity(name);

				security.Code = table.GetValue<string>(values, AlorSecurityColumns.Code);
				security.Name = table.GetValue<string>(values, AlorSecurityColumns.Name);
				security.ShortName = table.GetValue<string>(values, AlorSecurityColumns.ShortName);
				security.Board = secBoard;
				security.ExpiryDate = table.GetValue<DateTime>(values, AlorSecurityColumns.ExecutionDate);

				security.Decimals = table.GetValue<int>(values, AlorSecurityColumns.Decimals);
				security.Multiplier = table.GetValue<int>(values, AlorSecurityColumns.MinLotSize);
				security.MinStepPrice = table.GetValue<decimal>(values, AlorSecurityColumns.MinStepPrice);
				security.PriceStep = table.GetValue<decimal>(values, AlorSecurityColumns.MinStep);

				return security;
			}, security =>
			{
				using (security.BeginUpdate())
				{
					security.BestBid = new Quote
					{
						OrderDirection = OrderDirections.Buy,
						Price = table.GetValue<decimal>(values, AlorSecurityColumns.BestBidPrice),
						Volume = table.GetValue<int>(values, AlorSecurityColumns.BestBidVolume),
						Security = security,
					};

					security.BestAsk = new Quote
					{
						OrderDirection = OrderDirections.Sell,
						Price = table.GetValue<decimal>(values, AlorSecurityColumns.BestAskPrice),
						Volume = table.GetValue<int>(values, AlorSecurityColumns.BestBidVolume),
						Security = security,
					};

					security.OpenPrice = table.GetValue<decimal>(values, AlorSecurityColumns.OpenPrice);
					security.HighPrice = table.GetValue<decimal>(values, AlorSecurityColumns.HighPrice);
					security.LowPrice = table.GetValue<decimal>(values, AlorSecurityColumns.LowPrice);
					// NOTE:

					security.SettlementDate = table.GetValue<DateTime>(values, AlorSecurityColumns.CancellationDate);

					if (table.GetValue<string>(values, AlorSecurityColumns.OptionType).Trim().IsEmpty() ||
					    table.GetValue<string>(values, AlorSecurityColumns.OptionType) == "F")
					{
						if (table.GetValue<string>(values, AlorSecurityColumns.FutureCode).Trim().IsEmpty())
						{
							security.Type = SecurityTypes.Stock;
						}
						else
						{
							security.Type = SecurityTypes.Future;
							security.ExtensionInfo[AlorSecurityColumns.FutureCode] = table.GetValue<string>(values, AlorSecurityColumns.FutureCode);
						}
					}
					else
					{
						security.Type = SecurityTypes.Option;
						security.ExtensionInfo[AlorSecurityColumns.OptionType] = table.GetValue<string>(values, AlorSecurityColumns.OptionType);
					}

					security.LastTrade = new Trade
					{
						Security = security,
						Time = table.GetValue<DateTime>(values, AlorSecurityColumns.LastTradeTime),
						Price = table.GetValue<decimal>(values, AlorSecurityColumns.LastTradePrice),
						Volume = table.GetValue<int>(values, AlorSecurityColumns.LastTradeVolume),
					};

					security.MinPrice = table.GetValue<decimal>(values, AlorSecurityColumns.LowPriceLimit);
					security.MaxPrice = table.GetValue<decimal>(values, AlorSecurityColumns.HighPriceLimit);

					security.MarginBuy = table.GetValue<decimal>(values, AlorSecurityColumns.BuyDeposit);
					security.MarginSell = table.GetValue<decimal>(values, AlorSecurityColumns.SellDeposit);

					security.State = table.GetValue<string>(values, AlorSecurityColumns.TradingStatus) == "O" ? SecurityStates.Trading : SecurityStates.Stoped;

					table.FillNonMandatoryInfo(security, values);
				}
				return true;
			});
		}

		private void OnOrder(int tableId, object[] values)
		{
			var table = OrdersTable;

			var sfxOrder = (SfxOrder)_slot.GetOrder(table.GetValue<long>(values, AlorOrderColumns.OrderNo));
			sfxOrder.Load();

			var extRef = table.GetValue<string>(values, AlorOrderColumns.ExtRef).Trim();
			var transactionId = extRef.IsEmpty() ? 0 : extRef.To<long>();

			var orderType = table.GetValue<string>(values, AlorOrderColumns.Type).ToOrderType();

			GetOrder(GetSecurity(table, values, AlorOrderColumns.SecurityCode, AlorOrderColumns.SecurityBoard),
				orderType, transactionId,
				key =>
				{
					var security = GetSecurity(table, values, AlorOrderColumns.SecurityCode, AlorOrderColumns.SecurityBoard);
					var order = GetOrder(false, transactionId) ?? EntityFactory.CreateOrder(security, orderType, transactionId);

					order.Id = table.GetValue<long>(values, AlorOrderColumns.OrderNo);

					order.ExtensionInfo = new Dictionary<object, object>
					{
						{
							AlorOrderColumns.BrokerRef,
							table.GetValue<string>(values, AlorOrderColumns.BrokerRef)
						}
					};

					order.Security = GetSecurity(table, values, AlorOrderColumns.SecurityCode, AlorOrderColumns.SecurityBoard);
					order.Volume = table.GetValue<int>(values, AlorOrderColumns.Volume);
					order.Direction = table.GetValue<string>(values, AlorOrderColumns.Direction).ToOrderDirection();
					order.Portfolio = GetPortfolio(table.GetValue<string>(values, AlorOrderColumns.Account));

					order.Type = orderType;

					var executionCondition = table.GetValue<string>(values, AlorOrderColumns.ExecutionCondition);
					if (executionCondition != "")
						order.ExecutionCondition = executionCondition.ToOrderExecutionCondition();

					order.Price = table.GetValue<decimal>(values, AlorOrderColumns.Price);
					return order;
				},
				order =>
				{
					using (order.BeginUpdate())
					{
						order.Time = table.GetValue<DateTime>(values, AlorOrderColumns.Time);
						order.LastChangeTime = table.GetValue<DateTime>(values, AlorOrderColumns.CancelTime);
						order.Balance = table.GetValue<int>(values, AlorOrderColumns.Balance);
						order.State = table.GetValue<string>(values, AlorOrderColumns.State).ToOrderState();
						order.Status = table.GetValue<string>(values, AlorOrderColumns.State).ToOrderStatus();

						table.FillNonMandatoryInfo(order, values);
					}
					return true;
				});
		}

		private void OnStopOrder(int tableId, object[] values)
		{
			var table = StopOrdersTable;

			var extRef = table.GetValue<string>(values, AlorStopOrderColumns.ExtRef).Trim();
			var transactionId = extRef.IsEmpty() ? 0 : extRef.To<long>();

			GetOrder(GetSecurity(table, values, AlorStopOrderColumns.SecurityCode, AlorStopOrderColumns.SecurityBoard),
			    OrderTypes.Conditional, transactionId,
			    key =>
			    {
				    var security = GetSecurity(table, values, AlorStopOrderColumns.SecurityCode, AlorStopOrderColumns.SecurityBoard);
					var order = GetOrder(true, transactionId) ?? EntityFactory.CreateOrder(security, OrderTypes.Conditional, transactionId);

					order.Id = table.GetValue<long>(values, AlorStopOrderColumns.OrderNo);
				    order.Type = OrderTypes.Conditional;
				    order.ExtensionInfo = new Dictionary<object, object>
				    {
					    {
						    AlorStopOrderColumns.BrokerRef,
						    table.GetValue<string>(values, AlorStopOrderColumns.BrokerRef)
					    },
				    };
				    order.Security = GetSecurity(table, values, AlorStopOrderColumns.SecurityCode,
				                                AlorStopOrderColumns.SecurityBoard);
				    order.Volume = table.GetValue<int>(values, AlorStopOrderColumns.Volume);
				    order.Direction = table.GetValue<string>(values, AlorStopOrderColumns.Direction).ToOrderDirection();
				    order.Portfolio = GetPortfolio(table.GetValue<string>(values, AlorStopOrderColumns.Account));
				    order.Price = table.GetValue<decimal>(values, AlorStopOrderColumns.Price);
				    order.ExpiryDate = table.GetValue<DateTime>(values, AlorStopOrderColumns.ExpiryTime);
				    order.Condition = new AlorOrderCondition
				    {
					    StopPrice = table.GetValue<decimal>(values, AlorStopOrderColumns.StopPrice),
					    Type = table.GetValue<string>(values, AlorStopOrderColumns.StopType).ToOrderConditionType(),
				    };
				    return order;
			    },
			    order =>
			    {
				    using (order.BeginUpdate())
				    {
					    order.Time = table.GetValue<DateTime>(values, AlorStopOrderColumns.Time);

					    if (table.IsCorrectType(values, AlorStopOrderColumns.CancelTime))
						    order.LastChangeTime = table.GetValue<DateTime>(values, AlorStopOrderColumns.CancelTime);

					    order.State = table.GetValue<string>(values, AlorStopOrderColumns.State).ToOrderState();
					    order.Status = table.GetValue<string>(values, AlorStopOrderColumns.State).ToOrderStatus();
					    table.FillNonMandatoryInfo(order, values);
				    }
				    return true;
			    });
		}

		private void OnTrade(int tableId, object[] values)
		{
			var table = TradesTable;
			var security = GetSecurity(table, values, AlorTradeColumns.SecurityCode, AlorTradeColumns.SecurityBoard);
			var state = table.GetValue<long>(values, AlorTradeColumns.State);
			var isBuy = state.HasBits(0x40);
			var isSell = state.HasBits(0x80);
			var orderDirection = isBuy == isSell ? (OrderDirections?)null : (isBuy ? OrderDirections.Buy : OrderDirections.Sell);
			var tradeId = table.GetValue<long>(values, AlorTradeColumns.TradeNo);

			GetTrade(security, tradeId, id =>
			{
				var trade = EntityFactory.CreateTrade(security, id);

				trade.OrderDirection = orderDirection;
				trade.Price = table.GetValue<decimal>(values, AlorTradeColumns.Price);
				trade.Time = table.GetValue<DateTime>(values, AlorTradeColumns.Time);
				trade.Volume = table.GetValue<int>(values, AlorTradeColumns.Volume);
				security.LastTrade = trade;
				RaiseSecuritiesChanged(new[] { security });
				table.FillNonMandatoryInfo(trade, values);

				return trade;
			});
		}

		private void OnMyTrade(int tableId, object[] values)
		{
			var table = MyTradesTable;
			var tradeId = table.GetValue<long>(values, AlorMyTradeColumns.TradeNo);
			var orderId = table.GetValue<long>(values, AlorMyTradeColumns.OrderNo);
			var extRef = table.GetValue<string>(values, AlorMyTradeColumns.ExtRef).Trim();
			var transactionId = extRef.IsEmpty() ? 0 : extRef.To<long>();
			var security = GetSecurity(table, values, AlorMyTradeColumns.SecurityCode, AlorMyTradeColumns.SecurityBoard);

			AddMyTrade(security, orderId, transactionId, tradeId, id =>
			{
				var trade = EntityFactory.CreateTrade(security, id);
				trade.Price = table.GetValue<decimal>(values, AlorMyTradeColumns.Price);
				trade.Time = table.GetValue<DateTime>(values, AlorMyTradeColumns.Time);
				trade.Volume = table.GetValue<int>(values, AlorMyTradeColumns.Volume);
				trade.OrderDirection = table.GetValue<String>(values, AlorMyTradeColumns.OrderDirection).ToOrderDirection();
				table.FillNonMandatoryInfo(trade, values);
				return trade;
			}, t => { });
		}

		private void OnQuote(int tableId, int rowId, object fields)
		{
			var values = (object[])fields;
			var security = _orderBooks.SyncGet(d => d.TryGetValue(tableId));
			if (security == null)
				return;

			var table = QuotesTable[tableId];
			var depth = _orderBookData.ContainsKey(tableId) ? _orderBookData[tableId] : _orderBookData[tableId] = new MarketDepth(security);
			var id = table.GetValue<int>(values, AlorQuotesColumns.Id);

			var quote = new Quote
			{
				Security = depth.Security,
				OrderDirection = table.GetValue<string>(values, AlorQuotesColumns.Direction).ToOrderDirection(),
				Price = table.GetValue<decimal>(values, AlorQuotesColumns.Price),
				Volume = table.GetValue<int>(values, AlorQuotesColumns.Volume),
			};
			table.FillNonMandatoryInfo(quote, values);

			if (id == 1 && depth.Count != 0)
				RaiseMarketDepthChanged(GetMarketDepth(security).Update(depth.Bids, depth.Asks, true));

			if (id == 1)
				depth.Update(ArrayHelper<Quote>.EmptyArray, ArrayHelper<Quote>.EmptyArray, true);

			depth.UpdateQuote(quote);
		}

		private void OnPortfolio(int tableId, object[] values)
		{
			var table = PortfoliosTable;
			GetPortfolio(table.GetValue<string>(values, AlorPortfolioColumns.Account), portfolio =>
			{
				using (portfolio.BeginUpdate())
					table.FillNonMandatoryInfo(portfolio, values);

				return true;
			});
		}

		private void OnHolding(int tableId, object[] values)
		{
			var table = HoldingTable;
			var account = table.GetValue<string>(values, AlorHoldingColumns.Account);
			var security = GetSecurityBySecCode(table.GetValue<string>(values, AlorHoldingColumns.SecurityCode));
			var portfolio = account.Trim() != ""
				                ? GetPortfolio(table.GetValue<string>(values, AlorHoldingColumns.Account))
				                : Portfolios.ToList()[0];

			UpdatePosition(portfolio, security,
				position =>
				{
					using (position.BeginUpdate())
					{
						position.BeginValue = table.GetValue<int>(values, AlorHoldingColumns.BeginValue);
						position.CurrentValue = table.GetValue<int>(values, AlorHoldingColumns.CurrentValue);
						position.BlockedValue =
							table.GetValue<int>(values, AlorHoldingColumns.CurrentBidsVolume) +
							table.GetValue<int>(values, AlorHoldingColumns.CurrentAsksVolume);

						table.FillNonMandatoryInfo(position, values);
					}

					return true;
				});
		}

		private void OnMoneyPosition(int tableId, object[] values)
		{
			var table = MoneyPositionTable;
			if (table.GetValue<string>(values, AlorMoneyPositionsColumns.AccCode) == "1")
				return;

			GetPortfolio(table.GetValue<string>(values, AlorMoneyPositionsColumns.Account),
				portfolio =>
				{
					using (portfolio.BeginUpdate())
					{
						portfolio.BeginValue = table.GetValue<decimal>(values, AlorMoneyPositionsColumns.BeginValue);
						portfolio.CurrentValue = table.GetValue<decimal>(values, AlorMoneyPositionsColumns.CurrentValue);
						portfolio.Commission =
							table.GetValue<decimal>(values, AlorMoneyPositionsColumns.MarketCommission) +
							table.GetValue<decimal>(values, AlorMoneyPositionsColumns.BrokerCommission);

						table.FillNonMandatoryInfo(portfolio, values);
					}

					return true;
				});
		}

		private void OnTime(int tableId, object[] values)
		{
			var table = TimeTable;
			//var time = table.GetValue<DateTime>(values, AlorTimeColumns.Time);
			//MarketTimeOffset = time - DateTime.Now;
			CloseTable(table);
		}
	}
}