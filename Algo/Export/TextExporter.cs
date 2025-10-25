namespace StockSharp.Algo.Export;

using SmartFormat;
using SmartFormat.Core.Formatting;

/// <summary>
/// The export into text file.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TextExporter"/>.
/// </remarks>
/// <param name="dataType">Data type info.</param>
/// <param name="isCancelled">The processor, returning process interruption sign.</param>
/// <param name="stream">The stream to write to.</param>
/// <param name="template">The string formatting template.</param>
/// <param name="header">Header at the first line. Do not add header while empty string.</param>
public class TextExporter(DataType dataType, Func<int, bool> isCancelled, Stream stream, string template, string header) : BaseExporter(dataType, isCancelled)
{
	private readonly string _template = template.ThrowIfEmpty(nameof(template));
	private readonly string _header = header;

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportOrderLog(IEnumerable<ExecutionMessage> messages)
		=> Do(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTicks(IEnumerable<ExecutionMessage> messages)
		=> Do(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTransactions(IEnumerable<ExecutionMessage> messages)
		=> Do(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<QuoteChangeMessage> messages)
		=> Do(messages.ToTimeQuotes());

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<Level1ChangeMessage> messages)
		=> Do(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<CandleMessage> messages)
		=> Do(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<NewsMessage> messages)
		=> Do(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<SecurityMessage> messages)
		=> Do(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<PositionChangeMessage> messages)
		=> Do(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<IndicatorValue> values)
		=> Do(values);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardStateMessage> messages)
		=> Do(messages);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardMessage> messages)
		=> Do(messages);

	private (int, DateTimeOffset?) Do<TValue>(IEnumerable<TValue> values)
	{
		var count = 0;
		var lastTime = default(DateTimeOffset?);

		using (var writer = new StreamWriter(stream, Encoding, leaveOpen: true))
		{
			if (!_header.IsEmpty())
				writer.WriteLine(_header);

			FormatCache templateCache = null;
			var formater = Smart.Default;

			foreach (var value in values)
			{
				if (!CanProcess())
					break;

				writer.WriteLine(formater.FormatWithCache(ref templateCache, _template, value));
				
				count++;

				if (value is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;
			}

			//writer.Flush();
		}

		return (count, lastTime);
	}
}