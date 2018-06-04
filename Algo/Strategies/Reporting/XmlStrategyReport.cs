#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Reporting.Algo
File: XmlStrategyReport.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies.Reporting
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Xml.Linq;

	using Ecng.Collections;

	using StockSharp.Algo.Strategies;

	/// <summary>
	/// The report generator for the strategy in the xml format.
	/// </summary>
	public class XmlStrategyReport : StrategyReport
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlStrategyReport"/>.
		/// </summary>
		/// <param name="strategy">The strategy, requiring the report generation.</param>
		/// <param name="fileName">The name of the file, in which report in the Xml format will be generated.</param>
		public XmlStrategyReport(Strategy strategy, string fileName)
			: this(new[] { strategy }, fileName)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlStrategyReport"/>.
		/// </summary>
		/// <param name="strategies">Strategies, requiring the report generation.</param>
		/// <param name="fileName">The name of the file, in which report in the xml format will be generated.</param>
		public XmlStrategyReport(IEnumerable<Strategy> strategies, string fileName)
			: base(strategies, fileName)
		{
		}

		/// <summary>
		/// To generate the report.
		/// </summary>
		public override void Generate()
		{
			new XElement("strategies",
				Strategies.Select(strategy =>
					new XElement("strategy",
						new XElement("name", strategy.Name),
						new XElement("security", strategy.Security != null ? strategy.Security.Id : string.Empty),
						new XElement("portfolio", strategy.Portfolio != null ? strategy.Portfolio.Name : string.Empty),
						new XElement("parameters",
							strategy.Parameters.CachedValues.Select(p =>
								new XElement("parameter",
									new XElement("name", p.Name),
									new XElement("value", p.Value is TimeSpan ts ? Format(ts) : p.Value)
									))),
						new XElement("totalWorkingTime", Format(strategy.TotalWorkingTime)),
						new XElement("commission", strategy.Commission),
						new XElement("position", strategy.Position),
						new XElement("PnL", strategy.PnL),
						new XElement("slippage", strategy.Slippage),
						new XElement("latency", Format(strategy.Latency)),
						new XElement("statisticParameters",
							strategy.StatisticManager.Parameters.SyncGet(c => c.ToArray()).Select(p =>
								new XElement("parameter",
									new XElement("name", p.Name),
									new XElement("value", p.Value is TimeSpan ts ? Format(ts) : p.Value)
									))),
						new XElement("orders",
							strategy.Orders.OrderBy(o => o.TransactionId).Select(o =>
								new XElement("order",
									new XElement("id", o.Id),
									new XElement("transactionId", o.TransactionId),
									new XElement("direction", o.Direction),
									new XElement("time", Format(o.Time)),
									new XElement("price", o.Price),
									new XElement("averagePrice", o.GetAveragePrice(strategy.Connector)),
									new XElement("state", o.State),
									new XElement("balance", o.Balance),
									new XElement("volume", o.Volume),
									new XElement("type", o.Type),
									new XElement("latencyRegistration", Format(o.LatencyRegistration)),
									new XElement("latencyCancellation", Format(o.LatencyCancellation))
									))),
						new XElement("trades",
							strategy.MyTrades.OrderBy(t => t.Order.TransactionId).Select(t =>
								new XElement("trade",
									new XElement("id", t.Trade.Id),
									new XElement("transactionId", t.Order.TransactionId),
									new XElement("time", Format(t.Trade.Time)),
									new XElement("price", t.Trade.Price),
									new XElement("volume", t.Trade.Volume),
									new XElement("order", t.Order.Id),
									new XElement("PnL", strategy.PnLManager.ProcessMessage(t.ToMessage()).PnL),
									new XElement("slippage", t.Slippage)
									))),
						new XElement("stopOrders",
							strategy.StopOrders.OrderBy(o => o.TransactionId).Select(o =>
								new XElement("order",
									new XElement("id", o.Id),
									new XElement("transactionId", o.TransactionId),
									new XElement("direction", o.Direction),
									new XElement("time", Format(o.Time)),
									new XElement("price", o.Price),
									new XElement("state", o.State),
									new XElement("volume", o.Volume),
									new XElement("latencyRegistration", Format(o.LatencyRegistration)),
									new XElement("latencyCancellation", Format(o.LatencyCancellation)),
									//new XElement("derivedOrderId", o.DerivedOrder != null ? (object)o.DerivedOrder.Id : string.Empty),
									new XElement("parameters", o.Condition.Parameters.Select(p => new XElement(p.Key, p.Value)))
								)))
					))
				).Save(FileName);
		}
	}
}