namespace StockSharp.Algo;

using StockSharp.Algo.Expressions;

/// <summary>
/// Basket security processors provider.
/// </summary>
public class BasketSecurityProcessorProvider : IBasketSecurityProcessorProvider
{
	private readonly SynchronizedDictionary<string, (Type processor, Type security)> _processors = new(StringComparer.InvariantCultureIgnoreCase);

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

	void IBasketSecurityProcessorProvider.UnRegister(string basketCode)
	{
		if (basketCode.IsEmpty())
			throw new ArgumentNullException(nameof(basketCode));

		_processors.Remove(basketCode);
	}

	private (Type processor, Type security) GetInfo(string basketCode)
	{
		if (basketCode.IsEmpty())
			throw new ArgumentNullException(nameof(basketCode));

		if (_processors.TryGetValue(basketCode, out var processor))
			return processor;

		throw new ArgumentException(LocalizedStrings.UnknownType.Put(basketCode));
	}

	Type IBasketSecurityProcessorProvider.GetProcessorType(string basketCode)
		=> GetInfo(basketCode).processor;

	Type IBasketSecurityProcessorProvider.GetSecurityType(string basketCode)
		=> GetInfo(basketCode).security;
}