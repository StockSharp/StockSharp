namespace StockSharp.Charting
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Algo.Indicators;
	using StockSharp.Configuration;
	using StockSharp.Logging;

	/// <summary>
	/// Provider <see cref="IndicatorType"/>.
	/// </summary>
	public interface IIndicatorProvider
	{
		/// <summary>
		/// Get all indicator types.
		/// </summary>
		/// <returns>All indicator types.</returns>
		IEnumerable<IndicatorType> GetIndicatorTypes();
	}

	/// <summary>
	/// <see cref="IIndicatorProvider"/>
	/// </summary>
	public class DummyIndicatorProvider : IIndicatorProvider
	{
		private IndicatorType[] _indicatorTypes;
		private readonly IndicatorType[] _customIndicators;

		/// <summary>
		/// Initializes a new instance of the <see cref="DummyIndicatorProvider"/>.
		/// </summary>
		public DummyIndicatorProvider()
		{
			_customIndicators = Extensions.RootSection?.CustomIndicators.SafeAdd<IndicatorElement, IndicatorType>(elem => new IndicatorType(elem.Type.To<Type>(), elem.Painter.To<Type>())) ?? Array.Empty<IndicatorType>();
		}

		IEnumerable<IndicatorType> IIndicatorProvider.GetIndicatorTypes()
		{
			if (_indicatorTypes is null)
			{
				var ns = typeof(IIndicator).Namespace;

				_indicatorTypes = typeof(IIndicator).Assembly
					.GetTypes()
					.Where(t => t.Namespace == ns &&
								!t.IsAbstract &&
								typeof(IIndicator).IsAssignableFrom(t) &&
								t.GetConstructor(Type.EmptyTypes) != null &&
								t.IsBrowsable())
					.Select(t => new IndicatorType(t, null))
					.Concat(_customIndicators)
					.OrderBy(t => t.Name)
					.ToArray();
			}

			return _indicatorTypes;
		}
	}
}