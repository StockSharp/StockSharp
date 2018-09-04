namespace StockSharp.Algo
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Expressions;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Basket security processors provider.
	/// </summary>
	public class BasketSecurityProcessorProvider : IBasketSecurityProcessorProvider
	{
		private readonly SynchronizedDictionary<string, Tuple<Type, Type>> _processors = new SynchronizedDictionary<string, Tuple<Type, Type>>(StringComparer.InvariantCultureIgnoreCase);

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

			var attr = securityType.GetAttribute<BasketCodeAttribute>();

			if (attr == null)
				throw new ArgumentException(nameof(securityType));

			_processors.Add(attr.Code, Tuple.Create(processorType, securityType));
		}

		private static void Validate(string basketCode)
		{
			if (basketCode.IsEmpty())
				throw new ArgumentNullException(nameof(basketCode));

			if (basketCode.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(basketCode));
		}

		void IBasketSecurityProcessorProvider.Register(string basketCode, Type processorType, Type securityType)
		{
			Validate(basketCode);

			if (processorType == null)
				throw new ArgumentNullException(nameof(processorType));

			if (securityType == null)
				throw new ArgumentNullException(nameof(securityType));

			_processors.Add(basketCode, Tuple.Create(processorType, securityType));
		}

		void IBasketSecurityProcessorProvider.UnRegister(string basketCode)
		{
			Validate(basketCode);

			_processors.Remove(basketCode);
		}

		private Tuple<Type, Type> GetInfo(string basketCode)
		{
			Validate(basketCode);

			if (_processors.TryGetValue(basketCode, out var processor))
				return processor;

			throw new ArgumentException(LocalizedStrings.Str2140Params.Put(basketCode));
		}

		Type IBasketSecurityProcessorProvider.GetProcessorType(string basketCode)
		{
			return GetInfo(basketCode).Item1;
		}

		Type IBasketSecurityProcessorProvider.GetSecurityType(string basketCode)
		{
			return GetInfo(basketCode).Item2;
		}
	}
}