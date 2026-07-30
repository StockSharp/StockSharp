namespace StockSharp.Fix.Native;

/// <summary>
/// The interface describing the reader of data recorded in the FIX protocol format.
/// </summary>
public interface IFixReader : IFixBase
{
	/// <summary>
	/// Whether the tag value was read.
	/// </summary>
	bool IsValueRead { get; }

	/// <summary>
	/// Last read tag.
	/// </summary>
	FixTags LastTag { get; }

	/// <summary>
	/// To read the following tag.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The next tag. The -1 indicates the end of data.</returns>
	ValueTask<FixTags> ReadTagAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Get data log.
	/// </summary>
	/// <returns>Data log.</returns>
	string FlushDump();

	/// <summary>
	/// Read <see cref="DateTime"/> value asynchronously.
	/// </summary>
	/// <param name="parser">Time parser. Required if data will be transferred as string.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="DateTime"/> value.</returns>
	ValueTask<DateTime> ReadDateTimeAsync(FastDateTimeParser parser, CancellationToken cancellationToken);

	/// <summary>
	/// Read <see cref="TimeSpan"/> value.
	/// </summary>
	/// <param name="parser">Time parser. Required if data will be transferred as string.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="TimeSpan"/> value.</returns>
	ValueTask<TimeSpan> ReadTimeSpanAsync(FastTimeSpanParser parser, CancellationToken cancellationToken);

	/// <summary>
	/// Read <see cref="int"/> value.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="int"/> value.</returns>
	ValueTask<int> ReadIntAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Read <see cref="long"/> value.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="long"/> value.</returns>
	ValueTask<long> ReadLongAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Read <see cref="decimal"/> value.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="decimal"/> value.</returns>
	ValueTask<decimal> ReadDecimalAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Read <see cref="char"/> value.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="char"/> value.</returns>
	ValueTask<char> ReadCharAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Read <see cref="string"/> value.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="string"/> value.</returns>
	ValueTask<string> ReadStringAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Read <see cref="byte"/> array value.
	/// </summary>
	/// <param name="buffer">Buffer.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask ReadBytesAsync(Memory<byte> buffer, CancellationToken cancellationToken);

	/// <summary>
	/// Read <see cref="bool"/> value.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see cref="bool"/> value.</returns>
	ValueTask<bool> ReadBoolAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Skip value.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask SkipValueAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Clear state.
	/// </summary>
	void ClearState();
}