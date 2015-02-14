namespace StockSharp.OpenECry
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using OEC.API;

	using StockSharp.Logging;
	using StockSharp.Messages;

	public sealed partial class OECTrader
	{
		/// <summary>
		/// OrderMap выполняет загрузку заявок и соответствующих им сделок из OEC при первом подключении.
		/// Так же OrderMap ассоциирует внутренний тип заявок <see cref="BusinessEntities.Order"/>
		/// с соответствующим ему типом OEC <see cref="OEC.API.Version"/>
		/// и поддерживает эту ассоциацию между переподключениями к серверу OEC.
		/// </summary>
		private sealed class OrderMap
		{
			private readonly OECTrader _connector;

			private OECClient Oec
			{
				get { return _connector._sessionHolder.Session; }
			}

			private readonly PairSet<OEC.API.Version, BusinessEntities.Order> _versionOrderSet = new PairSet<OEC.API.Version, BusinessEntities.Order>();

			public OrderMap(OECTrader connector)
			{
				_connector = connector;
			}

			private void OnConnected()
			{
				
			}

			private void ReloadMyTrades()
			{
				var myTrades = _connector.MyTrades.ToDictionary(trade => trade.Trade.Id);

				foreach (var oecOrder in Oec.Orders)
				{
					var security = _connector.FindSecurityByContract(oecOrder.Contract);
					foreach (var fill in oecOrder.Fills)
					{
						if (myTrades.ContainsKey(fill.ID))
							continue;

						var version = fill.GetOECOrderVersion();
						if (version == null)
						{
							_connector.AddErrorLog("Заявка для сделки '{0}' (id={1}, security={2}, datetime={3}) не найдена.", fill, fill.ID, security.Code, fill.Timestamp);
							continue;
						}

						var order = GetLocalOrder(version);
						if (order == null)
						{
							_connector.AddErrorLog("Заявка для версии '{0}' OEC заявки '{1}' не найдена.", version, oecOrder.OrderString());
							continue;
						}

						var trade = _connector.EntityFactory.CreateTrade(security, fill.ID);

						trade.OrderDirection = fill.Order.Side.ToStockSharp();
						trade.Price = fill.Price.To<decimal>();
						trade.Time = fill.Timestamp;
						trade.Volume = fill.Quantity;

						//trade.InitLatency();

						_connector.AddMyTrade(security, order.Id, order.TransactionId, trade);
					}
				}
			}

			private void ReloadOrders()
			{
				_versionOrderSet.Clear();

				int countOld = 0, countNew = 0;

				var orders = _connector.Orders.Where(order => order.Id > 0).ToDictionary(order => order.Id);

				var processedOrders = new List<BusinessEntities.Order>();

				foreach (var oecOrder in Oec.Orders)
				{
					foreach (var ver in oecOrder.Versions)
					{
						try
						{
							if (orders.ContainsKey(ver.ID))
							{
								++countOld;
								var order = orders[ver.ID];
								processedOrders.Add(order);
								BindOrderToOecOrderVersion(order, ver);

								if (order.IsInFinalState() && ver.IsInFinalState())
									continue;

								order.CopyFromOECOrderVersion(ver);
								_connector.GetOrder(order.Security, order.Type, order.Id, id => order, o => false);
							}
							else
							{
								var portfolio = _connector.FindPortfolioByAccount(ver.Order.Account, null);
								var security = _connector.FindSecurityByContract(ver.Order.Contract);
								var vercopy = ver;
								var orderType = ver.Type.ToStockSharp();
								var order = _connector.GetOrder(security, orderType, ver.ID, id =>
								{
									var ord = _connector.EntityFactory.CreateOrder(security, orderType, vercopy.ID);
									ord.Id = id;
									// TODO
									//_connector.InitNewOrder(ord);
									return ord;
								}, ord =>
								{
									ord.Portfolio = portfolio;
									ord.Security = security;
									ord.CopyFromOECOrderVersion(vercopy);

									return true;
								});
								++countNew;
								BindOrderToOecOrderVersion(order, ver);
								processedOrders.Add(order);
							}
						}
						catch (Exception ex)
						{
							var msg = "Ошибка загрузки заявки #{0} ({1}): {2}".Put(ver.ID, ver, ex);
							_connector.AddErrorLog(msg);
							_connector.TransactionAdapter.SendOutMessage(new ErrorMessage { Error = ex });
						}
					}
				}

				var countFailed = 0;
				// incomplete local orders which weren't found in OEC
				var otherIncompleteOrders = _connector.Orders.Except(processedOrders).Where(order => !order.IsInFinalState());
				foreach (var order in otherIncompleteOrders)
				{
					++countFailed;
					var ordercopy = order;

					_connector.GetOrder(order.Security, order.Type, order.TransactionId, id => ordercopy, o =>
					{
						o.Id = 0;
						o.State = OrderStates.Done;
						o.Status = OrderStatus.NotDone;
						_connector.RaiseOrderFailed(o, new OECException("После переподключения заявка была не найдена среди заявок OEC."));

						return true;
					});
				}

				_connector.AddInfoLog("{0} заявок OEC связано с существующими заявками S#; {1} новых заявок загружено из OEC; {2} незавершенных заявок OEC были завершены с ошибкой.", countOld, countNew, countFailed);
			}

			private void OnDisconnected()
			{
				CleanupOrders();
			}

			private void CleanupOrders()
			{
				var dt = DateTime.UtcNow;
				Exception ex = null;

				try
				{
					foreach (var co in _versionOrderSet)
					{
						var order = co.Value;
						if (!order.IsInFinalState() && order.Id <= 0)
						{
							order.State = 0;
							order.State = OrderStates.Done;
							order.Status = OrderStatus.GateError;
							order.LastChangeTime = dt;

							if (ex == null)
								ex = new OECException("Соединение разорвано во время отправки заявки на биржу.");
							
							_connector.RaiseOrderFailed(order, ex);
						}
					}
				}
				catch (Exception e)
				{
					_connector.AddErrorLog("Ошибка во время обработки OnDisconnected: {0}", e);
				}

				_versionOrderSet.Clear();
			}

			public void BindOrderToOecOrderVersion(BusinessEntities.Order order, OEC.API.Version ver)
			{
				if (ver.ID > 0)
					order.Id = ver.ID;

				if (_versionOrderSet.ContainsKey(ver))
				{
					if (_versionOrderSet[ver] != order)
					{
						_connector.AddWarningLog("Старая версия заявки не была удалена из коллекции.");
						_versionOrderSet.Remove(ver);
						_versionOrderSet[ver] = order;
					}
				}
				else
				{
					_versionOrderSet[ver] = order;
				}
			}

			public BusinessEntities.Order GetLocalOrder(OEC.API.Version version, bool create = true)
			{
				if (_versionOrderSet.ContainsKey(version))
					return _versionOrderSet[version];

				_connector.AddWarningLog("GetLocalOrder({0}): заявка не найдена", version.ToString());

				if (!create || version.ID <= 0)
					return null;

				try
				{
					var portfolio = _connector.FindPortfolioByAccount(version.Order.Account, null);
					var security = _connector.FindSecurityByContract(version.Order.Contract);
					var vercopy = version;
					var orderType = version.Type.ToStockSharp();
					var order = _connector.GetOrder(security, orderType, version.ID, id =>
					{
						var ord = _connector.EntityFactory.CreateOrder(security, orderType, vercopy.ID);
						ord.Id = id;
						// TODO
						//_connector.InitNewOrder(ord);
						return ord;
					}, ord =>
					{
						ord.Portfolio = portfolio;
						ord.Security = security;
						ord.CopyFromOECOrderVersion(vercopy);

						return true;
					});

					BindOrderToOecOrderVersion(order, version);

					return order;
				}
				catch (Exception ex)
				{
					var msg = "Ошибка создания заявки S# по OEC версии '{0}': {1}".Put(version.ToString(), ex);
					_connector.AddWarningLog(msg);
					_connector.TransactionAdapter.SendOutMessage(new ErrorMessage { Error = ex });

					return null;
				}
			}

			public BusinessEntities.Order GetLocalOrder(OEC.API.Order oecOrder, bool create = true)
			{
				return GetLocalOrder(oecOrder.Versions.Current, create);
			}

			public OEC.API.Order GetOecOrder(BusinessEntities.Order order)
			{
				if (!_versionOrderSet.ContainsValue(order))
				{
					_connector.AddWarningLog("GetOecOrder({0}): команда не найдена", order.Id);
					return null;
				}
				return _versionOrderSet[order].Order;
			}
		}
	}
}