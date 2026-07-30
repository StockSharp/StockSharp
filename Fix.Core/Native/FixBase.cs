namespace StockSharp.Fix.Native;

/// <summary>
/// Data reader/writer base class.
/// </summary>
/// <remarks>
/// Initialize <see cref="FixBase"/>.
/// </remarks>
/// <param name="stream">The stream.</param>
/// <param name="encoding">Text encoding.</param>
/// <param name="ownsStream">Whether to dispose the stream when this instance is disposed.</param>
public abstract class FixBase(Stream stream, Encoding encoding, bool ownsStream = false) : Disposable
{
	private readonly AllocationArray<byte> _dump = new(FileSizes.KB);
	private readonly bool _ownsStream = ownsStream;
	
	/// <summary>
	/// The stream.
	/// </summary>
	public Stream Stream { get; } = stream ?? throw new ArgumentNullException(nameof(stream));

	/// <summary>
	/// Text encoding.
	/// </summary>
	public Encoding Encoding { get; } = encoding ?? throw new ArgumentNullException(nameof(encoding));

	/// <summary>
	/// <see cref="CheckSum"/> disabled.
	/// </summary>
	public bool CheckSumDisabled { get; set; }

	/// <summary>
	/// Check sum.
	/// </summary>
#pragma warning disable 3003
	public uint CheckSum { get; set; }
#pragma warning restore 3003

	/// <summary>
	/// Gets a value indicating whether the log incoming data required.
	/// </summary>
	public bool IsDump { get; set; }

	/// <summary>
	/// Last tag.
	/// </summary>
	public FixTags LastTag { get; protected set; }

	/// <summary>
	/// Whether the tag value was read.
	/// </summary>
	public bool IsValueRead { get; protected set; }

	private int _maxBytes = int.MaxValue;

	/// <summary>
	/// Gets and sets the maximum allowed bytes per read/write operation.
	/// </summary>
	public int MaxBytes
	{
		get => _maxBytes;
		set
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException();

			_maxBytes = value;
		}
	}

	private int _bytesCount;

	/// <summary>
	/// Total read/write bytes.
	/// </summary>
	public int BytesCount
	{
		get => _bytesCount;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			if (MaxBytes != int.MaxValue && value > MaxBytes)
				throw new InvalidOperationException(LocalizedStrings.MaxBytesExceeded.Put(value, MaxBytes));

			_bytesCount = value;
		}
	}

	/// <summary>
	/// Calculate the check sum.
	/// </summary>
	/// <param name="value">New byte.</param>
	protected void CalcCheckSum(byte value)
	{
		// Count bytes even when the checksum is disabled, so the MaxBytes cap still fires and an
		// unbounded (no-SOH) field cannot grow without limit and OOM the host.
		BytesCount++;

		if (CheckSumDisabled)
			return;

		CheckSum += value;
	}

	/// <summary>
	/// Calculate the check sum.
	/// </summary>
	/// <param name="value">Buffer.</param>
	protected void CalcCheckSum(Span<byte> value)
	{
		// Count bytes even when the checksum is disabled, so the MaxBytes cap still fires (see the
		// single-byte overload).
		BytesCount += value.Length;

		if (CheckSumDisabled)
			return;

		for (int i = 0; i < value.Length; i++)
			CheckSum += value[i];
	}

	/// <summary>
	/// Calculate the check sum.
	/// </summary>
	/// <param name="value">Buffer.</param>
	protected void CalcCheckSum(ReadOnlySpan<byte> value)
	{
		if (CheckSumDisabled)
			return;

		BytesCount += value.Length;

		for (int i = 0; i < value.Length; i++)
			CheckSum += value[i];
	}

	/// <summary>
	/// Add to the data log a new byte.
	/// </summary>
	/// <param name="value">New byte.</param>
	protected void Dump(byte value)
	{
		if (!IsDump)
			return;

		_dump.Add(value.TryReplaceSoh());
	}

	/// <summary>
	/// Add to the data log a news byte.
	/// </summary>
	/// <param name="value">Buffer.</param>
	protected void Dump(Span<byte> value)
	{
		if (!IsDump)
			return;

		var copy = value.ToArray();

		for (int i = 0; i < copy.Length; i++)
			copy[i] = copy[i].TryReplaceSoh();

		_dump.Add(copy, 0, copy.Length);
	}

	/// <summary>
	/// Add to the data log a news byte.
	/// </summary>
	/// <param name="value">Buffer.</param>
	protected void Dump(ReadOnlySpan<byte> value)
	{
		if (!IsDump)
			return;

		var copy = value.ToArray();

		for (int i = 0; i < copy.Length; i++)
			copy[i] = copy[i].TryReplaceSoh();

		_dump.Add(copy, 0, copy.Length);
	}

	/// <summary>
	/// Add to the data log a new string.
	/// </summary>
	/// <typeparam name="T">Type of value.</typeparam>
	/// <param name="value">Value.</param>
	protected void Dump<T>(T value)
	{
		if (!IsDump)
			return;

		var str = value.To<string>() ?? string.Empty;
		var bytes = Encoding.GetBytes(str);
		_dump.Add(bytes, 0, bytes.Length);
		_dump.Add((byte)'|');
	}

	/// <summary>
	/// Clear state.
	/// </summary>
	public virtual void ClearState()
	{
		LastTag = Extensions.EmptyTag;
		IsValueRead = false;
		CheckSum = 0;
		BytesCount = 0;
	}

	/// <summary>
	/// Get data log.
	/// </summary>
	/// <returns>Data log.</returns>
	public string FlushDump()
	{
		var temp = Encoding.GetString(_dump.Buffer, 0, _dump.Count);

		_dump.Reset();

		return temp;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		if (_ownsStream)
			Stream.Dispose();

		base.DisposeManaged();
	}
}