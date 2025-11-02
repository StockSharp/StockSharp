namespace StockSharp.Algo.Strategies.Reporting;

using System.Xml;

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
	public override async ValueTask Generate(Strategy strategy, Stream stream, CancellationToken cancellationToken)
	{
		var settings = new XmlWriterSettings
		{
			Indent = true,
			CloseOutput = false,
			Async = true,
		};

		using var writer = XmlWriter.Create(new StreamWriter(stream, Encoding, leaveOpen: true), settings);

		Task WriteStartElement(string name) => writer.WriteStartElementAsync(null, name, null);
		Task WriteEndElement() => writer.WriteEndElementAsync();
		Task WriteAttributeString(string name, object value) => writer.WriteAttributeStringAsync(null, name, null, value is TimeSpan ts ? ts.Format() : (value is DateTime dto ? dto.Format() : value.To<string>()));

		await WriteStartElement("strategy");

		await WriteAttributeString("name", strategy.Name);
		await WriteAttributeString("totalWorkingTime", strategy.TotalWorkingTime);
		await WriteAttributeString("commission", strategy.Commission);
		await WriteAttributeString("position", strategy.Position);
		await WriteAttributeString("PnL", strategy.PnL);
		await WriteAttributeString("slippage", strategy.Slippage);
		await WriteAttributeString("latency", strategy.Latency);

		await WriteStartElement("parameters");

		foreach (var p in strategy.GetParameters())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (p.Value is WorkingTime)
				continue;

			await WriteStartElement("parameter");

			await WriteAttributeString("name", p.GetName());
			await WriteAttributeString("value", p.Value);

			await WriteEndElement();
		}

		await WriteEndElement();

		await WriteStartElement("statistics");

		foreach (var p in strategy.StatisticManager.Parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			await WriteStartElement("parameter");

			await WriteAttributeString("name", p.Name);
			await WriteAttributeString("value", p.Value);

			await WriteEndElement();
		}

		await WriteEndElement();

		if (IncludeOrders)
		{
			await WriteStartElement("orders");

			foreach (var o in strategy.Orders)
			{
				cancellationToken.ThrowIfCancellationRequested();

				await WriteStartElement("order");

				await WriteAttributeString("id", o.Id);
				await WriteAttributeString("transactionId", o.TransactionId);
				await WriteAttributeString("direction", o.Side);
				await WriteAttributeString("time", o.Time);
				await WriteAttributeString("price", o.Price);
				await WriteAttributeString("state", o.State);
				await WriteAttributeString("balance", o.Balance);
				await WriteAttributeString("volume", o.Volume);
				await WriteAttributeString("type", o.Type);
				await WriteAttributeString("comment", o.Comment);

				await WriteEndElement();
			}

			await WriteEndElement();
		}

		if (IncludeTrades)
		{
			await WriteStartElement("trades");

			foreach (var t in strategy.MyTrades)
			{
				cancellationToken.ThrowIfCancellationRequested();

				await WriteStartElement("trade");

				await WriteAttributeString("id", t.Trade.Id);
				await WriteAttributeString("transactionId", t.Order.TransactionId);
				await WriteAttributeString("time", t.Trade.ServerTime);
				await WriteAttributeString("price", t.Trade.Price);
				await WriteAttributeString("volume", t.Trade.Volume);
				await WriteAttributeString("order", t.Order.Id);
				await WriteAttributeString("PnL", t.PnL);
				await WriteAttributeString("slippage", t.Slippage);

				await WriteEndElement();
			}

			await WriteEndElement();
		}

		await WriteEndElement();
	}
}