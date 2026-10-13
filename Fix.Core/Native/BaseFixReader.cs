namespace StockSharp.Fix.Native;

/// <summary>
/// Data reader base class.
/// </summary>
/// <remarks>
/// Initialize <see cref="BaseFixReader"/>.
/// </remarks>
/// <param name="stream">The stream from which data will be read.</param>
/// <param name="encoding">Text encoding.</param>
/// <param name="ownsStream">Whether to dispose the stream when this instance is disposed.</param>
public abstract class BaseFixReader(Stream stream, Encoding encoding, bool ownsStream = false) : FixBase(stream, encoding, ownsStream)
{
	private readonly byte[] _buffer = new byte[1];

	/// <summary>
	/// Get byte.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Byte.</returns>
	public async ValueTask<byte> ReadByteAsync(CancellationToken cancellationToken)
	{
		await Stream.ReadExactlyAsync(_buffer, 0, 1, cancellationToken).NoWait();

		var b = _buffer[0];

		CalcCheckSum(b);

		return b;
	}

	/// <summary>
	/// Read <see cref="byte"/> array value.
	/// </summary>
	/// <param name="buffer">Buffer.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async ValueTask ReadBytesAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		await Stream.ReadExactlyAsync(buffer, cancellationToken).NoWait();

		CalcCheckSum(buffer.Span);
		Dump(buffer.Span);
	}
}