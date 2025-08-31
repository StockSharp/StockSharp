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
	public override ValueTask Generate(Strategy strategy, string fileName, CancellationToken cancellationToken)
	{
		using var writer = new JsonTextWriter(new StreamWriter(fileName, false, Encoding.UTF8)) { Formatting = Formatting.Indented };

		void WriteStartElement()
			=> writer.WriteStartObject();

		void WriteEndElement()
			=> writer.WriteEndObject();

		void WriteStartArray()
			=> writer.WriteStartArray();

		void WriteEndArray()
			=> writer.WriteEndArray();

		void WritePropertyName(string name)
			=> writer.WritePropertyName(name);

		void WriteElementString(string name, object value)
		{
			WritePropertyName(name);

			if (value is null)
			{
				writer.WriteNull();
				return;
			}

			switch (value)
			{
				case string s:
					writer.WriteValue(s);
					break;
				case bool b:
					writer.WriteValue(b);
					break;
				case sbyte sb:
					writer.WriteValue(sb);
					break;
				case byte by:
					writer.WriteValue(by);
					break;
				case short sh:
					writer.WriteValue(sh);
					break;
				case ushort ush:
					writer.WriteValue(ush);
					break;
				case int i:
					writer.WriteValue(i);
					break;
				case uint ui:
					writer.WriteValue(ui);
					break;
				case long l:
					writer.WriteValue(l);
					break;
				case ulong ul:
					writer.WriteValue(ul);
					break;
				case float f:
					writer.WriteValue(f);
					break;
				case double d:
					writer.WriteValue(d);
					break;
				case decimal m:
					writer.WriteValue(m);
					break;
				case DateTime dt:
					writer.WriteValue(dt);
					break;
				case DateTimeOffset dto:
					writer.WriteValue(dto);
					break;
				case TimeSpan ts:
					writer.WriteValue(ts);
					break;
				case Guid g:
					writer.WriteValue(g);
					break;
				default:
					writer.WriteValue(value.To<string>());
					break;
			}
		}

		WriteStartElement();

		WriteElementString("name", strategy.Name);

		foreach (var p in strategy.GetParameters())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (p.Value is WorkingTime)
				continue;

			WriteElementString(p.GetName(), p.Value);
		}

		WriteElementString("totalWorkingTime", strategy.TotalWorkingTime);
		WriteElementString("commission", strategy.Commission);
		WriteElementString("position", strategy.Position);
		WriteElementString("PnL", strategy.PnL);
		WriteElementString("slippage", strategy.Slippage);
		WriteElementString("latency", strategy.Latency);

		WritePropertyName("statistics");
		WriteStartElement();

		foreach (var p in strategy.StatisticManager.Parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			WriteElementString(p.Name, p.Value);
		}

		WriteEndElement();

		if (IncludeOrders)
		{
			WritePropertyName("orders");
			WriteStartArray();

			foreach (var o in strategy.Orders)
			{
				cancellationToken.ThrowIfCancellationRequested();

				WriteStartElement();

				WriteElementString("id", o.Id);
				WriteElementString("transactionId", o.TransactionId);
				WriteElementString("direction", o.Side);
				WriteElementString("time", o.Time);
				WriteElementString("price", o.Price);
				WriteElementString("state", o.State);
				WriteElementString("balance", o.Balance);
				WriteElementString("volume", o.Volume);
				WriteElementString("type", o.Type);

				WriteEndElement();
			}

			WriteEndArray();
		}

		if (IncludeTrades)
		{
			WritePropertyName("trades");
			WriteStartArray();

			foreach (var t in strategy.MyTrades)
			{
				cancellationToken.ThrowIfCancellationRequested();

				WriteStartElement();

				WriteElementString("id", t.Trade.Id);
				WriteElementString("transactionId", t.Order.TransactionId);
				WriteElementString("time", t.Trade.ServerTime);
				WriteElementString("price", t.Trade.Price);
				WriteElementString("volume", t.Trade.Volume);
				WriteElementString("order", t.Order.Id);
				WriteElementString("PnL", t.PnL);
				WriteElementString("slippage", t.Slippage);

				WriteEndElement();
			}

			WriteEndArray();
		}

		WriteEndElement();

		return default;
	}
}
