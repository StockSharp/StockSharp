namespace StockSharp.ETrade.Native
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;

	partial class ETradeClient
	{
		private class ETradeOrderModule : ETradeModule
		{
			#region params dictionary

			class OrderListRequestParams
			{
				public OrderListRequestParams() { NumOrdersToRequest = ETradeGetOrderListRequest.MaxAllowedOrderCount; }
				public int NumOrdersToRequest {get; set;}
				public long OldestIncompleteOrderId {get; set;}
			}

			class OrderListRequestParamsDict
			{
				readonly Dictionary<string, OrderListRequestParams> _dict = new Dictionary<string, OrderListRequestParams>();

				public OrderListRequestParams this[string portfolioName]
				{
					get
					{
						var p = _dict.TryGetValue(portfolioName);
						if (p == null)
						{
							p = new OrderListRequestParams();
							_dict[portfolioName] = p;
						}

						return p;
					}
				}

				public void Clear() { _dict.Clear(); }
			}

			#endregion

			readonly OrderListRequestParamsDict _paramsDict = new OrderListRequestParamsDict();

			public ETradeOrderModule(ETradeClient client) : base("order", client) {}

			protected override void Start()
			{
				if (IsStarted) return;

				_paramsDict.Clear();

				base.Start();
			}

			protected override void ProcessResponse(ETradeResponse response) {
				var request = response.Request as ETradeGetOrderListRequest;
				if (request != null && request.IsDone && response.Exception == null)
				{
					var params1 = _paramsDict[request.PortfolioName];

					if (request.FoundOldestIncompleteOrderPosition < 0)
					{
						Client.AddDebugLog("Auto-updates for portfolio '{0}' were stopped.", request.PortfolioName);
						params1.NumOrdersToRequest = 0;
						params1.OldestIncompleteOrderId = 0;
					}
					else
					{
						params1.NumOrdersToRequest = request.FoundOldestIncompleteOrderPosition >= ETradeGetOrderListRequest.MaxAllowedOrderCount ?
													ETradeGetOrderListRequest.MaxAllowedOrderCount : request.FoundOldestIncompleteOrderPosition + 1;
						params1.OldestIncompleteOrderId = request.FoundOldestIncompleteOrderId;

						Client.AddDebugLog("Auto-updates for portfolio '{0}': NumOrdersToRequest={1}, OldestIncompleteOrderId={2}", 
											request.PortfolioName, params1.NumOrdersToRequest, params1.OldestIncompleteOrderId);
					}
				}

				base.ProcessResponse(response);
			}

			protected override ETradeRequest GetNextAutoRequest()
			{
				if (!Client.IsConnected || !Client.IsExportStarted)
					return null;

				var portfolios = Client._portfolioNames.ToArray();
				if (portfolios.Length == 0)
					return null;

				var firstIndex = 0;

				if (CurrentAutoRequest != null)
				{
					var req = (ETradeGetOrderListRequest)CurrentAutoRequest;
					firstIndex = portfolios.IndexOf(req.PortfolioName);
					firstIndex = firstIndex < 0 ? 0 : firstIndex + 1;
				}

				for (var i = firstIndex; i < firstIndex + portfolios.Length; ++i)
				{
					var index = i % portfolios.Length;
					var params1 = _paramsDict[portfolios[index]];

					if(params1.NumOrdersToRequest > 0)
						return new ETradeGetOrderListRequest(portfolios[index], params1.NumOrdersToRequest, params1.OldestIncompleteOrderId)
						{
							ResponseHandler = GetOrderListResponseHandler
						};
				}

				return null;
			}

			private void GetOrderListResponseHandler(ETradeResponse<List<Order>> response)
			{
				var portfName = ((ETradeGetOrderListRequest)response.Request).PortfolioName;
				Client.OrdersData.SafeInvoke(portfName, response.Data, response.Exception);
			}

			public void ResetOrderUpdateSettings(string portfName, bool resetRememberedLastOrder)
			{
				Action<string> reset = name =>
				{
					var params1 = _paramsDict[name];

					params1.NumOrdersToRequest = ETradeGetOrderListRequest.MaxAllowedOrderCount;

					if (resetRememberedLastOrder)
						params1.OldestIncompleteOrderId = 0;
				};

				if (portfName != null)
				{
					reset(portfName);
				}
				else
				{
					Client._portfolioNames.ToArray().ForEach(reset);
				}

				Wakeup();
			}
		}

		public void RegisterOrder(string portfName, string secCode, Sides side, decimal price, decimal volume, long transId, bool allOrNone, DateTimeOffset? expiryDate, OrderTypes type, ETradeOrderCondition cond)
		{
			var order = new BusinessEntities.Order
			{
				Portfolio = new Portfolio { Name = portfName },
				Security = new Security { Code = secCode },
				Direction = side,
				Price = price,
				Volume = volume,
				TransactionId = transId,
				TimeInForce = allOrNone ? TimeInForce.MatchOrCancel : TimeInForce.PutInQueue,
				ExpiryDate = expiryDate,
				Type = type,
				Condition = cond
			};

			_orderModule.ExecuteUserRequest(new ETradeRegisterOrderRequest(order), response =>
			{
				OrderRegisterResult.SafeInvoke(transId, response.Data, response.Exception);
				_orderModule.ResetOrderUpdateSettings(portfName, false);
			});
		}

		public void ReRegisterOrder(long oldOrderId, string portfName, decimal price, decimal volume, long transId, bool allOrNone, DateTimeOffset? expiryDate, OrderTypes type, ETradeOrderCondition cond)
		{
			var newOrder = new BusinessEntities.Order
			{
				Portfolio = new Portfolio { Name = portfName },
				Price = price,
				Volume = volume,
				TransactionId = transId,
				TimeInForce = allOrNone ? TimeInForce.MatchOrCancel : TimeInForce.PutInQueue,
				ExpiryDate = expiryDate,
				Type = type,
				Condition = cond
			};

			_orderModule.ExecuteUserRequest(new ETradeReRegisterOrderRequest((int)oldOrderId, newOrder), response =>
			{
				OrderReRegisterResult.SafeInvoke(transId, response.Data, response.Exception);
				_orderModule.ResetOrderUpdateSettings(portfName, false);
			});
		}

		public void CancelOrder(long cancelTransactionId, long orderId, string porfName)
		{
			_orderModule.ExecuteUserRequest(new ETradeCancelOrderRequest((int)orderId, porfName), response =>
			{
				OrderCancelResult.SafeInvoke(cancelTransactionId, orderId, response.Data, response.Exception);
				_orderModule.ResetOrderUpdateSettings(porfName, false);
			});
		}
	}

	class ETradeRegisterOrderRequest : ETradeRequest<PlaceEquityOrderResponse2>
	{
		readonly BusinessEntities.Order _order;

		public ETradeRegisterOrderRequest(BusinessEntities.Order order) { _order = order; }

		protected override ETradeResponse ExecuteNextPartInternal(ETradeApi api)
		{
			Exception ex;

			var result = PerformApiRequest(() => api.SendOrder(_order), out ex);

			return ETradeResponse.Create(this, result, ex);
		}
	}

	class ETradeReRegisterOrderRequest : ETradeRequest<PlaceEquityOrderResponse2>
	{
		readonly int _oldOrderId;
		readonly BusinessEntities.Order _newOrder;

		public ETradeReRegisterOrderRequest(int oldOrderId, BusinessEntities.Order newOrder)
		{
			_oldOrderId = oldOrderId;
			_newOrder = newOrder;
		}

		protected override ETradeResponse ExecuteNextPartInternal(ETradeApi api)
		{
			Exception ex;

			var result = PerformApiRequest(() => api.SendOrderChange(_oldOrderId, _newOrder), out ex);

			return ETradeResponse.Create(this, result, ex);
		}
	}

	class ETradeCancelOrderRequest : ETradeRequest<CancelOrderResponse2>
	{
		readonly int _orderId;
		readonly string _portfolioName;

		public ETradeCancelOrderRequest(int orderId, string portfName)
		{
			_orderId = orderId;
			_portfolioName = portfName;
		}

		protected override ETradeResponse ExecuteNextPartInternal(ETradeApi api)
		{
			Exception ex;

			var result = PerformApiRequest(() => api.CancelOrder(_orderId, _portfolioName), out ex);

			return ETradeResponse.Create(this, result, ex);
		}
	}

	class ETradeGetOrderListRequest : ETradeRequest<List<Order>>
	{
		public const int MaxAllowedOrderCount = 25;

		public string PortfolioName {get; private set;}
		int _orderCounter;
		string _marker;
		readonly int _firstRequestCount;
		readonly long _oldestIncompleteOrderId;

		bool _done;

		public long FoundOldestIncompleteOrderId {get; private set;}
		public int FoundOldestIncompleteOrderPosition {get; private set;}

		public ETradeGetOrderListRequest(string porftName, int firstRequestCount, long oldestIncompleteOrderId)
		{
			if(firstRequestCount < 1) throw new ArgumentException("firstRequestCount");

			PortfolioName = porftName;
			_firstRequestCount = firstRequestCount;
			_oldestIncompleteOrderId = oldestIncompleteOrderId;
			FoundOldestIncompleteOrderPosition = -1;
		}

		protected override ETradeResponse ExecuteNextPartInternal(ETradeApi api)
		{
			Exception ex;

			var result = PerformApiRequest(() => 
						api.GetOrderList(PortfolioName, _orderCounter == 0 ? _firstRequestCount : MaxAllowedOrderCount, ref _marker),
						out ex);

			if(_marker.IsEmpty()) _done = true;

			if (ex == null)
			{
				for (var i = 0; i < result.Count; ++i)
				{
					var nativeOrder = result[i];
					if(nativeOrder == null) continue;

					if(!ETradeUtil.IsOrderInFinalState(nativeOrder))
					{
						FoundOldestIncompleteOrderPosition = _orderCounter + i;
						FoundOldestIncompleteOrderId = nativeOrder.orderId;
					}

					if (nativeOrder.orderId == _oldestIncompleteOrderId)
					{
						_done = true;
						break;
					}
				}

				_orderCounter += result.Count;
			}

			if(result != null)
				result.RemoveAll(o => o == null);
			else
				result = new List<Order>();

			return ETradeResponse.Create(this, result, ex);
		}

		protected override bool GetIsDone() { return _done; }
	}
}