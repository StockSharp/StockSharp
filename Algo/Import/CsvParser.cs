namespace StockSharp.Algo.Import;

using System.Runtime.CompilerServices;

/// <summary>
/// Messages parser from text file in CSV format.
/// </summary>
public class CsvParser : BaseLogReceiver
{
	/// <summary>
	/// Data type info.
	/// </summary>
	public DataType DataType { get; }

	/// <summary>
	/// Importing fields.
	/// </summary>
	public IEnumerable<FieldMapping> Fields { get; }

	/// <summary>
	/// Extended info storage.
	/// </summary>
	public IExtendedInfoStorageItem ExtendedInfoStorageItem { get; set; }

	/// <summary>
	/// Ignore securities without identifiers.
	/// </summary>
	public bool IgnoreNonIdSecurities { get; set; } = true;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvParser"/>.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <param name="fields">Importing fields.</param>
	public CsvParser(DataType dataType, IEnumerable<FieldMapping> fields)
	{
		if (dataType == null)
			throw new ArgumentNullException(nameof(dataType));

		if (fields == null)
			throw new ArgumentNullException(nameof(fields));

		if (dataType.MessageType == null)
			throw new ArgumentException(LocalizedStrings.InvalidValue, nameof(dataType));

		DataType = dataType;
		Fields = [.. fields];

		if (Fields.IsEmpty())
			throw new ArgumentException(LocalizedStrings.InvalidValue, nameof(fields));
	}

	private string _columnSeparator = ",";

	/// <summary>
	/// Column separator. Tabulation is denoted by TAB.
	/// </summary>
	public string ColumnSeparator
	{
		get => _columnSeparator;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_columnSeparator = value;
		}
	}

	private string _lineSeparator = StringHelper.RN;

	/// <summary>
	/// Line separator.
	/// </summary>
	public string LineSeparator
	{
		get => _lineSeparator;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_lineSeparator = value;
		}
	}

	private int _skipFromHeader;

	/// <summary>
	/// Number of lines to be skipped from the beginning of the file (if they contain meta information).
	/// </summary>
	public int SkipFromHeader
	{
		get => _skipFromHeader;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_skipFromHeader = value;
		}
	}

	/// <summary>
	/// Parse CSV file.
	/// </summary>
	/// <param name="stream">The file stream.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Parsed instances.</returns>
	public async IAsyncEnumerable<Message> Parse(Stream stream, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(stream);

		var columnSeparator = ColumnSeparator.ReplaceIgnoreCase("TAB", "\t");

		using var reader = new CsvFileReader(stream, LineSeparator) { Delimiter = columnSeparator[0] };

		var skipLines = SkipFromHeader;
		var lineIndex = 0;

		var fields = Fields.ToArray();

		fields.ForEach(f => f.Reset());

		var cells = new List<string>();

		var isDepth = DataType == DataType.MarketDepth;

		var quoteMsg = isDepth ? new QuoteChangeMessage() : null;
		var bids = isDepth ? new List<QuoteChange>() : null;
		var asks = isDepth ? new List<QuoteChange>() : null;
		var hasPos = false;

		void AddQuote(TimeQuoteChange quote)
		{
			var qq = quote.Quote;

			if (qq.StartPosition != default || qq.EndPosition != default)
				hasPos = true;

			(quote.Side == Sides.Buy ? bids : asks).Add(qq);
		}

		void FillQuote(TimeQuoteChange quote)
		{
			quoteMsg.ServerTime = quote.ServerTime;
			quoteMsg.SecurityId = quote.SecurityId;
			quoteMsg.LocalTime = quote.LocalTime;

			AddQuote(quote);
		}

		void FlushQuotes()
		{
			quoteMsg.Bids = [.. bids];
			quoteMsg.Asks = [.. asks];
			quoteMsg.HasPositions = hasPos;
		}

		var adapters = new Dictionary<Type, IMessageAdapter>();

		while (await reader.ReadRowAsync(cells, cancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			lineIndex++;

			if (skipLines > 0)
			{
				skipLines--;
				continue;
			}

			dynamic instance = CreateInstance(isDepth);

			var mappings = new Dictionary<string, SecurityIdMapping>(StringComparer.InvariantCultureIgnoreCase);

			foreach (var field in fields)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (field.Order >= cells.Count)
					throw new InvalidOperationException(LocalizedStrings.IndexMoreThanLen.Put(field.DisplayName, field.Order, cells.Count));

				try
				{
					if (field.Order == null)
					{
						if (field.IsRequired)
							field.ApplyDefaultValue(instance);
					}
					else
					{
						var cell = cells[field.Order.Value];

						if (field.IsAdapter)
						{
							var adapter = adapters.SafeAdd(field.AdapterType, key => key.CreateAdapter());
							var info = mappings.SafeAdd(adapter.StorageName, _ => new());

							field.ApplyFileValue(info, cell);
						}
						else
							field.ApplyFileValue(instance, cell);
					}
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException(LocalizedStrings.CsvImportError.Put(lineIndex, field.Order, field.Order == null ? "NULL" : cells[field.Order.Value], field.DisplayName), ex);
				}
			}

			if (instance is not SecurityMessage secMsg)
			{
				switch (instance)
				{
					case ExecutionMessage execMsg:
						execMsg.DataTypeEx = DataType;
						break;
					case CandleMessage candleMsg:
						candleMsg.State = CandleStates.Finished;
						break;
				}
			}
			else
			{
				if (secMsg.SecurityId.SecurityCode.IsEmpty() || secMsg.SecurityId.BoardCode.IsEmpty())
				{
					if (!IgnoreNonIdSecurities)
						LogError(LocalizedStrings.LineNoSecurityId.Put(reader.CurrLine));

					continue;
				}
				else
				{
					foreach (var pair in mappings)
					{
						cancellationToken.ThrowIfCancellationRequested();

						var info = pair.Value;

						if (info.AdapterId.SecurityCode.IsEmpty())
							continue;

						if (info.AdapterId.BoardCode.IsEmpty())
						{
							var adapterId = info.AdapterId;
							adapterId.BoardCode = secMsg.SecurityId.BoardCode;
							info.AdapterId = adapterId;
						}

						info.StockSharpId = secMsg.SecurityId;

						yield return new SecurityMappingMessage
						{
							StorageName = pair.Key,
							Mapping = info,
						};
					}
				}
			}

			if (quoteMsg != null)
			{
				var quote = (TimeQuoteChange)instance;

				if (bids.IsEmpty() && asks.IsEmpty())
				{
					FillQuote(quote);
				}
				else
				{
					if (quoteMsg.ServerTime == quote.ServerTime && quoteMsg.SecurityId == quote.SecurityId)
					{
						AddQuote(quote);
					}
					else
					{
						FlushQuotes();
						yield return quoteMsg;

						quoteMsg = new QuoteChangeMessage();
						bids = [];
						asks = [];
						hasPos = false;
						FillQuote(quote);
					}
				}
			}
			else
				yield return instance;
		}

		if (quoteMsg != null && !bids.IsEmpty() && !asks.IsEmpty())
		{
			FlushQuotes();
			yield return quoteMsg;
		}
	}

	private object CreateInstance(bool isDepth)
	{
		var instance = isDepth
			? new TimeQuoteChange()
			: DataType.MessageType.CreateInstance<object>();

		return instance;
	}
}