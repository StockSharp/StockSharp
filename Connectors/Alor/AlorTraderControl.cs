using System;

using Ecng.Collections;
using Ecng.Common;

using StockSharp.Alor.Metadata;
using StockSharp.BusinessEntities;

namespace StockSharp.Alor
{
	using StockSharp.Messages;
	using StockSharp.Localization;

	public sealed partial class AlorTrader
	{
		/// <summary>
		/// Зарегистрировать заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, содержащая информацию для регистрации.</param>
		protected override void OnRegisterOrder(Order order)
		{
			string exexCondition;

			switch (order.TimeInForce)
			{
				case TimeInForce.PutInQueue:
					exexCondition = " ";
					break;
				case TimeInForce.MatchOrCancel:
					exexCondition = "N";
					break;
				case TimeInForce.CancelBalance:
					exexCondition = "W";
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			const string enterType = "P";
			const string splitFlag = "S";
			const string issueCode = "";

			var direction = order.Direction == OrderDirections.Buy ? "B" : "S";
			var extRef = order.TransactionId.To<String>();

			int resCode;
			string res;

			if (order.Type == OrderTypes.Conditional)
			{
				var condition = (AlorOrderCondition)order.Condition;
				resCode = _slot.AddStopOrder(order.Portfolio.Name, direction, order.Price == 0 ? "M" : "L", splitFlag,
				                            exexCondition, enterType, order.Security.Board.Code, order.Security.Code, issueCode,
				                            condition.Type.ToAlorConditionType(), order.ExpiryDate,
				                            (double)condition.StopPrice, (double)order.Price, (int)order.Volume, _slot.BrokerRef, extRef, out res);
			}
			else
			{

				resCode = _slot.AddOrder(order.Portfolio.Name, direction,
				                        order.Type == OrderTypes.Market ? "M" : "L", splitFlag, exexCondition, enterType, order.Security.Board.Code,
				                        order.Security.Code, issueCode, (double)order.Price, (int)order.Volume, _slot.BrokerRef, extRef, out res);
			}

			var exception = AlorExceptionHelper.GetException(resCode, res);
			if (exception != null)
				RaiseOrderFailed(order, exception);

			order.Messages.Add(res);
		}

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять.</param><param name="newOrder">Новая заявка, которую нужно зарегистрировать.</param>
		protected override void OnReRegisterOrder(Order oldOrder, Order newOrder)
		{
			if (oldOrder.Security.Board.IsSupportAtomicReRegister)
			{
				var newOrderId1 = oldOrder.Id;
				var newOrderId2 = 0L;

				string res;
				var resCode = _slot.MoveOrders(1, ref newOrderId1, (double)newOrder.Price, (int)newOrder.Volume,
				                              newOrder.TransactionId.To<string>(), ref newOrderId2, 0, 0, "", out res);

				var exception = AlorExceptionHelper.GetException(resCode, res);

				if (exception != null)
				{
					RaiseOrderFailed(newOrder, exception);
					RaiseOrderFailed(oldOrder, exception);
				}

				newOrder.Messages.Add(res);
			}
			else
			{
				base.OnReRegisterOrder(oldOrder, newOrder);
			}
		}

		/// <summary>
		/// Перерегистрировать пару заявок на бирже.
		/// </summary>
		/// <param name="oldOrder1">Первая заявка, которую нужно снять.</param><param name="newOrder1">Первая новая заявка, которую нужно зарегистрировать.</param><param name="oldOrder2">Вторая заявка, которую нужно снять.</param><param name="newOrder2">Вторая новая заявка, которую нужно зарегистрировать.</param>
		protected override void OnReRegisterOrderPair(Order oldOrder1, Order newOrder1, Order oldOrder2, Order newOrder2)
		{
			//если хоть одна заявка не поддерживает перестановку они выполняются отдельно
			if (!(oldOrder1.Security.Board.IsSupportAtomicReRegister) || !(oldOrder2.Security.Board.IsSupportAtomicReRegister))
			{
				OnReRegisterOrder(oldOrder1, newOrder1);
				OnReRegisterOrder(oldOrder2, newOrder2);
				return;
			}

			var newOrderId1 = oldOrder1.Id;
			var newOrderId2 = oldOrder2.Id;

			string res;
			var resCode = _slot.MoveOrders(1, ref newOrderId1, (double)newOrder1.Price, (int)newOrder1.Volume,
			                              newOrder1.TransactionId.To<string>(), ref newOrderId2,
			                              (double)newOrder2.Price, (int)newOrder2.Volume,
			                              newOrder2.TransactionId.To<string>(), out res);

			var exception = AlorExceptionHelper.GetException(resCode, res);

			if (exception != null)
			{
				RaiseOrderFailed(oldOrder1, exception);
				RaiseOrderFailed(newOrder1, exception);
				if (newOrderId2 != 0)
				{
					RaiseOrderFailed(oldOrder2, exception);
					RaiseOrderFailed(newOrder2, exception);
				}
			}

			newOrder1.Messages.Add(res);
			newOrder2.Messages.Add(res);
		}

		/// <summary>
		/// Отменить заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, которую нужно отменять.</param>
		protected override void OnCancelOrder(Order order)
		{
			var id = order.Id;
			string res;
			var resCode = order.Type == OrderTypes.Conditional
				              ? _slot.DeleteStopOrder(id, out res)
				              : _slot.DeleteOrder(id, out res);

			var exception = AlorExceptionHelper.GetException(resCode, res);

			if (exception != null)
				RaiseOrderFailed(order, exception);

			order.Messages.Add(res);
		}

		/// <summary>
		/// Начать получать котировки (стакан) по инструменту.
		/// Значение котировок можно получить через метод <see cref="IConnector.GetMarketDepth"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать котировки.</param>
		protected override bool OnRegisterMarketDepth(Security security)
		{
			var quotesTable = new AlorTable(AlorTableTypes.Quote, "ORDERBOOK", RaiseProcessDataError);
			OpenTable(quotesTable, null, false);

			_orderBooks.SyncDo(d =>
			{
				if (!d.ContainsValue(security))
				{
					var id = quotesTable.MetaTable.OpenOrderbook(_slot.ID, security.Board.Code, security.Code);
					if (id < 1)
						throw new InvalidOperationException(LocalizedStrings.Str3709Params.Put(security.Id));

					QuotesTable[id] = quotesTable;
					d.Add(id, security);
				}
			});

			return true;
		}

		/// <summary>
		/// Остановить получение котировок по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение котировок.</param>
		protected override void OnUnRegisterMarketDepth(Security security)
		{
			_orderBooks.SyncDo(d =>
			{
				if (_orderBooks.ContainsValue(security))
				{
					var key = _orderBooks.GetKey(security);

					if (ConnectionState == ConnectionStates.Connected)
						QuotesTable[key].MetaTable.Close(key);

					QuotesTable.Remove(key);
					_orderBooks.Remove(key);

					if (_orderBookData.ContainsKey((key)))
						_orderBookData.Remove(key);
				}
			});
		}
	}
}