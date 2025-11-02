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
/// <param name="stream">The stream to write to.</param>
/// <param name="template">The string formatting template.</param>
/// <param name="header">Header at the first line. Do not add header while empty string.</param>
public class TextExporter(DataType dataType, Stream stream, string template, string header) : BaseExporter(dataType)
{
	private readonly string _template = template.ThrowIfEmpty(nameof(template));
	private readonly string _header = header;

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportOrderLog(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> Do(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTicks(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> Do(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTransactions(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> Do(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<QuoteChangeMessage> messages, CancellationToken cancellationToken)
		=> Do(messages.ToTimeQuotes(), cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<Level1ChangeMessage> messages, CancellationToken cancellationToken)
		=> Do(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<CandleMessage> messages, CancellationToken cancellationToken)
		=> Do(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<NewsMessage> messages, CancellationToken cancellationToken)
		=> Do(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<SecurityMessage> messages, CancellationToken cancellationToken)
		=> Do(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<PositionChangeMessage> messages, CancellationToken cancellationToken)
		=> Do(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<IndicatorValue> values, CancellationToken cancellationToken)
		=> Do(values, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<BoardStateMessage> messages, CancellationToken cancellationToken)
		=> Do(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<BoardMessage> messages, CancellationToken cancellationToken)
		=> Do(messages, cancellationToken);

	private async Task<(int, DateTime?)> Do<TValue>(IEnumerable<TValue> values, CancellationToken cancellationToken)
	{
		var count = 0;
		var lastTime = default(DateTime?);

		using (var writer = new StreamWriter(stream, Encoding, leaveOpen: true))
		{
			if (!_header.IsEmpty())
				await writer.WriteLineAsync(_header.AsMemory(), cancellationToken);

			FormatCache templateCache = null;
			var formater = Smart.Default;

			foreach (var value in values)
			{
				await writer.WriteLineAsync(formater.FormatWithCache(ref templateCache, _template, value).AsMemory(), cancellationToken);

				count++;

				if (value is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;
			}

			//await writer.FlushAsync();
		}

		return (count, lastTime);
	}
}