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
	protected override Task<(int, DateTime?)> ExportOrderLogAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTicksAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTransactionsAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<QuoteChangeMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages.SelectMany(m => m.ToTimeQuotes()), cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<Level1ChangeMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<CandleMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<NewsMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<SecurityMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<PositionChangeMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<IndicatorValue> values, CancellationToken cancellationToken)
		=> DoAsync(values, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<BoardStateMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<BoardMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, cancellationToken);

	private async Task<(int, DateTime?)> DoAsync<TValue>(IAsyncEnumerable<TValue> values, CancellationToken cancellationToken)
	{
		var count = 0;
		var lastTime = default(DateTime?);

		using (var writer = new StreamWriter(stream, Encoding, leaveOpen: true))
		{
			if (!_header.IsEmpty())
				await writer.WriteLineAsync(_header.AsMemory(), cancellationToken);

			FormatCache templateCache = null;
			var formater = Smart.Default;

			await foreach (var value in values.WithCancellation(cancellationToken))
			{
				await writer.WriteLineAsync(formater.FormatWithCache(ref templateCache, _template, value).AsMemory(), cancellationToken);

				count++;

				if (value is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;
			}
		}

		return (count, lastTime);
	}
}