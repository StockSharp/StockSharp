namespace StockSharp.Quik
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	enum DerivativeLimitTypes
	{
		Money,
		Bail,
		Clearing,
		ClearingBail,
		Spot,
		Term,
	}

	static class QuikDdeFormatter
	{
		public static void Deserialize(this DdeTable table, IList<IList<object>> rows, Action<IList<object>, Func<DdeTableColumn, object>> handler, Action<Exception> errorHandler, bool skipErrors)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			if (rows == null)
				throw new ArgumentNullException("rows");

			if (handler == null)
				throw new ArgumentNullException("handler");

			if (errorHandler == null)
				throw new ArgumentNullException("errorHandler");

			try
			{
				var errors = new List<Exception>();

				foreach (var r in rows)
				{
					try
					{
						var row = r;

						handler(row, column =>
						{
							var index = table.Columns.IndexOf(column);

							if (index == -1)
								throw new InvalidOperationException(LocalizedStrings.Str1711Params.Put(table.Caption, column.Name));

							if (row.Count <= index)
								throw new InvalidOperationException(LocalizedStrings.Str1703Params.Put(table.Caption, column.Name, row.Count, index));

							return row[index];
						});
					}
					catch (Exception ex)
					{
						if (skipErrors)
							errors.Add(ex);
						else
							throw;
					}
				}

				foreach (var error in errors)
					errorHandler(error);
			}
			catch (Exception ex)
			{
				errorHandler(ex);
			}
		}

		public static OrderStates? GetState(this Func<DdeTableColumn, object> func, DdeTableColumn column)
		{
			if (func == null)
				throw new ArgumentNullException("func");

			var value = (string)func(column);

			switch (value)
			{
				case "ACTIVE":
					return OrderStates.Active;
				case "FILLED":
					return OrderStates.Done;
				case "KILLED":
					return null;
				default:
					throw new ArgumentOutOfRangeException("column", value, LocalizedStrings.Str1712);
			}
		}

		public static Sides ToSide(this object value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			switch ((string)value)
			{
				case "B":
					return Sides.Buy;
				case "S":
					return Sides.Sell;
				default:
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1713);
			}
		}

		public static DateTimeOffset GetExpiryDate(this Func<DdeTableColumn, object> func, DdeTableColumn column)
		{
			if (func == null)
				throw new ArgumentNullException("func");

			var expiryDate = func.Get<string>(column);

			return expiryDate.CompareIgnoreCase("до отмены") || expiryDate.IsEmpty()
				? DateTimeOffset.MaxValue
				: expiryDate.To<DateTime>().ApplyTimeZone(TimeHelper.Moscow);
		}

		public static T Get<T>(this Func<DdeTableColumn, object> func, DdeTableColumn column)
		{
			if (func == null)
				throw new ArgumentNullException("func");

			var value = func(column);
			return Get<T>(value, column);
		}

		private static T Get<T>(object value, DdeTableColumn column)
		{
			if (column == null)
				throw new ArgumentNullException("column");

			try
			{
				return value.To<T>();
			}
			catch (Exception ex)
			{
				throw new ArgumentException(LocalizedStrings.Str1714Params.Put(column.Name, value, typeof(T).Name), "value", ex);
			}
		}

		public static object Get(object value, DdeTableColumn column)
		{
			if (column == null)
				throw new ArgumentNullException("column");

			try
			{
				if (value is string && (string)value == string.Empty)
					return null;
				else
					return value.To(column.DataType);
			}
			catch (Exception ex)
			{
				throw new ArgumentException(LocalizedStrings.Str1714Params.Put(column.Name, value, column.DataType.Name), "value", ex);
			}
		}

		public static T GetNullable<T>(this Func<DdeTableColumn, object> func, DdeTableColumn column, T defaultValue = default(T))
			where T : struct
		{
			return func.GetNullable2<T>(column) ?? defaultValue;
		}

		public static T? GetNullable2<T>(this Func<DdeTableColumn, object> func, DdeTableColumn column)
			where T : struct
		{
			if (func == null)
				throw new ArgumentNullException("func");

			var value = func(column);

			if (value is T)
				return (T)value;
			else
			{
				if (value is string && (string)value == string.Empty)
					return null;
				else
					return Get<T>(value, column);
			}
		}

		public static T? GetZeroable<T>(this Func<DdeTableColumn, object> func, DdeTableColumn column)
			where T : struct
		{
			var value = func.Get<double>(column);
			return value == 0 ? (T?)null : Get<T>(value, column);
		}

		public static Unit GetUnit(this Func<DdeTableColumn, object> func, DdeTableColumn typeColumn, DdeTableColumn valueColumn)
		{
			UnitTypes? type;

			var typeStr = Get<string>(func, typeColumn);

			switch (typeStr)
			{
				case "":
					type = null;
					break;
				case "Д":
					type = UnitTypes.Absolute;
					break;
				case "%":
					type = UnitTypes.Percent;
					break;
				default:
					throw new ArgumentOutOfRangeException("func", typeStr, LocalizedStrings.Str1715Params.Put(typeColumn.Name));
			}

			return type == null ? null : new Unit { Value = Get<decimal>(func, valueColumn), Type = (UnitTypes)type };
		}

		public static bool? GetBool(this Func<DdeTableColumn, object> func, DdeTableColumn column)
		{
			var value = Get<string>(func, column);

			switch (value)
			{
				case "":
					return null;
				case "Да":
					return true;
				case "Нет":
					return false;
				default:
					throw new ArgumentOutOfRangeException("func", value, LocalizedStrings.Str1716Params.Put(column.Name));
			}
		}

		public static QuikOrderConditionTypes GetStopOrderType(this Func<DdeTableColumn, object> func)
		{
			var value = Get<string>(func, DdeStopOrderColumns.Type);

			switch (value)
			{
				case "Со связ. заявкой":
					return QuikOrderConditionTypes.LinkedOrder;
				case "Стоп-лимит":
				case "Стоп-лимит по заявке":
					return QuikOrderConditionTypes.StopLimit;
				case "СЦ по др. бумаге":
					return QuikOrderConditionTypes.OtherSecurity;
				case "Тэйк-профит":
				case "Тэйк профит по заявке":
					return QuikOrderConditionTypes.TakeProfit;
				case "Тэйк-профит и стоп-лимит":
				case "Тэйк-профит и стоп-лимит по заявке":
					return QuikOrderConditionTypes.TakeProfitStopLimit;
				default:
					throw new ArgumentOutOfRangeException("func", value, LocalizedStrings.Str1717);
			}
		}

		public static OrderTypes GetOrderType(this Func<DdeTableColumn, object> func)
		{
			var type = func.Get<string>(DdeOrderColumns.Type);
			return type[0] == 'L' ? OrderTypes.Limit : OrderTypes.Market;
		}

		public static TimeInForce GetTimeInForce(this Func<DdeTableColumn, object> func)
		{
			var type = func.Get<string>(DdeOrderColumns.Type);

			switch (type[2])
			{
				case 'K':
					return TimeInForce.MatchOrCancel;
				case 'Q':
					return TimeInForce.PutInQueue;
				default:
					return TimeInForce.CancelBalance;
			}
		}

		public static SecurityStates GetSecurityState(this Func<DdeTableColumn, object> func)
		{
			var state = func.Get<string>(DdeSecurityColumns.Status);
			return state == "торгуется" ? SecurityStates.Trading : SecurityStates.Stoped;
		}

		public static QuikStopPriceConditions GetStopPriceCondition(this Func<DdeTableColumn, object> func)
		{
			return func.Get<string>(DdeStopOrderColumns.StopPriceCondition) == ">=" ? QuikStopPriceConditions.MoreOrEqual : QuikStopPriceConditions.LessOrEqual;
		}

		public static QuikOrderConditionResults? GetStopResult(this Func<DdeTableColumn, object> func)
		{
			if (func == null)
				throw new ArgumentNullException("func");

			var value = func.Get<string>(DdeStopOrderColumns.Result);

			switch (value)
			{
				case "":
					return null;
				case "ORDER SENT TO TS":
					return QuikOrderConditionResults.SentToTS;
				case "REJECTED BY TS":
					return QuikOrderConditionResults.RejectedByTS;
				case "KILLED":
					return QuikOrderConditionResults.Killed;
				case "COORDER KILLED":
					return QuikOrderConditionResults.LinkedOrderKilled;
				case "COORDER FILLED":
					return QuikOrderConditionResults.LinkedOrderFilled;
				case "WAITING FOR ACTIVATION":
					return QuikOrderConditionResults.WaitingForActivation;
				case "CALCULATE MIN/MAX":
					return QuikOrderConditionResults.CalculateMinMax;
				case "LIMITS CHECK FAILED":
					return QuikOrderConditionResults.LimitControlFailed;
				case "WAITING FOR ACTIVATION AND CALCULATE MIN/MAX":
					return QuikOrderConditionResults.CalculateMinMaxAndWaitForActivation;
				default:
					throw new ArgumentOutOfRangeException("func", value, LocalizedStrings.Str1718);
			}
		}

		public static DerivativeLimitTypes GetLimitType(this Func<DdeTableColumn, object> func)
		{
			if (func == null)
				throw new ArgumentNullException("func");

			var value = (string)func(DdeDerivativePortfolioColumns.LimitType);

			switch (value)
			{
				case "Ден.средства":
				case "Рубли":
					return DerivativeLimitTypes.Money;
				case "Залоговые ден.средства":
				case "Залоговые рубли":
					return DerivativeLimitTypes.Bail;
				case "Клиринговые ден.средства":
				case "Клиринговые рубли":
					return DerivativeLimitTypes.Clearing;
				case "Клиринговые залоговые ден.средства":
				case "Клиринговые залоговые рубли":
					return DerivativeLimitTypes.ClearingBail;
				case "Лимит открытых позиций на спот-рынке":
					return DerivativeLimitTypes.Spot;
				case "По совокупным средствам":
					return DerivativeLimitTypes.Term;
				default:
					throw new ArgumentOutOfRangeException("func", value, LocalizedStrings.Str1719);
			}
		}

		public static OptionTypes? GetOptionType(this Func<DdeTableColumn, object> func)
		{
			switch (func.Get<string>(DdeSecurityColumns.OptionType))
			{
				case "Call":
					return OptionTypes.Call;
				case "Put":
					return OptionTypes.Put;
				default:
					return null;
			}
		}

		public static TPlusLimits GetTNLimitType(this Func<DdeTableColumn, object> func, DdeTableColumn column)
		{
			var value = func.Get<string>(column);
			switch (value)
			{
				case "T0":
					return TPlusLimits.T0;
				case "T1":
					return TPlusLimits.T1;
				case "T2":
					return TPlusLimits.T2;
				default:
					throw new ArgumentOutOfRangeException(LocalizedStrings.Str1720Params.Put(value));
			}
		}

		private static SynchronizedDictionary<DdeTableColumn, Func<object, KeyValuePair<Level1Fields, object>>> _ddeColumnValueToSecurityChangeConverters;

		public static SynchronizedDictionary<DdeTableColumn, Func<object, KeyValuePair<Level1Fields, object>>> DdeColumnValueToSecurityChangeConverters
		{
			get { return _ddeColumnValueToSecurityChangeConverters ?? (_ddeColumnValueToSecurityChangeConverters = CreateDdeColumnValueToSecurityChangeConverters()); }
		}

		private static SynchronizedDictionary<DdeTableColumn, Func<object, KeyValuePair<Level1Fields, object>>> CreateDdeColumnValueToSecurityChangeConverters()
		{
			return new SynchronizedDictionary<DdeTableColumn, Func<object, KeyValuePair<Level1Fields, object>>>
			{
				{ DdeSecurityColumns.OpenPrice, v => new KeyValuePair<Level1Fields, object>(Level1Fields.OpenPrice, v.To<decimal>()) },
				{ DdeSecurityColumns.HighPrice, v => new KeyValuePair<Level1Fields, object>(Level1Fields.HighPrice, v.To<decimal>()) },
				{ DdeSecurityColumns.LowPrice, v => new KeyValuePair<Level1Fields, object>(Level1Fields.LowPrice, v.To<decimal>()) },
				{ DdeSecurityColumns.ClosePrice, v => new KeyValuePair<Level1Fields, object>(Level1Fields.ClosePrice, v.To<decimal>()) },
				{ DdeSecurityColumns.StepPrice, v => new KeyValuePair<Level1Fields, object>(Level1Fields.StepPrice, v.To<decimal>()) },
				{ DdeSecurityColumns.ImpliedVolatility, v => new KeyValuePair<Level1Fields, object>(Level1Fields.ImpliedVolatility, v.To<decimal>()) },
				{ DdeSecurityColumns.TheorPrice, v => new KeyValuePair<Level1Fields, object>(Level1Fields.TheorPrice, v.To<decimal>()) },
				{ DdeSecurityColumns.OpenPositions, v => new KeyValuePair<Level1Fields, object>(Level1Fields.OpenInterest, v.To<decimal>()) },
				{ DdeSecurityColumns.MinPrice, v => new KeyValuePair<Level1Fields, object>(Level1Fields.MinPrice, v.To<decimal>()) },
				{ DdeSecurityColumns.MaxPrice, v => new KeyValuePair<Level1Fields, object>(Level1Fields.MaxPrice, v.To<decimal>()) },
				{ DdeSecurityColumns.BidsCount, v => new KeyValuePair<Level1Fields, object>(Level1Fields.BidsCount, v.To<int>()) },
				{ DdeSecurityColumns.BidsVolume, v => new KeyValuePair<Level1Fields, object>(Level1Fields.BidsVolume, v.To<decimal>()) },
				{ DdeSecurityColumns.AsksCount, v => new KeyValuePair<Level1Fields, object>(Level1Fields.AsksCount, v.To<int>()) },
				{ DdeSecurityColumns.AsksVolume, v => new KeyValuePair<Level1Fields, object>(Level1Fields.AsksVolume, v.To<decimal>()) },
				{ DdeSecurityColumns.MarginBuy, v => new KeyValuePair<Level1Fields, object>(Level1Fields.MarginBuy, v.To<decimal>()) },
				{ DdeSecurityColumns.MarginSell, v => new KeyValuePair<Level1Fields, object>(Level1Fields.MarginSell, v.To<decimal>()) },
				{ DdeSecurityColumns.BestBidPrice, v => new KeyValuePair<Level1Fields, object>(Level1Fields.BestBidPrice, v.To<decimal>()) },
				{ DdeSecurityColumns.BestAskPrice, v => new KeyValuePair<Level1Fields, object>(Level1Fields.BestAskPrice, v.To<decimal>()) },
				{ DdeSecurityColumns.BestBidVolume, v => new KeyValuePair<Level1Fields, object>(Level1Fields.BestBidVolume, v.To<decimal>()) },
				{ DdeSecurityColumns.BestAskVolume, v => new KeyValuePair<Level1Fields, object>(Level1Fields.BestAskVolume, v.To<decimal>()) },
			};
		}

		public static DateTimeOffset GetTime(this Func<DdeTableColumn, object> func, DdeTable table, DdeTableColumn dateColumn, DdeTableColumn timeColumn, DdeTableColumn mcsColumn)
		{
			return func.GetNullableTime(table, dateColumn, timeColumn, mcsColumn) ?? DateTime.MinValue;
		}

		public static DateTimeOffset? GetNullableTime(this Func<DdeTableColumn, object> func, DdeTable table, DdeTableColumn dateColumn, DdeTableColumn timeColumn, DdeTableColumn mcsColumn)
		{
			if (func == null)
				throw new ArgumentNullException("func");

			var date = func.GetNullable2<DateTime>(dateColumn);

			if (date == null)
				return null;

			var time = func.GetNullable2<TimeSpan>(timeColumn);

			if (time == null)
				return null;

			var dateTime = date.Value + time.Value;

			if (table.Columns.Contains(mcsColumn))
				dateTime = dateTime.AddMilliseconds(func.Get<double>(mcsColumn) / 1000);

			return dateTime.ApplyTimeZone(TimeHelper.Moscow);
		}

		public static string GetSecurityClass(this IMessageSessionHolder sessionHolder, SecurityId securityId)
		{
			if (sessionHolder == null)
				throw new ArgumentNullException("sessionHolder");

			var pairs = sessionHolder.SecurityClassInfo
				.Where(p =>
					(securityId.SecurityType == null || p.Value.First == securityId.SecurityType) &&
					p.Value.Second.CompareIgnoreCase(securityId.BoardCode)
				).ToArray();

			switch (pairs.Length)
			{
				case 0:
					return securityId.BoardCode;
				//case 1:
				//	return pairs[0].Key;
				default:
					return pairs[0].Key;
			}
		}
	}
}