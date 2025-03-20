namespace StockSharp.Algo;

using Ecng.Common;
using Ecng.Reflection;

using StockSharp.Algo.Indicators;

/// <summary>
/// <see cref="IndicatorType"/> provider.
/// </summary>
public class IndicatorProvider : IIndicatorProvider
{
	private readonly CachedSynchronizedSet<IndicatorType> _indicatorTypes = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="IndicatorProvider"/>.
	/// </summary>
	public IndicatorProvider()
	{
	}

	/// <inheritdoc />
	public virtual void Init()
	{
		_indicatorTypes.Clear();

		var ns = typeof(IIndicator).Namespace;

		_indicatorTypes.AddRange(typeof(BaseIndicator)
			.Assembly
			.FindImplementations<IIndicator>(showObsolete: true, extraFilter: t => t.Namespace == ns && t.GetConstructor(Type.EmptyTypes) != null && t.GetAttribute<IndicatorHiddenAttribute>() is null)
			.Select(t => new IndicatorType(t))
			.OrderBy(t => t.Name));
	}

	/// <inheritdoc />
	public IEnumerable<IndicatorType> All => _indicatorTypes.Cache;

	/// <inheritdoc />
	public void Add(IndicatorType type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		_indicatorTypes.Add(type);
	}

	void IIndicatorProvider.Remove(IndicatorType type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		_indicatorTypes.Remove(type);
	}
}