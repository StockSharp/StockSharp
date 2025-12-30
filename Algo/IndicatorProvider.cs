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

	/// <inheritdoc />
	public IEnumerable<IndicatorType> All => _all.Cache;

	void ICustomProvider<IndicatorType>.Add(IndicatorType type) => _all.Add(type);
	void ICustomProvider<IndicatorType>.Remove(IndicatorType type) => _all.Remove(type);
}