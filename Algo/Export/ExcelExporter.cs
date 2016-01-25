#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Algo
File: ExcelExporter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Media;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Interop;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The export into Excel.
	/// </summary>
	public class ExcelExporter : BaseExporter
	{
		private readonly Action _breaked;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExcelExporter"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="arg">The data parameter.</param>
		/// <param name="isCancelled">The processor, returning export interruption sign.</param>
		/// <param name="fileName">The path to file.</param>
		/// <param name="breaked">The processor, which will be called if maximal value of strings is exceeded.</param>
		public ExcelExporter(Security security, object arg, Func<int, bool> isCancelled, string fileName, Action breaked)
			: base(security, arg, isCancelled, fileName)
		{
			if (breaked == null)
				throw new ArgumentNullException(nameof(breaked));

			_breaked = breaked;
		}

		/// <summary>
		/// To export <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<ExecutionMessage> messages)
		{
			switch ((ExecutionTypes)Arg)
			{
				case ExecutionTypes.Tick:
				{
					Do(worker =>
					{
						worker
							.SetCell(0, 0, LocalizedStrings.Id).SetStyle(0, typeof(string))
							.SetCell(1, 0, LocalizedStrings.Time).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff zzz")
							.SetCell(2, 0, LocalizedStrings.Price).SetStyle(2, typeof(decimal))
							.SetCell(3, 0, LocalizedStrings.Volume).SetStyle(3, typeof(decimal))
							.SetCell(4, 0, LocalizedStrings.Str128)
							.SetCell(5, 0, LocalizedStrings.OI).SetStyle(5, typeof(decimal))
							.SetCell(6, 0, "UP_DOWN").SetStyle(5, typeof(bool));

						worker.SetConditionalFormatting(4, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Buy), null, Colors.Green);
						worker.SetConditionalFormatting(4, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Sell), null, Colors.Red);

						var index = 1;

						foreach (var message in messages)
						{
							worker
								.SetCell(0, index, message.TradeId == null ? message.TradeStringId : message.TradeId.To<string>())
								.SetCell(1, index, message.ServerTime)
								.SetCell(2, index, message.TradePrice)
								.SetCell(3, index, message.TradeVolume)
								.SetCell(4, index, message.OriginSide)
								.SetCell(5, index, message.OpenInterest)
								.SetCell(6, index, message.IsUpTick);

							index++;

							if (!Check(index))
								break;
						}
					});

					break;
				}
				case ExecutionTypes.OrderLog:
				{
					Do(worker =>
					{
						worker
							.SetCell(0, 0, LocalizedStrings.Id).SetStyle(0, typeof(string))
							.SetCell(1, 0, LocalizedStrings.Time).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff zzz")
							.SetCell(2, 0, LocalizedStrings.Price).SetStyle(2, typeof(decimal))
							.SetCell(3, 0, LocalizedStrings.Volume).SetStyle(3, typeof(decimal))
							.SetCell(4, 0, LocalizedStrings.Str128)
							.SetCell(5, 0, LocalizedStrings.Str722)
							.SetCell(6, 0, LocalizedStrings.Type)
							.SetCell(7, 0, LocalizedStrings.Str342)
							.SetCell(8, 0, LocalizedStrings.Str723).SetStyle(8, typeof(string))
							.SetCell(9, 0, LocalizedStrings.Str724).SetStyle(9, typeof(decimal))
							.SetCell(10, 0, LocalizedStrings.Str725).SetStyle(10, typeof(decimal));

						worker.SetConditionalFormatting(4, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Buy), null, Colors.Green);
						worker.SetConditionalFormatting(4, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Sell), null, Colors.Red);

						var index = 1;

						foreach (var message in messages)
						{
							worker
								.SetCell(0, index, message.OrderId == null ? message.OrderStringId : message.OrderId.To<string>())
								.SetCell(1, index, message.ServerTime)
								.SetCell(2, index, message.OrderPrice)
								.SetCell(3, index, message.OrderVolume)
								.SetCell(4, index, message.Side)
								.SetCell(5, index, message.OrderState)
								.SetCell(6, index, message.TimeInForce)
								.SetCell(7, index, message.IsSystem);

							if (message.TradePrice != null)
							{
								worker
									.SetCell(8, index, message.TradeId == null ? message.TradeStringId : message.TradeId.To<string>())
									.SetCell(9, index, message.TradePrice)
									.SetCell(10, index, message.OpenInterest);
							}

							index++;

							if (!Check(index))
								break;
						}
					});

					break;
				}
				case ExecutionTypes.Transaction:
				{
					Do(worker =>
					{
						worker
							.SetCell(0, 0, LocalizedStrings.Time).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff zzz")
							.SetCell(1, 0, LocalizedStrings.Portfolio)
							.SetCell(2, 0, LocalizedStrings.TransactionId)
							.SetCell(3, 0, LocalizedStrings.OrderId)
							.SetCell(4, 0, LocalizedStrings.Price).SetStyle(2, typeof(decimal))
							.SetCell(5, 0, LocalizedStrings.Volume).SetStyle(3, typeof(decimal))
							.SetCell(6, 0, LocalizedStrings.Str1325).SetStyle(3, typeof(decimal))
							.SetCell(7, 0, LocalizedStrings.Str128)
							.SetCell(8, 0, LocalizedStrings.Str132)
							.SetCell(9, 0, LocalizedStrings.Str134)
							.SetCell(10, 0, LocalizedStrings.Str506)
							.SetCell(11, 0, LocalizedStrings.TradePrice).SetStyle(3, typeof(decimal));

						worker.SetConditionalFormatting(7, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Buy), null, Colors.Green);
						worker.SetConditionalFormatting(7, ComparisonOperator.Equal, "\"{0}\"".Put(Sides.Sell), null, Colors.Red);

						worker.SetConditionalFormatting(9, ComparisonOperator.Equal, "\"{0}\"".Put(OrderStates.Active), null, Colors.Blue);
						worker.SetConditionalFormatting(9, ComparisonOperator.Equal, "\"{0}\"".Put(OrderStates.Done), null, Colors.Green);
						worker.SetConditionalFormatting(9, ComparisonOperator.Equal, "\"{0}\"".Put(OrderStates.Failed), null, Colors.Red);

						var index = 1;

						foreach (var message in messages)
						{
							worker
								.SetCell(0, index, message.ServerTime)
								.SetCell(1, index, message.PortfolioName)
								.SetCell(2, index, message.TransactionId)
								.SetCell(3, index, message.OrderId == null ? message.OrderStringId : message.OrderId.To<string>())
								.SetCell(4, index, message.OrderPrice)
								.SetCell(5, index, message.OrderVolume)
								.SetCell(6, index, message.Balance)
								.SetCell(7, index, message.Side)
								.SetCell(8, index, message.OrderType)
								.SetCell(9, index, message.OrderState)
								.SetCell(10, index, message.TradeId == null ? message.TradeStringId : message.TradeId.To<string>())
								.SetCell(11, index, message.TradePrice)
								.SetCell(12, index, message.HasOrderInfo)
								.SetCell(13, index, message.HasTradeInfo);

							index++;

							if (!Check(index))
								break;
						}
					});

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// To export <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Do(worker =>
			{
				var rowIndex = 0;

				foreach (var message in messages)
				{
					worker
						.SetCell(0, rowIndex, LocalizedStrings.Time)
						.SetCell(1, rowIndex, message.ServerTime);

					var columnIndex = 0;

					foreach (var quote in message.Bids.Concat(message.Asks).OrderByDescending(q => q.Price))
					{
						worker
							.SetCell(columnIndex, rowIndex + (quote.Side == Sides.Buy ? 1 : 3), quote.Price)
							.SetCell(columnIndex, rowIndex + 2, quote.Volume);

						columnIndex++;
					}

					rowIndex += 4;

					if (!Check(rowIndex))
						break;
				}
			});
		}

		/// <summary>
		/// To export <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Do(worker =>
			{
				var columns = new Dictionary<Level1Fields, int>();
				//{
				//	{ Level1Fields.LastTradeId, 1 },
				//	{ Level1Fields.LastTradePrice, 2 },
				//	{ Level1Fields.LastTradeVolume, 3 },
				//	{ Level1Fields.LastTradeOrigin, 4 },
				//	{ Level1Fields.BestBidPrice, 5 },
				//	{ Level1Fields.BestBidVolume, 6 },
				//	{ Level1Fields.BestAskPrice, 7 },
				//	{ Level1Fields.BestAskVolume, 8 },
				//	{ Level1Fields.StepPrice, 9 },
				//	{ Level1Fields.OpenInterest, 10 },
				//	{ Level1Fields.TheorPrice, 11 },
				//	{ Level1Fields.ImpliedVolatility, 12 },
				//	{ Level1Fields.OpenPrice, 13 },
				//	{ Level1Fields.HighPrice, 14 },
				//	{ Level1Fields.LowPrice, 15 },
				//	{ Level1Fields.ClosePrice, 16 },
				//	{ Level1Fields.Volume, 17 },
				//};

				worker
					.SetCell(0, 0, LocalizedStrings.Time).SetStyle(0, "yyyy-MM-dd HH:mm:ss.fff");

				//foreach (var pair in columns)
				//{
				//	var field = pair.Key;
				//	var columnIndex = pair.Value;

				//	worker.SetCell(columnIndex, 0, field.GetDisplayName());

				//	ApplyCellStyle(worker, field, columnIndex);
				//}

				var row = 1;

				foreach (var message in messages)
				{
					worker.SetCell(0, row, message.LocalTime);

					foreach (var pair in message.Changes)
					{
						var field = pair.Key;

						var columnIndex = columns.TryGetValue2(field);

						if (columnIndex == null)
						{
							columnIndex = columns.Count;
							columns.Add(field, columnIndex.Value);

							worker.SetCell(columnIndex.Value, 0, field.GetDisplayName());
							ApplyCellStyle(worker, field, columnIndex.Value);
						}

						worker.SetCell(columns[field], row, pair.Value);
					}

					if (!Check(++row))
						break;
				}
			});
		}

		private static void ApplyCellStyle(ExcelWorker worker, Level1Fields field, int column)
		{
			switch (field)
			{
				case Level1Fields.LastTrade:
				case Level1Fields.BestAsk:
				case Level1Fields.BestBid:
					break;
				case Level1Fields.LastTradeId:
				case Level1Fields.BidsCount:
				case Level1Fields.AsksCount:
				case Level1Fields.TradesCount:
					worker.SetStyle(column, typeof(long));
					break;
				case Level1Fields.LastTradeTime:
				case Level1Fields.BestAskTime:
				case Level1Fields.BestBidTime:
					worker.SetStyle(column, typeof(DateTimeOffset));
					break;
				default:
					worker.SetStyle(column, typeof(decimal));
					break;
			}
		}

		/// <summary>
		/// To export <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			Do(worker =>
			{
				worker
					.SetCell(0, 0, LocalizedStrings.Str726).SetStyle(0, "yyyy-MM-dd HH:mm:ss.fff")
					.SetCell(1, 0, LocalizedStrings.Str727).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff")
					.SetCell(2, 0, "O").SetStyle(2, typeof(decimal))
					.SetCell(3, 0, "H").SetStyle(3, typeof(decimal))
					.SetCell(4, 0, "L").SetStyle(4, typeof(decimal))
					.SetCell(5, 0, "C").SetStyle(5, typeof(decimal))
					.SetCell(6, 0, "V").SetStyle(6, typeof(decimal))
					.SetCell(7, 0, LocalizedStrings.OI).SetStyle(7, typeof(decimal));

				var index = 1;

				foreach (var candle in messages)
				{
					worker
						.SetCell(0, index, candle.OpenTime)
						.SetCell(1, index, candle.CloseTime)
						.SetCell(2, index, candle.OpenPrice)
						.SetCell(3, index, candle.HighPrice)
						.SetCell(4, index, candle.LowPrice)
						.SetCell(5, index, candle.ClosePrice)
						.SetCell(6, index, candle.TotalVolume)
						.SetCell(7, index, candle.OpenInterest);

					index++;

					if (!Check(index))
						break;
				}
			});
		}

		/// <summary>
		/// To export <see cref="NewsMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			Do(worker =>
			{
				worker
					.SetCell(0, 0, LocalizedStrings.Id).SetStyle(0, typeof(string))
					.SetCell(1, 0, LocalizedStrings.Time).SetStyle(1, "yyyy-MM-dd HH:mm:ss.fff")
					.SetCell(2, 0, LocalizedStrings.Security).SetStyle(2, typeof(string))
					.SetCell(3, 0, LocalizedStrings.Board).SetStyle(3, typeof(string))
					.SetCell(4, 0, LocalizedStrings.Str215).SetStyle(4, typeof(string))
					.SetCell(5, 0, LocalizedStrings.Str217).SetStyle(5, typeof(string))
					.SetCell(6, 0, LocalizedStrings.Str213).SetStyle(6, typeof(string))
					.SetCell(7, 0, LocalizedStrings.Str221).SetStyle(6, typeof(string));

				var index = 1;

				foreach (var n in messages)
				{
					worker
						.SetCell(0, index, n.Id)
						.SetCell(1, index, n.ServerTime)
						.SetCell(2, index, n.SecurityId == null ? null : n.SecurityId.Value.SecurityCode)
						.SetCell(3, index, n.BoardCode)
						.SetCell(4, index, n.Headline)
						.SetCell(5, index, n.Story)
						.SetCell(6, index, n.Source)
						.SetCell(7, index, n.Url);

					index++;

					if (!Check(index))
						break;
				}
			});
		}

		/// <summary>
		/// To export <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="messages">Messages.</param>
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			Do(worker =>
			{
				var colIndex = 0;

				worker
					.SetCell(colIndex, 0, LocalizedStrings.Code).SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, LocalizedStrings.Board).SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, LocalizedStrings.Name).SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, LocalizedStrings.Str363).SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, LocalizedStrings.PriceStep).SetStyle(colIndex++, typeof(decimal))
					.SetCell(colIndex, 0, LocalizedStrings.VolumeStep).SetStyle(colIndex++, typeof(decimal))
					.SetCell(colIndex, 0, LocalizedStrings.Str330).SetStyle(colIndex++, typeof(decimal))
					.SetCell(colIndex, 0, LocalizedStrings.Type).SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, LocalizedStrings.Decimals).SetStyle(colIndex++, typeof(decimal))
					.SetCell(colIndex, 0, LocalizedStrings.Str551).SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, LocalizedStrings.Strike).SetStyle(colIndex++, typeof(decimal))
					.SetCell(colIndex, 0, LocalizedStrings.UnderlyingAsset).SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, LocalizedStrings.ExpiryDate).SetStyle(colIndex++, "yyyy-MM-dd")
					.SetCell(colIndex, 0, LocalizedStrings.SettlementDate).SetStyle(colIndex++, "yyyy-MM-dd")
					.SetCell(colIndex, 0, LocalizedStrings.Currency).SetStyle(colIndex++, typeof(string))

					.SetCell(colIndex, 0, "Bloomberg").SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, "CUSIP").SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, "IQFeed").SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, "InteractiveBrokers").SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, "ISIN").SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, "Plaza").SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, "RIC").SetStyle(colIndex++, typeof(string))
					.SetCell(colIndex, 0, "SEDOL").SetStyle(colIndex, typeof(string));

				var rowIndex = 1;

				foreach (var security in messages)
				{
					colIndex = 0;

					worker
						.SetCell(colIndex++, rowIndex, security.SecurityId.SecurityCode)
						.SetCell(colIndex++, rowIndex, security.SecurityId.BoardCode)
						.SetCell(colIndex++, rowIndex, security.Name)
						.SetCell(colIndex++, rowIndex, security.ShortName)
						.SetCell(colIndex++, rowIndex, security.PriceStep)
						.SetCell(colIndex++, rowIndex, security.VolumeStep)
						.SetCell(colIndex++, rowIndex, security.Multiplier)
						.SetCell(colIndex++, rowIndex, security.SecurityType == null ? string.Empty : security.SecurityType.Value.GetDisplayName())
						.SetCell(colIndex++, rowIndex, security.Decimals)
						.SetCell(colIndex++, rowIndex, security.OptionType == null ? string.Empty : security.OptionType.Value.GetDisplayName())
						.SetCell(colIndex++, rowIndex, security.Strike)
						.SetCell(colIndex++, rowIndex, security.BinaryOptionType)
						.SetCell(colIndex++, rowIndex, security.UnderlyingSecurityCode)
						.SetCell(colIndex++, rowIndex, security.ExpiryDate)
						.SetCell(colIndex++, rowIndex, security.SettlementDate)
						.SetCell(colIndex++, rowIndex, security.Currency == null ? string.Empty : security.Currency.Value.GetDisplayName())
						.SetCell(colIndex++, rowIndex, security.SecurityId.Bloomberg)
						.SetCell(colIndex++, rowIndex, security.SecurityId.Cusip)
						.SetCell(colIndex++, rowIndex, security.SecurityId.IQFeed)
						.SetCell(colIndex++, rowIndex, security.SecurityId.InteractiveBrokers)
						.SetCell(colIndex++, rowIndex, security.SecurityId.Isin)
						.SetCell(colIndex++, rowIndex, security.SecurityId.Plaza)
						.SetCell(colIndex++, rowIndex, security.SecurityId.Ric)
						.SetCell(colIndex, rowIndex, security.SecurityId.Sedol);

					rowIndex++;

					if (!Check(rowIndex))
						break;
				}
			});
		}

		private void Do(Action<ExcelWorker> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			using (var worker = new ExcelWorker())
			{
				action(worker);
				worker.Save(Path, false);
			}
		}

		private bool Check(int index)
		{
			// http://office.microsoft.com/en-us/excel-help/excel-specifications-and-limits-HA103980614.aspx
			if (index < 1048576)
			//if (index < (ushort.MaxValue - 1))
			{
				return CanProcess();
			}
			else
			{
				_breaked();
				return false;
			}
		}
	}
}