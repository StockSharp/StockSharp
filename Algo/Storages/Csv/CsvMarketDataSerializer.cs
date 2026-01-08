namespace StockSharp.Algo.Storages.Csv;

class CsvMetaInfo(DateTime date, Encoding encoding, Func<FastCsvReader, object> readId, Func<FastCsvReader, bool> readIncrementalOnly = null) : MetaInfo(date)
{
	private readonly Encoding _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

	//public override CsvMetaInfo Clone()
	//{
	//	return new CsvMetaInfo(Date, _encoding, _toId)
	//	{
	//		Count = Count,
	//		FirstTime = FirstTime,
	//		LastTime = LastTime,
	//		PriceStep = PriceStep,
	//		VolumeStep = VolumeStep,
	//	};
	//}

	private object _lastId;

	public override object LastId
	{
		get => _lastId;
		set => _lastId = value;
	}

	public bool? IncrementalOnly { get; set; }

	public override void Write(Stream stream)
	{
	}

	public override async ValueTask ReadAsync(Stream stream, CancellationToken cancellationToken)
	{
		await Do.InvariantAsync(async () =>
		{
			var count = 0;

			var firstTimeRead = false;
			string lastLine = null;

			using var reader = stream.CreateCsvReader(_encoding);

			while (await reader.NextLineAsync(cancellationToken))
			{
				lastLine = reader.CurrentLine;

				if (!firstTimeRead)
				{
					FirstTime = reader.ReadTime(Date);
					firstTimeRead = true;
				}

				count++;
			}

			Count = count;

			if (lastLine != null)
			{
				using var lastLineReader = new FastCsvReader(lastLine, StringHelper.RN);

				if (!await lastLineReader.NextLineAsync(cancellationToken))
					throw new InvalidOperationException();

				LastTime = lastLineReader.ReadTime(Date);
				_lastId = readId?.Invoke(lastLineReader);
				IncrementalOnly = readIncrementalOnly?.Invoke(lastLineReader);
			}

			stream.Position = 0;
		});
	}
}

/// <summary>
/// The serializer in the CSV format.
/// </summary>
/// <typeparam name="TData">Data type.</typeparam>
public abstract class CsvMarketDataSerializer<TData> : IMarketDataSerializer<TData>
	where TData : IServerTimeMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CsvMarketDataSerializer{T}"/>.
	/// </summary>
	/// <param name="encoding">Encoding.</param>
	protected CsvMarketDataSerializer(Encoding encoding)
		: this(default, encoding)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvMarketDataSerializer{T}"/>.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="encoding">Encoding.</param>
	protected CsvMarketDataSerializer(SecurityId securityId, Encoding encoding)
	{
		// force hash code caching
		securityId.GetHashCode();

		SecurityId = securityId;
		Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
	}

	/// <summary>
	/// Encoding.
	/// </summary>
	public Encoding Encoding { get; }

	/// <summary>
	/// Security ID.
	/// </summary>
	public SecurityId SecurityId { get; }

	/// <summary>
	/// Storage format.
	/// </summary>
	public StorageFormats Format => StorageFormats.Csv;

	/// <summary>
	/// Time precision.
	/// </summary>
	public TimeSpan TimePrecision { get; } = TimeSpan.FromTicks(1);

	/// <summary>
	/// To create empty meta-information.
	/// </summary>
	/// <param name="date">Date.</param>
	/// <returns>Meta-information on data for one day.</returns>
	public virtual IMarketDataMetaInfo CreateMetaInfo(DateTime date)
	{
		return new CsvMetaInfo(date, Encoding, null);
	}

	ValueTask IMarketDataSerializer.SerializeAsync(Stream stream, IEnumerable data, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken)
	{
		return SerializeAsync(stream, data.Cast<TData>(), metaInfo, cancellationToken);
	}

	/// <summary>
	/// Save data into stream.
	/// </summary>
	/// <param name="stream">Data stream.</param>
	/// <param name="data">Data.</param>
	/// <param name="metaInfo">Meta-information on data for one day.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	public virtual ValueTask SerializeAsync(Stream stream, IEnumerable<TData> data, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken)
	{
		return Do.InvariantAsync(async () =>
		{
			using var writer = stream.CreateCsvWriter(Encoding);

			foreach (var item in data)
			{
				await WriteAsync(writer, item, metaInfo, cancellationToken);
				metaInfo.LastTime = item.ServerTime;
			}
		}).AsValueTask();
	}

	/// <summary>
	/// Write data to the specified writer.
	/// </summary>
	/// <param name="writer">CSV writer.</param>
	/// <param name="data">Data.</param>
	/// <param name="metaInfo">Meta-information on data for one day.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	protected abstract ValueTask WriteAsync(CsvFileWriter writer, TData data, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken);

	private readonly struct CsvAsyncEnumerable(CsvMarketDataSerializer<TData> serializer, Stream stream, IMarketDataMetaInfo metaInfo) : IAsyncEnumerable<TData>
	{
		private class CsvEnumerator(CsvMarketDataSerializer<TData> serializer, FastCsvReader reader, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken) : IAsyncEnumerator<TData>
		{
			private readonly CsvMarketDataSerializer<TData> _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			private readonly FastCsvReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));
			private readonly IMarketDataMetaInfo _metaInfo = metaInfo ?? throw new ArgumentNullException(nameof(metaInfo));
			private readonly CancellationToken _cancellationToken = cancellationToken;
			private TData _current;
            TData IAsyncEnumerator<TData>.Current => _current;

			ValueTask IAsyncDisposable.DisposeAsync()
			{
				_current = default;
				_reader.Dispose();
				return default;
			}

			async ValueTask<bool> IAsyncEnumerator<TData>.MoveNextAsync()
			{
				var retVal = await _reader.NextLineAsync(_cancellationToken);

				if (retVal)
					_current = _serializer.Read(_reader, _metaInfo);

				return retVal;
			}
		}

		private readonly CsvMarketDataSerializer<TData> _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
		private readonly IMarketDataMetaInfo _metaInfo = metaInfo ?? throw new ArgumentNullException(nameof(metaInfo));

		IAsyncEnumerator<TData> IAsyncEnumerable<TData>.GetAsyncEnumerator(CancellationToken cancellationToken)
		{
			var reader = _stream.CreateCsvReader(_serializer.Encoding);
			return new CsvEnumerator(_serializer, reader, _metaInfo, cancellationToken);
		}
	}

	/// <inheritdoc />
	public virtual IAsyncEnumerable<TData> DeserializeAsync(Stream stream, IMarketDataMetaInfo metaInfo)
		=> new CsvAsyncEnumerable(this, stream, metaInfo);

	/// <summary>
	/// Read data from the specified reader.
	/// </summary>
	/// <param name="reader">CSV reader.</param>
	/// <param name="metaInfo">Meta-information on data for one day.</param>
	/// <returns>Data.</returns>
	protected abstract TData Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo);
}