namespace StockSharp.Messages;

/// <summary>
/// Information about connection.
/// </summary>
public class ConnectorInfo
{
	/// <summary>
	/// The connection name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The connection description.
	/// </summary>
	public string Description { get; }

	/// <summary>
	/// The connection description.
	/// </summary>
	public string Category { get; }

	/// <summary>
	/// Category order.
	/// </summary>
	public int CategoryOrder { get; }

	/// <summary>
	/// The target audience.
	/// </summary>
	public string PreferLanguage { get; }

	/// <summary>
	/// Platform.
	/// </summary>
	public Platforms Platform { get; }

	/// <summary>
	/// The type of adapter.
	/// </summary>
	public Type AdapterType { get; set; }

	/// <summary>
	/// Storage name.
	/// </summary>
	public string StorageName { get; }

	/// <summary>
	/// Icon.
	/// </summary>
	public Uri Icon { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ConnectorInfo"/>.
	/// </summary>
	/// <param name="adapter">Adapter.</param>
	public ConnectorInfo(IMessageAdapter adapter)
		: this(adapter.GetType())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ConnectorInfo"/>.
	/// </summary>
	/// <param name="adapterType">The type of transaction or market data adapter.</param>
	public ConnectorInfo(Type adapterType)
	{
		if (adapterType == null)
			throw new ArgumentNullException(nameof(adapterType));

		if (!adapterType.Is<IMessageAdapter>())
			throw new ArgumentException(LocalizedStrings.TypeNotImplemented.Put(adapterType, typeof(IMessageAdapter)), nameof(adapterType));

		AdapterType = adapterType;
		Name = adapterType.GetDisplayName();
		Description = adapterType.GetDescription();
		Category = adapterType.GetCategory(LocalizedStrings.Common);
		StorageName = adapterType.Namespace.Remove(nameof(StockSharp)).Remove(".");

		Platform = adapterType.GetPlatform();

		PreferLanguage = (adapterType.GetAttribute<MessageAdapterCategoryAttribute>()?.Categories).GetPreferredLanguage();

		Icon = adapterType.TryGetIconUrl();

		const int miscOrder = 3;

		CategoryOrder =
			Category.IsEmptyOrWhiteSpace() ? miscOrder :
			_categoryOrder.TryGetValue(Category, out var order) ? order : miscOrder;
	}

	private static readonly Dictionary<string, int> _categoryOrder = [];

	static ConnectorInfo()
	{
		var ru = LocalizedStrings.ActiveLanguage == LocalizedStrings.RuCode;

		void refreshCategories()
		{
			_categoryOrder.Clear();

			_categoryOrder.Add(LocalizedStrings.Russia, ru ? 1 : 4);
			_categoryOrder.Add(LocalizedStrings.America, ru ? 4 : 1);
			_categoryOrder.Add(LocalizedStrings.Cryptocurrency, 0);
			_categoryOrder.Add(LocalizedStrings.Forex, 2);
			_categoryOrder.Add(LocalizedStrings.MarketData, 5);
		}

		LocalizedStrings.ActiveLanguageChanged += refreshCategories;
		refreshCategories();
	}

	/// <inheritdoc />
	public override string ToString() => Name;
}