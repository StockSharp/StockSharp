namespace StockSharp.Algo.Export;

using SmartFormat;
using SmartFormat.Core.Formatting;

/// <summary>
/// The export into text file.
/// </summary>
public class TextExporter : BaseExporter
{
	private readonly string _template;
	private readonly string _header;

	/// <summary>
	/// Initializes a new instance of the <see cref="TextExporter"/>.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <param name="isCancelled">The processor, returning process interruption sign.</param>
	/// <param name="fileName">The path to file.</param>
	/// <param name="template">The string formatting template.</param>
	/// <param name="header">Header at the first line. Do not add header while empty string.</param>
	public TextExporter(DataType dataType, Func<int, bool> isCancelled, string fileName, string template, string header)
		: base(dataType, isCancelled, fileName)
	{
		if (template.IsEmpty())
			throw new ArgumentNullException(nameof(template));

		_template = template;
		_header = header;
	}

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

		using (var writer = new StreamWriter(Path, true))
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