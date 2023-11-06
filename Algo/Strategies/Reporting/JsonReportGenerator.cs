namespace StockSharp.Algo.Strategies.Reporting;

using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using Newtonsoft.Json;

using StockSharp.Messages;

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
			writer.WriteValue(value?.To<string>());
		}

		WriteStartElement();

		WriteElementString("name", strategy.Name);
		WriteElementString("security", strategy.Security?.Id);
		WriteElementString("portfolio", strategy.Portfolio?.Name);

		foreach (var p in strategy.Parameters.CachedValues)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (p.Value is WorkingTime)
				continue;

			WriteElementString(p.Name, p.Value);
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
				WriteElementString("PnL", strategy.PnLManager.ProcessMessage(t.ToMessage())?.PnL);
				WriteElementString("slippage", t.Slippage);

				WriteEndElement();
			}

			WriteEndArray();
		}

		WriteEndElement();

		return default;
	}
}
