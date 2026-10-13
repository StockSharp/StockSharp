namespace StockSharp.Fix.Native;

using Ecng.Reflection;

/// <summary>
/// Dialect source.
/// </summary>
/// <typeparam name="TInterfaceType">Dialect interface type (e.g. IFixDialect).</typeparam>
/// <typeparam name="TAsmHolder">Assembly holder type.</typeparam>
public class DialectSource<TInterfaceType, TAsmHolder> : ItemsSourceBase<Type>
{
	private static readonly CachedSynchronizedSet<Type> _source = [];

	static DialectSource()
	{
		foreach (var type in typeof(TAsmHolder).Assembly.FindImplementations<TInterfaceType>(true, true))
		{
			Add(type);
		}
	}

	/// <summary>
	/// Add new dialect.
	/// </summary>
	/// <param name="type">Type.</param>
	public static void Add(Type type) => _source.Add(type ?? throw new ArgumentNullException(nameof(type)));

	/// <summary>
	/// Get values.
	/// </summary>
	/// <returns>Values.</returns>
	protected override IEnumerable<Type> GetValues() => _source.Cache;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	protected override string GetName(Type value) => value.GetDisplayName();

	/// <summary>
	/// 
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	protected override string GetDescription(Type value) => value.GetDescription();

	/// <summary>
	/// 
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	protected override Uri GetIcon(Type value) => value.TryGetIconUrl();
}