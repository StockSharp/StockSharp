#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Reporting.Algo
File: ExcelStrategyReport.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies.Reporting
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Interop;
	using Ecng.Collections;

	using StockSharp.Algo.PnL;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// The report generator for the strategy in the Excel format.
	/// </summary>
	public class ExcelStrategyReport : StrategyReport
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ExcelStrategyReport"/>.
		/// </summary>
		/// <param name="strategy">The strategy, requiring the report generation.</param>
		/// <param name="fileName">The name of the file, in which report is generated in the Excel format.</param>
		public ExcelStrategyReport(Strategy strategy, string fileName)
			: this(new[] { strategy }, fileName)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ExcelStrategyReport"/>.
		/// </summary>
		/// <param name="strategies">Strategies, requiring the report generation.</param>
		/// <param name="fileName">The name of the file, in which report is generated in the Excel format.</param>
		public ExcelStrategyReport(IEnumerable<Strategy> strategies, string fileName)
			: base(strategies, fileName)
		{
			//SheetName = "Отчет по " + strategy;
			ExcelVersion = 2007;
			IncludeOrders = true;
			Decimals = 2;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ExcelStrategyReport"/>.
		/// </summary>
		/// <param name="strategies">Strategies, requiring the report generation.</param>
		/// <param name="fileName">The name of the file, in which report is generated in the Excel format.</param>
		/// <param name="template">The template file, to be copied into <see cref="StrategyReport.FileName"/>.</param>
		public ExcelStrategyReport(IEnumerable<Strategy> strategies, string fileName, string template)
			: this(strategies, fileName)
		{
			if (template.IsEmpty())
				throw new ArgumentNullException(nameof(template));

			Template = template;
		}

		///// <summary>
		///// Создать генератор Excel отчетов.
		///// </summary>
		///// <param name="strategy">Стратегия, для которой необходимо сгенерировать отчет.</param>
		///// <param name="fileName">Название файла, в котором сгенерируется отчет в формате Excel.</param>
		///// <param name="template">Файл-шаблон, который будет скопирован в <see cref="FileName"/> и дозаполнен листом <see cref="SheetName"/>.</param>
		///// <param name="sheetName">Названия листа, в который будет записан отчет.</param>
		//public ExcelStrategyReport(Strategy strategy, string fileName, string template, string sheetName)
		//	: this(strategy, fileName, template)
		//{
		//	if (sheetName.IsEmpty())
		//		throw new ArgumentNullException(nameof(sheetName));

		//	SheetName = sheetName;
		//}

		/// <summary>
		/// The template file, to be copied into <see cref="StrategyReport.FileName"/> and filled up with Strategy, Orders and Trades sheets.
		/// </summary>
		public string Template { get; }

		///// <summary>
		///// Названия листа, в который будет записан отчет.
		///// </summary>
		//public string SheetName { get; private set; }

		/// <summary>
		/// The Excel version. It affects the maximal number of strings. The default value is 2007.
		/// </summary>
		public int ExcelVersion { get; set; }

		/// <summary>
		/// To add orders to the report. Orders are added by default.
		/// </summary>
		public bool IncludeOrders { get; set; }

		/// <summary>
		/// The number of decimal places. By default, it equals to 2.
		/// </summary>
		public int Decimals { get; set; }

		/// <summary>
		/// To generate the report.
		/// </summary>
		public override void Generate()
		{
			var hasTemplate = Template.IsEmpty();

			using (var worker = hasTemplate ? new ExcelWorker() : new ExcelWorker(Template))
			{
				foreach (var strategy in Strategies)
				{
					if (Template.IsEmpty())
					{
						worker.RenameSheet(strategy.Name);
					}
					else
					{
						if (worker.ContainsSheet(strategy.Name))
							worker.AddSheet(strategy.Name);
					}

					worker
						.SetCell(0, 0, LocalizedStrings.Str1331)

						.SetCell(0, 1, LocalizedStrings.Security + ":")
						.SetCell(1, 1, strategy.Security != null ? strategy.Security.Id : string.Empty)

						.SetCell(0, 2, LocalizedStrings.Portfolio + ":")
						.SetCell(1, 2, strategy.Portfolio != null ? strategy.Portfolio.Name : string.Empty)

						.SetCell(0, 3, LocalizedStrings.Str1334)
						.SetCell(1, 3, Format(strategy.TotalWorkingTime))

						.SetCell(0, 4, LocalizedStrings.Str1335)
						//.SetCell(1, 4, FormatTime(base.Strategy.TotalCPUTime))

						.SetCell(0, 5, LocalizedStrings.Str862 + ":")
						.SetCell(1, 5, strategy.Position)

						.SetCell(0, 6, LocalizedStrings.PnL + ":")
						.SetCell(1, 6, strategy.PnL)

						.SetCell(0, 7, LocalizedStrings.Str159 + ":")
						.SetCell(1, 7, strategy.Commission)

						.SetCell(0, 8, LocalizedStrings.Str163 + ":")
						.SetCell(1, 8, strategy.Slippage)

						.SetCell(0, 9, LocalizedStrings.Str161 + ":")
						.SetCell(1, 9, Format(strategy.Latency));

					var rowIndex = 11;

					foreach (var parameter in strategy.StatisticManager.Parameters.SyncGet(c => c.ToArray()))
					{
						var value = parameter.Value;

						if (value is TimeSpan)
							value = Format((TimeSpan)value);
						else if (value is decimal)
							value = MathHelper.Round((decimal)value, Decimals);

						worker
							.SetCell(0, rowIndex, parameter.Name)
							.SetCell(1, rowIndex, value);

						rowIndex++;
					}

					rowIndex += 2;
					worker.SetCell(0, rowIndex, LocalizedStrings.Str1340);
					rowIndex++;

					foreach (var strategyParam in strategy.Parameters.CachedValues)
					{
						var value = strategyParam.Value;

						if (value is TimeSpan)
							value = Format((TimeSpan)value);
						else if (value is decimal)
							value = MathHelper.Round((decimal)value, Decimals);

						worker
							.SetCell(0, rowIndex, strategyParam.Name)
							.SetCell(1, rowIndex, value);

						rowIndex++;
					}

					//rowIndex += 2;
					//worker.SetCell(0, rowIndex, "Комиссия по типам:");
					//rowIndex++;

					//foreach (var group in Strategy.CommissionManager.Rules.SyncGet(c => c.ToArray()).GroupBy(c => c.GetType().GetDisplayName()))
					//{
					//	var commission = group.Sum(r => r.Commission);

					//	if (commission == 0)
					//		continue;

					//	worker
					//		.SetCell(0, rowIndex, group.Key)
					//		.SetCell(1, rowIndex, commission);

					//	rowIndex++;
					//}

					var columnShift = 3;

					worker
						.SetCell(columnShift + 0, 0, LocalizedStrings.Str985)

						.SetCell(columnShift + 0, 1, LocalizedStrings.Str1192).SetStyle(columnShift + 0, typeof(long))
						.SetCell(columnShift + 1, 1, LocalizedStrings.Transaction).SetStyle(columnShift + 1, typeof(long))
						.SetCell(columnShift + 2, 1, LocalizedStrings.Time).SetStyle(columnShift + 2, "HH:mm:ss.fff")
						.SetCell(columnShift + 3, 1, LocalizedStrings.Price).SetStyle(columnShift + 3, typeof(decimal))
						.SetCell(columnShift + 4, 1, LocalizedStrings.Str1341).SetStyle(columnShift + 4, typeof(decimal))
						.SetCell(columnShift + 5, 1, LocalizedStrings.Volume).SetStyle(columnShift + 5, typeof(decimal))
						.SetCell(columnShift + 6, 1, LocalizedStrings.Str128)
						.SetCell(columnShift + 7, 1, LocalizedStrings.Str1190).SetStyle(columnShift + 7, typeof(long))
						.SetCell(columnShift + 8, 1, LocalizedStrings.Str163).SetStyle(columnShift + 8, typeof(decimal))
						.SetCell(columnShift + 9, 1, LocalizedStrings.Str135)
						.SetCell(columnShift + 10, 1, LocalizedStrings.Str1342).SetStyle(columnShift + 11, typeof(decimal))
						.SetCell(columnShift + 11, 1, LocalizedStrings.Str1343).SetStyle(columnShift + 12, typeof(decimal))
						.SetCell(columnShift + 12, 1, LocalizedStrings.Str1344).SetStyle(columnShift + 13, typeof(decimal))
						.SetCell(columnShift + 13, 1, LocalizedStrings.Str1345).SetStyle(columnShift + 14, typeof(decimal))
						.SetCell(columnShift + 14, 1, LocalizedStrings.Str862).SetStyle(columnShift + 15, typeof(decimal));

					worker
						.SetConditionalFormatting(columnShift + 10, ComparisonOperator.Less, "0", null, Colors.Red)
						.SetConditionalFormatting(columnShift + 11, ComparisonOperator.Less, "0", null, Colors.Red)
						.SetConditionalFormatting(columnShift + 12, ComparisonOperator.Less, "0", null, Colors.Red)
						.SetConditionalFormatting(columnShift + 13, ComparisonOperator.Less, "0", null, Colors.Red);

					var totalPnL = 0m;
					var position = 0m;

					var queues = new Dictionary<Security, PnLQueue>();

					rowIndex = 2;
					foreach (var trade in strategy.MyTrades.ToArray())
					{
						var info = strategy.PnLManager.ProcessMessage(trade.ToMessage());

						totalPnL += info.PnL;
						position += trade.GetPosition() ?? 0;

						var queue = queues.SafeAdd(trade.Trade.Security, key => new PnLQueue(key.ToSecurityId()));

						var localInfo = queue.Process(trade.ToMessage());

						worker
							.SetCell(columnShift + 0, rowIndex, trade.Trade.Id)
							.SetCell(columnShift + 1, rowIndex, trade.Order.TransactionId)
							.SetCell(columnShift + 2, rowIndex, Format(trade.Trade.Time))
							.SetCell(columnShift + 3, rowIndex, trade.Trade.Price)
							.SetCell(columnShift + 4, rowIndex, trade.Order.Price)
							.SetCell(columnShift + 5, rowIndex, trade.Trade.Volume)
							.SetCell(columnShift + 6, rowIndex, Format(trade.Order.Direction))
							.SetCell(columnShift + 7, rowIndex, trade.Order.Id)
							.SetCell(columnShift + 8, rowIndex, trade.Slippage)
							.SetCell(columnShift + 9, rowIndex, trade.Order.Comment)
							.SetCell(columnShift + 10, rowIndex, MathHelper.Round(info.PnL, Decimals))
							.SetCell(columnShift + 11, rowIndex, MathHelper.Round(localInfo.PnL, Decimals))
							.SetCell(columnShift + 12, rowIndex, MathHelper.Round(totalPnL, Decimals))
							.SetCell(columnShift + 13, rowIndex, MathHelper.Round(queue.RealizedPnL, Decimals))
							.SetCell(columnShift + 14, rowIndex, position);

						rowIndex++;
					}

					if (IncludeOrders)
					{
						columnShift += 17;

						worker
							.SetCell(columnShift + 0, 0, LocalizedStrings.Orders)

							.SetCell(columnShift + 0, 1, LocalizedStrings.Str1190).SetStyle(columnShift + 0, typeof(long))
							.SetCell(columnShift + 1, 1, LocalizedStrings.Transaction).SetStyle(columnShift + 1, typeof(long))
							.SetCell(columnShift + 2, 1, LocalizedStrings.Str128)
							.SetCell(columnShift + 3, 1, LocalizedStrings.Str1346).SetStyle(columnShift + 3, "HH:mm:ss.fff")
							.SetCell(columnShift + 4, 1, LocalizedStrings.Str1347).SetStyle(columnShift + 4, "HH:mm:ss.fff")
							.SetCell(columnShift + 5, 1, LocalizedStrings.Str1348)
							.SetCell(columnShift + 6, 1, LocalizedStrings.Price).SetStyle(columnShift + 6, typeof(decimal))
							.SetCell(columnShift + 7, 1, LocalizedStrings.Str1323).SetStyle(columnShift + 7, typeof(decimal))
							.SetCell(columnShift + 8, 1, LocalizedStrings.Str1324)
							.SetCell(columnShift + 9, 1, LocalizedStrings.State)
							.SetCell(columnShift + 10, 1, LocalizedStrings.Str1325).SetStyle(columnShift + 10, typeof(decimal))
							.SetCell(columnShift + 11, 1, LocalizedStrings.Volume).SetStyle(columnShift + 11, typeof(decimal))
							.SetCell(columnShift + 12, 1, LocalizedStrings.Type)
							.SetCell(columnShift + 13, 1, LocalizedStrings.Str1326)
							.SetCell(columnShift + 14, 1, LocalizedStrings.Str1327)
							.SetCell(columnShift + 15, 1, LocalizedStrings.Str135);

						worker
							.SetConditionalFormatting(columnShift + 9, ComparisonOperator.Equal, "\"{0}\"".Put(LocalizedStrings.Str1329), null, Colors.Green)
							.SetConditionalFormatting(columnShift + 9, ComparisonOperator.Equal, "\"{0}\"".Put(LocalizedStrings.Str238), null, Colors.Red);

						rowIndex = 2;
						foreach (var order in strategy.Orders.ToArray())
						{
							worker
								.SetCell(columnShift + 0, rowIndex, order.Id)
								.SetCell(columnShift + 1, rowIndex, order.TransactionId)
								.SetCell(columnShift + 2, rowIndex, Format(order.Direction))
								.SetCell(columnShift + 3, rowIndex, Format(order.Time))
								.SetCell(columnShift + 4, rowIndex, Format(order.LastChangeTime))
								.SetCell(columnShift + 5, rowIndex, Format(order.LastChangeTime - order.Time))
								.SetCell(columnShift + 6, rowIndex, order.Price)
								.SetCell(columnShift + 7, rowIndex, MathHelper.Round(order.GetAveragePrice(strategy.Connector), Decimals))
								.SetCell(columnShift + 8, rowIndex, Format(order.State))
								.SetCell(columnShift + 9, rowIndex, order.IsMatched() ? LocalizedStrings.Str1328 : (order.IsCanceled() ? LocalizedStrings.Str1329 : LocalizedStrings.Str238))
								.SetCell(columnShift + 10, rowIndex, order.Balance)
								.SetCell(columnShift + 11, rowIndex, order.Volume)
								.SetCell(columnShift + 12, rowIndex, Format(order.Type))
								.SetCell(columnShift + 13, rowIndex, Format(order.LatencyRegistration))
								.SetCell(columnShift + 14, rowIndex, Format(order.LatencyCancellation))
								.SetCell(columnShift + 15, rowIndex, order.Comment);

							rowIndex++;
						}

						var stopOrders = strategy.StopOrders.ToArray();

						if (stopOrders.Length > 0)
						{
							rowIndex += 2;

							worker
								.SetCell(columnShift + 0, rowIndex - 1, LocalizedStrings.Str1351)

								.SetCell(columnShift + 0, rowIndex, LocalizedStrings.Str1190).SetStyle(columnShift + 0, typeof(long))
								.SetCell(columnShift + 1, rowIndex, LocalizedStrings.Transaction).SetStyle(columnShift + 1, typeof(long))
								.SetCell(columnShift + 2, rowIndex, LocalizedStrings.Str128)
								.SetCell(columnShift + 3, rowIndex, LocalizedStrings.Time).SetStyle(columnShift + 3, "HH:mm:ss.fff")
								.SetCell(columnShift + 4, rowIndex, LocalizedStrings.Price).SetStyle(columnShift + 4, typeof(decimal))
								.SetCell(columnShift + 5, rowIndex, LocalizedStrings.Str1324)
								.SetCell(columnShift + 6, rowIndex, LocalizedStrings.State)
								.SetCell(columnShift + 7, rowIndex, LocalizedStrings.Volume).SetStyle(columnShift + 7, typeof(decimal))
								.SetCell(columnShift + 8, rowIndex, LocalizedStrings.Str1326)
								.SetCell(columnShift + 9, rowIndex, LocalizedStrings.Str1327)
								//.SetCell(columnShift + 10, rowIndex, LocalizedStrings.Str1352).SetStyle(columnShift + 9, typeof(long))
								;

							var stopParams = stopOrders[0].Condition.Parameters.Keys.ToArray();

							for (var i = 0; i < stopParams.Length; i++)
								worker.SetCell(columnShift + 11 + i, rowIndex, stopParams[i]);

							foreach (var order in stopOrders)
							{
								worker
									.SetCell(columnShift + 0, rowIndex, order.Id)
									.SetCell(columnShift + 1, rowIndex, order.TransactionId)
									.SetCell(columnShift + 2, rowIndex, Format(order.Direction))
									.SetCell(columnShift + 3, rowIndex, Format(order.Time))
									.SetCell(columnShift + 4, rowIndex, order.Price)
									.SetCell(columnShift + 5, rowIndex, Format(order.State))
									.SetCell(columnShift + 6, rowIndex, order.IsMatched() ? LocalizedStrings.Str1328 : (order.IsCanceled() ? LocalizedStrings.Str1329 : string.Empty))
									.SetCell(columnShift + 7, rowIndex, order.Volume)
									.SetCell(columnShift + 8, rowIndex, Format(order.LatencyRegistration))
									.SetCell(columnShift + 9, rowIndex, Format(order.LatencyCancellation))
									//.SetCell(columnShift + 10, rowIndex, order.DerivedOrder != null ? (object)order.DerivedOrder.Id : string.Empty)
									;

								for (var i = 0; i < stopParams.Length; i++)
									worker.SetCell(columnShift + 11 + i, rowIndex, order.Condition.Parameters[stopParams[i]] ?? string.Empty);

								rowIndex++;
							}
						}
					}
				}

				worker.Save(FileName, true);
			}
		}
	}
}