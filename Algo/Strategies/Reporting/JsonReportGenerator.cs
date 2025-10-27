namespace StockSharp.Algo.Strategies.Reporting;

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
	public override async ValueTask Generate(Strategy strategy, Stream stream, CancellationToken cancellationToken)
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

		await WriteElementAsync("name", strategy.Name);

		foreach (var p in strategy.GetParameters())
		{
			if (p.Value is WorkingTime)
				continue;

			var value = p.Value;

			if (value is Security sec)
				value = sec.Id;
			else if (value is Portfolio pf)
				value = pf.Name;

			await WriteElementAsync(p.GetName(), value);
		}

		await WriteElementAsync("totalWorkingTime", strategy.TotalWorkingTime);
		await WriteElementAsync("commission", strategy.Commission);
		await WriteElementAsync("position", strategy.Position);
		await WriteElementAsync("PnL", strategy.PnL);
		await WriteElementAsync("slippage", strategy.Slippage);
		await WriteElementAsync("latency", strategy.Latency);

		await WritePropertyName("statistics");
		await WriteStartElement();

		foreach (var p in strategy.StatisticManager.Parameters)
		{
			await WriteElementAsync(p.Name, p.Value);
		}

		await WriteEndElement();

		if (IncludeOrders)
		{
			await WritePropertyName("orders");
			await WriteStartArray();

			foreach (var o in strategy.Orders)
			{
				await WriteStartElement();

				await WriteElementAsync("id", o.Id);
				await WriteElementAsync("transactionId", o.TransactionId);
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

			foreach (var t in strategy.MyTrades)
			{
				await WriteStartElement();

				await WriteElementAsync("id", t.Trade.Id);
				await WriteElementAsync("transactionId", t.Order.TransactionId);
				await WriteElementAsync("time", t.Trade.ServerTime);
				await WriteElementAsync("price", t.Trade.Price);
				await WriteElementAsync("volume", t.Trade.Volume);
				await WriteElementAsync("order", t.Order.Id);
				await WriteElementAsync("PnL", t.PnL);
				await WriteElementAsync("slippage", t.Slippage);

				await WriteEndElement();
			}

			await WriteEndArray();
		}

		await WriteEndElement();
	}
}
