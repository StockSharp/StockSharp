namespace StockSharp.Reporting;

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
	protected override async ValueTask OnGenerate(IReportSource source, Stream stream, CancellationToken cancellationToken)
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

		await WriteAttributeString("name", source.Name);
		await WriteAttributeString("totalWorkingTime", source.TotalWorkingTime);
		await WriteAttributeString("commission", source.Commission);
		await WriteAttributeString("position", source.Position);
		await WriteAttributeString("PnL", source.PnL);
		await WriteAttributeString("slippage", source.Slippage);
		await WriteAttributeString("latency", source.Latency);

		await WriteStartElement("parameters");

		foreach (var (name, value) in source.Parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (value is WorkingTime)
				continue;

			await WriteStartElement("parameter");

			await WriteAttributeString("name", name);
			await WriteAttributeString("value", value);

			await WriteEndElement();
		}

		await WriteEndElement();

		await WriteStartElement("statistics");

		foreach (var (name, value) in source.StatisticParameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			await WriteStartElement("parameter");

			await WriteAttributeString("name", name);
			await WriteAttributeString("value", value);

			await WriteEndElement();
		}

		await WriteEndElement();

		if (IncludeOrders)
		{
			await WriteStartElement("orders");

			foreach (var o in source.Orders)
			{
				cancellationToken.ThrowIfCancellationRequested();

				await WriteStartElement("order");

				await WriteAttributeString("id", o.Id);
				await WriteAttributeString("transactionId", o.TransactionId);
				await WriteAttributeString("securityId", o.SecurityId.ToStringId());
				await WriteAttributeString("direction", o.Side);
				await WriteAttributeString("time", o.Time);
				await WriteAttributeString("price", o.Price);
				await WriteAttributeString("state", o.State);
				await WriteAttributeString("balance", o.Balance);
				await WriteAttributeString("volume", o.Volume);
				await WriteAttributeString("type", o.Type);

				await WriteEndElement();
			}

			await WriteEndElement();
		}

		if (IncludeTrades)
		{
			await WriteStartElement("trades");

			foreach (var t in source.OwnTrades)
			{
				cancellationToken.ThrowIfCancellationRequested();

				await WriteStartElement("trade");

				await WriteAttributeString("id", t.TradeId);
				await WriteAttributeString("transactionId", t.OrderTransactionId);
				await WriteAttributeString("securityId", t.SecurityId.ToStringId());
				await WriteAttributeString("time", t.Time);
				await WriteAttributeString("price", t.TradePrice);
				await WriteAttributeString("volume", t.Volume);
				await WriteAttributeString("order", t.OrderId);
				await WriteAttributeString("PnL", t.PnL);
				await WriteAttributeString("slippage", t.Slippage);

				await WriteEndElement();
			}

			await WriteEndElement();
		}

		await WriteEndElement();
	}
}
