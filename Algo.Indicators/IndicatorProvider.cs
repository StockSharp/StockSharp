namespace StockSharp.Algo;

using Ecng.Common;
using Ecng.Reflection;

using StockSharp.Algo.Indicators;

/// <summary>
/// <see cref="IndicatorType"/> provider.
/// </summary>
public class IndicatorProvider : IIndicatorProvider
{
	private readonly CachedSynchronizedSet<IndicatorType> _all = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="IndicatorProvider"/>.
	/// </summary>
	public IndicatorProvider()
	{
	}

	/// <inheritdoc />
	public virtual void Init()
	{
		var ns = typeof(IIndicator).Namespace;

		_all.AddRange(typeof(BaseIndicator)
			.Assembly
			.FindImplementations<IIndicator>(showObsolete: true, extraFilter: t => t.Namespace == ns && t.GetConstructor(Type.EmptyTypes) != null && t.GetAttribute<IndicatorHiddenAttribute>() is null)
			.Select(t => new IndicatorType(t))
			.OrderBy(t => t.Name));
	}

	/// <summary>
	/// Finds the indicator type answering to <paramref name="name"/>, matched case-insensitively
	/// against type names. Short names resolve as well as full ones, because the short forms are
	/// declared as types in their own right - <c>SMA</c>, <c>BB</c>, <c>MACDH</c>.
	/// </summary>
	/// <param name="name">Indicator type name.</param>
	/// <returns>The type, or <see langword="null"/> when nothing answers to that name.</returns>
	public static Type TryFind(string name)
	{
		if (name.IsEmptyOrWhiteSpace())
			return null;

		name = name.Trim();

		foreach (var type in typeof(BaseIndicator).Assembly.GetTypes())
		{
			if (type.IsAbstract || !type.Is<IIndicator>() || !type.Name.EqualsIgnoreCase(name))
				continue;

			// A hidden indicator is an inner part of another one and has no identity of its own to
			// be asked for; one without a parameterless constructor cannot be created at all.
			return type.GetAttribute<IndicatorHiddenAttribute>() is null && type.GetConstructor(Type.EmptyTypes) is not null
				? type
				: null;
		}

		return null;
	}

	/// <inheritdoc />
	public IEnumerable<IndicatorType> All => _all.Cache;

	void ICustomProvider<IndicatorType>.Add(IndicatorType type) => _all.Add(type);
	void ICustomProvider<IndicatorType>.Remove(IndicatorType type) => _all.Remove(type);
}