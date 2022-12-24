namespace StockSharp.Algo.Strategies.Reporting;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Ecng.Common;

/// <summary>
/// The report generator for the strategy in the xml format.
/// </summary>
public class XmlReportGenerator : BaseReportGenerator
{
	/// <inheritdoc />
	public override string Name => "XML";

	/// <inheritdoc />
	public override string Extension => "xml";

	/// <inheritdoc />
	public override ValueTask Generate(Strategy strategy, string fileName, CancellationToken cancellationToken)
	{
		using var writer = new XmlTextWriter(fileName, Encoding.UTF8) { Formatting = Formatting.Indented };

		void WriteStartElement(string name)
			=> writer.WriteStartElement(name);

		void WriteEndElement()
			=> writer.WriteEndElement();

		void WriteElementString(string name, object value)
			=> writer.WriteElementString(name, value is TimeSpan ts ? ts.Format() : value.To<string>());

		WriteStartElement("strategy");

		WriteElementString("name", strategy.Name);
		WriteElementString("security", strategy.Security?.Id);
		WriteElementString("portfolio", strategy.Portfolio?.Name);

		WriteStartElement("parameters");

		foreach (var p in strategy.Parameters.CachedValues)
		{
			cancellationToken.ThrowIfCancellationRequested();

			WriteStartElement("parameter");

			WriteElementString("name", p.Name);
			WriteElementString("value", p.Value);

			WriteEndElement();
		}

		WriteEndElement();

		WriteElementString("totalWorkingTime", strategy.TotalWorkingTime);
		WriteElementString("commission", strategy.Commission);
		WriteElementString("position", strategy.Position);
		WriteElementString("PnL", strategy.PnL);
		WriteElementString("slippage", strategy.Slippage);
		WriteElementString("latency", strategy.Latency);

		WriteStartElement("statisticParameters");

		foreach (var p in strategy.StatisticManager.Parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			WriteStartElement("parameter");

			WriteElementString("name", p.Name);
			WriteElementString("value", p.Value);

			WriteEndElement();
		}

		WriteEndElement();

		WriteStartElement("orders");

		foreach (var o in strategy.Orders)
		{
			cancellationToken.ThrowIfCancellationRequested();

			WriteStartElement("order");

			WriteElementString("id", o.Id);
			WriteElementString("transactionId", o.TransactionId);
			WriteElementString("direction", o.Direction);
			WriteElementString("time", o.Time);
			WriteElementString("price", o.Price);
			WriteElementString("state", o.State);
			WriteElementString("balance", o.Balance);
			WriteElementString("volume", o.Volume);
			WriteElementString("type", o.Type);

			WriteEndElement();
		}

		WriteEndElement();

		WriteStartElement("trades");

		foreach (var t in strategy.MyTrades)
		{
			cancellationToken.ThrowIfCancellationRequested();

			WriteStartElement("trade");

			WriteElementString("id", t.Trade.Id);
			WriteElementString("transactionId", t.Order.TransactionId);
			WriteElementString("time", t.Trade.Time);
			WriteElementString("price", t.Trade.Price);
			WriteElementString("volume", t.Trade.Volume);
			WriteElementString("order", t.Order.Id);
			WriteElementString("PnL", strategy.PnLManager.ProcessMessage(t.ToMessage())?.PnL);
			WriteElementString("slippage", t.Slippage);

			WriteEndElement();
		}

		WriteEndElement();

		WriteEndElement();

		return default;
	}
}