namespace StockSharp.Fix.Native;

/// <summary>
/// The reader of data recorded in the text FIX protocol format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TextFixReader"/>.
/// </remarks>
/// <param name="stream">The stream from which data will be read.</param>
/// <param name="encoding">Text encoding.</param>
/// <param name="ownsStream">Whether to dispose the stream when this instance is disposed.</param>
public class TextFixReader(Stream stream, Encoding encoding, bool ownsStream = false) : BaseFixReader(stream, encoding, ownsStream), IFixReader
{
	/// <inheritdoc />
	public long? MsgSeqNum { get; private set; }

	private static bool IsEnd(int letter)
		=> letter == (int)AsciiSymbols.Soh || letter == -1;

	private async ValueTask<int> ReadWithDumpAsync(CancellationToken cancellationToken)
	{
		var value = await ReadByteAsync(cancellationToken).NoWait();
		Dump(value);
		return value;
	}

	async ValueTask<FixTags> IFixReader.ReadTagAsync(CancellationToken cancellationToken)
	{
		IsValueRead = false;

		var nextByte = await ReadWithDumpAsync(cancellationToken).NoWait();
		var (tag, _) = await ReadIntWithSignAsync(nextByte, cancellationToken).NoWait();

		if (tag == null)
		{
			LastTag = Extensions.EmptyTag;
			return LastTag;
		}

		LastTag = (FixTags)tag.Value;

		// A new message starts at its version tag, and its sequence number is not known yet.
		if (LastTag == FixTags.BeginString)
			MsgSeqNum = null;

		return LastTag;
	}

	async ValueTask<DateTime> IFixReader.ReadDateTimeAsync(FastDateTimeParser parser, CancellationToken cancellationToken)
	{
		var str = await ((IFixReader)this).ReadStringAsync(cancellationToken).NoWait();

		try
		{
			return parser.Parse(str);
		}
		catch
		{
			return default;
		}
	}

	async ValueTask<TimeSpan> IFixReader.ReadTimeSpanAsync(FastTimeSpanParser parser, CancellationToken cancellationToken)
	{
		var s = await ((IFixReader)this).ReadStringAsync(cancellationToken).NoWait();

		if (s.EqualsIgnoreCase("N/A"))
			return TimeSpan.Zero;

		return parser.Parse(s);
	}

	async ValueTask<int> IFixReader.ReadIntAsync(CancellationToken cancellationToken)
	{
		var next = await ReadWithDumpAsync(cancellationToken).NoWait();

		var (result, finalByte) = await ReadIntWithSignAsync(next, cancellationToken).NoWait();

		if (result == null || !IsEnd(finalByte))
			throw new InvalidOperationException();

		IsValueRead = true;

		return result.Value;
	}

	async ValueTask<long> IFixReader.ReadLongAsync(CancellationToken cancellationToken)
	{
		var next = await ReadWithDumpAsync(cancellationToken).NoWait();
		var (result, finalByte) = await ReadInt64WithSignAsync(next, cancellationToken).NoWait();

		if (result == null || !IsEnd(finalByte))
			throw new InvalidOperationException();

		IsValueRead = true;

		if (LastTag == FixTags.MsgSeqNum)
			MsgSeqNum = result.Value;

		return result.Value;
	}

	async ValueTask<decimal> IFixReader.ReadDecimalAsync(CancellationToken cancellationToken)
	{
		var next = await ReadWithDumpAsync(cancellationToken).NoWait();
		var (result, finalByte) = await ReadDecimalInternalAsync(next, cancellationToken).NoWait();

		if (result == null || !IsEnd(finalByte))
			throw new InvalidOperationException();

		IsValueRead = true;

		return result.Value;
	}

	async ValueTask<char> IFixReader.ReadCharAsync(CancellationToken cancellationToken)
	{
		var letter = await ReadWithDumpAsync(cancellationToken).NoWait();

		if (IsEnd(letter))
			throw new InvalidOperationException();

		var end = await ReadWithDumpAsync(cancellationToken).NoWait();

		if (!IsEnd(end))
			throw new InvalidOperationException();

		IsValueRead = true;

		return (char)(byte)letter;
	}

	async ValueTask<string> IFixReader.ReadStringAsync(CancellationToken cancellationToken)
	{
		try
		{
			var chars = new List<byte>();

			while (true)
			{
				var letter = await ReadWithDumpAsync(cancellationToken).NoWait();

				if (IsEnd(letter))
					break;

				chars.Add((byte)letter);
			}

			if (!chars.Any())
				return null;

			return Encoding.GetString([.. chars]);
		}
		finally
		{
			IsValueRead = true;
		}
	}

	async ValueTask<bool> IFixReader.ReadBoolAsync(CancellationToken cancellationToken)
	{
		return await ((IFixReader)this).ReadCharAsync(cancellationToken).NoWait() switch
		{
			'Y' => true,
			'N' => false,
			_ => throw new InvalidOperationException(),
		};
	}

	async ValueTask IFixReader.SkipValueAsync(CancellationToken cancellationToken)
	{
		while (await ReadWithDumpAsync(cancellationToken).NoWait() != (int)AsciiSymbols.Soh)
		{
		}

		IsValueRead = true;
	}

	private async ValueTask<(bool? minus, int nextByte)> GetMinusOrPlusAsync(int nextByte, CancellationToken cancellationToken)
	{
		bool? minus = null;

		if (nextByte == (int)AsciiSymbols.Minus || nextByte == (int)AsciiSymbols.Plus)
		{
			minus = nextByte == (int)AsciiSymbols.Minus;
			nextByte = await ReadWithDumpAsync(cancellationToken).NoWait();
		}

		return (minus, nextByte);
	}

	private async ValueTask<(int? result, int nextByte)> ReadIntWithSignAsync(int nextByte, CancellationToken cancellationToken)
	{
		if (nextByte == -1)
			return (null, nextByte);

		var (minus, byteAfterSign) = await GetMinusOrPlusAsync(nextByte, cancellationToken).NoWait();
		var (result, finalByte) = await ReadIntInternalAsync(byteAfterSign, cancellationToken).NoWait();

		if (minus != null)
		{
			if (result == null) //есть знак но нет числа.
				throw new InvalidOperationException();

			if (minus.Value)
				result = -result.Value;
		}

		return (result, finalByte);
	}

	private async ValueTask<(long? result, int nextByte)> ReadInt64WithSignAsync(int nextByte, CancellationToken cancellationToken)
	{
		if (nextByte == -1)
			return (null, nextByte);

		var (minus, byteAfterSign) = await GetMinusOrPlusAsync(nextByte, cancellationToken).NoWait();
		var (result, finalByte) = await ReadInt64InternalAsync(byteAfterSign, cancellationToken).NoWait();

		if (minus != null)
		{
			if (result == null) //есть знак но нет числа.
				throw new InvalidOperationException();

			if (minus.Value)
				result = -result.Value;
		}

		return (result, finalByte);
	}

	private async ValueTask<(decimal? result, int nextByte)> ReadDecimalInternalAsync(int nextByte, CancellationToken cancellationToken)
	{
		if (nextByte == -1)
			return (null, nextByte);

		var (minus, byteAfterSign) = await GetMinusOrPlusAsync(nextByte, cancellationToken).NoWait();

		var (intPart, countInt, byteAfterInt) = await ReadInt128InternalAsync(byteAfterSign, cancellationToken).NoWait();

		decimal? result = intPart;
		int currentByte = byteAfterInt;

		if (currentByte == (int)AsciiSymbols.Point)
		{
			checked
			{
				currentByte = await ReadWithDumpAsync(cancellationToken).NoWait();

				var (resultFrac, count, byteAfterFrac) = await ReadInt128InternalAsync(currentByte, cancellationToken).NoWait();

				if (resultFrac == null)
					throw new InvalidOperationException("Fractional value is empty.");

				result ??= 0;

				result += resultFrac.Value / 10M.Pow(count);
				currentByte = byteAfterFrac;
			}
		}

		if (minus != null)
		{
			if (result == null)
				throw new InvalidOperationException(); //есть знак но нет числа.

			if (minus.Value)
				result = -result.Value;
		}

		return (result, currentByte);
	}

	private async ValueTask<(int? result, int nextByte)> ReadIntInternalAsync(int nextByte, CancellationToken cancellationToken)
	{
		var result = nextByte - (int)AsciiSymbols.Zero;

		if (result < 0 || 9 < result)
			return (null, nextByte);

		while (true)
		{
			var letter = await ReadWithDumpAsync(cancellationToken).NoWait();
			var val = letter - (int)AsciiSymbols.Zero;

			if (val < 0 || 9 < val)
			{
				return (result, letter);
			}

			checked
			{
				result = result * 10 + val;
			}
		}
	}

	private async ValueTask<(long? result, int nextByte)> ReadInt64InternalAsync(int nextByte, CancellationToken cancellationToken)
	{
		long result = nextByte - (int)AsciiSymbols.Zero;
		if (result < 0 || 9 < result)
			return (null, nextByte);

		while (true)
		{
			var letter = await ReadWithDumpAsync(cancellationToken).NoWait();
			var val = letter - (int)AsciiSymbols.Zero;

			if (val < 0 || 9 < val)
			{
				return (result, letter);
			}

			checked
			{
				result = result * 10 + val;
			}
		}
	}

	private async ValueTask<(decimal? result, int count, int nextByte)> ReadInt128InternalAsync(int nextByte, CancellationToken cancellationToken)
	{
		int count = 0;

		decimal result = nextByte - (int)AsciiSymbols.Zero;

		if (result < 0 || 9 < result)
			return (null, 0, nextByte);

		count++;

		while (true)
		{
			var letter = await ReadWithDumpAsync(cancellationToken).NoWait();
			var val = letter - (int)AsciiSymbols.Zero;

			if (val < 0 || 9 < val)
			{
				return (result, count, letter);
			}

			checked
			{
				result = result * 10 + val;
				count++;
			}
		}
	}
}