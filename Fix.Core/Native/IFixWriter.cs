namespace StockSharp.Fix.Native;

/// <summary>
/// The interface describing the recorder of data in the FIX protocol format.
/// </summary>
public interface IFixWriter : IFixBase
{
	/// <summary>
	/// Last written tag.
	/// </summary>
	FixTags LastTag { get; }

	/// <summary>
	/// Get data log.
	/// </summary>
	/// <returns>Data log.</returns>
	string FlushDump();

	/// <summary>
	/// Flushes the stream asynchronously.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	Task FlushAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Write tag.
	/// </summary>
	/// <param name="tag">Tag.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask WriteAsync(FixTags tag, CancellationToken cancellationToken);

	/// <summary>
	/// To record the <see cref="bool"/> value.
	/// </summary>
	/// <param name="value"><see cref="bool"/> value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask WriteAsync(bool value, CancellationToken cancellationToken);

	/// <summary>
	/// To record the <see cref="int"/> value.
	/// </summary>
	/// <param name="value"><see cref="int"/> value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask WriteAsync(int value, CancellationToken cancellationToken);

	/// <summary>
	/// To record the <see cref="long"/> value.
	/// </summary>
	/// <param name="value"><see cref="long"/> value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask WriteAsync(long value, CancellationToken cancellationToken);

	/// <summary>
	/// To record the <see cref="decimal"/> value.
	/// </summary>
	/// <param name="value"><see cref="decimal"/> value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask WriteAsync(decimal value, CancellationToken cancellationToken);

	/// <summary>
	/// To record the <see cref="DateTime"/> value.
	/// </summary>
	/// <param name="value"><see cref="DateTime"/> value.</param>
	/// <param name="parser">Time parser. Required if data will be transferred as string.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask WriteAsync(DateTime value, FastDateTimeParser parser, CancellationToken cancellationToken);

	/// <summary>
	/// To record the <see cref="TimeSpan"/> value.
	/// </summary>
	/// <param name="value"><see cref="TimeSpan"/> value.</param>
	/// <param name="parser">Time parser. Required if data will be transferred as string.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask WriteAsync(TimeSpan value, FastTimeSpanParser parser, CancellationToken cancellationToken);

	/// <summary>
	/// To record the <see cref="string"/> value.
	/// </summary>
	/// <param name="value"><see cref="string"/> value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask WriteAsync(string value, CancellationToken cancellationToken);

	/// <summary>
	/// To record the <see cref="char"/> value.
	/// </summary>
	/// <param name="value"><see cref="char"/> value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask WriteAsync(char value, CancellationToken cancellationToken);

	/// <summary>
	/// To record an array of bytes.
	/// </summary>
	/// <param name="buffer">Bytes buffer.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask WriteBytesAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

	/// <summary>
	/// Clear state.
	/// </summary>
	void ClearState();
}