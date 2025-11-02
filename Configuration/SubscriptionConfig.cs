namespace StockSharp.Configuration;

/// <summary>
/// Represents a persisted market data subscription configuration.
/// </summary>
public record SubscriptionConfig : IPersistable
{
	/// <summary>
	/// <see cref="SecurityId"/>
	/// </summary>
	public SecurityId? Security { get; set; }

	/// <summary>
	/// Market data type identifier (for example: Level1, MarketDepth, Trades, Candles).
	/// </summary>
	public DataType DataType { get; set; }

	/// <summary>
	/// Build mode for derived data (when data needs to be built from another source).
	/// </summary>
	public MarketDataBuildModes? BuildMode { get; set; }

	/// <summary>
	/// Source data type used to build the requested data (e.g., build candles from trades).
	/// </summary>
	public DataType BuildFrom { get; set; }

	/// <summary>
	/// Field used during building (for example, a price or volume field name).
	/// </summary>
	public Level1Fields? BuildField { get; set; }

	/// <summary>
	/// Optional start time (UTC) of the requested data interval.
	/// </summary>
	public DateTime? From { get; set; }

	/// <summary>
	/// Optional end time (UTC) of the requested data interval.
	/// </summary>
	public DateTime? To { get; set; }

	/// <summary>
	/// Optional maximum number of data items to request.
	/// </summary>
	public long? Count { get; set; }

	/// <summary>
	/// Optional maximum depth for order book data.
	/// </summary>
	public int? MaxDepth { get; set; }

	void IPersistable.Load(SettingsStorage storage)
	{
		DataType load(string name)
		{
			var str = storage.GetValue<string>(name);

			if (str.IsEmpty())
				return null;

			return DataType.FromSerializableString(str);
		}

		Security = storage.GetValue<string>(nameof(Security))?.ToSecurityId();
		DataType = load(nameof(DataType));
		BuildMode = storage.GetValue<MarketDataBuildModes?>(nameof(BuildMode));
		BuildFrom = load(nameof(BuildFrom));
		BuildField = storage.GetValue<Level1Fields?>(nameof(BuildField));
		From = storage.GetValue<long?>(nameof(From))?.To<DateTime>().UtcKind();
		To = storage.GetValue<long?>(nameof(To))?.To<DateTime>().UtcKind();
		Count = storage.GetValue<long?>(nameof(Count));
		MaxDepth = storage.GetValue<int?>(nameof(MaxDepth));
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Security), Security?.ToStringId())
			.Set(nameof(DataType), DataType?.ToSerializableString())
			.Set(nameof(BuildMode), BuildMode)
			.Set(nameof(BuildFrom), BuildFrom?.ToSerializableString())
			.Set(nameof(BuildField), BuildField)
			.Set(nameof(From), From?.To<long>())
			.Set(nameof(To), To?.To<long>())
			.Set(nameof(Count), Count)
			.Set(nameof(MaxDepth), MaxDepth)
		;
	}
}