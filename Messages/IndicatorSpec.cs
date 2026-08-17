namespace StockSharp.Messages;

/// <summary>
/// Which indicator to run, with what parameters, on which candle series.
/// </summary>
/// <remarks>
/// This names a computation completely, so it can be the argument of a <see cref="DataType"/>: two
/// requests for the same indicator, with the same parameters, on the same series are the same
/// subscription and share one computation. That only holds while this is a value - equal by what it
/// says rather than by which instance it is - so it is compared and hashed by its contents, and its
/// contents do not change once it has been built.
/// </remarks>
[DataContract]
[Serializable]
public class IndicatorSpec : Equatable<IndicatorSpec>, IPersistable
{
	private static readonly IReadOnlyDictionary<string, object> _noParameters
		= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the <see cref="IndicatorSpec"/>. For deserialization; use
	/// <see cref="Create"/> to build one.
	/// </summary>
	public IndicatorSpec()
	{
	}

	/// <summary>
	/// Builds a specification.
	/// </summary>
	/// <param name="kind">
	/// Indicator type name, matched case-insensitively. Either the full name, such as
	/// <c>BollingerBands</c>, or a short alias the indicator declares, such as <c>BB</c>.
	/// </param>
	/// <param name="candleType">Candle series the indicator runs on.</param>
	/// <param name="parameters">
	/// Tuning parameters keyed by the indicator's property names, matched case-insensitively.
	/// Omitted properties keep the indicator's defaults.
	/// </param>
	/// <returns>The specification.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="kind"/> or <paramref name="candleType"/> is null.</exception>
	/// <exception cref="ArgumentException">Two parameters name the same property.</exception>
	public static IndicatorSpec Create(string kind, DataType candleType, IEnumerable<KeyValuePair<string, object>> parameters)
	{
		if (kind.IsEmpty())
			throw new ArgumentNullException(nameof(kind));

		return new()
		{
			Kind = kind,
			CandleType = candleType ?? throw new ArgumentNullException(nameof(candleType)),
			Parameters = Freeze(parameters),
		};
	}

	private static IReadOnlyDictionary<string, object> Freeze(IEnumerable<KeyValuePair<string, object>> parameters)
	{
		if (parameters is null)
			return _noParameters;

		var frozen = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		foreach (var (key, value) in parameters)
		{
			// Names are matched case-insensitively against the indicator's properties, so two keys
			// differing only in case name the same property and the request has no single meaning.
			if (!frozen.TryAdd(key, Normalize(value)))
				throw new ArgumentException(LocalizedStrings.HasDuplicates.Put(key), nameof(parameters));
		}

		return frozen;
	}

	// A length of 20 written in code is an int and the same length off a JSON frame is a long, and
	// boxed values of different types are never equal. Two requests for the same indicator have to
	// be the same specification whichever side they came from, so numbers are stored one way.
	private static object Normalize(object value) => value switch
	{
		byte or sbyte or short or ushort or int or uint or long or ulong => value.To<long>(),
		float or double or decimal => value.To<decimal>(),
		_ => value,
	};

	/// <summary>
	/// Indicator type name, matched case-insensitively.
	/// </summary>
	[DataMember]
	public string Kind { get; private set; }

	/// <summary>
	/// Candle series the indicator runs on. Part of what the specification names: the same indicator
	/// on a different series is a different computation.
	/// </summary>
	[DataMember]
	public DataType CandleType { get; private set; }

	/// <summary>
	/// Tuning parameters keyed by the indicator's property names, matched case-insensitively.
	/// </summary>
	[DataMember]
	public IReadOnlyDictionary<string, object> Parameters { get; private set; } = _noParameters;

	/// <inheritdoc />
	protected override bool OnEquals(IndicatorSpec other)
	{
		if (!Kind.EqualsIgnoreCase(other.Kind) || !Equals(CandleType, other.CandleType))
			return false;

		if (Parameters.Count != other.Parameters.Count)
			return false;

		foreach (var (key, value) in Parameters)
		{
			if (!other.Parameters.TryGetValue(key, out var otherValue) || !Equals(value, otherValue))
				return false;
		}

		return true;
	}

	/// <inheritdoc cref="object.GetHashCode" />
	public override int GetHashCode()
	{
		var hash = new HashCode();

		hash.Add(Kind, StringComparer.OrdinalIgnoreCase);
		hash.Add(CandleType);

		// Parameters are a set, so their order must not reach the hash - combined pairwise and
		// summed, two specifications that list the same parameters differently still agree.
		var parameters = 0;

		foreach (var (key, value) in Parameters)
			parameters ^= HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(key), value);

		hash.Add(parameters);

		return hash.ToHashCode();
	}

	/// <inheritdoc />
	public override IndicatorSpec Clone()
		=> new()
		{
			Kind = Kind,
			CandleType = CandleType?.TypedClone(),
			Parameters = Parameters,
		};

	/// <inheritdoc />
	public void Load(SettingsStorage storage)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		Kind = storage.GetValue<string>(nameof(Kind));
		CandleType = storage.GetValue<SettingsStorage>(nameof(CandleType))?.Load<DataType>();

		var parameters = storage.GetValue<SettingsStorage>(nameof(Parameters));

		Parameters = parameters is null
			? _noParameters
			: Freeze(parameters.Select(pair => new KeyValuePair<string, object>(pair.Key, pair.Value)));
	}

	/// <inheritdoc />
	public void Save(SettingsStorage storage)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		storage.SetValue(nameof(Kind), Kind);
		storage.SetValue(nameof(CandleType), CandleType?.Save());

		var parameters = new SettingsStorage();

		foreach (var (key, value) in Parameters)
			parameters.SetValue(key, value);

		storage.SetValue(nameof(Parameters), parameters);
	}

	/// <inheritdoc />
	public override string ToString()
		=> Parameters.Count == 0
			? $"{Kind}({CandleType})"
			: $"{Kind}({CandleType}, {Parameters.Select(p => $"{p.Key}={p.Value}").JoinComma()})";
}
