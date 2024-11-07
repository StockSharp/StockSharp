namespace StockSharp.Algo;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Reflection;

using StockSharp.Algo.Indicators;

/// <summary>
/// <see cref="IndicatorType"/> provider.
/// </summary>
public class IndicatorProvider : IIndicatorProvider
{
	private readonly CachedSynchronizedSet<IndicatorType> _indicatorTypes = [];
	private readonly SynchronizedDictionary<Type, Type> _painterTypes = [];
	private readonly CachedSynchronizedDictionary<IndicatorType, AssemblyLoadContextTracker> _asmContexts = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="IndicatorProvider"/>.
	/// </summary>
	public IndicatorProvider()
	{
	}

	/// <inheritdoc />
	public virtual void Init() => OnInit(new Dictionary<Type, Type>());

	/// <summary>
	/// Initialize provider.
	/// </summary>
	/// <param name="painterTypes">Painter types.</param>
	protected virtual void OnInit(IDictionary<Type, Type> painterTypes)
	{
		if (painterTypes is null)
			throw new ArgumentNullException(nameof(painterTypes));

		_asmContexts.CachedValues.ForEach(c => c.Dispose());

		_indicatorTypes.Clear();
		_painterTypes.Clear();
		_asmContexts.Clear();

		_painterTypes.AddRange(painterTypes);

		var ns = typeof(IIndicator).Namespace;

		_indicatorTypes.AddRange(typeof(BaseIndicator)
			.Assembly
			.FindImplementations<IIndicator>(showObsolete: true, extraFilter: t => t.Namespace == ns && t.GetConstructor(Type.EmptyTypes) != null && t.GetAttribute<IndicatorHiddenAttribute>() is null)
			.Select(t => new IndicatorType(t, _painterTypes.TryGetValue(t)))
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

		if (_asmContexts.TryGetAndRemove(type, out var ctx))
			ctx.Dispose();
	}
}