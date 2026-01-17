namespace StockSharp.Reporting;

using Newtonsoft.Json;

/// <summary>
/// The report generator for the strategy in the json format.
/// </summary>
public class JsonReportGenerator : BaseReportGenerator
{
	/// <inheritdoc />
	public override string Name => "JSON";

	/// <inheritdoc />
	public override string Extension => "json";

	/// <inheritdoc />
	protected override async ValueTask OnGenerate(IReportSource source, Stream stream, CancellationToken cancellationToken)
	{
		using var textWriter = new StreamWriter(stream, Encoding, leaveOpen: true);
		using var writer = new JsonTextWriter(textWriter) { Formatting = Formatting.Indented };

		Task WriteStartElement() => writer.WriteStartObjectAsync(cancellationToken);
		Task WriteEndElement() => writer.WriteEndObjectAsync(cancellationToken);
		Task WriteStartArray() => writer.WriteStartArrayAsync(cancellationToken);
		Task WriteEndArray() => writer.WriteEndArrayAsync(cancellationToken);
		Task WritePropertyName(string name) => writer.WritePropertyNameAsync(name, cancellationToken);

		async Task WriteElementAsync(string name, object value)
		{
			await WritePropertyName(name);

			if (value is null)
				await writer.WriteNullAsync(cancellationToken);
			else
				await writer.WriteValueAsync(value, cancellationToken);
		}

		await WriteStartElement();

		await WriteElementAsync("name", source.Name);

		foreach (var (name, value) in source.Parameters)
		{
			if (value is WorkingTime)
			{
				cancellationToken.ThrowIfCancellationRequested();
				continue;
			}

			await WriteElementAsync(name, value);
		}

		await WriteElementAsync("totalWorkingTime", source.TotalWorkingTime);
		await WriteElementAsync("commission", source.Commission);
		await WriteElementAsync("position", source.Position);
		await WriteElementAsync("PnL", source.PnL);
		await WriteElementAsync("slippage", source.Slippage);
		await WriteElementAsync("latency", source.Latency);

		await WritePropertyName("statistics");
		await WriteStartElement();

		foreach (var (name, value) in source.StatisticParameters)
		{
			await WriteElementAsync(name, value);
		}

		await WriteEndElement();

		if (IncludeOrders)
		{
			await WritePropertyName("orders");
			await WriteStartArray();

			foreach (var o in source.Orders)
			{
				await WriteStartElement();

				await WriteElementAsync("id", o.Id);
				await WriteElementAsync("transactionId", o.TransactionId);
				await WriteElementAsync("securityId", o.SecurityId.ToStringId());
				await WriteElementAsync("direction", o.Side);
				await WriteElementAsync("time", o.Time);
				await WriteElementAsync("price", o.Price);
				await WriteElementAsync("state", o.State);
				await WriteElementAsync("balance", o.Balance);
				await WriteElementAsync("volume", o.Volume);
				await WriteElementAsync("type", o.Type);

				await WriteEndElement();
			}

			await WriteEndArray();
		}

		if (IncludeTrades)
		{
			await WritePropertyName("trades");
			await WriteStartArray();

			foreach (var t in source.OwnTrades)
			{
				await WriteStartElement();

				await WriteElementAsync("id", t.TradeId);
				await WriteElementAsync("transactionId", t.OrderTransactionId);
				await WriteElementAsync("securityId", t.SecurityId.ToStringId());
				await WriteElementAsync("time", t.Time);
				await WriteElementAsync("price", t.TradePrice);
				await WriteElementAsync("volume", t.Volume);
				await WriteElementAsync("order", t.OrderId);
				await WriteElementAsync("PnL", t.PnL);
				await WriteElementAsync("slippage", t.Slippage);

				await WriteEndElement();
			}

			await WriteEndArray();
		}

		await WriteEndElement();
	}
}
