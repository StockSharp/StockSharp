namespace StockSharp.Fix.Native;

/// <summary>
/// The base class of the recorder.
/// </summary>
/// <remarks>
/// Initialize <see cref="BaseFixWriter"/>.
/// </remarks>
/// <param name="stream">The stream.</param>
/// <param name="encoding">Text encoding.</param>
/// <param name="ownsStream">Whether to dispose the stream when this instance is disposed.</param>
public abstract class BaseFixWriter(Stream stream, Encoding encoding, bool ownsStream = false) : FixBase(stream, encoding, ownsStream)
{
	private readonly byte[] _buffer = new byte[1];

	/// <summary>
	/// To record the <see cref="Byte"/> value.
	/// </summary>
	/// <param name="value"><see cref="Byte"/> value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected async ValueTask WriteByteAsync(byte value, CancellationToken cancellationToken)
	{
		_buffer[0] = value;
		await Stream.WriteAsync(_buffer, 0, 1, cancellationToken).NoWait();

		CalcCheckSum(value);
	}

	/// <summary>
	/// To record an array of bytes asynchronously.
	/// </summary>
	/// <param name="buffer">Bytes buffer.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async ValueTask WriteBytesAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		await Stream.WriteAsync(buffer, cancellationToken).NoWait();

		CalcCheckSum(buffer.Span);
		Dump(buffer.Span);
	}

	/// <summary>
	/// Flushes the stream asynchronously.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public Task FlushAsync(CancellationToken cancellationToken)
		=> Stream.FlushAsync(cancellationToken);
}