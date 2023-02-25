namespace StockSharp.Algo.Candles;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Messages;

/// <summary>
/// Provider <see cref="ICandlePattern"/>.
/// </summary>
public interface ICandlePatternProvider
{
	/// <summary>
	/// Get all <see cref="ICandlePattern"/>.
	/// </summary>
	/// <returns>All <see cref="ICandlePattern"/>.</returns>
	IEnumerable<ICandlePattern> GetPatterns();
}

/// <summary>
/// Default implementation of <see cref="ICandlePatternProvider"/>
/// </summary>
public class CandlePatternProvider : ICandlePatternProvider
{
	private ICandlePattern[] _patterns;

	IEnumerable<ICandlePattern> ICandlePatternProvider.GetPatterns()
		=> _patterns ??= typeof(ICandlePattern)
			.Assembly
			.FindImplementations<ICandlePattern>(extraFilter: t => t.GetConstructor(Type.EmptyTypes) != null && t != typeof(ComplexCandlePattern))
			.Select(t => t.CreateInstance<ICandlePattern>())
			.ToArray();
}