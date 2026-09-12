namespace StockSharp.Messages;

/// <summary>
/// Change actions.
/// </summary>
[DataContract]
[Serializable]
public enum QuoteChangeActions : byte
{
	/// <summary>
	/// New quote for <see cref="QuoteChange.StartPosition"/>.
	/// </summary>
	[EnumMember]
	New,

	/// <summary>
	/// Update quote for <see cref="QuoteChange.StartPosition"/>.
	/// </summary>
	[EnumMember]
	Update,

	/// <summary>
	/// Delete quotes from <see cref="QuoteChange.StartPosition"/> till <see cref="QuoteChange.EndPosition"/>.
	/// </summary>
	[EnumMember]
	Delete,
}

/// <summary>
/// Quote conditions.
/// </summary>
[DataContract]
[Serializable]
public enum QuoteConditions : byte
{
	/// <summary>
	/// Active.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ActiveKey)]
	Active,

	/// <summary>
	/// Indicative.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IndicativeKey)]
	Indicative,
}

/// <summary>
/// Market depth quote representing bid or ask.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QuoteChange"/>.
/// </remarks>
/// <param name="price">Quote price.</param>
/// <param name="volume">Quote volume.</param>
/// <param name="ordersCount">Orders count.</param>
/// <param name="condition">Quote condition.</param>
[DataContract]
[Serializable]
public struct QuoteChange(decimal price, decimal volume, int? ordersCount = null, QuoteConditions condition = default)
{

	/// <summary>
	/// Quote price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceKey,
		Description = LocalizedStrings.QuotePriceKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal Price { get; set; } = price;

	/// <summary>
	/// Quote volume.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeKey,
		Description = LocalizedStrings.QuoteVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal Volume { get; set; } = volume;

	/// <summary>
	/// Electronic board code.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BoardKey,
		Description = LocalizedStrings.BoardCodeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string BoardCode { get; set; } = null;

	/// <summary>
	/// Orders count.
	/// </summary>
	[DataMember]
	public int? OrdersCount { get; set; } = ordersCount;

	/// <summary>
	/// Start position, related for <see cref="Action"/>.
	/// </summary>
	[DataMember]
	public int? StartPosition { get; set; } = null;

	/// <summary>
	/// End position, related for <see cref="Action"/>.
	/// </summary>
	[DataMember]
	public int? EndPosition { get; set; } = null;

	/// <summary>
	/// Change action.
	/// </summary>
	[DataMember]
	public QuoteChangeActions? Action { get; set; } = null;

	/// <summary>
	/// Quote condition.
	/// </summary>
	[DataMember]
	public QuoteConditions Condition { get; set; } = condition;

	private QuoteChange[] _innerQuotes = null;

	/// <summary>
	/// Collection of enclosed quotes, which are combined into a single quote.
	/// </summary>
	public QuoteChange[] InnerQuotes
	{
		readonly get => _innerQuotes;
		set
		{
			var wasNonNull = _innerQuotes != null;

			_innerQuotes = value;

			if (_innerQuotes is null)
			{
				if (wasNonNull)
				{
					Volume = default;
					OrdersCount = default;
				}
			}
			else
			{
				var volume = 0m;
				var ordersCount = 0;

				foreach (var item in value)
				{
					volume += item.Volume;

					if (item.OrdersCount != null)
						ordersCount += item.OrdersCount.Value;
				}

				Volume = volume;
				OrdersCount = ordersCount.DefaultAsNull();
			}
		}
	}

	/// <summary>
	/// Create a copy that shares nothing with this quote.
	/// </summary>
	/// <returns>Copy.</returns>
	/// <remarks>
	/// The struct itself is copied by assignment, but <see cref="InnerQuotes"/> is an array and two
	/// copies would otherwise write into one set of quotes.
	/// </remarks>
	public readonly QuoteChange Clone()
	{
		var clone = this;

		if (_innerQuotes is not null)
		{
			var inner = new QuoteChange[_innerQuotes.Length];

			for (var i = 0; i < inner.Length; i++)
				inner[i] = _innerQuotes[i].Clone();

			// Assigned to the field so the already copied Volume and OrdersCount are kept as they are.
			clone._innerQuotes = inner;
		}

		return clone;
	}

	/// <inheritdoc />
	public override readonly string ToString() => $"{Price} {Volume}";
}