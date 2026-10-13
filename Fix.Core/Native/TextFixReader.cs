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

	private async ValueTask<int> ReadWithDumpAsync(CancellationToken cancellationToken)
	{
		try
		{
			var value = await ReadByteAsync(cancellationToken).NoWait();
			Dump(value);
			return value;
		}
		catch (EndOfStreamException)
		{
			return -1;
		}
	}

	async ValueTask<FixTags> IFixReader.ReadTagAsync(CancellationToken cancellationToken)
	{
		IsValueRead = false;

		var nextByte = await ReadWithDumpAsync(cancellationToken).NoWait();

		if (nextByte == -1)
		{
			LastTag = Extensions.EmptyTag;
			return LastTag;
		}

		var (tag, separator) = await ReadIntWithSignAsync(nextByte, cancellationToken).NoWait();

		if (tag is null || tag <= 0 || separator != (int)AsciiSymbols.Eq)
			throw new InvalidOperationException("Invalid FIX tag.");

		LastTag = (FixTags)tag.Value;

		// A new message starts at its version tag, and its sequence number is not known yet.
		if (LastTag == FixTags.BeginString)
			MsgSeqNum = null;

		return LastTag;
	}

	async ValueTask<DateTime> IFixReader.ReadDateTimeAsync(FastDateTimeParser parser, CancellationToken cancellationToken)
	{
		var str = await ((IFixReader)this).ReadStringAsync(cancellationToken).NoWait();

		return parser.Parse(str);
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

		if (result == null || finalByte != (int)AsciiSymbols.Soh)
			throw new InvalidOperationException();

		IsValueRead = true;

		return result.Value;
	}

	async ValueTask<long> IFixReader.ReadLongAsync(CancellationToken cancellationToken)
	{
		var next = await ReadWithDumpAsync(cancellationToken).NoWait();
		var (result, finalByte) = await ReadInt64WithSignAsync(next, cancellationToken).NoWait();

		if (result == null || finalByte != (int)AsciiSymbols.Soh)
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

		if (result == null || finalByte != (int)AsciiSymbols.Soh)
			throw new InvalidOperationException();

		IsValueRead = true;

		return result.Value;
	}

	async ValueTask<char> IFixReader.ReadCharAsync(CancellationToken cancellationToken)
	{
		var letter = await ReadWithDumpAsync(cancellationToken).NoWait();

		if (letter == -1 || letter == (int)AsciiSymbols.Soh)
			throw new InvalidOperationException();

		var end = await ReadWithDumpAsync(cancellationToken).NoWait();

		if (end != (int)AsciiSymbols.Soh)
			throw new InvalidOperationException();

		IsValueRead = true;

		return (char)(byte)letter;
	}

	async ValueTask<string> IFixReader.ReadStringAsync(CancellationToken cancellationToken)
	{
		var chars = new List<byte>();

		while (true)
		{
			var letter = await ReadWithDumpAsync(cancellationToken).NoWait();

			if (letter == -1)
				throw new EndOfStreamException("The FIX value is not terminated by SOH.");

			if (letter == (int)AsciiSymbols.Soh)
				break;

			chars.Add((byte)letter);
		}

		IsValueRead = true;

		return chars.Count == 0 ? null : Encoding.GetString([.. chars]);
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
		while (true)
		{
			var letter = await ReadWithDumpAsync(cancellationToken).NoWait();

			if (letter == -1)
				throw new EndOfStreamException("The FIX value is not terminated by SOH.");

			if (letter == (int)AsciiSymbols.Soh)
				break;
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
		var maxMagnitude = minus == true ? (uint)int.MaxValue + 1 : int.MaxValue;
		var (magnitude, finalByte) = await ReadUInt32InternalAsync(byteAfterSign, maxMagnitude, cancellationToken).NoWait();

		if (magnitude is null)
		{
			if (minus != null)
				throw new InvalidOperationException("A sign must be followed by digits.");

			return (null, finalByte);
		}

		if (minus != true)
			return ((int)magnitude.Value, finalByte);

		return (magnitude.Value == (uint)int.MaxValue + 1 ? int.MinValue : -(int)magnitude.Value, finalByte);
	}

	private async ValueTask<(long? result, int nextByte)> ReadInt64WithSignAsync(int nextByte, CancellationToken cancellationToken)
	{
		if (nextByte == -1)
			return (null, nextByte);

		var (minus, byteAfterSign) = await GetMinusOrPlusAsync(nextByte, cancellationToken).NoWait();
		var maxMagnitude = minus == true ? (ulong)long.MaxValue + 1 : long.MaxValue;
		var (magnitude, finalByte) = await ReadUInt64InternalAsync(byteAfterSign, maxMagnitude, cancellationToken).NoWait();

		if (magnitude is null)
		{
			if (minus != null)
				throw new InvalidOperationException("A sign must be followed by digits.");

			return (null, finalByte);
		}

		if (minus != true)
			return ((long)magnitude.Value, finalByte);

		return (magnitude.Value == (ulong)long.MaxValue + 1 ? long.MinValue : -(long)magnitude.Value, finalByte);
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

	private async ValueTask<(uint? result, int nextByte)> ReadUInt32InternalAsync(int nextByte, uint maxValue, CancellationToken cancellationToken)
	{
		var digit = nextByte - (int)AsciiSymbols.Zero;

		if (digit < 0 || 9 < digit)
			return (null, nextByte);

		var result = (uint)digit;

		while (true)
		{
			var letter = await ReadWithDumpAsync(cancellationToken).NoWait();
			digit = letter - (int)AsciiSymbols.Zero;

			if (digit < 0 || 9 < digit)
				return (result, letter);

			if (result > (maxValue - (uint)digit) / 10)
				throw new OverflowException();

			result = result * 10 + (uint)digit;
		}
	}

	private async ValueTask<(ulong? result, int nextByte)> ReadUInt64InternalAsync(int nextByte, ulong maxValue, CancellationToken cancellationToken)
	{
		var digit = nextByte - (int)AsciiSymbols.Zero;

		if (digit < 0 || 9 < digit)
			return (null, nextByte);

		var result = (ulong)digit;

		while (true)
		{
			var letter = await ReadWithDumpAsync(cancellationToken).NoWait();
			digit = letter - (int)AsciiSymbols.Zero;

			if (digit < 0 || 9 < digit)
				return (result, letter);

			if (result > (maxValue - (ulong)digit) / 10)
				throw new OverflowException();

			result = result * 10 + (ulong)digit;
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
