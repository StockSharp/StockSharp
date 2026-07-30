namespace StockSharp.Fix.Native;

/// <summary>
/// The data recorder which records in the text FIX protocol format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TextFixWriter"/>.
/// </remarks>
/// <param name="stream">Writing stream.</param>
/// <param name="encoding">Text encoding.</param>
/// <param name="ownsStream">Whether to dispose the stream when this instance is disposed.</param>
public class TextFixWriter(Stream stream, Encoding encoding, bool ownsStream = false) : BaseFixWriter(stream, encoding, ownsStream), IFixWriter
{
	private async ValueTask WriteWithDumpAsync(byte value, CancellationToken cancellationToken)
	{
		await WriteByteAsync(value, cancellationToken);
		Dump(value);
	}

	private async ValueTask WriteNumberAsync(long value, CancellationToken cancellationToken)
	{
		if (value < 0)
		{
			value = value.Abs();
			await WriteWithDumpAsync((byte)AsciiSymbols.Minus, cancellationToken);
		}

		if (value < 10)
		{
			await WriteWithDumpAsync((byte)((int)value + AsciiSymbols.Zero), cancellationToken);
			return;
		}
		else if (value == 10)
		{
			await WriteWithDumpAsync((byte)(1 + AsciiSymbols.Zero), cancellationToken);
			await WriteWithDumpAsync((byte)(0 + AsciiSymbols.Zero), cancellationToken);
			return;
		}

		var m = 1L;
		var num = value;
		while (num >= 10)
		{
			num /= 10;
			m *= 10;
		}

		while (m != 0)
		{
			var digit = (int)(value / m);

			if (digit < 0 || digit > 9)
				throw new InvalidOperationException();

			await WriteWithDumpAsync((byte)(digit + AsciiSymbols.Zero), cancellationToken);

			value -= digit * m;
			m /= 10;
		}
	}

	async ValueTask IFixWriter.WriteAsync(FixTags tag, CancellationToken cancellationToken)
	{
		await WriteNumberAsync((long)tag, cancellationToken);
		await WriteWithDumpAsync((byte)AsciiSymbols.Eq, cancellationToken);

		LastTag = tag;
	}

	async ValueTask IFixWriter.WriteAsync(bool value, CancellationToken cancellationToken)
	{
		await WriteWithDumpAsync((byte)(value ? 'Y' : 'N'), cancellationToken);
		await WriteSohAsync(cancellationToken);
	}

	async ValueTask IFixWriter.WriteAsync(int value, CancellationToken cancellationToken)
	{
		await WriteNumberAsync(value, cancellationToken);
		await WriteSohAsync(cancellationToken);
	}

	async ValueTask IFixWriter.WriteAsync(long value, CancellationToken cancellationToken)
	{
		await WriteNumberAsync(value, cancellationToken);
		await WriteSohAsync(cancellationToken);
	}

	async ValueTask IFixWriter.WriteAsync(decimal value, CancellationToken cancellationToken)
	{
		if (value == 0)
		{
			await WriteWithDumpAsync((byte)AsciiSymbols.Zero, cancellationToken);
			await WriteSohAsync(cancellationToken);
			return;
		}

		if (value < 0)
		{
			value = value.Abs();
			await WriteWithDumpAsync((byte)AsciiSymbols.Minus, cancellationToken);
		}

		var pow = (int)Math.Floor(Math.Log10((double)value));
		var m = (decimal)Math.Pow(10, pow);

		var pointAdded = false;

		if (pow < 0)
		{
			await WriteWithDumpAsync((byte)AsciiSymbols.Zero, cancellationToken);
			await WriteWithDumpAsync((byte)AsciiSymbols.Point, cancellationToken);

			pointAdded = true;
			pow++;

			while (pow++ < 0)
			{
				await WriteWithDumpAsync((byte)AsciiSymbols.Zero, cancellationToken);
			}
		}

		while (value != 0 || 1 <= m)
		{
			var digit = (int)(value / m);

			if (digit < 0 || 9 < digit)
				throw new InvalidOperationException();

			if (!pointAdded && m < 1)
			{
				await WriteWithDumpAsync((byte)AsciiSymbols.Point, cancellationToken);
				pointAdded = true;
			}

			value -= digit * m;
			await WriteWithDumpAsync((byte)(digit + AsciiSymbols.Zero), cancellationToken);
			m /= 10;
		}

		await WriteSohAsync(cancellationToken);
	}

	ValueTask IFixWriter.WriteAsync(DateTime value, FastDateTimeParser parser, CancellationToken cancellationToken)
		=> ((IFixWriter)this).WriteAsync(parser.ToString(value), cancellationToken);

	ValueTask IFixWriter.WriteAsync(TimeSpan value, FastTimeSpanParser parser, CancellationToken cancellationToken)
		=> ((IFixWriter)this).WriteAsync(parser.ToString(value), cancellationToken);

	async ValueTask IFixWriter.WriteAsync(string value, CancellationToken cancellationToken)
	{
		var bytes = Encoding.GetBytes(value);
		await WriteBytesAsync(bytes, cancellationToken);
		await WriteSohAsync(cancellationToken);
	}

	async ValueTask IFixWriter.WriteAsync(char value, CancellationToken cancellationToken)
	{
		await WriteWithDumpAsync((byte)value, cancellationToken);
		await WriteSohAsync(cancellationToken);
	}

	private ValueTask WriteSohAsync(CancellationToken cancellationToken)
		=> WriteWithDumpAsync((byte)AsciiSymbols.Soh, cancellationToken);
}