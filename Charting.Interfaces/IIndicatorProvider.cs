namespace StockSharp.Charting
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Reflection;

	using StockSharp.Algo.Indicators;
	using Ecng.Common;

	/// <summary>
	/// Provider <see cref="IndicatorType"/>.
	/// </summary>
	public interface IIndicatorProvider
	{
		/// <summary>
		/// Initialize provider.
		/// </summary>
		void Init();

		/// <summary>
		/// All indicator types.
		/// </summary>
		IEnumerable<IndicatorType> All { get; }

		/// <summary>
		/// Add <see cref="IndicatorType"/>.
		/// </summary>
		/// <param name="type"><see cref="IndicatorType"/></param>
		void Add(IndicatorType type);

		/// <summary>
		/// Remove <see cref="IndicatorType"/>.
		/// </summary>
		/// <param name="type"><see cref="IndicatorType"/></param>
		void Remove(IndicatorType type);
	}

	/// <summary>
	/// <see cref="IIndicatorProvider"/>
	/// </summary>
	public class DummyIndicatorProvider : IIndicatorProvider
	{
		private readonly CachedSynchronizedSet<IndicatorType> _indicatorTypes = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="DummyIndicatorProvider"/>.
		/// </summary>
		public DummyIndicatorProvider()
		{
		}

		void IIndicatorProvider.Init()
		{
			var ns = typeof(IIndicator).Namespace;

			_indicatorTypes.Clear();
			_indicatorTypes.AddRange(typeof(IIndicator)
				.Assembly
				.FindImplementations<IIndicator>(true, extraFilter: t => t.Namespace == ns && t.GetConstructor(Type.EmptyTypes) != null && t.GetAttribute<IndicatorHiddenAttribute>() is null)
				.Select(t => new IndicatorType(t, null))
				.OrderBy(t => t.Name));
		}

		IEnumerable<IndicatorType> IIndicatorProvider.All => _indicatorTypes.Cache;

		void IIndicatorProvider.Add(IndicatorType type) => _indicatorTypes.Add(type);
		void IIndicatorProvider.Remove(IndicatorType type) => _indicatorTypes.Remove(type);
	}
}