namespace StockSharp.Algo;

using StockSharp.Algo.Expressions;

/// <summary>
/// Basket security processors provider.
/// </summary>
public class BasketSecurityProcessorProvider : IBasketSecurityProcessorProvider
{
	private readonly CachedSynchronizedDictionary<string, (Type processor, Type security)> _processors = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the <see cref="BasketSecurityProcessorProvider"/>.
	/// </summary>
	public BasketSecurityProcessorProvider()
	{
		Register(typeof(ContinuousSecurityExpirationProcessor), typeof(ExpirationContinuousSecurity));
		Register(typeof(ContinuousSecurityVolumeProcessor), typeof(VolumeContinuousSecurity));
		Register(typeof(ExpressionIndexSecurityProcessor), typeof(ExpressionIndexSecurity));
		Register(typeof(WeightedIndexSecurityProcessor), typeof(WeightedIndexSecurity));
	}

	/// <inheritdoc />
	public IEnumerable<string> AllCodes => _processors.CachedKeys;

	private void Register(Type processorType, Type securityType)
	{
		if (processorType == null)
			throw new ArgumentNullException(nameof(processorType));

		if (securityType == null)
			throw new ArgumentNullException(nameof(securityType));

		var attr = securityType.GetAttribute<BasketCodeAttribute>()
			?? throw new ArgumentException(securityType.ToString(), nameof(securityType));

		_processors.Add(attr.Code, (processorType, securityType));
	}

	void IBasketSecurityProcessorProvider.Register(string basketCode, Type processorType, Type securityType)
	{
		if (basketCode.IsEmpty())
			throw new ArgumentNullException(nameof(basketCode));

		if (processorType == null)
			throw new ArgumentNullException(nameof(processorType));

		if (securityType == null)
			throw new ArgumentNullException(nameof(securityType));

		_processors.Add(basketCode, (processorType, securityType));
	}

	bool IBasketSecurityProcessorProvider.UnRegister(string basketCode)
	{
		if (basketCode.IsEmpty())
			throw new ArgumentNullException(nameof(basketCode));

		return _processors.Remove(basketCode);
	}

	private bool TryGetInfo(string basketCode, out (Type processor, Type security) info)
	{
		info = default;

		if (basketCode.IsEmpty())
			return false;

		return _processors.TryGetValue(basketCode, out info);
	}

	bool IBasketSecurityProcessorProvider.TryGetProcessorType(string basketCode, out Type processorType)
	{
		if (TryGetInfo(basketCode, out var info))
		{
			processorType = info.processor;
			return true;
		}

		processorType = null;
		return false;
	}

	bool IBasketSecurityProcessorProvider.TryGetSecurityType(string basketCode, out Type securityType)
	{
		if (TryGetInfo(basketCode, out var info))
		{
			securityType = info.security;
			return true;
		}

		securityType = null;
		return false;
	}
}