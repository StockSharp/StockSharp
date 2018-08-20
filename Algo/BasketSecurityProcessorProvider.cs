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
			Register(typeof(ContinuousSecurityExpirationProcessor), typeof(ContinuousSecurity));
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

		void IBasketSecurityProcessorProvider.Register(string code, Type processorType, Type securityType)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException(nameof(code));

			if (code.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(code));

			if (processorType == null)
				throw new ArgumentNullException(nameof(processorType));

			if (securityType == null)
				throw new ArgumentNullException(nameof(securityType));

			_processors.Add(code, Tuple.Create(processorType, securityType));
		}

		void IBasketSecurityProcessorProvider.UnRegister(string code)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException(nameof(code));

			if (code.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(code));

			_processors.Remove(code);
		}

		private Tuple<Type, Type> GetInfo(string expression)
		{
			if (expression.IsEmpty())
				throw new ArgumentNullException(nameof(expression));

			if (expression.Length < 4)
				throw new ArgumentOutOfRangeException(nameof(expression));

			var code = expression.Substring(0, 2);

			if (_processors.TryGetValue(code, out var processor))
				return processor;

			throw new ArgumentException(LocalizedStrings.Str2140Params.Put(code));
		}

		Type IBasketSecurityProcessorProvider.GetProcessorType(string expression)
		{
			return GetInfo(expression).Item1;
		}

		Type IBasketSecurityProcessorProvider.GetSecurityType(string expression)
		{
			return GetInfo(expression).Item2;
		}
	}
}